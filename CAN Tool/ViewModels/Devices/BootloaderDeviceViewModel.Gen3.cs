using CommunityToolkit.Mvvm.Input;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Threading.Tasks;
using System.Windows;
using static CAN_Tool.Libs.Helper;

namespace OmniProtocol
{
    // Gen3: BootFirmware[3] >= Gen3MinBootBuild. Протокол PGN110/111 - magic words для защиты
    // erase/write от случайной активации и настоящий CRC32/ISO-HDLC вместо старой слабой
    // контрольной суммы.
    public partial class BootloaderDeviceViewModel
    {
        // Загрузчики с этой сборки (BootFirmware[3], VER_ASSEMBLAGE_NUMBER) и новее понимают
        // протокол 3-го поколения (PGN110/111). Используется в Generation для выбора протокола.
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

        // CAN Tool sends data padded to 8-byte CAN frame boundaries; the bootloader
        // accumulates exactly that many bytes, so CRC and length for verification must
        // use the same padded size, not f.Length.
        private static int VerifyLen(CodeFragment f) => (f.Length + 7) / 8 * 8;

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
            Transmit(msg.ToCanMessage());
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
            Transmit(msg.ToCanMessage());
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
                    Bus.CurrentTask.OnFail(GetString("t_cant_check_transmission"));
                    return false;
                }
                Transmit(msg.ToCanMessage());
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
                    Bus.CurrentTask.OnFail(GetString("t_cant_set_address"));
                    return;
                }
                Transmit(msg.ToCanMessage());
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

                if (Bus.CurrentTask.Cts.IsCancellationRequested)
                    return;

                if (k == 15)
                {
                    Bus.CurrentTask.OnFail(GetString("t_cant_transmit_data"));
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
                // байт данным (см. VerifyLen/CalcFragmentCrc - тот же приём для новой КС).
                var len = VerifyLen(f);
                for (var i = 0; i < len / 8; i++)
                {
                    for (var j = 0; j < 8; j++)
                        msg.Data[j] = f.Data[i * 8 + j];
                    Transmit(msg.ToCanMessage());
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
                    Bus.CurrentTask.OnFail(GetString("t_cant_flash_memory"));
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
                if (!Bus.CurrentTask.Capture("Memory Erasing")) return;
                LogWriteLine(GetString("t_starting_flash_erase"));
                for (var i = 0; i < 4; i++)
                {
                    if (i == 3)
                    {
                        Bus.CurrentTask.OnFail(GetString("t_cant_erase_memory"));
                        return;
                    }

                    await EraseFlash110();
                    if (WaitForFlag(ref flagEraseDone, 5000)) break;
                }

                Bus.CurrentTask.OnDone();

                Bus.CurrentTask.Capture("Programming");

                var cnt = 0;
                foreach (var f in fragmentsArg)
                {
                    FlashFragment110(f);
                    Bus.CurrentTask.PercentComplete = cnt++ * 100 / fragmentsArg.Count;
                    if (Bus.CurrentTask.Cts.IsCancellationRequested) return;
                }
                LogWriteLine(GetString("t_firmware_update_success"));
                Bus.CurrentTask.OnDone();
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
            if (BootFirmware[0] != 123 || BootFirmware[3] < Gen3MinBootBuild)
            {
                MessageBox.Show($"Gen.3 protocol requires bootloader (type 123) version 123.0.0.{Gen3MinBootBuild} or newer.",
                                "Unsupported bootloader", MessageBoxButton.OK, MessageBoxImage.Warning);
                return;
            }
            Task.Run(() => UpdateFirmware110(fragments));
        }

        // Returns true=match, false=CRC mismatch, null=no response (abort)
        // Проверка по CRC доступна только с загрузчика build 13+ (см. VerifyFirmwareCommand),
        // а с этой версии загрузчик уже понимает PGN110 - поэтому используем его вместе с
        // настоящим CRC32 (Crc32() выше) вместо старого слабого алгоритма PGN105/10.
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
                Transmit(msg.ToCanMessage());
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

            if (!Bus.CurrentTask.Capture("Verifying")) return false;
            LogWriteLine("=== CRC verification ===");

            var cnt = 0;
            var mismatchCount = 0;
            foreach (var f in fragmentsArg)
            {
                var result = VerifyFragment(f);
                if (result == null)
                {
                    Bus.CurrentTask.OnFail("Verification aborted: no response");
                    return false;
                }
                if (result == false)
                    mismatchCount++;

                Bus.CurrentTask.PercentComplete = ++cnt * 100 / fragmentsArg.Count;
                if (Bus.CurrentTask.Cts.IsCancellationRequested)
                {
                    Bus.CurrentTask.OnFail("Cancelled");
                    return false;
                }
            }

            if (mismatchCount == 0)
                LogWriteLine($"=== CRC verification passed: {fragmentsArg.Count} fragment(s) ===");
            else
                LogWriteLine($"=== CRC verification done: {fragmentsArg.Count} fragment(s), {mismatchCount} mismatch(es) ===");

            Bus.CurrentTask.OnDone();
            return mismatchCount == 0;
        }

        [RelayCommand]
        private void VerifyFirmware()
        {
            if (fragments.Count == 0)
            {
                MessageBox.Show(GetString("t_load_hex_first"));
                return;
            }
            if (BootFirmware[0] != 123 || BootFirmware[3] < Gen3MinBootBuild)
            {
                MessageBox.Show($"CRC verification (PGN110/CRC32) requires bootloader version 123.0.0.{Gen3MinBootBuild} or newer.",
                                "Unsupported bootloader", MessageBoxButton.OK, MessageBoxImage.Warning);
                return;
            }
            Task.Run(() => VerifyFlash(fragments));
        }
    }
}
