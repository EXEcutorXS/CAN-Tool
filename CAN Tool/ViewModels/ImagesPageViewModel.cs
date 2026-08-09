using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using Microsoft.Win32;
using System;
using System.Collections.ObjectModel;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using CAN_Tool.Libs;
using static CAN_Tool.Libs.Helper;

namespace CAN_Tool.ViewModels
{
    public partial class PanelImageEntry : ObservableObject
    {
        public string Path { get; set; }
        public string FileName => System.IO.Path.GetFileName(Path);

        [ObservableProperty] private string status = "";
        [ObservableProperty] private ImageSource thumbnail;

        // Best-effort: some ".bmp" files in an image set aren't real bitmaps at all (see
        // PanelImageConverter) - those just don't get a thumbnail, which is fine, the table
        // still shows the filename and status for them.
        public void LoadThumbnail()
        {
            try
            {
                var bmp = new BitmapImage();
                bmp.BeginInit();
                bmp.UriSource = new Uri(Path);
                bmp.DecodePixelWidth = 48;
                bmp.CacheOption = BitmapCacheOption.OnLoad;
                bmp.EndInit();
                bmp.Freeze();
                Thumbnail = bmp;
            }
            catch
            {
                Thumbnail = null;
            }
        }
    }

    // Uploads the ПУ-28 panel bootloader's raster images (icons/pictures shown on its
    // display) over the CAN bus, replacing the closed-source Delphi tool that used to do
    // this. The bootloader's CAN protocol (reverse engineered from User/Activity/Boot/boot.cpp)
    // is NOT part of OmniProtocol - it is the panel's own raw extended-CAN-ID scheme, so this
    // page talks to CanAdapter directly instead of going through Omni.
    public partial class ImagesPageViewModel : ObservableObject
    {
        // Command frames: bits 26:20 of the extended ID = 1. The low 20 bits don't matter for
        // dispatch on the device side, so a fixed ID is used for every outgoing command.
        private const int CommandCanId = 1 << 20;

        // Bulk image-data frames: bits 23:16 of the extended ID = 0xFF (marker), bits 15:8 =
        // total length of the current logical packet, bits 7:0 = this frame's byte offset
        // within that packet.
        private const int RawDataIdMarker = 0xFF << 16;

        // All bootloader responses (command acks and image-data acks) come back with bits
        // 26:20 = 2, regardless of the rest of the ID - so that's all we filter on.
        private const int ResponseCategory = 2;

        public MainWindowViewModel Vm { set; get; }

        public ObservableCollection<PanelImageEntry> Images { get; } = new();

        [ObservableProperty] private string log;
        [ObservableProperty] private int progress;

        [NotifyPropertyChangedFor(nameof(IsNotBusy))]
        [ObservableProperty] private bool isBusy;

        public bool IsNotBusy => !IsBusy;

        [ObservableProperty] private WriteableBitmap previewBitmap;

        // Adds spacing between frames/packets on the legacy (<15.5) send path. A quiet bench
        // setup can get away with none (see SendLegacyPacket), but on a noisy/shared bus there's
        // more traffic to collide with and less margin, so this trades speed for a safer margin.
        [ObservableProperty] private bool legacySlowMode;

        private CancellationTokenSource cts;

        private readonly object ackLock = new();
        private byte[] pendingAck;

        private void LogWriteLine(string str)
        {
            Log = $"{DateTime.Now:HH:mm:ss.fff} {str}" + Environment.NewLine + Log;
        }

        // ---- Live preview: paints pixels into PreviewBitmap as their bytes are actually
        // confirmed sent, so the picture visibly builds up during transfer - the same idea as
        // the original Delphi tool's own PIXEL_IMAGE1 live-draw, just re-derived here from
        // whatever bytes just got acknowledged instead of at conversion time. Runs from the
        // background send thread; only the WritePixels calls need to hop onto the UI thread,
        // and those are throttled since Dispatcher.Invoke blocks the sender while it waits.

        private int previewWidth, previewHeight;
        private byte[] previewRowMajor; // Bgr24, row-major, top-down
        private int previewByteIndex;   // position within the logical R,G,B,R,G,B... stream
        private bool previewRunActive;
        private byte previewRunValue;
        private DateTime previewLastFlush = DateTime.MinValue;

        private void StartPreview(int w, int h)
        {
            previewWidth = w;
            previewHeight = h;
            previewByteIndex = 0;
            previewRunActive = false;
            previewRowMajor = new byte[w * h * 3];
            Application.Current.Dispatcher.Invoke(() => PreviewBitmap = new WriteableBitmap(w, h, 96, 96, PixelFormats.Bgr24, null));
        }

