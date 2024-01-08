using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using Microsoft.Win32;
using OmniProtocol;
using System.IO;
using System.Windows.Controls;
using System.Collections.Generic;
using System;
using System.Threading;
using System.Threading.Tasks;
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

        private List<CodeFragment> fragments = new();

        [ObservableProperty]
        private int fragmentSize = 512;

        [ObservableProperty]
        private string log;

        public MainWindowViewModel Vm { set; get; }

        private void LogWrite(string str)
        {
            Log += str;
        }

        private void LogWriteLine(string str)
        {
            Log += str + Environment.NewLine;
        }

        [RelayCommand]
        private void SwitchToBootLoader()
        {
            OmniMessage msg = new();
            msg.Pgn = 1;
            msg.ReceiverId.Address = Vm.SelectedConnectedDevice.Id.Address;
            msg.ReceiverId.Type = Vm.SelectedConnectedDevice.Id.Type;
            msg.Data[0] = 0;
            msg.Data[1] = 22;
            msg.Data[2] = 0;
            Vm.CanAdapter.Transmit(msg.ToCanMessage());
        }




        [RelayCommand]
        private void RequestBootLoaderVersion()
        {
            OmniMessage msg = new();
            msg.Pgn = 6;
            msg.ReceiverId.Type = 123;
            msg.Data[0] = 0;
            msg.Data[1] = 18;
            Vm.CanAdapter.Transmit(msg.ToCanMessage());
        }

        [RelayCommand]
        private void SwitchToMainProgram()
        {
            OmniMessage msg = new();
            msg.Pgn = 1;
            msg.ReceiverId.Type = 123;
            msg.Data[0] = 0;
            msg.Data[1] = 22;
            msg.Data[2] = 1;
            Vm.CanAdapter.Transmit(msg.ToCanMessage());
        }

        [RelayCommand]
        private void LoadHex()
        {
            OpenFileDialog dialog = new();
            dialog.Filter = "Hex Files|*.hex";
            if (!(bool)dialog.ShowDialog()) return;
            fragments = ParseHexFile(dialog.FileName, FragmentSize);
            LogWriteLine($"Hex is loaded, contains {fragments.Count} fragments.");
        }

        [RelayCommand]
        private void GetVersion()
        {
            OmniMessage msg = new();
            msg.Pgn = 6;
            msg.ReceiverId.Address = Vm.SelectedConnectedDevice.Id.Address;
            msg.ReceiverId.Type = Vm.SelectedConnectedDevice.Id.Type;
            msg.Data[0] = 0;
            msg.Data[1] = 18;
            Vm.CanAdapter.Transmit(msg.ToCanMessage());
        }

        private void EraseFlash()
        {
            OmniMessage msg = new();
            msg.Pgn = 105;
            msg.ReceiverId.Type = 123;
            msg.Data[0] = 6;
            msg.Data[1] = 255;  //Стереть всю память
            Vm.CanAdapter.Transmit(msg.ToCanMessage());
            Vm.SelectedConnectedDevice.flagEraseDone = false;
        }

        private void StartFlashing()
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
            if (!flag)
                return false;

            flag = false;
            return true;
        }
        private void FlashFragment(CodeFragment f)
        {
            WriteFragmentToRam(f);
            for (var i = 0; i < 4; i++)
            {
                Vm.SelectedConnectedDevice.flagProgramDone = false;
                if (i == 3)
                {
                    Vm.OmniInstance.CurrentTask.OnFail("Can't flash memory");
                    return;
                }
                StartFlashing();
                if (WaitForFlag(ref Vm.SelectedConnectedDevice.flagProgramDone, 100))
                    break;
            }
        }

        private bool CheckTransmittedData(int len, uint crc)
        {
            OmniMessage msg = new()
            {
                Pgn = 105,
                ReceiverId = new(123,7),
                Data =
                {
                    [0] = 2
                }
            };


            for (var i = 0; i < 6; i++)
            {
                i++;
                Vm.SelectedConnectedDevice.flagTransmissionCheck = false;
                if (i == 5)
                {
                    Vm.OmniInstance.CurrentTask.OnFail("Can't check transmission result");
                    return false;
                }
                Vm.CanAdapter.Transmit(msg.ToCanMessage());
                WaitForFlag(ref Vm.SelectedConnectedDevice.flagTransmissionCheck, 100);

                LogWriteLine($"Len:{Vm.SelectedConnectedDevice.receivedFragmentLength},CRC:0x{Vm.SelectedConnectedDevice.receivedFragmentCrc:X08}");
                if (crc == Vm.SelectedConnectedDevice.receivedFragmentCrc && len == Vm.SelectedConnectedDevice.receivedFragmentLength)
                    return true;
                else
                    LogWriteLine("###Transmission failed!");

            }
            return false;
        }

        private void SetFragmentAdr(CodeFragment f)
        {

            OmniMessage msg = new()
            {
                Pgn = 105,
                ReceiverId = new(123, 7),
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
                Vm.SelectedConnectedDevice.flagSetAdrDone = false;
                if (i == 3)
                {
                    Vm.OmniInstance.CurrentTask.OnFail("Can't set address");
                    return;
                }
                Vm.CanAdapter.Transmit(msg.ToCanMessage());
                if (!WaitForFlag(ref Vm.SelectedConnectedDevice.flagSetAdrDone, 300)) continue;
                if (Vm.SelectedConnectedDevice.fragmentAddress == f.StartAddress)
                    break;
            }
        }
        private void WriteFragmentToRam(CodeFragment f)
        {
            OmniMessage msg = new()
            {
                Pgn = 106,
                ReceiverId = new(123, 7),
            };

            LogWrite($"Fragment {f.StartAddress:X08}...");
            for (var k = 0; k < 16; k++)
            {
                SetFragmentAdr(f);

                if (Vm.OmniInstance.CurrentTask.Cts.IsCancellationRequested)
                    return;

                if (k == 15) { Vm.OmniInstance.CurrentTask.OnFail("Can't transmit data pack"); return; }
                if (k > 0)
                    LogWriteLine($"Try: {k + 1}");
                uint crc = 0;
                var len = 0;
                Vm.SelectedConnectedDevice.receivedFragmentCrc = 0;
                Vm.SelectedConnectedDevice.receivedFragmentLength = 0;

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

        private void UpdateFirmware(List<CodeFragment> fragmentsArg)
        {
            LogWriteLine("Starting Firmware updating procedure");
            Vm.OmniInstance.CurrentTask.Capture("Memory Erasing");
            LogWriteLine("Starting flash erasing");
            for (var i = 0; i < 4; i++)
            {
                if (i == 3) { Vm.OmniInstance.CurrentTask.OnFail("Can't erase memory"); return; }
                EraseFlash();
                if (WaitForFlag(ref Vm.SelectedConnectedDevice.flagEraseDone, 5000)) break;
            }

            Vm.OmniInstance.CurrentTask.OnDone();

            Vm.OmniInstance.CurrentTask.Capture("Programming...");

            var cnt = 0;
            foreach (var f in fragmentsArg)
            {
                FlashFragment(f);
                Vm.OmniInstance.CurrentTask.PercentComplete = cnt++ * 100 / fragmentsArg.Count;
                if (Vm.OmniInstance.CurrentTask.Cts.IsCancellationRequested) return;
            }
            LogWriteLine("Firmware updating success");
            Vm.OmniInstance.CurrentTask.OnDone();

        }
        #region oldVersionBootloader
        /*
        private void flashFragmentOld(CodeFragment f)
        {
            writeFragmentToRamOld(f);
            for (int i = 0; i < 4; i++)
            {
                if (i == 3) { Vm.AC2PInstance.CurrentTask.onFail("Can't flash memory"); return; }
                startFlashing();
                if (WaitForFlag(ref Vm.SelectedConnectedDevice.flagProgramDone, 20)) break;
            }
        }

        private void bootloaderSetAdrLen(uint adress, int len)
        {
            LogWriteLine($"Setting adress to 0X{adress:X}");
            AC2PMessage msg = new();
            msg.Pgn = 100;
            msg.ReceiverId.Type = 123;
            msg.Data[0] = 5;
            msg.Data[1] = 0xFF;
            msg.Data[2] = (byte)((adress >> 24) & 0xFF);
            msg.Data[3] = (byte)((adress >> 16) & 0xFF);
            msg.Data[4] = (byte)((adress >> 8) & 0xFF);
            msg.Data[5] = (byte)((adress >> 0) & 0xFF);
            msg.Data[6] = 0xff;
            msg.Data[7] = 0xff;

            for (int i = 0; i < 6; i++)
            {
                if (i == 5) { Vm.AC2PInstance.CurrentTask.onFail("Can't set start adress"); return; }
                Vm.CanAdapter.Transmit(msg);
                if (WaitForFlag(ref Vm.SelectedConnectedDevice.flagSetAdrDone, 100)) break;
            }
        }
        private void initAnddressOld()
        {
            AC2PMessage msg = new()
            {
                Pgn = 100,
                TransmitterId.Address = 6,
                TransmitterId.Type = 126,
                ReceiverId.Address = 0,
                ReceiverId.Type = 123
            };
            msg.Data[0] = 1;

            Vm.CanAdapter.TransmitForSure(msg, 100);
        }

        private void writeFragmentToRamOld(CodeFragment f)
        {
            int len = 0;
            AC2PMessage msg = new()
            {
                Pgn = 101,
                TransmitterId.Address = 6,
                TransmitterId.Type = 126,
                ReceiverId.Address = 0,
                ReceiverId.Type = 123
            };

            LogWriteLine($"Starting {f.StartAdress:X} fragment transmission");
            for (int i = 0; i < (f.Length + 7) / 8; i++)
            {
                for (int j = 0; j < 8; j++)
                {
                    msg.Data[j] = f.Data[i * 8 + j];
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
                Vm.CanAdapter.TransmitForSure(msg, 10);

            }

        }
        private void updateFirmwareOld(List<CodeFragment> fragments)
        {
            LogWriteLine("Starting Firmware updating procedure");
            Vm.AC2PInstance.CurrentTask.Capture("Memory Erasing");
            LogWriteLine("Starting flash erasing");
            for (int i = 0; i < 4; i++)
            {
                if (i == 3) { Vm.AC2PInstance.CurrentTask.onFail("Can't erase memory"); return; }
                eraseFlash();
                if (WaitForFlag(ref Vm.SelectedConnectedDevice.flagEraseDone, 5000)) break;
            }

            Vm.AC2PInstance.CurrentTask.onDone();

            Vm.AC2PInstance.CurrentTask.Capture("Programming...");

            int cnt = 0;

            initAnddressOld();

            foreach (var f in fragments)
            {
                flashFragmentOld(f);
                Vm.AC2PInstance.CurrentTask.PercentComplete = cnt++ * 100 / fragments.Count;
                if (Vm.AC2PInstance.CurrentTask.CTS.IsCancellationRequested) return;
            }
            LogWriteLine("Firmware updating success");
            Vm.AC2PInstance.CurrentTask.onDone();

        }
        */
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

        public FirmwarePageViewModel(MainWindowViewModel vm)
        {
            Vm = vm;
        }
    }
}
