using OmniProtocol;
using CAN_Tool.Infrastructure.Commands;
using CAN_Tool.ViewModels.Base;
using Microsoft.Win32;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Threading;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Input;
using System.Linq;

namespace CAN_Tool.ViewModels
{

    internal class CodeFragment
    {
        public CodeFragment(int len)
        {
            Data = new byte[len];
        }
        public uint StartAdress;
        public int Length;
        public byte[] Data;
    }

    internal class FirmwarePageViewModel : ViewModel
    {
        private int fragmentSize = 512;

        private string log;
        public string Log { get => log; private set => Set(ref log, value); }
        public int FragmentSize
        {
            get { return fragmentSize; }
            set { Set(ref fragmentSize, value); }
        }

        public MainWindowViewModel VM { set; get; }

        #region SwitchToBootloaderCommand
        public ICommand SwitchToBootloaderCommand { get; }

        private void LogWrite(string str)
        {
            //Debug.WriteLine(str);
            Log = (str) + Log;
        }

        private void LogWriteLine(string str)
        {
            //Debug.WriteLine(str);
            Log = (str + Environment.NewLine) + Log;
        }
        private void OnSwitchToBootloaderCommandExecuted(Object parameter)
        {

            OmniMessage msg = new();
            msg.PGN = 1;
            msg.ReceiverId = VM.SelectedConnectedDevice.ID;
            msg.Data[0] = 0;
            msg.Data[1] = 22;
            msg.Data[2] = 0;
            VM.CanAdapter.Transmit(msg.ToCanMessage());
        }

        #endregion

        #region SwitchToBootloaderCommand
        public ICommand RequestBootloaderVersionCommand { get; }

        private void OnRequestBootloaderVersioExecuted(Object parameter)
        {

            OmniMessage msg = new();
            msg.PGN = 6;
            msg.ReceiverType = 123;
            msg.Data[0] = 0;
            msg.Data[1] = 18;
            VM.CanAdapter.Transmit(msg.ToCanMessage());
        }

        #endregion

        #region SwitchToMainProgramCommand
        public ICommand SwitchToMainProgramCommand { get; }

        private void OnSwitchToMainProgramCommandExecuted(Object parameter)
        {

            OmniMessage msg = new();
            msg.PGN = 1;
            msg.ReceiverType = 123;
            msg.Data[0] = 0;
            msg.Data[1] = 22;
            msg.Data[2] = 1;
            VM.CanAdapter.Transmit(msg.ToCanMessage());
        }

        #endregion

        #region LoadHexCommand
        public ICommand LoadHexCommand { get; }

        private List<CodeFragment> fragments = new();
        private void OnLoadHexCommandExecuted(object parameter)
        {
            OpenFileDialog dialog = new OpenFileDialog();
            dialog.Filter = "Hex Files|*.hex";
            if (!(bool)dialog.ShowDialog()) return;
            fragments = parseHexFile(dialog.FileName, fragmentSize);
            LogWriteLine($"Hex is loaded, contains {fragments.Count} fragments.");
        }

        #endregion

        #region GetVersionCommand
        public ICommand GetVersionCommand { get; }

        private void OnGetVersionCommandExecuted(object parameter)
        {
            OmniMessage msg = new();
            msg.PGN = 6;
            msg.ReceiverId = VM.SelectedConnectedDevice.ID;
            msg.Data[0] = 0;
            msg.Data[1] = 18;
            VM.CanAdapter.Transmit(msg.ToCanMessage());
        }

        #endregion

        private void eraseFlash()
        {
            OmniMessage msg = new();
            msg.PGN = 105;
            msg.ReceiverType = 123;
            msg.Data[0] = 6;
            msg.Data[1] = 255;  //Стереть всю память
            VM.CanAdapter.Transmit(msg.ToCanMessage());
            VM.SelectedConnectedDevice.flagEraseDone = false;
        }

        private void startFlashing()
        {
            OmniMessage msg = new();
            msg.PGN = 105;
            msg.ReceiverType = 123;
            msg.Data[0] = 4;
            VM.CanAdapter.Transmit(msg.ToCanMessage());
        }

        public bool WaitForFlag(ref bool flag, int delay)
        {
            int wd = 0;
            while (!flag && wd < delay)
            {
                wd++;
                Thread.Sleep(1);
            }
            if (!flag)
            {
                flag = false;
                return false;
            }
            else
            {
                flag = false;
                return true;
            }
        }
        private void flashFragment(CodeFragment f)
        {
            writeFragmentToRam(f);
            for (int i = 0; i < 4; i++)
            {
                VM.SelectedConnectedDevice.flagProgramDone = false;
                if (i == 3)
                {
                    VM.OmniInstance.CurrentTask.onFail("Can't flash memory");
                    return;
                }
                startFlashing();
                if (WaitForFlag(ref VM.SelectedConnectedDevice.flagProgramDone, 100))
                    break;
            }
        }