        // Feeds bytes that have just been confirmed received by the panel, in the same
        // (value, count) RLE pairs the panel itself decodes - mirrors the firmware's decoder.
        private void FeedPreviewRle(byte[] data, int n)
        {
            for (var i = 0; i < n; i++)
            {
                var v = data[i];
                if (!previewRunActive) { previewRunValue = v; previewRunActive = true; }
                else
                {
                    if (v > 0) PlotPreviewPixels(previewRunValue, v);
                    previewRunActive = false;
                }
            }
        }

        // Feeds bytes that are themselves already raw pixel bytes (legacy, uncompressed path).
        private void FeedPreviewRaw(byte[] data, int offset, int n)
        {
            for (var i = 0; i < n; i++)
                PlotPreviewPixels(data[offset + i], 1);
        }

        private void PlotPreviewPixels(byte value, int count)
        {
            var total = previewWidth * previewHeight * 3;
            for (var k = 0; k < count; k++)
            {
                if (previewByteIndex < total)
                {
                    // matches PanelImageConverter's destPixel = (w-1-x)*h+y layout, R,G,B per pixel
                    var pixelIndex = previewByteIndex / 3;
                    var channel = previewByteIndex % 3;
                    var col = pixelIndex / previewHeight;
                    var row = pixelIndex % previewHeight;
                    var x = previewWidth - 1 - col;
                    var y = row;
                    var off = (y * previewWidth + x) * 3;
                    // WriteableBitmap is Bgr24: byte order B,G,R
                    previewRowMajor[off + (2 - channel)] = value;
                }
                previewByteIndex++;
            }
        }

        private void FlushPreview(bool force = false)
        {
            if (previewRowMajor == null) return;
            if (!force && (DateTime.Now - previewLastFlush).TotalMilliseconds < 150) return;
            previewLastFlush = DateTime.Now;
            var w = previewWidth;
            var h = previewHeight;
            var buf = previewRowMajor;
            Application.Current.Dispatcher.Invoke(() =>
            {
                if (PreviewBitmap != null && PreviewBitmap.PixelWidth == w && PreviewBitmap.PixelHeight == h)
                    PreviewBitmap.WritePixels(new Int32Rect(0, 0, w, h), buf, w * 3, 0);
            });
        }

        // ---- CRC8, matching calcCrc8() in User/Main/main.c exactly (bit-serial, feedback 0xA1) ----
        private static byte CalcCrc8(byte data, byte crc)
        {
            for (var j = 0; j < 8; j++)
            {
                var bt = (byte)(crc & 1);
                crc >>= 1;
                if (((data & 1) != 0) != (bt != 0)) crc ^= 0xA1;
                data >>= 1;
            }
            return crc;
        }

        private void OnCanMessage(object sender, EventArgs e)
        {
            var msg = (e as GotCanMessageEventArgs)?.receivedMessage;
            if (msg == null || !msg.Ide || msg.Dlc < 8) return;
            if (((msg.Id >> 20) & 0x7F) != ResponseCategory) return;
            lock (ackLock)
            {
                pendingAck = (byte[])msg.Data.Clone();
            }
        }

        private byte[] TransmitAndWaitAck(CanMessage msg, int timeoutMs)
        {
            lock (ackLock) { pendingAck = null; }
            Vm.CanAdapter.Transmit(msg);
            var sw = Stopwatch.StartNew();
            while (sw.ElapsedMilliseconds < timeoutMs)
            {
                lock (ackLock)
                {
                    if (pendingAck != null) return pendingAck;
                }
                if (cts?.IsCancellationRequested == true) return null;
                Thread.Sleep(1);
            }
            return null;
        }

        // Sends a 0x03xx bootloader command and waits for its ack (data[0..1] = echoed
        // command, data[2] == 4). Retries the whole command on timeout - commands are
        // idempotent (re-running "start image"/"finalize"/"verify" is harmless).
        private byte[] SendCommand(byte cmdHi, byte cmdLo, byte[] payload, int timeoutMs = 1000, int retries = 5)
            => SendCommandCore(cmdHi, cmdLo, payload, ack => ack[2] == 4, timeoutMs, retries);

