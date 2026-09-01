using CommunityToolkit.Mvvm.Input;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using System.Windows;
using static CAN_Tool.Libs.Helper;

namespace OmniProtocol
{
    // Gen1: старейший загрузчик (BootFirmware[3] <= Gen1MaxBuild). Протокол PGN100/101,
    // без подтверждения CRC при стирании (фиксированная пауза) и без проверки принятых данных.
    public partial class BootloaderDeviceViewModel
    {
        // Old bootloader flash command: PGN 100, Data[0]=3
        // (StartFlashing() из .Gen2.cs использует PGN 105, который старый загрузчик не понимает)
        private void StartFlashingOld()
        {
            OmniMessage msg = new();
            msg.Pgn = 100;
            msg.ReceiverId.Type = 123;
            msg.Data[0] = 3;
            Transmit(msg.ToCanMessage());
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
            // The address we pass is stored but ignored - bootloader always starts at its fixed constant.
            OmniMessage msg = new()
            {
                Pgn = 100,
                TransmitterId = new DeviceId(126, 6),
                ReceiverId = new DeviceId(123, 0)
            };
            msg.Data[0] = 2;
            Transmit(msg.ToCanMessage());
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
                Transmit(msg.ToCanMessage());
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
            Transmit(msg.ToCanMessage());
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
            if (!Bus.CurrentTask.Capture("Memory Erasing")) return;

            LogWriteLine(GetString("t_starting_flash_erase"));
            EraseFlashOld();
            Thread.Sleep(5000); // Old bootloader sends no erase confirmation; fixed wait

            Bus.CurrentTask.OnDone();
            Bus.CurrentTask.Capture("Programming...");

            int cnt = 0;
            foreach (var f in rawFragments)
            {
                flashFragmentOld(f);
                Bus.CurrentTask.UpdatePercent(cnt++ * 100 / rawFragments.Count);
                if (Bus.CurrentTask.Cts.IsCancellationRequested) return;
            }

            LogWriteLine(GetString("t_firmware_update_success"));
            Bus.CurrentTask.OnDone();
        }

        [RelayCommand]
        private void UpdateFirmwareOld()
        {
            Task.Run(() => UpdateFirmwareOld(fragments));
        }
    }
}
