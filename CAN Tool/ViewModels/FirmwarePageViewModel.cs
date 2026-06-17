using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using Microsoft.Win32;
using OmniProtocol;
using System.IO;
using System.Collections.Generic;
using System;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using System.Timers;
using System.Windows;
using static CAN_Tool.Libs.Helper;
using System.Diagnostics;

namespace CAN_Tool.ViewModels
{

    internal class CodeFragment
    {
        public CodeFragment(int len)
        {
            Data = new byte[len];
        }
        public uint StartAddress;
        public int Length;
        public byte[] Data;
    }

    public partial class FirmwarePageViewModel : ObservableObject
    {

        // ── Флаги прошивки ─────────────────────────────────────────────
        public bool flagEraseDone = false;
        public bool flagSetAdrDone = false;
        public bool flagProgramDone = false;
        public bool flagDataGetDone = false;
        public bool flagVerifyDone = false;
        public bool flagReadDone = false;
        public uint fragmentAddress = 0;
        public int receivedFragmentLength = 0;
        public uint receivedFragmentCrc = 0;
        public uint verifyResultCrc = 0;
        public bool verifyResultOk = false;
        public uint readResultData = 0;
        public bool readResultOk = false;

        private List<CodeFragment> fragments = new();

        [ObservableProperty]
        private int fragmentSize = 512;

        [ObservableProperty]
        private string log;

        public MainWindowViewModel Vm { set; get; }

        private void LogWrite(string str)
        {
            Log = str + Log;
        }

        private void LogWriteLine(string str)
        {
            Log = str + Environment.NewLine + Log;
        }

        [RelayCommand]
        private void SwitchToBootLoader()
        {
            if (Vm?.OmniInstance.SelectedConnectedDevice == null) return;
            if (BootloaderAlreadyOnBus())
            {
                MessageBox.Show(GetString("t_bootloader_already_on_bus"), GetString("t_bootloader_conflict_title"),
                    MessageBoxButton.OK, MessageBoxImage.Warning);
                return;
            }
            OmniMessage msg = new();
            msg.Pgn = 1;
            msg.ReceiverId.Address = Vm.OmniInstance.SelectedConnectedDevice.Id.Address;
            msg.ReceiverId.Type = Vm.OmniInstance.SelectedConnectedDevice.Id.Type;
            msg.Data[0] = 0;
            msg.Data[1] = 22;
            msg.Data[2] = 0;
            Vm.CanAdapter.Transmit(msg.ToCanMessage());
        }




        [RelayCommand]
        private async Task RequestBootLoaderVersion()
        {
            OmniMessage msg = new();
            msg.Pgn = 6;
            msg.ReceiverId.Type = 123;
            msg.Data[0] = 0;
            msg.Data[1] = 18;
            Vm.CanAdapter.Transmit(msg.ToCanMessage());
        }

        [RelayCommand]
        private async Task SwitchToMainProgram()
        {
            OmniMessage msg = new();
            msg.Pgn = 1;
            msg.ReceiverId.Type = 123;
            msg.Data[0] = 0;
            msg.Data[1] = 22;
            msg.Data[2] = 1;
            Vm.CanAdapter.Transmit(msg.ToCanMessage());
        }

        private string lastHexFilePath = "";

        [RelayCommand]
        private void LoadHex()
        {
            OpenFileDialog dialog = new();
            dialog.Filter = "Hex Files|*.hex";
            if (!(bool)dialog.ShowDialog()) return;
            lastHexFilePath = dialog.FileName;
            fragments = ParseHexFile(lastHexFilePath, FragmentSize);
            LogWriteLine($"Hex is loaded, contains {fragments.Count} fragments.");
        }

        [RelayCommand]
        private async Task GetVersion()
        {
            if (Vm?.OmniInstance.SelectedConnectedDevice == null) return;
            OmniMessage msg = new();
            msg.Pgn = 6;
            msg.ReceiverId.Address = Vm.OmniInstance.SelectedConnectedDevice.Id.Address;
            msg.ReceiverId.Type = Vm.OmniInstance.SelectedConnectedDevice.Id.Type;
            msg.Data[0] = 0;
            msg.Data[1] = 18;
            Vm.CanAdapter.Transmit(msg.ToCanMessage());
        }