        // Same as SendCommand, but lets the caller supply its own ack-validity check - needed
        // for the couple of commands whose response doesn't follow the usual
        // [echoed cmd][4][txLen][txData...] layout (0x0317 "get version" packs its own fields
        // straight into bytes 2-7 instead of the [2]==4 marker).
        private byte[] SendCommandCore(byte cmdHi, byte cmdLo, byte[] payload, Func<byte[], bool> isValidAck, int timeoutMs, int retries)
        {
            var msg = new CanMessage { Id = CommandCanId, Ide = true, Dlc = 8 };
            msg.Data[0] = cmdHi;
            msg.Data[1] = cmdLo;
            for (var i = 0; i < 6; i++)
                msg.Data[2 + i] = payload != null && i < payload.Length ? payload[i] : (byte)0;

            for (var attempt = 0; attempt < retries; attempt++)
            {
                if (cts?.IsCancellationRequested == true) return null;
                var ack = TransmitAndWaitAck(msg, timeoutMs);
                if (ack != null && ack[0] == cmdHi && ack[1] == cmdLo && isValidAck(ack))
                    return ack;
            }
            return null;
        }

        // 0x0317 "get bootloader/app version" - ack layout is [cmdHi][cmdLo][BOOT_VERSION]
        // [BOOT_SUB_VERSION][app version bytes...], no [2]==4 marker.
        private byte[] QueryBootVersion(int timeoutMs = 1000, int retries = 5)
            => SendCommandCore(0x03, 0x17, null, _ => true, timeoutMs, retries);

        // 0x0302 "jump to main program" - the bootloader calls NVIC_SystemReset() as part of
        // handling this command and never reaches the code that would send an ack, so (matching
        // the original tool) this just fires the command a few times and moves on.
        private void SendJumpToApp()
        {
            var msg = new CanMessage { Id = CommandCanId, Ide = true, Dlc = 8 };
            msg.Data[0] = 0x03;
            msg.Data[1] = 0x02;
            for (var i = 2; i < 8; i++) msg.Data[i] = 0xFF;
            for (var i = 0; i < 4; i++)
            {
                Vm.CanAdapter.Transmit(msg);
                Thread.Sleep(100);
            }
        }

        // Sends one 4-byte sub-frame of the 0x0316 "write 128-byte program block" command and
        // waits for its ack. numByte is the offset within the 132-byte logical group (0 = the
        // 3-byte address + CRC8 header, 4..131 = the 128 program bytes in 4-byte steps).
        private byte[] SendFirmwareSubFrame(int numByte, byte[] fourBytes, int timeoutMs = 300, int retries = 8)
        {
            var msg = new CanMessage { Id = CommandCanId, Ide = true, Dlc = 8 };
            msg.Data[0] = 0x03;
            msg.Data[1] = 0x16;
            msg.Data[2] = 132; // 128 program bytes + 4-byte address/CRC8 header
            msg.Data[3] = (byte)numByte;
            msg.Data[4] = fourBytes[0];
            msg.Data[5] = fourBytes[1];
            msg.Data[6] = fourBytes[2];
            msg.Data[7] = fourBytes[3];

            for (var attempt = 0; attempt < retries; attempt++)
            {
                if (cts?.IsCancellationRequested == true) return null;
                var ack = TransmitAndWaitAck(msg, timeoutMs);
                if (ack != null && ack[0] == 0x03 && ack[1] == 0x16)
                    return ack;
            }
            return null;
        }

        // Writes one 128-byte block at the given offset within the application image. The
        // block's CRC8 (checked by the bootloader against the assembled 128 bytes before it
        // programs flash) only fails in practice if a sub-frame's payload was silently wrong
        // despite passing the CAN bus's own hardware frame check - essentially never, since
        // the sub-frame retry above already covers lost/timed-out frames. On CRC failure this
        // deliberately does NOT retry automatically: the bootloader only erases flash on 2KB
        // page boundaries, so blindly reprogramming over a block that already landed wrong
        // could leave the page in a worse, unrecoverable state instead of a merely incomplete
        // one - better to stop and let the user re-run the whole update.
        private bool SendFirmwareBlock(int blockOffset, byte[] block128)
        {
            var crc8 = (byte)0xFF;
            foreach (var b in block128) crc8 = CalcCrc8(b, crc8);

            var header = new byte[]
            {
                (byte)(blockOffset & 0xFF), (byte)((blockOffset >> 8) & 0xFF), (byte)((blockOffset >> 16) & 0xFF), crc8
            };
            if (SendFirmwareSubFrame(0, header) == null) return false;

            for (var off = 0; off < 128; off += 4)
            {
                var chunk = new[] { block128[off], block128[off + 1], block128[off + 2], block128[off + 3] };
                var ack = SendFirmwareSubFrame(4 + off, chunk);
                if (ack == null) return false;

                if (off + 4 >= 128)
                {
                    if (ack[4] == 1) return true;
                    LogWriteLine($"  block at offset 0x{blockOffset:X} failed the panel's own CRC8 check (status {ack[4]})");
                    return false;
                }
            }
            return false; // unreachable
        }

