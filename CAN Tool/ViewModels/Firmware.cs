using AdversCan;
using CAN_Tool.ViewModels.Base;
using Microsoft.Win32;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using System.Windows.Input;
using System.Windows;
using CAN_Tool.Infrastructure.Commands;
using System.Diagnostics;

namespace CAN_Tool.ViewModels
{

    class CodeFragment
    {
        public CodeFragment(int len)
        {
            Data = new byte[len];
        }
        public uint StartAdress;
        public int Length;
        public byte[] Data;
    }
    internal class FirmwarePage : ViewModel
    {
        private int fragmentSize = 512;

        public int FragmentSize
        {
            get { return fragmentSize; }
            set { Set(ref fragmentSize, value); }
        }

        public MainWindowViewModel VM { set; get; }

        #region SwitchToBootloaderCommand
        public ICommand SwitchToBootloaderCommand { get; }

        private void OnSwitchToBootloaderCommandExecuted(Object parameter)
        {

            AC2PMessage msg = new();
            msg.PGN = 1;
            msg.TransmitterAddress = 6;
            msg.TransmitterType = 126;
            msg.ReceiverId = VM.SelectedConnectedDevice.ID;
            msg.Data[0] = 0;
            msg.Data[1] = 22;
            msg.Data[2] = 0;
            VM.canAdapter.Transmit(msg);
        }

        #endregion

        #region SwitchToBootloaderCommand
        public ICommand RequestBootloaderVersionCommand { get; }

        private void OnRequestBootloaderVersioExecuted(Object parameter)
        {

            AC2PMessage msg = new();
            msg.PGN = 6;
            msg.TransmitterAddress = 6;
            msg.TransmitterType = 126;
            msg.ReceiverAddress = 0;
            msg.ReceiverType = 123;
            msg.Data[0] = 0;
            msg.Data[1] = 18;
            VM.canAdapter.Transmit(msg);
        }

        #endregion

        #region SwitchToMainProgramCommand
        public ICommand SwitchToMainProgramCommand { get; }

        private void OnSwitchToMainProgramCommandExecuted(Object parameter)
        {

            AC2PMessage msg = new();
            msg.PGN = 1;
            msg.TransmitterAddress = 6;
            msg.TransmitterType = 126;
            msg.ReceiverType = 123;
            msg.ReceiverAddress = 0;
            msg.Data[0] = 0;
            msg.Data[1] = 22;
            msg.Data[2] = 1;
            VM.canAdapter.Transmit(msg);
        }

        #endregion

        #region LoadHexCommand
        public ICommand LoadHexCommand { get; }

        private List<CodeFragment> fragments = new();
        private void OnLoadHexCommandExecuted(object parameter)
        {
            OpenFileDialog dialog = new OpenFileDialog();
            dialog.Filter = "Hex Files|*.hex";
            dialog.ShowDialog();
            fragments = parseHexFile(dialog.FileName, fragmentSize);
            MessageBox.Show($"Hex is loaded, contains {fragments.Count} fragments.");
        }

        #endregion

        #region GetVersionCommand
        public ICommand GetVersionCommand { get; }

        private void OnGetVersionCommandExecuted(object parameter)
        {
            AC2PMessage msg = new();
            msg.PGN = 6;
            msg.TransmitterAddress = 6;
            msg.TransmitterType = 126;
            msg.ReceiverId = VM.SelectedConnectedDevice.ID;
            msg.Data[0] = 0;
            msg.Data[1] = 18;
            VM.canAdapter.Transmit(msg);
        }

        #endregion

        private void eraseFlash()
        {
            AC2PMessage msg = new();
            msg.PGN = 100;
            msg.TransmitterAddress = 6;
            msg.TransmitterType = 126;
            msg.ReceiverAddress = 0;
            msg.ReceiverType = 123;
            msg.Data[0] = 1;
            VM.canAdapter.Transmit(msg);
            VM.SelectedConnectedDevice.EraseDone = false;
        }

        private void startFlashing()
        {
            AC2PMessage msg = new();
            msg.PGN = 100;
            msg.TransmitterAddress = 6;
            msg.TransmitterType = 126;
            msg.ReceiverAddress = 0;
            msg.ReceiverType = 123;
            msg.Data[0] = 3;
            VM.canAdapter.Transmit(msg);
        }

