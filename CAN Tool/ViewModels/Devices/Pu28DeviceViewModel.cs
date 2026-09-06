using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using Microsoft.Win32;
using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;
using System.Windows;
using static CAN_Tool.Libs.Helper;

namespace OmniProtocol
{
    // ПУ28: умеет управлять внешней flash-микросхемой (PGN107/108/109) - стирание/дамп памяти
    // целиком и запись прошивок в слоты для раздачи по локальной шине - а также транслирует
    // версии сохранённого ПО (PGN110/14-17, см. OwnImageVersion/Slot0-2ImageVersion ниже).
    // Общий базовый класс для
    // двух физических режимов одного и того же устройства: BootloaderDeviceViewModel (сидит в
    // загрузчике под Id.Type==123) и PanelDeviceViewModel (работает штатно как пульт под
    // Id.Type==126) - это разные объекты на шине (разный Id.Type), но обрабатывают одни и те же
    // команды памяти каждый под своим собственным адресом (Id.Type), поэтому вся адресация ниже
    // идёт от Id.Type "этого" устройства, а не захардкожена на 123.
    public partial class Pu28DeviceViewModel : DeviceViewModel
    {
        public Pu28DeviceViewModel(DeviceId id) : base(id) { }

        // BootloaderDeviceViewModel переопределяет это реальной проверкой байтов версии
        // (не любой загрузчик - ПУ28), а для PanelDeviceViewModel тип 126 уже однозначно
        // означает ПУ28 (см. omnidata.json), поэтому там достаточно значения по умолчанию.
        public virtual bool IsPu28 => true;

        // ── Флаги для внешней flash-микросхемы (PGN 107/108/109, дамп памяти) ──
        public bool flagExtSetAdrDone = false;
        public bool flagExtDataGetDone = false;
        public bool flagExtProgramDone = false;
        public bool flagExtEraseDone = false;
        public bool flagExtBulkReadDone = false;
        public uint extFragmentAddress = 0;
        public int extReceivedFragmentLength = 0;
        public uint extReceivedFragmentCrc = 0;
        public uint extBulkReadLen = 0;
        public uint extBulkReadCrc = 0;

        private readonly List<byte> extReadBuffer = new();

        public void AppendExtReadData(byte[] data)
        {
            extReadBuffer.AddRange(data);
        }

        private const int ExtFragmentSize = 256; // размер буфера mExtData в загрузчике
        // Раньше рвалось на ~314 кадрах из-за бага прошивки самого USB-CAN адаптера (терял
        // кадры при быстрой пачке) - после его фикса держим чанк побольше, чтобы меньше
        // round-trip'ов на установку адреса на каждый чанк.
        private const int DumpReadChunkSize = 8192; // байт за один запрос PGN107 case16 (1024 кадра)

        private List<CodeFragment> dumpFragments = new();

        // Диапазон для чтения дампа - полный чип 8МБ читать долго (retries + 1мс/кадр),
        // поэтому даём указать поддиапазон. Старт - шестнадцатеричный адрес, длина - в КБ.
        [ObservableProperty]
        private string dumpReadStartHex = "0x0";
        [ObservableProperty]
        private int dumpReadLengthKb = 8192;

        // Старые прошивки USB-CAN адаптеров (до фикса переполнения буфера) не успевают
        // вычерпывать быстрый всплеск кадров PGN109 через USB и тихо роняют часть. Для них -
        // маленький чанк (умещается в их буфер) плюс пауза между чанками (дать адаптеру
        // успеть вычерпать предыдущий всплеск до следующего).
        [ObservableProperty]
        private bool legacyAdapterMode = false;
        private const int LegacyDumpReadChunkSize = 32; // байт (4 кадра)
        private const int LegacyInterChunkDelayMs = 150;

        [RelayCommand]
        private void LoadDumpHex()
        {
            OpenFileDialog dialog = new() { Filter = "Hex Files|*.hex" };
            if (!(bool)dialog.ShowDialog()) return;
            dumpFragments.Clear();
            ParseHexFile(dialog.FileName, ExtFragmentSize, dumpFragments);
            LogWriteLine($"Dump hex is loaded, contains {dumpFragments.Count} fragments.");
        }