        private void SendFirmwareInternal(string hexPath)
        {
            byte[] mem;
            int appStart, appEnd;
            try
            {
                mem = PanelFirmwareHex.LoadAndPatch(hexPath, out appStart, out appEnd, PatchFirmwareCrc);
            }
            catch (Exception ex)
            {
                LogWriteLine($"Firmware load failed: {ex.Message}");
                return;
            }

            var length = appEnd - appStart + 1;
            LogWriteLine($"Firmware: 0x{appStart:X}-0x{appEnd:X}, {length} bytes");

            var ver = QueryBootVersion();
            if (ver == null)
            {
                LogWriteLine("No response querying the bootloader version - is the panel in bootloader mode?");
                return;
            }
            int bootMajor = ver[2], bootMinor = ver[3];
            LogWriteLine($"Bootloader version: {bootMajor}.{bootMinor}");
            if (bootMajor != 15 || bootMinor < 4)
            {
                LogWriteLine("This firmware-flashing protocol needs bootloader 15.4 or newer - aborting");
                return;
            }

            var lenAck = SendCommand(0x03, 0x15, new[]
            {
                (byte)(length & 0xFF), (byte)((length >> 8) & 0xFF), (byte)((length >> 16) & 0xFF), (byte)((length >> 24) & 0xFF),
            });
            if (lenAck == null)
            {
                LogWriteLine("No response setting the firmware length");
                return;
            }

            for (var blockOffset = 0; blockOffset < length; blockOffset += 128)
            {
                if (cts?.IsCancellationRequested == true)
                {
                    LogWriteLine("Cancelled");
                    return;
                }
                var block = new byte[128];
                Array.Copy(mem, appStart + blockOffset, block, 0, 128); // mem is 0xFF-padded past appEnd already

                if (!SendFirmwareBlock(blockOffset, block))
                {
                    LogWriteLine($"Firmware update failed at offset 0x{blockOffset:X} - stopping. The panel's own firmware is untouched (the bootloader flash is separate); fix the connection and run the update again from the start.");
                    return;
                }
                Progress = (int)((long)(blockOffset + 128) * 100 / length);
            }

            LogWriteLine("Firmware written, jumping to the main program...");
            SendJumpToApp();
            LogWriteLine("Done");
        }

        // Sends one CAN frame of the bulk image-data path (up to 7 payload bytes + CRC8) and
        // retries just this frame on a CRC nack or timeout - this is the fix for images
        // getting corrupted in transit: previously there was no checksum on this path at all.
        private bool SendDataFrame(int packetLen, int numByte, byte[] payload, int n, out bool packetComplete, int maxRetries = 10)
        {
            packetComplete = false;
            var msg = new CanMessage { Id = RawDataIdMarker | (packetLen << 8) | numByte, Ide = true, Dlc = 8 };
            for (var i = 0; i < 7; i++)
                msg.Data[i] = i < n ? payload[i] : (byte)0xFF;
            var crc = (byte)0xFF;
            for (var i = 0; i < n; i++) crc = CalcCrc8(payload[i], crc);
            msg.Data[7] = crc;

            for (var attempt = 0; attempt < maxRetries; attempt++)
            {
                if (cts?.IsCancellationRequested == true) return false;
                var ack = TransmitAndWaitAck(msg, 300);
                if (ack == null) continue; // timeout, resend the same frame
                // Must match marker AND echo back this exact frame's (len, num_byte) - the
                // panel can be slow to respond (display drawing shares the same main loop as
                // CAN handling), so a stale ack for an EARLIER frame can arrive right after we
                // give up on it and move on to a later one. Without checking len/num_byte here,
                // that stale ack gets mistaken for the current frame's ack, so this frame's
                // data never actually gets decoded/written - which desyncs the RLE stream for
                // everything after it, producing images that are "almost right" with
                // artifacts. Anything that doesn't match is simply not our answer yet.
                if (ack[0] != 0x10 || ack[2] != packetLen || ack[3] != numByte) continue;
                var status = ack[1];
                if (status == 1) continue; // CRC mismatch on the device side, resend
                packetComplete = status == 2;
                return true;
            }
            return false;
        }