        private async Task EraseFlash()
        {
            OmniMessage msg = new();
            msg.Pgn = 105;
            msg.ReceiverId.Type = 123;
            msg.Data[0] = 6;
            msg.Data[1] = 255;  //Стереть всю память
            Debug.WriteLine("Отправляем запрос на стирание");
            Vm.CanAdapter.Transmit(msg.ToCanMessage());
            flagEraseDone = false;
        }

        private async Task StartFlashing()
        {
            OmniMessage msg = new();
            msg.Pgn = 105;
            msg.ReceiverId.Type = 123;
            msg.Data[0] = 4;
            Vm.CanAdapter.Transmit(msg.ToCanMessage());
        }

        public bool WaitForFlag(ref bool flag, int delay)
        {
            var wd = 0;
            while (!flag && wd < delay)
            {
                wd++;
                Thread.Sleep(1);
            }
            if (!flag) return false;
            flag = false;
            return true;
        }
        private async Task FlashFragment(CodeFragment f)
        {
            WriteFragmentToRam(f);
            for (var i = 0; i < 4; i++)
            {
                flagProgramDone = false;
                if (i == 3)
                {
                    Vm.OmniInstance.CurrentTask.OnFail(GetString("t_cant_flash_memory"));
                    return;
                }
                StartFlashing();
                if (WaitForFlag(ref flagProgramDone, 100))
                    break;
            }
        }

        private bool CheckTransmittedData(int len, uint crc)
        {
            OmniMessage msg = new()
            {
                Pgn = 105,
                ReceiverId = new(123, 0),
                Data =
                {
                    [0] = 2
                }
            };

            for (var i = 0; i < 6; i++)
            {
                flagDataGetDone = false;
                if (i == 5)
                {
                    Vm.OmniInstance.CurrentTask.OnFail(GetString("t_cant_check_transmission"));
                    return false;
                }
                Vm.CanAdapter.Transmit(msg.ToCanMessage());
                WaitForFlag(ref flagDataGetDone, 100);

                LogWriteLine($"Len:{receivedFragmentLength},CRC:0x{receivedFragmentCrc:X08}");
                if (crc == receivedFragmentCrc && len == receivedFragmentLength)
                    return true;

                Debug.WriteLine($"CRC mismatch: expected {crc:X08}, got {receivedFragmentCrc:X08}");
                LogWriteLine(GetString("t_transmission_failed"));
                return false;
            }
            return false;
        }

        private async Task SetFragmentAdr(CodeFragment f)
        {
            OmniMessage msg = new()
            {
                Pgn = 105,
                ReceiverId = new(123, 0),
                Data =
                {
                    [0] = 0,
                    [1] = (byte)(f.StartAddress >> 24),
                    [2] = (byte)(f.StartAddress >> 16),
                    [3] = (byte)(f.StartAddress >> 8),
                    [4] = (byte)(f.StartAddress >> 0)
                }
            };

            for (var i = 0; i < 4; i++)
            {
                flagSetAdrDone = false;
                if (i == 3)
                {
                    Vm.OmniInstance.CurrentTask.OnFail(GetString("t_cant_set_address"));
                    return;
                }
                Vm.CanAdapter.Transmit(msg.ToCanMessage());
                if (!WaitForFlag(ref flagSetAdrDone, 300)) continue;
                if (fragmentAddress == f.StartAddress)
                    break;
            }
        }
        private async void WriteFragmentToRam(CodeFragment f)
        {
            OmniMessage msg = new()
            {
                Pgn = 106,
                ReceiverId = new(123, 0),
            };
            LogWrite($"Fragment {f.StartAddress:X08}...");
            for (var k = 0; k < 16; k++)
            {
                SetFragmentAdr(f);

                if (Vm.OmniInstance.CurrentTask.Cts.IsCancellationRequested)
                    return;

                if (k == 15) { 
                    Vm.OmniInstance.CurrentTask.OnFail(GetString("t_cant_transmit_data"));
                    Debug.WriteLine($"Превышено число попыток передачи");
                    return; }
                if (k > 0)
                {
                    LogWriteLine($"Try: {k + 1}");
                }
                uint crc = 0;
                var len = 0;
                receivedFragmentCrc = 0;
                receivedFragmentLength = 0;

                for (var i = 0; i < (f.Length + 7) / 8; i++)
                {
                    for (var j = 0; j < 8; j++)
                    {
                        msg.Data[j] = f.Data[i * 8 + j];
                        crc += f.Data[i * 8 + j] * 170771U;
                        crc ^= ((crc >> 16) & 0xFFFFU);
                        len++;
                    }
                    msg.Data[0] = f.Data[i * 8];
                    msg.Data[1] = f.Data[i * 8 + 1];
                    msg.Data[2] = f.Data[i * 8 + 2];
                    msg.Data[3] = f.Data[i * 8 + 3];
                    msg.Data[4] = f.Data[i * 8 + 4];
                    msg.Data[5] = f.Data[i * 8 + 5];
                    msg.Data[6] = f.Data[i * 8 + 6];
                    msg.Data[7] = f.Data[i * 8 + 7];
                    Vm.CanAdapter.Transmit(msg.ToCanMessage());

                }
                if (CheckTransmittedData(len, crc)) break;
            }
        }