        private bool WaitForFlag(ref bool flag, int delay)
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
                if (i == 3) { VM.AC2PInstance.CurrentTask.onFail("Can't flash memory"); return; }
                startFlashing();
                if (WaitForFlag(ref VM.SelectedConnectedDevice.programDone, 20)) break;
            }
        }
        private void bootloaderSetAdrLen(uint adress, int len)
        {
            Debug.WriteLine($"Setting adress to 0X{adress:X}");
            AC2PMessage msg = new();
            msg.PGN = 100;
            msg.TransmitterAddress = 6;
            msg.TransmitterType = 126;
            msg.ReceiverAddress = 0;
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
                VM.canAdapter.Transmit(msg);
                if (WaitForFlag(ref VM.SelectedConnectedDevice.setAdrDone, 100)) break;
            }
        }

        private bool checkTransmittedData(int len, int crc)
        {
            AC2PMessage msg = new();
            msg.PGN = 100;
            msg.TransmitterAddress = 6;
            msg.TransmitterType = 126;
            msg.ReceiverId = VM.SelectedConnectedDevice.ID;
            msg.Data[0] = 4;
            Debug.WriteLine("Waiting for check info");
            for (int i = 0; i < 6; i++)
            {
                if (i == 5)
                {
                    VM.AC2PInstance.CurrentTask.onFail("Can't check transmission result");
                    return false;
                }
                VM.canAdapter.Transmit(msg);
                if (WaitForFlag(ref VM.SelectedConnectedDevice.checkDone, 100)) break;
            }
            Debug.WriteLine($"Len:{VM.SelectedConnectedDevice.DataLength}/{len},CRC:0x{VM.SelectedConnectedDevice.crc:X}/0X{crc:X}");
            if (crc == VM.SelectedConnectedDevice.crc && len == VM.SelectedConnectedDevice.dataLength)
                return true;
            else
            {
                Debug.WriteLine("###Transmission fail!");
                return false;
            }
        }
        private void writeFragmentToRam(CodeFragment f)
        {
            int crc;
            int len;
            AC2PMessage msg = new();
            msg.PGN = 101;
            msg.TransmitterAddress = 6;
            msg.TransmitterType = 126;
            msg.ReceiverAddress = 0;
            msg.ReceiverType = 123;

            Debug.WriteLine($"Starting {f.StartAdress:X} fragment transmission");
            for (int k = 0; k < 16; k++)
            {
                if (k == 15) { VM.AC2PInstance.CurrentTask.onFail("Can't transmit data pack"); return; }
                Debug.WriteLine($"Try: {k}");
                bootloaderSetAdrLen(f.StartAdress, f.Length);
                crc = 0;
                len = 0;
                VM.SelectedConnectedDevice.Crc = 0;
                VM.SelectedConnectedDevice.DataLength = 0;

                for (int i = 0; i < (f.Length + 7) / 8; i++)
                {
                    for (int j = 0; j < 8; j++)
                    {
                        msg.Data[j] = f.Data[i * 8 + j];
                        crc += f.Data[i * 8 + j] * 170771;
                        crc = crc ^ (crc >> 16);
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
                    VM.canAdapter.Transmit(msg);

                }
                if (checkTransmittedData(len, crc)) break;
            }
        }
        private void updateFirmware(List<CodeFragment> fragments)
        {
            Debug.WriteLine("Starting Firmware updating procedure");
            VM.AC2PInstance.CurrentTask.Capture("Memory Erasing");
            Debug.WriteLine("Starting flash erasing");
            for (int i = 0; i < 4; i++)
            {
                if (i == 3) { VM.AC2PInstance.CurrentTask.onFail("Can't erase memory"); return; }
                eraseFlash();
                if (WaitForFlag(ref VM.SelectedConnectedDevice.eraseDone, 5000)) break;
            }

            VM.AC2PInstance.CurrentTask.onDone();

            VM.AC2PInstance.CurrentTask.Capture("Programming...");

            int cnt = 0;
            foreach (var f in fragments)
            {
                flashFragment(f);
                VM.AC2PInstance.CurrentTask.PercentComplete = cnt++ * 100 / fragments.Count;
                if (VM.AC2PInstance.CurrentTask.CTS.IsCancellationRequested) return;
            }
            Debug.WriteLine("Firmware updating success");
            VM.AC2PInstance.CurrentTask.onDone();

        }
        public ICommand UpdateFirmwareCommand { get; }


        private List<CodeFragment> parseHexFile(string path, int maxFragment)
        {
            CodeFragment current = new(maxFragment);
            uint pageAdress = 0;
            uint localAdress = 0;

            if (path != null)
                using (StreamReader sr = new StreamReader(path))
                {
                    while (!sr.EndOfStream)
                    {
                        AC2PMessage msg = new();
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

        public FirmwarePage(MainWindowViewModel vm)
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