        private bool SendPixelData(byte[] pixelBytes)
        {
            var total = pixelBytes.Length;
            var offset = 0;
            while (offset < total)
            {
                if (cts?.IsCancellationRequested == true) return false;
                var packetLen = Math.Min(252, total - offset); // len is a single byte in the protocol (max 255)
                var numByte = 0;
                while (numByte < packetLen)
                {
                    var n = Math.Min(7, packetLen - numByte);
                    var chunk = new byte[n];
                    Array.Copy(pixelBytes, offset + numByte, chunk, 0, n);
                    if (!SendDataFrame(packetLen, numByte, chunk, n, out var complete))
                        return false;
                    FeedPreviewRle(chunk, n);
                    FlushPreview();
                    numByte += n;
                    if (complete) break;
                }
                offset += packetLen;
                Progress = (int)((long)offset * 100 / total);
            }
            return true;
        }

        private bool SendOneImage(string bmpPath, int index, int count)
        {
            byte[] pixelBytes;
            int width, height;
            try
            {
                pixelBytes = PanelImageConverter.ConvertBmpToPanelFormat(bmpPath, out width, out height);
            }
            catch (Exception ex)
            {
                LogWriteLine($"[{index + 1}/{count}] {System.IO.Path.GetFileName(bmpPath)}: {ex.Message}");
                return false;
            }

            var compressed = PanelImageConverter.RunLengthEncode(pixelBytes);
            LogWriteLine($"[{index + 1}/{count}] {System.IO.Path.GetFileName(bmpPath)} {width}x{height}, {pixelBytes.Length} bytes -> {compressed.Length} compressed");

            var header = SendCommand(0x03, 0x03, new byte[]
            {
                (byte)(width & 0xFF), (byte)((width >> 8) & 0xFF),
                (byte)(height & 0xFF), (byte)((height >> 8) & 0xFF),
            });
            if (header == null)
            {
                LogWriteLine("  no response to \"start image\" - is the panel in bootloader mode and the port open?");
                return false;
            }

            StartPreview(width, height);

            if (!SendPixelData(compressed))
            {
                LogWriteLine("  data transfer failed or was cancelled");
                return false;
            }
            FlushPreview(force: true);

            var fin = SendCommand(0x03, 0x05, null, timeoutMs: 5000);
            if (fin == null)
            {
                LogWriteLine("  no response to \"finalize image\"");
                return false;
            }

            // Independently compute what the CRC8 of the decoded pixel bytes should be and
            // compare against what the panel actually read back from flash after writing -
            // per-frame CRC8 only proves the compressed bytes arrived intact, not that the
            // RLE decoder or flash write reconstructed them correctly. This is what actually
            // closes the loop end-to-end.
            var expectedCrc = (byte)0xFF;
            foreach (var b in pixelBytes) expectedCrc = CalcCrc8(b, expectedCrc);
            var storedCrc = fin[4];
            if (storedCrc != expectedCrc)
            {
                LogWriteLine($"  MISMATCH: expected CRC8 0x{expectedCrc:X2}, panel reports 0x{storedCrc:X2} - what's stored in flash does not match what was sent");
                return false;
            }
            LogWriteLine($"  ok, CRC8 = 0x{storedCrc:X2}");
            return true;
        }

        // ---- Legacy image transfer for bootloaders older than 15.5 ----
        // Those don't have the RLE decoder, per-frame CRC8/ack, or the real end-to-end CRC8
        // check in 0x0305 - all of that is new in 15.5. Older firmware's raw-data handler just
        // writes whatever bytes it receives verbatim (RLE would land in flash uncompressed and
        // wrong), and only acks once a whole logical packet is fully received, not per frame.
        // This mirrors the original Delphi tool's approach (best-effort, no per-frame
        // verification) but at least retries a whole packet on timeout instead of firing once
        // and hoping, and clears any stale pending ack before each packet to reduce
        // misattribution risk.