        private async void UpdateFirmware(List<CodeFragment> fragmentsArg)
        {
            try
            {
                // Block new protocol on old bootloader (≤ v4): BootFirmware = {123, 0, 0, 4}
                var dev = Vm.OmniInstance.SelectedConnectedDevice;
                if (dev != null && dev.BootFirmware[0] == 123 && dev.BootFirmware[3] <= 4)
                {
                    MessageBox.Show(
                        GetString("t_old_bootloader_warning"),
                        GetString("t_old_bootloader_title"),
                        MessageBoxButton.OK, MessageBoxImage.Warning);
                    return;
                }

                if (fragmentsArg.Count == 0)
                {
                    MessageBox.Show(GetString("t_load_hex_first"));
                    return;
                }
                LogWriteLine(GetString("t_starting_firmware_update"));
                if (!Vm.OmniInstance.CurrentTask.Capture("Memory Erasing")) return;
                LogWriteLine(GetString("t_starting_flash_erase"));
                for (var i = 0; i < 4; i++)
                {
                    if (i == 3)
                    {
                        Vm.OmniInstance.CurrentTask.OnFail(GetString("t_cant_erase_memory"));
                        return;
                    }

                    await EraseFlash();
                    if (WaitForFlag(ref flagEraseDone, 5000)) break;
                }

                Vm.OmniInstance.CurrentTask.OnDone();

                Vm.OmniInstance.CurrentTask.Capture("Programming");

                var cnt = 0;
                foreach (var f in fragmentsArg)
                {
                    FlashFragment(f);
                    Vm.OmniInstance.CurrentTask.PercentComplete = cnt++ * 100 / fragmentsArg.Count;
                    if (Vm.OmniInstance.CurrentTask.Cts.IsCancellationRequested) return;
                }
                LogWriteLine(GetString("t_firmware_update_success"));
                Vm.OmniInstance.CurrentTask.OnDone();
            }
            catch(Exception ex)
            {
                MessageBox.Show(ex.Message);
            }

        }
        // CAN Tool sends data padded to 8-byte CAN frame boundaries; the bootloader
        // accumulates exactly that many bytes, so CRC and length for verification must
        // use the same padded size, not f.Length.
        private static int VerifyLen(CodeFragment f) => (f.Length + 7) / 8 * 8;

        private static uint CalcFragmentCrc(CodeFragment f)
        {
            uint crc = 0;
            var totalLen = VerifyLen(f);
            for (var i = 0; i < totalLen; i++)
            {
                crc += f.Data[i] * 170771U;
                crc ^= (crc >> 16) & 0xFFFFU;
            }
            return crc;
        }