        // Стирание/дамп/чтение всей микросхемы всегда идут на "себя" (адрес этого же объекта на
        // шине) - в отличие от загрузки прошивки в слот (см. UploadToSlot ниже), тут нет сценария
        // "выполнить операцию у другого устройства".
        private async System.Threading.Tasks.Task EraseExtFlash()
        {
            OmniMessage msg = new();
            msg.Pgn = 107;
            msg.ReceiverId.Type = Id.Type;
            msg.Data[0] = 14;
            msg.Data[1] = 0; //Стереть всю память
            Transmit(msg.ToCanMessage());
            flagExtEraseDone = false;
        }

        private async System.Threading.Tasks.Task StartExtFlashing(byte deviceType)
        {
            OmniMessage msg = new();
            msg.Pgn = 107;
            msg.ReceiverId.Type = deviceType;
            msg.Data[0] = 4;
            Transmit(msg.ToCanMessage());
        }

        private bool CheckExtTransmittedData(int len, uint crc, byte deviceType)
        {
            OmniMessage msg = new()
            {
                Pgn = 107,
                ReceiverId = new(deviceType, 0),
                Data =
                {
                    [0] = 2
                }
            };

            for (var i = 0; i < 6; i++)
            {
                flagExtDataGetDone = false;
                if (i == 5)
                {
                    Bus.CurrentTask.OnFail(GetString("t_cant_check_transmission"));
                    return false;
                }
                Transmit(msg.ToCanMessage());
                WaitForFlag(ref flagExtDataGetDone, 100);

                LogWriteLine($"Len:{extReceivedFragmentLength},CRC:0x{extReceivedFragmentCrc:X08}");
                if (crc == extReceivedFragmentCrc && len == extReceivedFragmentLength)
                    return true;

                LogWriteLine(GetString("t_transmission_failed"));
                return false;
            }
            return false;
        }

        private async System.Threading.Tasks.Task SetExtFragmentAdr(CodeFragment f, byte deviceType)
        {
            OmniMessage msg = new()
            {
                Pgn = 107,
                ReceiverId = new(deviceType, 0),
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
                flagExtSetAdrDone = false;
                if (i == 3)
                {
                    Bus.CurrentTask.OnFail(GetString("t_cant_set_address"));
                    return;
                }
                Transmit(msg.ToCanMessage());
                if (!WaitForFlag(ref flagExtSetAdrDone, 300)) continue;
                if (extFragmentAddress == f.StartAddress)
                    break;
            }
        }

        private async void WriteExtFragmentToRam(CodeFragment f, byte deviceType)
        {
            OmniMessage msg = new()
            {
                Pgn = 108,
                ReceiverId = new(deviceType, 0),
            };
            LogWrite($"Ext fragment {f.StartAddress:X08}...");
            for (var k = 0; k < 16; k++)
            {
                SetExtFragmentAdr(f, deviceType);

                if (Bus.CurrentTask.Cts.IsCancellationRequested)
                    return;

                if (k == 15)
                {
                    Bus.CurrentTask.OnFail(GetString("t_cant_transmit_data"));
                    return;
                }
                if (k > 0)
                {
                    LogWriteLine($"Try: {k + 1}");
                }
                uint crc = 0;
                var len = 0;
                extReceivedFragmentCrc = 0;
                extReceivedFragmentLength = 0;

                for (var i = 0; i < (f.Length + 7) / 8; i++)
                {
                    for (var j = 0; j < 8; j++)
                    {
                        msg.Data[j] = f.Data[i * 8 + j];
                        crc += f.Data[i * 8 + j] * 170771U;
                        crc ^= ((crc >> 16) & 0xFFFFU);
                        len++;
                    }
                    Transmit(msg.ToCanMessage());
                }
                if (CheckExtTransmittedData(len, crc, deviceType)) break;
            }
        }