        // data[0]=3,[1]=4,[2]=5 marker + [3]=len is how pre-15.5 firmware acks a completed
        // logical packet (see the "3,4,5" constants in the original, unpatched 0x0316-style
        // raw-data handler) - unrelated to the 0x10 marker the 15.5+ protocol uses.
        //
        // This is the weak point of the whole legacy path: there is no per-frame ack, so if any
        // one of a packet's frames gets silently dropped (old firmware buffers at most one - or
        // with the 15.x RX-queue fix, eight - incoming CAN messages, and a frame arriving before
        // the slow display-drawing main loop drains the previous one just overwrites/is
        // discarded), the *last* frame's arrival still triggers a "packet complete" ack even
        // though the middle of the packet is missing/stale - the panel reports success while
        // flash actually got garbage in the middle. There's no per-frame defense against that
        // without touching the old firmware.
        //
        // No inter-frame delay by default, matching the original Delphi tool exactly (confirmed
        // against real 15.4 hardware on a quiet bus) - its own "pacing" was really just the
        // incidental throughput limit of the slow serial-based Lawicel adapter it used, not a
        // deliberate delay, and it worked fine without one. Packet size matches Delphi's 240-byte
        // flush threshold too, to keep the ack round-trip overhead (the actual bulk of the time)
        // as low as Delphi's. On a noisy/shared bus, more of our own frames or the panel's
        // display-drawing pauses can collide with other traffic than on a quiet bench, so
        // LegacySlowMode adds a small per-frame delay to reduce that risk at the cost of speed.
        private bool SendLegacyPacket(int packetLen, byte[] data, int dataOffset, int maxRetries = 5)
        {
            for (var attempt = 0; attempt < maxRetries; attempt++)
            {
                if (cts?.IsCancellationRequested == true) return false;
                lock (ackLock) { pendingAck = null; }

                var numByte = 0;
                while (numByte < packetLen)
                {
                    var n = Math.Min(8, packetLen - numByte);
                    var msg = new CanMessage { Id = RawDataIdMarker | (packetLen << 8) | numByte, Ide = true, Dlc = 8 };
                    for (var i = 0; i < 8; i++)
                        msg.Data[i] = i < n ? data[dataOffset + numByte + i] : (byte)0xFF;
                    Vm.CanAdapter.Transmit(msg);
                    numByte += n;
                    if (LegacySlowMode) Thread.Sleep(1);
                }

                var sw = Stopwatch.StartNew();
                while (sw.ElapsedMilliseconds < 1000)
                {
                    byte[] ack;
                    lock (ackLock) { ack = pendingAck; }
                    if (ack != null && ack[0] == 3 && ack[1] == 4 && ack[2] == 5 && ack[3] == packetLen)
                        return true;
                    if (cts?.IsCancellationRequested == true) return false;
                    Thread.Sleep(1);
                }
                // timeout (no ack at all) - safe to retry: writePage() never ran for this
                // packet, so nothing has been committed to flash yet. Note this can't detect or
                // fix the "middle frame silently dropped but the last one still landed" case -
                // that gets an ack and looks like success, so there's nothing to retry against;
                // keeping the loss window small (packet size + per-frame spacing above) is the
                // only real defense against that one.
            }
            return false;
        }

        private bool SendPixelDataLegacy(byte[] data, bool isRle)
        {
            var total = data.Length;
            var offset = 0;
            while (offset < total)
            {
                if (cts?.IsCancellationRequested == true) return false;
                var packetLen = Math.Min(240, total - offset); // matches the original Delphi tool's flush threshold
                if (!SendLegacyPacket(packetLen, data, offset)) return false;
                // legacy only acks a whole packet at a time, so that's the finest granularity
                // the preview can update at here (vs. per-frame on the new protocol)
                if (isRle)
                {
                    var chunk = new byte[packetLen];
                    Array.Copy(data, offset, chunk, 0, packetLen);
                    FeedPreviewRle(chunk, packetLen);
                }
                else
                {
                    FeedPreviewRaw(data, offset, packetLen);
                }
                FlushPreview();
                offset += packetLen;
                Progress = (int)((long)offset * 100 / total);
                Thread.Sleep(LegacySlowMode ? 5 : 1); // small breathing room between bursts - packets are ~30x rarer than frames, so this is nearly free
            }
            return true;
        }