        // Returns true=match, false=CRC mismatch, null=no response (abort)
        private bool? VerifyFragment(CodeFragment f)
        {
            var verifyLen = VerifyLen(f);
            var expectedCrc = CalcFragmentCrc(f);

            OmniMessage msg = new()
            {
                Pgn = 105,
                ReceiverId = new(123, 0),
                Data =
                {
                    [0] = 10,
                    [1] = (byte)(f.StartAddress >> 24),
                    [2] = (byte)(f.StartAddress >> 16),
                    [3] = (byte)(f.StartAddress >> 8),
                    [4] = (byte)(f.StartAddress),
                    [5] = (byte)(verifyLen >> 16),
                    [6] = (byte)(verifyLen >> 8),
                    [7] = (byte)(verifyLen)
                }
            };

            for (var attempt = 0; attempt < 4; attempt++)
            {
                flagVerifyDone = false;
                Vm.CanAdapter.Transmit(msg.ToCanMessage());
                if (!WaitForFlag(ref flagVerifyDone, 2000))
                {
                    LogWriteLine($"Verify: no response for 0x{f.StartAddress:X08} (attempt {attempt + 1})");
                    continue;
                }
                if (!verifyResultOk)
                {
                    LogWriteLine($"Verify: bootloader rejected address 0x{f.StartAddress:X08}");
                    return null;
                }
                if (verifyResultCrc == expectedCrc)
                {
                    LogWriteLine($"Verify OK 0x{f.StartAddress:X08} len={verifyLen} CRC=0x{expectedCrc:X08}");
                    return true;
                }
                LogWriteLine($"Verify FAIL 0x{f.StartAddress:X08}: expected 0x{expectedCrc:X08}, got 0x{verifyResultCrc:X08}");
                return false;
            }

            LogWriteLine($"Verify: timeout for 0x{f.StartAddress:X08}");
            return null;
        }

        private bool VerifyFlash(List<CodeFragment> fragmentsArg)
        {
            if (fragmentsArg == null || fragmentsArg.Count == 0)
            {
                LogWriteLine("Verify: no fragments to check");
                return false;
            }

            if (!Vm.OmniInstance.CurrentTask.Capture("Verifying")) return false;
            LogWriteLine("=== CRC verification ===");

            var cnt = 0;
            var mismatchCount = 0;
            foreach (var f in fragmentsArg)
            {
                var result = VerifyFragment(f);
                if (result == null)
                {
                    Vm.OmniInstance.CurrentTask.OnFail("Verification aborted: no response");
                    return false;
                }
                if (result == false)
                    mismatchCount++;

                Vm.OmniInstance.CurrentTask.PercentComplete = ++cnt * 100 / fragmentsArg.Count;
                if (Vm.OmniInstance.CurrentTask.Cts.IsCancellationRequested)
                {
                    Vm.OmniInstance.CurrentTask.OnFail("Cancelled");
                    return false;
                }
            }

            if (mismatchCount == 0)
                LogWriteLine($"=== CRC verification passed: {fragmentsArg.Count} fragment(s) ===");
            else
                LogWriteLine($"=== CRC verification done: {fragmentsArg.Count} fragment(s), {mismatchCount} mismatch(es) ===");

            Vm.OmniInstance.CurrentTask.OnDone();
            return mismatchCount == 0;
        }

        // Reads one 32-bit word from the bootloader via PGN 105 case 8.
        // Returns false on timeout or if the bootloader rejected the address.
        private bool ReadWordAtAddress(uint address, out uint data)
        {
            data = 0;
            OmniMessage msg = new()
            {
                Pgn = 105,
                ReceiverId = new(123, 0),
                Data =
                {
                    [0] = 8,
                    [1] = (byte)(address >> 24),
                    [2] = (byte)(address >> 16),
                    [3] = (byte)(address >> 8),
                    [4] = (byte)(address)
                }
            };

            for (var attempt = 0; attempt < 4; attempt++)
            {
                flagReadDone = false;
                Vm.CanAdapter.Transmit(msg.ToCanMessage());
                if (!WaitForFlag(ref flagReadDone, 500)) continue;
                if (!readResultOk) return false;
                data = readResultData;
                return true;
            }
            return false;
        }

