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

        private List<CodeFragment> fragments = new();
        private List<CodeFragment> dumpFragments = new();

        [ObservableProperty]
        private int fragmentSize = 512;

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
            if (VulnerableMbcOnBus(Vm.OmniInstance.SelectedConnectedDevice))
            {
                MessageBox.Show(GetString("t_vulnerable_mbc_on_bus"), GetString("t_vulnerable_mbc_title"),
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
                    //Task.Delay(1).Wait(); // Enough for bootloader to process one 8-byte CAN frame

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

        // Returns true=match, false=CRC mismatch, null=no response (abort)
        // Проверка по CRC доступна только с загрузчика build 13+ (см. VerifyFirmwareCommand),
        // а с этой версии загрузчик уже понимает PGN110 - поэтому используем его вместе с
        // настоящим CRC32 (Crc32() ниже) вместо старого слабого алгоритма PGN105/10.
        private bool? VerifyFragment(CodeFragment f)
        {
            var verifyLen = VerifyLen(f);
            var expectedCrc = Crc32(f.Data, verifyLen);

            OmniMessage msg = new()
            {
                Pgn = 110,
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

        #region thirdGenerationBootloader (PGN110/111)

        // Загрузчики с этой сборки (BootFirmware[3], VER_ASSEMBLAGE_NUMBER) и новее понимают
        // протокол 3-го поколения (PGN110/111). Используется в RunAutoUpdate для выбора протокола.
        private const byte Gen3MinBootBuild = 13;

        // CRC-32/ISO-HDLC (poly 0xEDB88320, init/final 0xFFFFFFFF) - тот же алгоритм, что
        // Crc32() в прошивке (messages.cpp), используется вместо старого слабого счётчика
        // ("x*170771 ^ (x>>16)") в PGN105/2 и PGN105/10.
        private static uint Crc32(byte[] data, int length)
        {
            uint crc = 0xFFFFFFFFu;
            for (var i = 0; i < length; i++)
            {
                crc ^= data[i];
                for (var b = 0; b < 8; b++)
                    crc = (crc & 1) != 0 ? (crc >> 1) ^ 0xEDB88320u : (crc >> 1);
            }
            return crc ^ 0xFFFFFFFFu;
        }

        private async Task EraseFlash110()
        {
            OmniMessage msg = new();
            msg.Pgn = 110;
            msg.ReceiverId.Type = 123;
            msg.Data[0] = 6;
            msg.Data[1] = 1; // режим 1: только основная программа (настройки/чёрные ящики не трогаются)
            msg.Data[6] = 0xAA; // magic-слово, защита от случайного стирания (см. Messages::ProcessMessage, PGN110/6)
            msg.Data[7] = 0x55;
            Debug.WriteLine("Отправляем запрос на стирание (PGN110)");
            Vm.CanAdapter.Transmit(msg.ToCanMessage());
            flagEraseDone = false;
        }

        private async Task StartFlashing110()
        {
            OmniMessage msg = new();
            msg.Pgn = 110;
            msg.ReceiverId.Type = 123;
            msg.Data[0] = 4;
            msg.Data[6] = 0x55; // magic-слово, защита от случайной записи (см. Messages::ProcessMessage, PGN110/4)
            msg.Data[7] = 0xAA;
            Vm.CanAdapter.Transmit(msg.ToCanMessage());
        }

        private bool CheckTransmittedData110(int len, uint crc)
        {
            OmniMessage msg = new()
            {
                Pgn = 110,
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

                LogWriteLine($"Len:{receivedFragmentLength},CRC32:0x{receivedFragmentCrc:X08}");
                if (crc == receivedFragmentCrc && len == receivedFragmentLength)
                    return true;

                Debug.WriteLine($"CRC32 mismatch: expected {crc:X08}, got {receivedFragmentCrc:X08}");
                LogWriteLine(GetString("t_transmission_failed"));
                return false;
            }
            return false;
        }

        private async Task SetFragmentAdr110(CodeFragment f)
        {
            OmniMessage msg = new()
            {
                Pgn = 110,
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

        private async void WriteFragmentToRam110(CodeFragment f)
        {
            OmniMessage msg = new()
            {
                Pgn = 111,
                ReceiverId = new(123, 0),
            };
            LogWrite($"Fragment {f.StartAddress:X08}...");
            for (var k = 0; k < 16; k++)
            {
                SetFragmentAdr110(f);

                if (Vm.OmniInstance.CurrentTask.Cts.IsCancellationRequested)
                    return;

                if (k == 15)
                {
                    Vm.OmniInstance.CurrentTask.OnFail(GetString("t_cant_transmit_data"));
                    Debug.WriteLine($"Превышено число попыток передачи");
                    return;
                }
                if (k > 0)
                {
                    LogWriteLine($"Try: {k + 1}");
                }
                receivedFragmentCrc = 0;
                receivedFragmentLength = 0;

                // Бутлоадер буферизует ровно то, что получил в 8-байтных кадрах (без фильтрации
                // хвоста), поэтому локальный CRC32 считаем по тем же дополненным до границы 8
                // байт данным (см. VerifyLen/CalcFragmentCrc ниже - тот же приём для новой КС).
                var len = VerifyLen(f);
                for (var i = 0; i < len / 8; i++)
                {
                    for (var j = 0; j < 8; j++)
                        msg.Data[j] = f.Data[i * 8 + j];
                    Vm.CanAdapter.Transmit(msg.ToCanMessage());
                }
                var crc = Crc32(f.Data, len);
                if (CheckTransmittedData110(len, crc)) break;
            }
        }

        private async Task FlashFragment110(CodeFragment f)
        {
            WriteFragmentToRam110(f);
            for (var i = 0; i < 4; i++)
            {
                flagProgramDone = false;
                if (i == 3)
                {
                    Vm.OmniInstance.CurrentTask.OnFail(GetString("t_cant_flash_memory"));
                    return;
                }
                StartFlashing110();
                if (WaitForFlag(ref flagProgramDone, 100))
                    break;
            }
        }

        private async void UpdateFirmware110(List<CodeFragment> fragmentsArg)
        {
            try
            {
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

                    await EraseFlash110();
                    if (WaitForFlag(ref flagEraseDone, 5000)) break;
                }

                Vm.OmniInstance.CurrentTask.OnDone();

                Vm.OmniInstance.CurrentTask.Capture("Programming");

                var cnt = 0;
                foreach (var f in fragmentsArg)
                {
                    FlashFragment110(f);
                    Vm.OmniInstance.CurrentTask.PercentComplete = cnt++ * 100 / fragmentsArg.Count;
                    if (Vm.OmniInstance.CurrentTask.Cts.IsCancellationRequested) return;
                }
                LogWriteLine(GetString("t_firmware_update_success"));
                Vm.OmniInstance.CurrentTask.OnDone();
            }
            catch (Exception ex)
            {
                MessageBox.Show(ex.Message);
            }
        }

        [RelayCommand]
        private void UpdateFirmware110()
        {
            if (fragments.Count == 0)
            {
                MessageBox.Show(GetString("t_load_hex_first"));
                return;
            }
            var dev = Vm?.OmniInstance?.SelectedConnectedDevice;
            if (dev == null || dev.Id.Type != 123 || dev.BootFirmware[0] != 123 || dev.BootFirmware[3] < Gen3MinBootBuild)
            {
                MessageBox.Show($"Gen.3 protocol requires bootloader (type 123) version 123.0.0.{Gen3MinBootBuild} or newer.",
                                "Unsupported bootloader", MessageBoxButton.OK, MessageBoxImage.Warning);
                return;
            }
            Task.Run(() => UpdateFirmware110(fragments));
        }

        #endregion

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
                        if (current.Length == 0) // see ParseHexFile for why not StartAddress==0
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

        private List<CodeFragment> ParseHexFile(string path, int maxFragmentSize)
        {
            fragments.Clear();
            return ParseHexFile(path, maxFragmentSize, fragments);
        }

        private List<CodeFragment> ParseHexFile(string path, int maxFragmentSize, List<CodeFragment> target)
        {
            // Не логируем сюда построчно/пофрагментно: LogWriteLine делает Log = str + Log,
            // то есть каждый вызов копирует весь накопленный лог целиком - для файла на
            // несколько тысяч фрагментов (например, дамп в 1МБ при 256-байтных фрагментах)
            // это O(n^2) и превращает загрузку в дело на десятки секунд. Итоговое количество
            // фрагментов логируется один раз в LoadHex/LoadDumpHex после завершения парсинга.
            void AddFragment(CodeFragment fragment)
            {
                target.Add(fragment);
            }

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
                        }
                        if (currentFragment.Length == 0) //First line in data fragment, saving address
                        {
                            // Length==0, not StartAddress==0: a fragment whose true start address
                            // is exactly 0 (the very first fragment of a file starting at 0x000000)
                            // would otherwise be indistinguishable from "not yet set", so every
                            // following line at address 0, 16, 32... would keep overwriting
                            // StartAddress - firmware then wrote a full 256-byte page starting
                            // mid-page, and the flash's own page-program wrap corrupted the data.
                            currentFragment.StartAddress = pageAddress + localAddress;
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
                        break;
                    case 1:
                        if (currentFragment.Length > 0)
                            AddFragment(currentFragment);
                        return target;

                }
            }
            return target;
        }

        [RelayCommand]
        private void LoadDumpHex()
        {
            OpenFileDialog dialog = new() { Filter = "Hex Files|*.hex" };
            if (!(bool)dialog.ShowDialog()) return;
            dumpFragments.Clear();
            ParseHexFile(dialog.FileName, ExtFragmentSize, dumpFragments);
            LogWriteLine($"Dump hex is loaded, contains {dumpFragments.Count} fragments.");
        }

        private async Task EraseExtFlash()
        {
            OmniMessage msg = new();
            msg.Pgn = 107;
            msg.ReceiverId.Type = 123;
            msg.Data[0] = 14;
            msg.Data[1] = 0; //Стереть всю память
            Vm.CanAdapter.Transmit(msg.ToCanMessage());
            flagExtEraseDone = false;
        }

        private async Task StartExtFlashing(byte deviceType = 123)
        {
            OmniMessage msg = new();
            msg.Pgn = 107;
            msg.ReceiverId.Type = deviceType;
            msg.Data[0] = 4;
            Vm.CanAdapter.Transmit(msg.ToCanMessage());
        }

        private bool CheckExtTransmittedData(int len, uint crc, byte deviceType = 123)
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
                    Vm.OmniInstance.CurrentTask.OnFail(GetString("t_cant_check_transmission"));
                    return false;
                }
                Vm.CanAdapter.Transmit(msg.ToCanMessage());
                WaitForFlag(ref flagExtDataGetDone, 100);

                LogWriteLine($"Len:{extReceivedFragmentLength},CRC:0x{extReceivedFragmentCrc:X08}");
                if (crc == extReceivedFragmentCrc && len == extReceivedFragmentLength)
                    return true;

                LogWriteLine(GetString("t_transmission_failed"));
                return false;
            }
            return false;
        }

        private async Task SetExtFragmentAdr(CodeFragment f, byte deviceType = 123)
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
                    Vm.OmniInstance.CurrentTask.OnFail(GetString("t_cant_set_address"));
                    return;
                }
                Vm.CanAdapter.Transmit(msg.ToCanMessage());
                if (!WaitForFlag(ref flagExtSetAdrDone, 300)) continue;
                if (extFragmentAddress == f.StartAddress)
                    break;
            }
        }

        private async void WriteExtFragmentToRam(CodeFragment f, byte deviceType = 123)
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

                if (Vm.OmniInstance.CurrentTask.Cts.IsCancellationRequested)
                    return;

                if (k == 15)
                {
                    Vm.OmniInstance.CurrentTask.OnFail(GetString("t_cant_transmit_data"));
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
                    Vm.CanAdapter.Transmit(msg.ToCanMessage());
                }
                if (CheckExtTransmittedData(len, crc, deviceType)) break;
            }
        }

        private async Task FlashExtFragment(CodeFragment f, byte deviceType = 123)
        {
            WriteExtFragmentToRam(f, deviceType);
            for (var i = 0; i < 4; i++)
            {
                flagExtProgramDone = false;
                if (i == 3)
                {
                    Vm.OmniInstance.CurrentTask.OnFail(GetString("t_cant_flash_memory"));
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
        private async Task<bool> EraseExtMemory()
        {
            if (!Vm.OmniInstance.CurrentTask.Capture("Memory Erasing")) return false;
            LogWriteLine(GetString("t_starting_flash_erase"));
            for (var i = 0; i < 4; i++)
            {
                if (i == 3)
                {
                    Vm.OmniInstance.CurrentTask.OnFail(GetString("t_cant_erase_memory"));
                    return false;
                }

                await EraseExtFlash();
                if (WaitForFlag(ref flagExtEraseDone, 60000)) break;
            }

            Vm.OmniInstance.CurrentTask.OnDone();
            return true;
        }

        [RelayCommand]
        private void EraseMemory()
        {
            Task.Run(async () =>
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
                Vm.OmniInstance.CurrentTask.Capture("Programming");

                var cnt = 0;
                foreach (var f in fragmentsArg)
                {
                    FlashExtFragment(f);
                    Vm.OmniInstance.CurrentTask.PercentComplete = cnt++ * 100 / fragmentsArg.Count;
                    if (Vm.OmniInstance.CurrentTask.Cts.IsCancellationRequested) return;
                }
                LogWriteLine("Memory dump write completed.");
                Vm.OmniInstance.CurrentTask.OnDone();
            }
            catch (Exception ex)
            {
                MessageBox.Show(ex.Message);
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
            Task.Run(() => WriteDumpToMemory(dumpFragments));
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
        // PGN107/108 (slot upload) is now served by both the bootloader (always type 123) and
        // PU28-Timberline's main program (type 126) - which one actually answers depends on
        // whether the device is currently sitting in its bootloader or running normally. Rather
        // than a second, separate picker just for this panel, UploadToSlot() below reuses the
        // app's existing "selected device" concept (Vm.OmniInstance.SelectedConnectedDevice, the
        // same one Parameters/Presets already key off) - upload goes to whichever device is
        // currently selected there, bootloader or panel.
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
            var versionCandidate = Path.GetFileNameWithoutExtension(SlotHexFilePath).Split('_')[0];
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
                MessageBox.Show(ex.Message);
            }
        }

        // Bounded erase (as opposed to EraseExtFlash's whole-chip erase) - PGN 107 case 14,
        // D[1]=3: erase `blocks` sectors of 0x10000 starting at `addr`. The reply (D[0]=15) is
        // already decoded into the same flagExtEraseDone flag EraseExtFlash's reply (D[0]=7)
        // uses - see Omni.cs case 107 - so no new flag is needed here.
        private void EraseExtRegion(uint addr, byte blocks, byte deviceType = 123)
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
            Vm.CanAdapter.Transmit(msg.ToCanMessage());
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

        private async void UploadFirmwareToSlot(int slot, string hexFilePath, uint targetDeviceAddress, byte[] version, byte deviceType = 123)
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

                if (!Vm.OmniInstance.CurrentTask.Capture($"Erasing slot {slot}")) return;
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
                    Vm.OmniInstance.CurrentTask.OnFail(GetString("t_cant_erase_memory"));
                    return;
                }

                Vm.OmniInstance.CurrentTask.Capture("Uploading to slot...");
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
                    Vm.OmniInstance.CurrentTask.PercentComplete = (int)(cnt++ * 100 / fragmentCount);
                    if (Vm.OmniInstance.CurrentTask.Cts.IsCancellationRequested) return;
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

                SlotStatusText = $"Slot {slot}: {imageLen}B, CRC 0x{crc16:X4}, uploaded.";
                LogWriteLine($"Slot {slot} upload complete: {imageLen} bytes, CRC16 0x{crc16:X4}.");
                Vm.OmniInstance.CurrentTask.OnDone();
            }
            catch (Exception ex)
            {
                MessageBox.Show(ex.Message);
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
            var targetDevice = Vm.OmniInstance.SelectedConnectedDevice;
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
            Task.Run(() => UploadFirmwareToSlot(slot, path, targetAddr, version, deviceType));
        }

        #endregion

        // Устанавливает адрес и запрашивает у загрузчика чтение len байт (PGN107 case16),
        // получает их через поток кадров PGN109 и сверяет по CRC (case17).
        private bool ReadExtChunk(uint addr, int len, out byte[] data)
        {
            data = null;

            OmniMessage setMsg = new()
            {
                Pgn = 107,
                ReceiverId = new(123, 0),
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
                Vm.CanAdapter.Transmit(setMsg.ToCanMessage());
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
                ReceiverId = new(123, 0),
                Data =
                {
                    [0] = 16,
                    [1] = (byte)(len >> 16),
                    [2] = (byte)(len >> 8),
                    [3] = (byte)(len)
                }
            };
            flagExtBulkReadDone = false;
            Vm.CanAdapter.Transmit(readMsg.ToCanMessage());
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

                if (!Vm.OmniInstance.CurrentTask.Capture("Reading Memory Dump")) return;
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
                        Vm.OmniInstance.CurrentTask.OnFail($"Can't read chunk at 0x{addr:X08}");
                        return;
                    }
                    Array.Copy(data, 0, readData, addr - startAddr, chunk);
                    addr += (uint)chunk;
                    if (LegacyAdapterMode) Thread.Sleep(LegacyInterChunkDelayMs);
                    Vm.OmniInstance.CurrentTask.PercentComplete = (int)((ulong)(addr - startAddr) * 100 / readLen);
                    if (Vm.OmniInstance.CurrentTask.Cts.IsCancellationRequested)
                    {
                        Vm.OmniInstance.CurrentTask.OnCancel();
                        return;
                    }
                }

                stopwatch.Stop();
                var seconds = stopwatch.Elapsed.TotalSeconds;
                var rate = seconds > 0 ? readLen / seconds : 0;
                LogWriteLine($"Dump read complete in {seconds:F1}s ({rate / 1024:F1} KB/s), choose file to save...");
                Vm.OmniInstance.CurrentTask.OnDone();

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
                MessageBox.Show(ex.Message);
            }
        }

        [RelayCommand]
        private void ReadDump()
        {
            Task.Run(ReadDumpFromMemory);
        }

        private static void WriteHexRecord(StreamWriter sw, int len, ushort addr, byte type, byte[] payload)
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
            using var sw = new StreamWriter(path, false);
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
            if (fragments.Count == 0)
            {
                MessageBox.Show(GetString("t_load_hex_first"));
                return;
            }
            var dev = Vm?.OmniInstance?.SelectedConnectedDevice;
            if (dev == null || dev.Id.Type != 123 || dev.BootFirmware[0] != 123 || dev.BootFirmware[3] < Gen3MinBootBuild)
            {
                MessageBox.Show($"CRC verification (PGN110/CRC32) requires bootloader version 123.0.0.{Gen3MinBootBuild} or newer.",
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
                // Всегда запрашиваем hex при нажатии AUTO, даже если он уже был загружен ранее -
                // иначе повторное нажатие AUTO для другого устройства на шине молча прошивает
                // его тем же файлом, что и предыдущее устройство, и может окирпичить его.
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

                var omni = Vm.OmniInstance;

                if (BootloaderAlreadyOnBus())
                {
                    MessageBox.Show(GetString("t_bootloader_already_on_bus"), GetString("t_bootloader_conflict_title"),
                        MessageBoxButton.OK, MessageBoxImage.Warning);
                    return;
                }
                if (VulnerableMbcOnBus(omni.SelectedConnectedDevice))
                {
                    MessageBox.Show(GetString("t_vulnerable_mbc_on_bus"), GetString("t_vulnerable_mbc_title"),
                        MessageBoxButton.OK, MessageBoxImage.Warning);
                    return;
                }

                // Запоминаем исходный тип устройства перед входом в загрузчик
                var originalDeviceType = omni.SelectedConnectedDevice?.Id.Type ?? -1;

                // Проверяем версию прошиваемого hex против текущей версии ПО устройства (пока
                // оно ещё не перешло в загрузчик и не потеряло свою "личность") - защита от
                // прошивки бинарником другого изделия (например, PU28 в MBC-2). Сверяем первые
                // 2 байта версии (тип изделия и его вариант/подтип) - см. CalcVersionMismatch.
                if (!ConfirmVersionMatch(omni.SelectedConnectedDevice)) return;

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
                else if (boot != null && boot.BootFirmware[0] == 123 && boot.BootFirmware[3] >= Gen3MinBootBuild)
                    UpdateFirmware110(fragments);
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

        // Имя файла прошивки по соглашению начинается с версии, напр.
        // "126.0.4.19_STM_Main.hex" (см. также BrowseSlotHex). Порядок байт совпадает с
        // BootFirmware/Firmware: [0]=тип изделия, [1]=напряжение/вариант, [2]=подтип, [3]=сборка.
        private static bool TryParseVersionFromFileName(string path, out byte[] version)
        {
            version = null;
            if (string.IsNullOrEmpty(path)) return false;
            var candidate = Path.GetFileNameWithoutExtension(path).Split('_')[0];
            var parts = candidate.Split('.');
            if (parts.Length != 4 || !parts.All(p => byte.TryParse(p, out _))) return false;
            version = parts.Select(byte.Parse).ToArray();
            return true;
        }

        // Сверяет версию, зашитую в имя выбранного hex-файла, с версией ПО, которое сейчас
        // реально работает на устройстве (запрашиваем заново, чтобы не полагаться на
        // потенциально устаревшие данные). Если первые 2 байта версии (тип изделия и
        // подтип/вариант) не совпадают - это явный признак, что выбран бинарник от другого
        // изделия (например, прошивка PU28 для MBC-2), и такую прошивку легко можно
        // окирпичить. Если версия имени файла не распознана или версия устройства ещё
        // неизвестна, сравнение пропускается - молча проходить дальше без спроса не даём
        // только в случае явного расхождения.
        private bool ConfirmVersionMatch(DeviceViewModel dev)
        {
            if (dev == null) return true;
            if (!TryParseVersionFromFileName(lastHexFilePath, out var hexVersion)) return true;

            _ = GetVersion();
            Thread.Sleep(500);

            var curVersion = dev.Firmware;
            if (curVersion == null || (curVersion[0] == 0 && curVersion[1] == 0)) return true; // версия устройства неизвестна

            if (curVersion[0] == hexVersion[0] && curVersion[1] == hexVersion[1]) return true;

            var curStr = string.Join(".", curVersion);
            var hexStr = string.Join(".", hexVersion);
            var result = MessageBox.Show(
                string.Format(GetString("t_auto_version_mismatch"), curStr, hexStr),
                GetString("t_auto_version_mismatch_title"),
                MessageBoxButton.YesNo, MessageBoxImage.Warning);
            return result == MessageBoxResult.Yes;
        }

        // HCU (MBC-2, device type 125) firmware 125.0.0.5 - 125.0.0.15 has a CAN filter bug:
        // it wrongly accepts any frame addressed to ReceiverType 126 (Control device / remote
        // panel) as if it were its own, including the "enter bootloader" command. Flashing the
        // panel with such an MBC-2 on the bus makes both jump into the bootloader at once, and
        // the two bootloaders answering the flash protocol simultaneously bricks the flash.
        // Fixed properly in firmware (per-command target check) starting with 125.0.0.16.
        private const int VulnerableMbcDeviceType = 125;
        private const int VulnerableMbcMinBuild = 5;
        private const int VulnerableMbcMaxBuild = 15;

        // excludeDevice: the device actually being switched to bootloader is not itself a risk
        // (its own "enter bootloader" command is addressed to its own type, never to 126), and
        // must be excluded so that updating a vulnerable MBC-2 to fix it isn't blocked by itself.
        private bool VulnerableMbcOnBus(DeviceViewModel excludeDevice)
        {
            bool found = false;
            Application.Current.Dispatcher.Invoke(() =>
                found = Vm.OmniInstance.ConnectedDevices.Any(d =>
                    d != excludeDevice &&
                    d.Id.Type == VulnerableMbcDeviceType &&
                    d.Firmware[3] >= VulnerableMbcMinBuild &&
                    d.Firmware[3] <= VulnerableMbcMaxBuild));
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