        private bool SendOneImageLegacy(string bmpPath, int index, int count)
        {
            byte[] pixelBytes;
            int width, height;
            try
            {
                pixelBytes = PanelImageConverter.ConvertBmpToPanelFormat(bmpPath, out width, out height);
            }
            catch (Exception ex)
            {
                LogWriteLine($"[{index + 1}/{count}] {System.IO.Path.GetFileName(bmpPath)}: {ex.Message}");
                return false;
            }

            // Confirmed against real 15.4 hardware with the original Delphi tool: pre-15.5
            // bootloaders DO expect RLE-compressed data on this path, unconditionally - the
            // Delphi sender never had an "uncompressed" mode, so neither should this.
            var legacyData = PanelImageConverter.RunLengthEncode(pixelBytes);
            LogWriteLine($"[{index + 1}/{count}] {System.IO.Path.GetFileName(bmpPath)} {width}x{height}, {pixelBytes.Length} bytes -> {legacyData.Length} compressed (legacy)");

            var header = SendCommand(0x03, 0x03, new byte[]
            {
                (byte)(width & 0xFF), (byte)((width >> 8) & 0xFF),
                (byte)(height & 0xFF), (byte)((height >> 8) & 0xFF),
            });
            if (header == null)
            {
                LogWriteLine("  no response to \"start image\" - is the panel in bootloader mode and the port open?");
                return false;
            }

            StartPreview(width, height);

            if (!SendPixelDataLegacy(legacyData, isRle: true))
            {
                LogWriteLine("  data transfer failed or was cancelled");
                return false;
            }
            FlushPreview(force: true);

            // Older firmware's 0x0305 doesn't compute a real CRC8 (that's new in 15.5 too), so
            // there's nothing meaningful to verify here - just send it for protocol parity.
            SendCommand(0x03, 0x05, null, timeoutMs: 2000);
            LogWriteLine("  sent (no end-to-end verification available on this bootloader version)");
            return true;
        }

        // Source image sets are always named 0.bmp, 1.bmp, ... N.bmp in one folder (matching
        // the panel's own sequential image-chain ordering), so sort numerically rather than
        // alphabetically - otherwise "10.bmp" would sort before "2.bmp".
        private static int NumericSortKey(string path)
        {
            var name = System.IO.Path.GetFileNameWithoutExtension(path);
            return int.TryParse(name, out var n) ? n : int.MaxValue;
        }

        [RelayCommand]
        private void LoadImages()
        {
            using var dialog = new System.Windows.Forms.FolderBrowserDialog();
            if (dialog.ShowDialog() != System.Windows.Forms.DialogResult.OK) return;

            var files = Directory.GetFiles(dialog.SelectedPath, "*.bmp")
                .OrderBy(NumericSortKey)
                .ThenBy(f => f, StringComparer.OrdinalIgnoreCase)
                .ToArray();

            if (files.Length == 0)
            {
                MessageBox.Show("No .bmp files found in that folder");
                return;
            }

            Images.Clear();
            foreach (var f in files)
            {
                var entry = new PanelImageEntry { Path = f };
                entry.LoadThumbnail();
                Images.Add(entry);
            }
            LogWriteLine($"Loaded {Images.Count} file(s) from {dialog.SelectedPath}");
        }

        [RelayCommand]
        private void ClearList()
        {
            Images.Clear();
        }

        // Erasing without sending leaves the panel with no images at all, and sending without
        // erasing first appends onto whatever's already there instead of a clean set (Send
        // Images always starts from image 1, which only makes sense against an erased chip) -
        // neither one alone is a meaningful operation, so this is one button/command that does
        // both in the right order rather than two the user has to remember to run together.
        private bool EraseChip()
        {
            LogWriteLine("Erasing memory chip...");
            var ack = SendCommand(0x03, 0x00, null, timeoutMs: 15000, retries: 1);
            LogWriteLine(ack != null ? "Erase done" : "No response to erase command");
            return ack != null;
        }