        private bool VerifyFlashOld(List<CodeFragment> rawFragmentsArg)
        {
            if (rawFragmentsArg == null || rawFragmentsArg.Count == 0)
            {
                LogWriteLine("Verify: no fragments");
                return false;
            }

            if (!Vm.OmniInstance.CurrentTask.Capture("Verifying")) return false;
            LogWriteLine("=== Byte-by-byte verification ===");

            var totalBytes = rawFragmentsArg.Sum(f => f.Length);
            var checkedBytes = 0;
            var mismatchCount = 0;

            foreach (var f in rawFragmentsArg)
            {
                var wordCount = (f.Length + 3) / 4;
                for (var w = 0; w < wordCount; w++)
                {
                    if (Vm.OmniInstance.CurrentTask.Cts.IsCancellationRequested)
                    {
                        Vm.OmniInstance.CurrentTask.OnFail("Cancelled");
                        return false;
                    }

                    var addr = f.StartAddress + (uint)(w * 4);
                    if (!ReadWordAtAddress(addr, out var flashWord))
                    {
                        LogWriteLine($"Verify: no response reading 0x{addr:X08}");
                        Vm.OmniInstance.CurrentTask.OnFail("Verification aborted: no response");
                        return false;
                    }

                    var byteOffset = w * 4;
                    uint expectedWord = 0;
                    for (var b = 0; b < 4; b++)
                    {
                        var byteVal = (byteOffset + b < f.Length) ? f.Data[byteOffset + b] : (byte)0xFF;
                        expectedWord |= (uint)byteVal << (b * 8);
                    }

                    if (flashWord != expectedWord)
                    {
                        LogWriteLine($"  [!] 0x{addr:X08}: hex=0x{expectedWord:X08}  flash=0x{flashWord:X08}");
                        mismatchCount++;
                    }

                    checkedBytes += Math.Min(4, f.Length - byteOffset);
                    Vm.OmniInstance.CurrentTask.PercentComplete = checkedBytes * 100 / totalBytes;
                }
            }

            if (mismatchCount == 0)
                LogWriteLine($"=== Verification passed: {totalBytes} bytes, no mismatches ===");
            else
                LogWriteLine($"=== Verification done: {totalBytes} bytes, {mismatchCount} mismatch(es) ===");

            Vm.OmniInstance.CurrentTask.OnDone();
            return mismatchCount == 0;
        }

        #region oldVersionBootloader

        // Old bootloader flash command: PGN 100, Data[0]=3
        // (StartFlashing() uses PGN 105 which old bootloader does not understand)
        private void StartFlashingOld()
        {
            OmniMessage msg = new();
            msg.Pgn = 100;
            msg.ReceiverId.Type = 123;
            msg.Data[0] = 3;
            Vm.CanAdapter.Transmit(msg.ToCanMessage());
        }

        private void flashFragmentOld(CodeFragment f)
        {
            writeFragmentToRamOld(f);
            StartFlashingOld();
            Thread.Sleep(15); // STM32 programs 512 bytes in ~8ms; 15ms leaves margin
        }

        private void initAnddressOld()
        {
            // Case 2: bootloader resets mMemDataCnt=0 and mMemAddr=MAIN_PROGRAM_START_ADDRESS.
            // The address we pass is stored but ignored — bootloader always starts at its fixed constant.
            OmniMessage msg = new()
            {
                Pgn = 100,
                TransmitterId = new DeviceId(126, 6),
                ReceiverId = new DeviceId(123, 0)
            };
            msg.Data[0] = 2;
            Vm.CanAdapter.Transmit(msg.ToCanMessage());
            Thread.Sleep(20);
        }

        private void writeFragmentToRamOld(CodeFragment f)
        {
            OmniMessage msg = new()
            {
                Pgn = 101,
                TransmitterId = new DeviceId(126, 6),
                ReceiverId = new DeviceId(123, 0)
            };
            LogWriteLine($"Fragment 0x{f.StartAddress:X08} ({f.Length} bytes)");
            for (int i = 0; i < (f.Length + 7) / 8; i++)
            {
                for (int j = 0; j < 8; j++)
                    msg.Data[j] = f.Data[i * 8 + j];
                Vm.CanAdapter.Transmit(msg.ToCanMessage());
                Thread.Sleep(2); // Enough for bootloader to process one 8-byte CAN frame
            }
        }

