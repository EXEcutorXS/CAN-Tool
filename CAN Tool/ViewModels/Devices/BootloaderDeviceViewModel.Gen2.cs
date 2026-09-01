using CommunityToolkit.Mvvm.Input;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Threading;
using System.Threading.Tasks;
using System.Windows;
using static CAN_Tool.Libs.Helper;

namespace OmniProtocol
{
    // Gen2: "всё, что не попало в Gen1/Gen3" (BootFirmware[3] между Gen1MaxBuild и
    // Gen3MinBootBuild). Протокол PGN105/106 - слабая контрольная сумма вместо настоящего CRC32.
    public partial class BootloaderDeviceViewModel
    {
        private async Task EraseFlash()
        {
            OmniMessage msg = new();
            msg.Pgn = 105;
            msg.ReceiverId.Type = 123;
            msg.Data[0] = 6;
            msg.Data[1] = 255;  //Стереть всю память
            Debug.WriteLine("Отправляем запрос на стирание");
            Transmit(msg.ToCanMessage());
            flagEraseDone = false;
        }

        private async Task StartFlashing()
        {
            OmniMessage msg = new();
            msg.Pgn = 105;
            msg.ReceiverId.Type = 123;
            msg.Data[0] = 4;
            Transmit(msg.ToCanMessage());
        }

        private async Task FlashFragment(CodeFragment f)
        {
            WriteFragmentToRam(f);
            for (var i = 0; i < 4; i++)
            {
                flagProgramDone = false;
                if (i == 3)
                {
                    Bus.CurrentTask.OnFail(GetString("t_cant_flash_memory"));
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
                    Bus.CurrentTask.OnFail(GetString("t_cant_check_transmission"));
                    return false;
                }
                Transmit(msg.ToCanMessage());
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
                    Bus.CurrentTask.OnFail(GetString("t_cant_set_address"));
                    return;
                }
                Transmit(msg.ToCanMessage());
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
                    Transmit(msg.ToCanMessage());
                }
                if (CheckTransmittedData(len, crc)) break;
            }
        }

        private async void UpdateFirmware(List<CodeFragment> fragmentsArg)
        {
            try
            {
                // Block new protocol on old bootloader (Gen1): BootFirmware = {123, x, x, <=4}
                if (BootFirmware[0] == 123 && BootFirmware[3] <= Gen1MaxBuild)
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
                if (!Bus.CurrentTask.Capture("Memory Erasing")) return;
                LogWriteLine(GetString("t_starting_flash_erase"));
                for (var i = 0; i < 4; i++)
                {
                    if (i == 3)
                    {
                        Bus.CurrentTask.OnFail(GetString("t_cant_erase_memory"));
                        return;
                    }

                    await EraseFlash();
                    if (WaitForFlag(ref flagEraseDone, 5000)) break;
                }

                Bus.CurrentTask.OnDone();

                Bus.CurrentTask.Capture("Programming");

                var cnt = 0;
                foreach (var f in fragmentsArg)
                {
                    FlashFragment(f);
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
        private void UpdateFirmware()
        {
            Task.Run(() => UpdateFirmware(fragments));
        }
    }
}