        private async System.Threading.Tasks.Task FlashExtFragment(CodeFragment f, byte deviceType)
        {
            WriteExtFragmentToRam(f, deviceType);
            for (var i = 0; i < 4; i++)
            {
                flagExtProgramDone = false;
                if (i == 3)
                {
                    Bus.CurrentTask.OnFail(GetString("t_cant_flash_memory"));
                    return;
                }
                StartExtFlashing(deviceType);
                if (WaitForFlag(ref flagExtProgramDone, 100))
                    break;
            }
        }

        // Стирает внешнюю flash-микросхему целиком, ждёт подтверждения (с ретраями). Возвращает
        // true, если стирание завершилось успешно (задача Capture/OnDone уже закрыта самим
        // методом), false - если не удалось (уже сообщено пользователю через OnFail/OnCancel,
        // или задача занята другой операцией).
        private async System.Threading.Tasks.Task<bool> EraseExtMemory()
        {
            if (!Bus.CurrentTask.Capture("Memory Erasing")) return false;
            LogWriteLine(GetString("t_starting_flash_erase"));
            for (var i = 0; i < 4; i++)
            {
                if (i == 3)
                {
                    Bus.CurrentTask.OnFail(GetString("t_cant_erase_memory"));
                    return false;
                }

                await EraseExtFlash();
                if (WaitForFlag(ref flagExtEraseDone, 60000)) break;
            }

            Bus.CurrentTask.OnDone();
            return true;
        }

        [RelayCommand]
        private void EraseMemory()
        {
            System.Threading.Tasks.Task.Run(async () =>
            {
                if (await EraseExtMemory())
                    LogWriteLine("Memory erase completed.");
            });
        }

        private async void WriteDumpToMemory(List<CodeFragment> fragmentsArg)
        {
            try
            {
                if (fragmentsArg.Count == 0)
                {
                    MessageBox.Show(GetString("t_load_hex_first"));
                    return;
                }
                LogWriteLine("Starting memory dump write...");
                if (!await EraseExtMemory()) return;
                Bus.CurrentTask.Capture("Programming");

                var deviceType = (byte)Id.Type;
                var cnt = 0;
                foreach (var f in fragmentsArg)
                {
                    FlashExtFragment(f, deviceType);
                    Bus.CurrentTask.UpdatePercent(cnt++ * 100 / fragmentsArg.Count);
                    if (Bus.CurrentTask.Cts.IsCancellationRequested) return;
                }
                LogWriteLine("Memory dump write completed.");
                Bus.CurrentTask.OnDone();
            }
            catch (Exception ex)
            {
                MessageBox.Show(ex.ToString());
            }
        }

        [RelayCommand]
        private void WriteDumpToMemory()
        {
            if (dumpFragments.Count == 0)
            {
                MessageBox.Show(GetString("t_load_hex_first"));
                return;
            }
            System.Threading.Tasks.Task.Run(() => WriteDumpToMemory(dumpFragments));
        }

        #region Firmware slots (stage-then-distribute over local CAN)

        // Mirrors the layout in PU28-BOOT-CAN's User/Main/memory.h (MEMORY_FIRMWARE_SLOT_*) -
        // keep these in sync with that file if the firmware's slot layout ever changes.
        private const int SlotCount = 3;
        private const uint SlotMetaSize = 0x10000;
        private const uint SlotDataSize = 0x80000;
        private const uint SlotSize = SlotMetaSize + SlotDataSize;
        private const uint ChipSize = 0x800000;
        private const uint SlotsStart = ChipSize - SlotCount * SlotSize;
        private static uint SlotMetaAddr(int slot) => (uint)(SlotsStart + slot * SlotSize);
        private static uint SlotDataAddr(int slot) => SlotMetaAddr(slot) + SlotMetaSize;

        [ObservableProperty] private int slotIndex = 0;
        [ObservableProperty] private string slotHexFilePath = "";
        [ObservableProperty] private string slotTargetAddressHex = "0x008000";
        [ObservableProperty] private string slotVersionText = "1.0.0.0";
        // PGN107/108 (slot upload) обслуживается и загрузчиком (Id.Type==123), и основной
        // программой ПУ28 (Id.Type==126, PanelDeviceViewModel) - в зависимости от того, в каком
        // физическом режиме сейчас устройство. Поэтому UploadToSlot() ниже не ограничивается
        // "своим" устройством (this), а переиспользует общий выбор в списке подключённых
        // устройств (Bus.SelectedConnectedDevice, тот же, что уже используют Parameters/Presets) -
        // загрузка идёт в то устройство, что сейчас выбрано там, будь то загрузчик или пульт.
        [ObservableProperty] private string slotStatusText = "";