        private void EraseFlashOld()
        {
            OmniMessage msg = new();
            msg.Pgn = 100;
            msg.ReceiverId.Type = 123;
            msg.Data[0] = 1;
            msg.Data[1] = 255;
            Vm.CanAdapter.Transmit(msg.ToCanMessage());
        }

        // Parses hex file including ALL bytes (even 0xFF).
        // Required for old bootloader: its write pointer advances for every received byte,
        // so skipping reserved 0xFF blocks would misalign subsequent data in flash.
        private List<CodeFragment> ParseHexFileRaw(string path, int maxFragmentSize)
        {
            var result = new List<CodeFragment>();
            CodeFragment current = new(maxFragmentSize);
            uint pageAddress = 0;
            uint lastLineAddress = 0;

            if (string.IsNullOrEmpty(path)) return result;
            using StreamReader sr = new(path);
            while (!sr.EndOfStream)
            {
                var line = sr.ReadLine()?[1..];
                var bytes = new byte[60];
                for (var i = 0; i < line?.Length / 2; i++)
                    bytes[i] = Convert.ToByte(line.Substring(i * 2, 2), 16);

                int recordLen = bytes[0];
                switch (bytes[3])
                {
                    case 0:
                        var localAddr = (uint)(bytes[1] * 256 + bytes[2]);
                        uint absAddr = pageAddress + localAddr;
                        if (lastLineAddress != 0 && absAddr != lastLineAddress + (uint)recordLen)
                        {
                            if (current.Length > 0) { result.Add(current); current = new(maxFragmentSize); }
                        }
                        if (current.StartAddress == 0)
                            current.StartAddress = absAddr;
                        lastLineAddress = absAddr;
                        for (var i = 0; i < recordLen; i++)
                        {
                            current.Data[current.Length++] = bytes[i + 4];
                            if (current.Length == maxFragmentSize)
                            { result.Add(current); current = new(maxFragmentSize); }
                        }
                        break;
                    case 4:
                        if (current.Length > 0) { result.Add(current); current = new(maxFragmentSize); }
                        pageAddress = (uint)(bytes[4] * 256 + bytes[5]) << 16;
                        lastLineAddress = 0;
                        break;
                    case 1:
                        if (current.Length > 0) result.Add(current);
                        return result;
                }
            }
            return result;
        }

        private void UpdateFirmwareOld(List<CodeFragment> _)
        {
            if (string.IsNullOrEmpty(lastHexFilePath))
            {
                MessageBox.Show(GetString("t_load_hex_first"));
                return;
            }

            // Re-parse hex including all 0xFF bytes so the bootloader's
            // sequential write pointer stays aligned with flash addresses.
            var rawFragments = ParseHexFileRaw(lastHexFilePath, FragmentSize);
            if (rawFragments.Count == 0)
            {
                MessageBox.Show(GetString("t_load_hex_first"));
                return;
            }

            LogWriteLine(string.Format(GetString("t_starting_old_boot_prog"), rawFragments.Count));
            if (!Vm.OmniInstance.CurrentTask.Capture("Memory Erasing")) return;

            LogWriteLine(GetString("t_starting_flash_erase"));
            EraseFlashOld();
            Thread.Sleep(5000); // Old bootloader sends no erase confirmation; fixed wait

            Vm.OmniInstance.CurrentTask.OnDone();
            Vm.OmniInstance.CurrentTask.Capture("Programming...");

            initAnddressOld();
            Thread.Sleep(20);

            int cnt = 0;
            foreach (var f in rawFragments)
            {
                flashFragmentOld(f);
                Vm.OmniInstance.CurrentTask.PercentComplete = cnt++ * 100 / rawFragments.Count;
                if (Vm.OmniInstance.CurrentTask.Cts.IsCancellationRequested) return;
            }

            LogWriteLine(GetString("t_firmware_update_success"));
            Vm.OmniInstance.CurrentTask.OnDone();
        }

        #endregion