        private bool checkTransmittedData(int len, uint crc)
        {
            OmniMessage msg = new()
            {
                PGN = 105,
                ReceiverType = 123
            };
            msg.Data[0] = 2;
            
            for (int i = 0; i < 6; i++)
            {
                VM.SelectedConnectedDevice.flagTransmissionCheck = false;
                if (i == 5)
                {
                    VM.OmniInstance.CurrentTask.onFail("Can't check transmission result");
                    return false;
                }
                VM.CanAdapter.Transmit(msg.ToCanMessage());
                WaitForFlag(ref VM.SelectedConnectedDevice.flagTransmissionCheck, 100);

                LogWriteLine($"Len:{VM.SelectedConnectedDevice.receivedFragmentLength}/{len},CRC:0x{VM.SelectedConnectedDevice.receivedFragmentCrc:X}/0X{crc:X}");
                if (crc == VM.SelectedConnectedDevice.receivedFragmentCrc && len == VM.SelectedConnectedDevice.receivedFragmentLength)
                    return true;
                else
                {
                    LogWriteLine("###Transmission failed!");
                    return false;
                }
            }
            return false;
        }

        private void setFragmentAdr(CodeFragment f)
        {

            OmniMessage msg = new()
            {
                PGN = 105,
                ReceiverType = 123
            };
            msg.Data[0] = 0;
            msg.Data[1] = (byte)(f.StartAdress >> 24);
            msg.Data[2] = (byte)(f.StartAdress >> 16);
            msg.Data[3] = (byte)(f.StartAdress >> 8);
            msg.Data[4] = (byte)(f.StartAdress >> 0);
            for (int i = 0; i < 4; i++)
            {
                VM.SelectedConnectedDevice.flagSetAdrDone = false;
                if (i == 3)
                {
                    VM.OmniInstance.CurrentTask.onFail("Can't set address");
                    return;
                }
                VM.CanAdapter.Transmit(msg.ToCanMessage());
                if (WaitForFlag(ref VM.SelectedConnectedDevice.flagSetAdrDone, 300))
                {
                    if (VM.SelectedConnectedDevice.fragmentAdress == f.StartAdress)
                        break;
                    else
                        continue;
                }

            }
        }
        private void writeFragmentToRam(CodeFragment f)
        {
            uint crc;
            int len;


            OmniMessage msg = new()
            {
                PGN = 106,
                ReceiverType = 123
            };

            LogWrite($"Starting {f.StartAdress:X} fragment tx...");
            for (int k = 0; k < 16; k++)
            {
                setFragmentAdr(f);

                if (VM.OmniInstance.CurrentTask.CTS.IsCancellationRequested)
                    return;

                if (k == 15) { VM.OmniInstance.CurrentTask.onFail("Can't transmit data pack"); return; }
                if (k>0)
                LogWriteLine($"Try: {k+1}");
                crc = 0;
                len = 0;
                VM.SelectedConnectedDevice.receivedFragmentCrc = 0;
                VM.SelectedConnectedDevice.receivedFragmentLength = 0;

                for (int i = 0; i < (f.Length + 7) / 8; i++)
                {
                    for (int j = 0; j < 8; j++)
                    {
                        msg.Data[j] = f.Data[i * 8 + j];
                        crc += f.Data[i * 8 + j] * 170771U;
                        crc = crc ^ ((crc >> 16) & 0xFFFFU);
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
                    VM.CanAdapter.Transmit(msg.ToCanMessage());

                }
                if (checkTransmittedData(len, crc)) break;
            }
        }

        private void updateFirmware(List<CodeFragment> fragments)
        {
            LogWriteLine("Starting Firmware updating procedure");
            VM.OmniInstance.CurrentTask.Capture("Memory Erasing");
            LogWriteLine("Starting flash erasing");
            for (int i = 0; i < 4; i++)
            {
                if (i == 3) { VM.OmniInstance.CurrentTask.onFail("Can't erase memory"); return; }
                eraseFlash();
                if (WaitForFlag(ref VM.SelectedConnectedDevice.flagEraseDone, 5000)) break;
            }

            VM.OmniInstance.CurrentTask.onDone();

            VM.OmniInstance.CurrentTask.Capture("Programming...");

            int cnt = 0;
            foreach (var f in fragments)
            {
                flashFragment(f);
                VM.OmniInstance.CurrentTask.PercentComplete = cnt++ * 100 / fragments.Count;
                if (VM.OmniInstance.CurrentTask.CTS.IsCancellationRequested) return;
            }
            LogWriteLine("Firmware updating success");
            VM.OmniInstance.CurrentTask.onDone();

        }
        #region oldVersionBootloader
        /*
        private void flashFragmentOld(CodeFragment f)
        {
            writeFragmentToRamOld(f);
            for (int i = 0; i < 4; i++)
            {
                if (i == 3) { VM.AC2PInstance.CurrentTask.onFail("Can't flash memory"); return; }
                startFlashing();
                if (WaitForFlag(ref VM.SelectedConnectedDevice.flagProgramDone, 20)) break;
            }
        }

        private void bootloaderSetAdrLen(uint adress, int len)
        {
            LogWriteLine($"Setting adress to 0X{adress:X}");
            AC2PMessage msg = new();
            msg.PGN = 100;
            msg.ReceiverType = 123;
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
                if (i == 5) { VM.AC2PInstance.CurrentTask.onFail("Can't set start adress"); return; }
                VM.CanAdapter.Transmit(msg);
                if (WaitForFlag(ref VM.SelectedConnectedDevice.flagSetAdrDone, 100)) break;
            }
        }
        private void initAnddressOld()
        {
            AC2PMessage msg = new()
            {
                PGN = 100,
                TransmitterAddress = 6,
                TransmitterType = 126,
                ReceiverAddress = 0,
                ReceiverType = 123
            };
            msg.Data[0] = 1;

            VM.CanAdapter.TransmitForSure(msg, 100);
        }

        private void writeFragmentToRamOld(CodeFragment f)
        {
            int len = 0;
            AC2PMessage msg = new()
            {
                PGN = 101,
                TransmitterAddress = 6,
                TransmitterType = 126,
                ReceiverAddress = 0,
                ReceiverType = 123
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
                VM.CanAdapter.TransmitForSure(msg, 10);

            }

        }
        private void updateFirmwareOld(List<CodeFragment> fragments)
        {
            LogWriteLine("Starting Firmware updating procedure");
            VM.AC2PInstance.CurrentTask.Capture("Memory Erasing");
            LogWriteLine("Starting flash erasing");
            for (int i = 0; i < 4; i++)
            {
                if (i == 3) { VM.AC2PInstance.CurrentTask.onFail("Can't erase memory"); return; }
                eraseFlash();
                if (WaitForFlag(ref VM.SelectedConnectedDevice.flagEraseDone, 5000)) break;
            }

            VM.AC2PInstance.CurrentTask.onDone();

            VM.AC2PInstance.CurrentTask.Capture("Programming...");

            int cnt = 0;

            initAnddressOld();

            foreach (var f in fragments)
            {
                flashFragmentOld(f);
                VM.AC2PInstance.CurrentTask.PercentComplete = cnt++ * 100 / fragments.Count;
                if (VM.AC2PInstance.CurrentTask.CTS.IsCancellationRequested) return;
            }
            LogWriteLine("Firmware updating success");
            VM.AC2PInstance.CurrentTask.onDone();

        }
        */
        #endregion

        public ICommand UpdateFirmwareCommand { get; }

        private List<CodeFragment> parseHexFile(string path, int maxFragment)
        {
            fragments.Clear();
            CodeFragment current = new(maxFragment);
            uint pageAdress = 0;
            uint localAdress = 0;

            if (path != null && path.Length > 0)
                using (StreamReader sr = new StreamReader(path))
                {
                    while (!sr.EndOfStream)
                    {
                        OmniMessage msg = new();
                        string line = sr.ReadLine().Substring(1);
                        byte[] bytes = new byte[25];
                        for (int i = 0; i < line.Length / 2; i++)
                            bytes[i] = Convert.ToByte(line.Substring(i * 2, 2), 16);
                        int recordLen = bytes[0];
                        switch (bytes[3])
                        {
                            case 0:
                                localAdress = (uint)(bytes[1] * 256 + bytes[2]);
                                if (current.StartAdress == 0) current.StartAdress = pageAdress + localAdress;
                                for (int i = 0; i < recordLen; i++)
                                {
                                    current.Data[current.Length++] = bytes[i + 4];
                                    if (current.Length == maxFragment)
                                    {
                                        fragments.Add(current);
                                        current = new(maxFragment);
                                    }
                                }
                                break;
                            case 4:
                                if (current.Length != 0)
                                {
                                    fragments.Add(current);
                                    current = new(maxFragment);
                                }
                                pageAdress = (uint)(bytes[4] * 256 + bytes[5]) << 16;
                                break;
                            case 1:
                                if (current.Length > 0)
                                    fragments.Add(current);
                                return fragments;

                        }
                    }
                    return fragments;
                }
            return null;
        }

        private void OnUpdateFirmwareCommandExecuted(object parameter)
        {
            Task.Run(() => updateFirmware(fragments));
        }

        private bool CanUpdateFirmwareCommandExecute(object parameter)
        {
            return (VM.SelectedConnectedDevice != null && VM.SelectedConnectedDevice.ID.Type == 123 && fragments.Count > 0);
        }

        public FirmwarePageViewModel(MainWindowViewModel vm)
        {
            VM = vm;

            UpdateFirmwareCommand = new LambdaCommand(OnUpdateFirmwareCommandExecuted, CanUpdateFirmwareCommandExecute);
            LoadHexCommand = new LambdaCommand(OnLoadHexCommandExecuted, p => true);
            SwitchToBootloaderCommand = new LambdaCommand(OnSwitchToBootloaderCommandExecuted, VM.deviceSelected);
            SwitchToMainProgramCommand = new LambdaCommand(OnSwitchToMainProgramCommandExecuted, VM.deviceSelected);
            RequestBootloaderVersionCommand = new LambdaCommand(OnRequestBootloaderVersioExecuted, VM.portOpened);
            GetVersionCommand = new LambdaCommand(OnGetVersionCommandExecuted, VM.deviceSelected);
        }
    }
}