        [RelayCommand]
        private void BrowseSlotHex()
        {
            OpenFileDialog dialog = new() { Filter = "Hex Files|*.hex" };
            if (!(bool)dialog.ShowDialog()) return;
            SlotHexFilePath = dialog.FileName;

            // Firmware files are conventionally named "<major>.<minor>.<patch>.<build>_<rest>",
            // e.g. "126.0.4.19_STM_Main.hex" - the version is the part before the first
            // underscore. Only overwrite the field if that part actually parses as four
            // byte-sized numbers, so an unrelated filename just leaves it alone.
            var versionCandidate = System.IO.Path.GetFileNameWithoutExtension(SlotHexFilePath).Split('_')[0];
            var versionParts = versionCandidate.Split('.');
            if (versionParts.Length == 4 && versionParts.All(p => byte.TryParse(p, out _)))
                SlotVersionText = versionCandidate;

            // Auto-fill the target address from the file's own lowest address record - a hex
            // file already encodes where on the target device it belongs (this is exactly the
            // same normalization UploadFirmwareToSlot itself does), so there's no reason to
            // make the user retype it. They can still edit the field afterward if needed.
            try
            {
                var raw = ParseHexFile(SlotHexFilePath, ExtFragmentSize, new List<CodeFragment>());
                if (raw is { Count: > 0 })
                {
                    var baseAddr = raw.Min(f => f.StartAddress);
                    SlotTargetAddressHex = $"0x{baseAddr:X}";
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show(ex.ToString());
            }
        }

        // Bounded erase (as opposed to EraseExtFlash's whole-chip erase) - PGN 107 case 14,
        // D[1]=3: erase `blocks` sectors of 0x10000 starting at `addr`. The reply (D[0]=15) is
        // already decoded into the same flagExtEraseDone flag EraseExtFlash's reply (D[0]=7)
        // uses - see Omni.cs case 107 - so no new flag is needed here.
        private void EraseExtRegion(uint addr, byte blocks, byte deviceType)
        {
            OmniMessage msg = new()
            {
                Pgn = 107,
                ReceiverId = new(deviceType, 0),
                Data =
                {
                    [0] = 14,
                    [1] = 3,
                    [2] = (byte)(addr >> 24),
                    [3] = (byte)(addr >> 16),
                    [4] = (byte)(addr >> 8),
                    [5] = (byte)addr,
                    [6] = blocks
                }
            };
            Transmit(msg.ToCanMessage());
        }

        // CRC16/ARC (poly 0xA001, init 0xFFFF, LSB-first) - matches calcCrc() in main.c and
        // Memory::calcCRC() in memory.cpp, which is the algorithm Boot::distributeSlot()
        // uses to verify a slot's integrity before pushing it out over CAN.
        private static ushort Crc16Arc(byte[] data, int length)
        {
            ushort crc = 0xFFFF;
            for (var i = 0; i < length; i++)
            {
                var b = data[i];
                for (var k = 0; k < 8; k++)
                {
                    var bit = (crc & 1) != 0;
                    crc >>= 1;
                    if (((b & 1) != 0) != bit) crc ^= 0xA001;
                    b >>= 1;
                }
            }
            return crc;
        }

        private async void UploadFirmwareToSlot(int slot, string hexFilePath, uint targetDeviceAddress, byte[] version, byte deviceType)
        {
            try
            {
                var raw = ParseHexFile(hexFilePath, ExtFragmentSize, new List<CodeFragment>());
                if (raw == null || raw.Count == 0)
                {
                    MessageBox.Show(GetString("t_load_hex_first"));
                    return;
                }

                // Normalize to a 0-based image - the hex file's own addressing convention
                // doesn't matter here, only the image's byte content and length. The slot's
                // meta records where the TARGET device should put it (SlotTargetAddressHex),
                // which is unrelated to whatever addresses the hex file happens to use.
                var baseAddr = raw.Min(f => f.StartAddress);
                var imageLen = raw.Max(f => f.StartAddress + (uint)f.Length) - baseAddr;
                if (imageLen == 0 || imageLen > SlotDataSize)
                {
                    MessageBox.Show($"Image is {imageLen} bytes, a slot holds at most {SlotDataSize}.");
                    return;
                }
                // Real firmware hex files commonly hold several disjoint sections (separate
                // ELA/type-04 records - e.g. a small vector-table stub near the base address,
                // then the real app tens or hundreds of KB further up), not one contiguous
                // blob. Filling the untouched gaps between them with 0xFF (erased flash's
                // actual reset state), not 0x00, matters twice over: it's what a freshly
                // erased slot already reads as - matching it means the CRC computed here
                // agrees with what Boot::distributeSlot() recomputes from the slot afterward -
                // and it lets the send loop below skip those gaps outright instead of
                // transmitting page after page of meaningless zero data.
                var image = new byte[imageLen];
                Array.Fill(image, (byte)0xFF);
                foreach (var f in raw)
                    Array.Copy(f.Data, 0, image, f.StartAddress - baseAddr, f.Length);

                if (!Bus.CurrentTask.Capture($"Erasing slot {slot}")) return;
                LogWriteLine($"Erasing slot {slot} ({imageLen} bytes to upload)...");

                var erased = false;
                for (var i = 0; i < 4 && !erased; i++)
                {
                    flagExtEraseDone = false;
                    EraseExtRegion(SlotMetaAddr(slot), (byte)(SlotSize / 0x10000), deviceType);
                    erased = WaitForFlag(ref flagExtEraseDone, 60000);
                }
                if (!erased)
                {
                    Bus.CurrentTask.OnFail(GetString("t_cant_erase_memory"));
                    return;
                }

                Bus.CurrentTask.Capture("Uploading to slot...");
                var fragmentCount = (imageLen + ExtFragmentSize - 1) / ExtFragmentSize;
                uint offset = 0;
                var cnt = 0;
                while (offset < imageLen)
                {
                    var chunkLen = (int)Math.Min((uint)ExtFragmentSize, imageLen - offset);

                    // A chunk that's still all 0xFF is exactly what the erase step already left
                    // there - skip sending it. This is what actually saves the time; it's
                    // typical for a chunk of these gaps to be page after page of nothing but
                    // the vector-table-stub-to-main-app jump.
                    var allErased = true;
                    for (var i = 0; i < chunkLen && allErased; i++)
                        if (image[offset + i] != 0xFF) allErased = false;

                    if (!allErased)
                    {
                        // WriteExtFragmentToRam below reads f.Data in 8-byte groups rounded UP
                        // from f.Length ((Length+7)/8*8), same as every other fragment producer
                        // in this file (see ParseHexFile) - allocate the full ExtFragmentSize
                        // regardless of how much of it is meaningful, or a short last chunk
                        // throws IndexOutOfRangeException reading past a tightly-sized array.
                        var f = new CodeFragment(ExtFragmentSize) { StartAddress = SlotDataAddr(slot) + offset, Length = chunkLen };
                        Array.Copy(image, offset, f.Data, 0, chunkLen);
                        FlashExtFragment(f, deviceType);
                    }
                    offset += (uint)chunkLen;
                    Bus.CurrentTask.UpdatePercent((int)(cnt++ * 100 / fragmentCount));
                    if (Bus.CurrentTask.Cts.IsCancellationRequested) return;
                }

                LogWriteLine("Writing slot metadata...");
                var crc16 = Crc16Arc(image, image.Length);
                var meta = new byte[14];
                meta[0] = (byte)targetDeviceAddress; meta[1] = (byte)(targetDeviceAddress >> 8);
                meta[2] = (byte)(targetDeviceAddress >> 16); meta[3] = (byte)(targetDeviceAddress >> 24);
                meta[4] = (byte)imageLen; meta[5] = (byte)(imageLen >> 8);
                meta[6] = (byte)(imageLen >> 16); meta[7] = (byte)(imageLen >> 24);
                meta[8] = (byte)crc16; meta[9] = (byte)(crc16 >> 8);
                meta[10] = version[0]; meta[11] = version[1]; meta[12] = version[2]; meta[13] = version[3];
                var metaFragment = new CodeFragment(ExtFragmentSize) { StartAddress = SlotMetaAddr(slot), Length = 14 };
                Array.Copy(meta, metaFragment.Data, meta.Length);
                FlashExtFragment(metaFragment, deviceType);

                RunOnUi(() => SlotStatusText = $"Slot {slot}: {imageLen}B, CRC 0x{crc16:X4}, uploaded.");
                LogWriteLine($"Slot {slot} upload complete: {imageLen} bytes, CRC16 0x{crc16:X4}.");
                Bus.CurrentTask.OnDone();
            }
            catch (Exception ex)
            {
                MessageBox.Show(ex.ToString());
            }
        }

        [RelayCommand]
        private void UploadToSlot()
        {
            if (string.IsNullOrEmpty(SlotHexFilePath))
            {
                MessageBox.Show(GetString("t_load_hex_first"));
                return;
            }
            var targetDevice = Bus.SelectedConnectedDevice;
            if (targetDevice == null)
            {
                MessageBox.Show("Select a target device (bootloader or panel) in the device list first.");
                return;
            }
            var addrText = SlotTargetAddressHex.Trim();
            if (addrText.StartsWith("0x") || addrText.StartsWith("0X")) addrText = addrText[2..];
            if (!uint.TryParse(addrText, System.Globalization.NumberStyles.HexNumber, null, out var targetAddr))
            {
                MessageBox.Show("Target address must be hex, e.g. 0x008000.");
                return;
            }
            var parts = SlotVersionText.Split('.');
            var version = new byte[4];
            for (var i = 0; i < 4 && i < parts.Length; i++) byte.TryParse(parts[i], out version[i]);

            var slot = SlotIndex;
            var path = SlotHexFilePath;
            var deviceType = (byte)targetDevice.Id.Type;
            System.Threading.Tasks.Task.Run(() => UploadFirmwareToSlot(slot, path, targetAddr, version, deviceType));
        }

        #endregion

        #region Own/slot image versions (PGN110 subpackets 14-17, periodic broadcast)

        // ПУ28 периодически транслирует версии собственного образа ПО и трёх OTA-слотов
        // внешней flash-памяти (Omni.cs case 110 -> DecodePu28ImageVersions, Data[0] 14/15/16/17 -
        // см. протокол). Кодирование версии как у Firmware/BootFirmware: 255.255.255.255 - нет
        // ПО (пустой слот), 0.0.0.0 - несовпадение контрольной суммы.
        [ObservableProperty] private BindingList<int> ownImageVersion = new() { 0, 0, 0, 0 };
        [ObservableProperty] private BindingList<int> slot0ImageVersion = new() { 0, 0, 0, 0 };
        [ObservableProperty] private BindingList<int> slot1ImageVersion = new() { 0, 0, 0, 0 };
        [ObservableProperty] private BindingList<int> slot2ImageVersion = new() { 0, 0, 0, 0 };

        #endregion

        // Устанавливает адрес и запрашивает у устройства (this, а не жёстко 123) чтение len байт
        // (PGN107 case16), получает их через поток кадров PGN109 и сверяет по CRC (case17).
        private bool ReadExtChunk(uint addr, int len, out byte[] data)
        {
            data = null;
            var deviceType = (byte)Id.Type;

            OmniMessage setMsg = new()
            {
                Pgn = 107,
                ReceiverId = new(deviceType, 0),
                Data =
                {
                    [0] = 0,
                    [1] = (byte)(addr >> 24),
                    [2] = (byte)(addr >> 16),
                    [3] = (byte)(addr >> 8),
                    [4] = (byte)(addr)
                }
            };
            var addrOk = false;
            for (var i = 0; i < 4; i++)
            {
                flagExtSetAdrDone = false;
                Transmit(setMsg.ToCanMessage());
                if (WaitForFlag(ref flagExtSetAdrDone, 300) && extFragmentAddress == addr)
                {
                    addrOk = true;
                    break;
                }
            }
            if (!addrOk)
            {
                LogWriteLine($"Dump read: can't set address 0x{addr:X08}");
                return false;
            }

            extReadBuffer.Clear();
            OmniMessage readMsg = new()
            {
                Pgn = 107,
                ReceiverId = new(deviceType, 0),
                Data =
                {
                    [0] = 16,
                    [1] = (byte)(len >> 16),
                    [2] = (byte)(len >> 8),
                    [3] = (byte)(len)
                }
            };
            flagExtBulkReadDone = false;
            Transmit(readMsg.ToCanMessage());
            // Каждый из ~len/8 кадров PGN109 маршалится в UI-поток синхронно
            // (UIContext.Send в MainWindowViewModel.NewMessgeReceived), так что
            // запас по времени должен считаться не от битрейта шины, а от этого overhead.
            var timeoutMs = Math.Max(5000, len * 3);
            if (!WaitForFlag(ref flagExtBulkReadDone, timeoutMs))
            {
                LogWriteLine($"Dump read: timeout at 0x{addr:X08}, got {extReadBuffer.Count}/{len} bytes");
                return false;
            }
            if (extBulkReadLen != (uint)len || extReadBuffer.Count < len)
            {
                LogWriteLine($"Dump read: device rejected chunk at 0x{addr:X08} (got {extReadBuffer.Count}/{len} bytes, reported len={extBulkReadLen})");
                return false;
            }

            uint crc = 0;
            for (var i = 0; i < len; i++)
            {
                crc += extReadBuffer[i] * 170771U;
                crc ^= (crc >> 16) & 0xFFFFU;
            }
            if (crc != extBulkReadCrc)
            {
                LogWriteLine($"Dump read: CRC mismatch at 0x{addr:X08} (expected 0x{extBulkReadCrc:X08}, got 0x{crc:X08})");
                return false;
            }

            data = extReadBuffer.Take(len).ToArray();
            return true;
        }

        private void ReadDumpFromMemory()
        {
            try
            {
                const uint chipSize = 0x800000;

                var startHex = DumpReadStartHex.Trim();
                if (startHex.StartsWith("0x", StringComparison.OrdinalIgnoreCase)) startHex = startHex[2..];
                if (startHex.Length == 0) startHex = "0";
                if (!uint.TryParse(startHex, System.Globalization.NumberStyles.HexNumber, null, out var startAddr))
                {
                    MessageBox.Show("Bad start address (hex)");
                    return;
                }
                if (DumpReadLengthKb <= 0)
                {
                    MessageBox.Show("Bad length (KB)");
                    return;
                }
                var readLen = (uint)DumpReadLengthKb * 1024;
                if (startAddr >= chipSize || (ulong)startAddr + readLen > chipSize || readLen == 0)
                {
                    MessageBox.Show($"Range must fit within the chip (0..0x{chipSize:X}) and be non-zero");
                    return;
                }

                if (!Bus.CurrentTask.Capture("Reading Memory Dump")) return;
                LogWriteLine($"Starting dump read: 0x{startAddr:X08}..0x{(startAddr + readLen):X08}...");
                var stopwatch = System.Diagnostics.Stopwatch.StartNew();

                var readData = new byte[readLen];
                uint addr = startAddr;
                var endAddr = startAddr + readLen;
                var effectiveChunkSize = LegacyAdapterMode ? LegacyDumpReadChunkSize : DumpReadChunkSize;
                while (addr < endAddr)
                {
                    var chunk = (int)Math.Min(effectiveChunkSize, endAddr - addr);
                    byte[] data = null;
                    var ok = false;
                    // В legacy-режиме потери не стопроцентные, а retry не всегда пробивает за
                    // 4 попытки - даём больше шансов вместо обрыва всей операции.
                    var maxAttempts = LegacyAdapterMode ? 20 : 4;
                    for (var attempt = 0; attempt < maxAttempts; attempt++)
                    {
                        if (ReadExtChunk(addr, chunk, out data)) { ok = true; break; }
                        LogWriteLine($"Retry chunk at 0x{addr:X08} (attempt {attempt + 1})");
                    }
                    if (!ok)
                    {
                        Bus.CurrentTask.OnFail($"Can't read chunk at 0x{addr:X08}");
                        return;
                    }
                    Array.Copy(data, 0, readData, addr - startAddr, chunk);
                    addr += (uint)chunk;
                    if (LegacyAdapterMode) System.Threading.Thread.Sleep(LegacyInterChunkDelayMs);
                    Bus.CurrentTask.UpdatePercent((int)((ulong)(addr - startAddr) * 100 / readLen));
                    if (Bus.CurrentTask.Cts.IsCancellationRequested)
                    {
                        Bus.CurrentTask.OnCancel();
                        return;
                    }
                }

                stopwatch.Stop();
                var seconds = stopwatch.Elapsed.TotalSeconds;
                var rate = seconds > 0 ? readLen / seconds : 0;
                LogWriteLine($"Dump read complete in {seconds:F1}s ({rate / 1024:F1} KB/s), choose file to save...");
                Bus.CurrentTask.OnDone();

                string savePath = null;
                Application.Current.Dispatcher.Invoke(() =>
                {
                    SaveFileDialog dialog = new() { Filter = "Hex Files|*.hex", FileName = "dump.hex" };
                    if ((bool)dialog.ShowDialog()) savePath = dialog.FileName;
                });
                if (string.IsNullOrEmpty(savePath)) return;

                WriteIntelHexFile(savePath, readData, startAddr);
                LogWriteLine($"Dump saved to {savePath}");
            }
            catch (Exception ex)
            {
                MessageBox.Show(ex.ToString());
            }
        }

        [RelayCommand]
        private void ReadDump()
        {
            System.Threading.Tasks.Task.Run(ReadDumpFromMemory);
        }

        private static void WriteHexRecord(System.IO.StreamWriter sw, int len, ushort addr, byte type, byte[] payload)
        {
            var sb = new System.Text.StringBuilder();
            sb.Append(':');
            var sum = 0;
            void AppendByte(int b) { sb.Append(b.ToString("X2")); sum += b; }
            AppendByte(len);
            AppendByte((addr >> 8) & 0xFF);
            AppendByte(addr & 0xFF);
            AppendByte(type);
            foreach (var b in payload) AppendByte(b);
            var checksum = (byte)(0x100 - (sum & 0xFF));
            sb.Append(checksum.ToString("X2"));
            sw.WriteLine(sb.ToString());
        }

        // Пишет дамп в Intel HEX, пропуская 16-байтные строки из одних 0xFF (нестёртые/неиспользуемые
        // области чипа), чтобы файл оставался компактным и симметричным записи через WriteDumpToMemory
        // (которая тоже пропускает чистые 0xFF записи). baseAddress - реальный адрес в чипе, с которого
        // начинается data (для частичного чтения, не только с нуля).
        private static void WriteIntelHexFile(string path, byte[] data, uint baseAddress = 0)
        {
            using var sw = new System.IO.StreamWriter(path, false);
            var lastUpperAddr = uint.MaxValue;
            const int lineLen = 16;
            for (var offset = 0; offset < data.Length; offset += lineLen)
            {
                var len = Math.Min(lineLen, data.Length - offset);
                var allFF = true;
                for (var i = 0; i < len; i++)
                    if (data[offset + i] != 0xFF) { allFF = false; break; }
                if (allFF) continue;

                var absAddr = baseAddress + (uint)offset;
                var upperAddr = absAddr >> 16;
                if (upperAddr != lastUpperAddr)
                {
                    WriteHexRecord(sw, 2, 0, 4, new byte[] { (byte)(upperAddr >> 8), (byte)upperAddr });
                    lastUpperAddr = upperAddr;
                }

                var lowerAddr = (ushort)(absAddr & 0xFFFF);
                var lineData = new byte[len];
                Array.Copy(data, offset, lineData, 0, len);
                WriteHexRecord(sw, len, lowerAddr, 0, lineData);
            }
            sw.WriteLine(":00000001FF");
        }
    }
}