        private void AddFragment(CodeFragment fragment)
        {
            fragments.Add(fragment);
            LogWriteLine($"Fragment added, {fragment.Length} bytes");
        }
        private List<CodeFragment> ParseHexFile(string path, int maxFragmentSize)
        {
            fragments.Clear();
            CodeFragment currentFragment = new(maxFragmentSize);
            uint pageAddress = 0;

            if (path is not { Length: > 0 }) return null;
            uint lastLineAddress = 0;
            using StreamReader sr = new(path);
            while (!sr.EndOfStream)
            {
                OmniMessage msg = new();
                var line = sr.ReadLine()?[1..];
                var bytes = new byte[60];
                for (var i = 0; i < line?.Length / 2; i++)
                    bytes[i] = Convert.ToByte(line.Substring(i * 2, 2), 16);
                int recordLen = bytes[0];

                var lastLineSize = recordLen;
                switch (bytes[3])
                {
                    case 0:
                        var localAddress = (uint)(bytes[1] * 256 + bytes[2]);
                        if ((pageAddress + localAddress != lastLineAddress + lastLineSize) && (lastLineAddress != 0)) //Current line is not just after previous, fragment must be divided
                        {
                            AddFragment(currentFragment);
                            currentFragment = new CodeFragment(maxFragmentSize);
                            LogWriteLine($"Line is not after previous. Current:0x{(pageAddress + localAddress):X08},prev:0x{lastLineAddress:X08} + {lastLineSize} bytes");
                        }
                        if (currentFragment.StartAddress == 0) //First line in data fragment, saving address
                        {
                            currentFragment.StartAddress = pageAddress + localAddress;
                            LogWriteLine($"New Fragment Start:0x{(pageAddress + localAddress):X08}");
                        }
                        lastLineAddress = pageAddress + localAddress;
                        var gotNotReserveData = false;

                        for (var i = 0; i < recordLen; i++)
                        {
                            if (bytes[i + 4] == 0xff) continue;
                            gotNotReserveData = true;
                            break;
                        }

                        if (gotNotReserveData)
                            for (var i = 0; i < recordLen; i++)
                            {
                                currentFragment.Data[currentFragment.Length++] = bytes[i + 4];
                                if (currentFragment.Length != maxFragmentSize) continue;
                                AddFragment(currentFragment);
                                currentFragment = new CodeFragment(maxFragmentSize);
                            }

                        break;
                    case 4:
                        if (currentFragment.Length != 0)
                        {
                            AddFragment(currentFragment);
                            currentFragment = new CodeFragment(maxFragmentSize);
                        }
                        pageAddress = (uint)(bytes[4] * 256 + bytes[5]) << 16;
                        lastLineAddress = 0;
                        LogWriteLine($"Base address:0x{pageAddress:X08}");
                        break;
                    case 1:
                        if (currentFragment.Length > 0)
                            AddFragment(currentFragment);
                        return fragments;

                }
            }
            return fragments;
        }

        [RelayCommand]
        private void UpdateFirmware()
        {
            Task.Run(() => UpdateFirmware(fragments));
        }

        [RelayCommand]
        private void UpdateFirmwareOld()
        {
            Task.Run(() => UpdateFirmwareOld(fragments));
        }

        [RelayCommand]
        private void VerifyFirmware()
        {
            var dev = Vm?.OmniInstance?.SelectedConnectedDevice;
            if (dev == null || dev.BootFirmware[0] != 123 || dev.BootFirmware[3] < 13)
            {
                MessageBox.Show("CRC verification requires bootloader version 123.0.0.13 or newer.",
                                "Unsupported bootloader", MessageBoxButton.OK, MessageBoxImage.Warning);
                return;
            }
            Task.Run(() => VerifyFlash(fragments));
        }

        [RelayCommand]
        private void VerifyFirmwareBytes()
        {
            if (string.IsNullOrEmpty(lastHexFilePath))
            {
                MessageBox.Show(GetString("t_load_hex_first"));
                return;
            }
            var rawFragments = ParseHexFileRaw(lastHexFilePath, FragmentSize);
            Task.Run(() => VerifyFlashOld(rawFragments));
        }

        [RelayCommand]
        private void AutoUpdateFirmware()
        {
            Task.Run(() => RunAutoUpdate());
        }