        [RelayCommand]
        private void SendImages()
        {
            if (!Vm.CanAdapter.PortOpened)
            {
                MessageBox.Show(GetString("t_port_not_open"));
                return;
            }
            if (Images.Count == 0)
            {
                MessageBox.Show("Load some .bmp files first");
                return;
            }
            var result = MessageBox.Show(
                "This erases ALL images (and any panel/heater firmware backup images) currently on the panel's flash chip, then sends the whole loaded set. Continue?",
                "Erase and send images", MessageBoxButton.YesNo, MessageBoxImage.Warning);
            if (result != MessageBoxResult.Yes) return;

            cts?.Dispose();
            cts = new CancellationTokenSource();
            Task.Run(() =>
            {
                IsBusy = true;
                try
                {
                    if (!EraseChip())
                    {
                        LogWriteLine("Stopping: erase failed, not sending images onto an unknown flash state.");
                        return;
                    }

                    // Pick the transfer algorithm by bootloader version: 15.5+ gets the new
                    // CRC8-protected/RLE-compressed protocol, anything older falls back to the
                    // legacy uncompressed/best-effort one it actually supports.
                    var useNewProtocol = true;
                    var ver = QueryBootVersion(timeoutMs: 1000, retries: 3);
                    if (ver == null)
                    {
                        LogWriteLine("Could not query the bootloader version - assuming 15.5+ and using the new protocol. If images come out wrong, this bootloader is probably older than 15.5.");
                    }
                    else
                    {
                        int bootMajor = ver[2], bootMinor = ver[3];
                        LogWriteLine($"Bootloader version: {bootMajor}.{bootMinor}");
                        useNewProtocol = bootMajor > 15 || (bootMajor == 15 && bootMinor >= 5);
                        if (!useNewProtocol)
                            LogWriteLine("Bootloader is older than 15.5 - using the legacy image transfer (uncompressed, no per-frame CRC, no end-to-end verification)");
                    }

                    // Always send the whole set from the first image - images are appended
                    // sequentially into flash (memory.searchEnd() just continues right after
                    // whatever's already there), so resuming partway through only makes sense
                    // if you know for certain every earlier image already landed correctly. An
                    // "Erase all images" always precedes a real run, so a partial resend would
                    // just misalign the whole chain. Send Images always starts from image 1.
                    var ok = 0;
                    var aborted = false;
                    for (var i = 0; i < Images.Count; i++)
                    {
                        if (cts.IsCancellationRequested) { aborted = true; break; }
                        var entry = Images[i];
                        entry.Status = "sending...";
                        var success = useNewProtocol
                            ? SendOneImage(entry.Path, i, Images.Count)
                            : SendOneImageLegacy(entry.Path, i, Images.Count);
                        entry.Status = success ? "ok" : "failed";
                        if (success) { ok++; continue; }

                        // Stop immediately: every image after this one gets appended right after
                        // whatever actually landed in flash, so continuing past a failed transfer
                        // means silently dropping this image from the sequence rather than just
                        // reporting it as missing - better to fix the connection and resume.
                        aborted = true;
                        LogWriteLine($"Stopping: \"{entry.FileName}\" failed to send. Fix the connection and resend starting from this file.");
                        break;
                    }
                    LogWriteLine($"Done: {ok}/{Images.Count} image(s) sent successfully" + (aborted ? " (stopped early)" : ""));

                    if (!aborted && useNewProtocol)
                    {
                        // 0x0318 (verify all images) only exists on 15.5+ - don't bother asking
                        // an older bootloader, it won't know the command.
                        var verify = SendCommand(0x03, 0x18, null, timeoutMs: 10000);
                        if (verify != null)
                            LogWriteLine($"Panel reports {verify[4]} image(s) with a valid CRC8 in flash");
                        else
                            LogWriteLine("No response to the verify-images command");
                    }
                }
                finally { IsBusy = false; }
            }, cts.Token);
        }

        [RelayCommand]
        private void CancelSend()
        {
            cts?.Cancel();
        }

        private string firmwareHexPath;
        [ObservableProperty] private string firmwareFileName = "";

        // Whether to patch the self-referential length+CRC16 the bootloader's own startup
        // check expects at flash offset 0x1C000 (see PanelFirmwareHex). On by default since
        // most .hex files don't already have it; turn off if yours was built with it baked in.
        [ObservableProperty] private bool patchFirmwareCrc = true;

        [RelayCommand]
        private void LoadFirmware()
        {
            var dialog = new OpenFileDialog { Filter = "Hex files|*.hex" };
            if (dialog.ShowDialog() != true) return;
            firmwareHexPath = dialog.FileName;
            FirmwareFileName = System.IO.Path.GetFileName(firmwareHexPath);
            LogWriteLine($"Firmware hex loaded: {FirmwareFileName}");
        }

        [RelayCommand]
        private void SendFirmware()
        {
            if (!Vm.CanAdapter.PortOpened)
            {
                MessageBox.Show(GetString("t_port_not_open"));
                return;
            }
            if (string.IsNullOrEmpty(firmwareHexPath))
            {
                MessageBox.Show("Load a panel firmware .hex file first");
                return;
            }

            cts?.Dispose();
            cts = new CancellationTokenSource();
            var path = firmwareHexPath;
            Task.Run(() =>
            {
                IsBusy = true;
                try { SendFirmwareInternal(path); }
                finally { IsBusy = false; }
            }, cts.Token);
        }

        public ImagesPageViewModel(MainWindowViewModel vm)
        {
            Vm = vm;
            Vm.CanAdapter.GotNewMessage += OnCanMessage;
        }
    }
}