        private void RunAutoUpdate()
        {
            try
            {
                if (fragments.Count == 0)
                {
                    bool loaded = false;
                    Application.Current.Dispatcher.Invoke(() =>
                    {
                        OpenFileDialog dialog = new() { Filter = "Hex Files|*.hex" };
                        if ((bool)dialog.ShowDialog())
                        {
                            lastHexFilePath = dialog.FileName;
                            fragments = ParseHexFile(lastHexFilePath, FragmentSize);
                            loaded = fragments.Count > 0;
                        }
                    });
                    if (!loaded) return;
                }

                var omni = Vm.OmniInstance;

                if (BootloaderAlreadyOnBus())
                {
                    MessageBox.Show(GetString("t_bootloader_already_on_bus"), GetString("t_bootloader_conflict_title"),
                        MessageBoxButton.OK, MessageBoxImage.Warning);
                    return;
                }

                // Запоминаем исходный тип устройства перед входом в загрузчик
                var originalDeviceType = omni.SelectedConnectedDevice?.Id.Type ?? -1;

                // Шаг 1: переход в загрузчик
                SwitchToBootLoader();

                // Шаг 2: ждём появления устройства-загрузчика (тип 123)
                LogWriteLine(GetString("t_auto_waiting_bootloader"));
                DeviceViewModel bootDev = null;
                for (var i = 0; i < 150; i++) // 15 сек
                {
                    Thread.Sleep(100);
                    Application.Current.Dispatcher.Invoke(() =>
                        bootDev = omni.ConnectedDevices.FirstOrDefault(d => d.Id.Type == 123));
                    if (bootDev != null) break;
                }

                if (bootDev == null)
                {
                    LogWriteLine(GetString("t_auto_bootloader_timeout"));
                    return;
                }

                Application.Current.Dispatcher.Invoke(() => omni.SelectedConnectedDevice = bootDev);

                // Шаг 3: запрашиваем версию загрузчика и ждём ответа
                _ = RequestBootLoaderVersion();
                Thread.Sleep(500);

                // Шаг 4: прошиваем (выбор протокола по версии загрузчика)
                var boot = omni.SelectedConnectedDevice;
                if (boot != null && boot.BootFirmware[0] == 123 && boot.BootFirmware[3] <= 4)
                    UpdateFirmwareOld(fragments);
                else
                    UpdateFirmware(fragments);

                // Шаг 5: возврат в основную программу
                _ = SwitchToMainProgram();

                if (originalDeviceType < 0) return;

                // Шаг 6: ждём повторного появления исходного устройства
                LogWriteLine(GetString("t_auto_waiting_device"));
                DeviceViewModel originalDev = null;
                for (var i = 0; i < 150; i++) // 15 сек
                {
                    Thread.Sleep(100);
                    Application.Current.Dispatcher.Invoke(() =>
                        originalDev = omni.ConnectedDevices.FirstOrDefault(d => d.Id.Type == originalDeviceType));
                    if (originalDev != null) break;
                }

                if (originalDev == null)
                    LogWriteLine(GetString("t_auto_device_timeout"));
                else
                {
                    Application.Current.Dispatcher.Invoke(() => omni.SelectedConnectedDevice = originalDev);
                    LogWriteLine(GetString("t_auto_done"));
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show(ex.Message);
            }
        }

        private void SendWhoIsHere(object sender, ElapsedEventArgs e)
        {
            if (Vm?.CanAdapter == null) return;
            OmniMessage msg = new();
            msg.Pgn = 6;
            msg.ReceiverId.Type = 123;
            msg.Data[0] = 0;
            msg.Data[1] = 18;
            Vm.CanAdapter.Transmit(msg.ToCanMessage());
        }

        private bool BootloaderAlreadyOnBus()
        {
            bool found = false;
            Application.Current.Dispatcher.Invoke(() =>
                found = Vm.OmniInstance.ConnectedDevices.Any(d => d.Id.Type == 123));
            return found;
        }

        public FirmwarePageViewModel(MainWindowViewModel vm)
        {
            Vm = vm;
            var whoIsHereTimer = new System.Timers.Timer(1000);
            whoIsHereTimer.Elapsed += SendWhoIsHere;
            whoIsHereTimer.Start();
        }
    }
}
