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
            fragments = parseHexFile(dialog.FileName, 128);
            MessageBox.Show($"Hex is loaded, contains {fragments.Count} fragments.");
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

        private void WaitFor(ref bool flag, int delay)
        {
            DateTime startTime = DateTime.Now;
            while (!flag && (DateTime.Now - startTime) < new TimeSpan(0, 0, delay)) ;
            if (!flag) throw new Exception("Device is not answering");
            flag = false;
            return;
        }
        private void flashFragment(CodeFragment f)
        {
            writeFragmentToRam(f);
            startFlashing();
            Thread.Sleep(100);
            //WaitFor(ref VM.SelectedConnectedDevice.programDone, 2);
        }
        private void bootloaderSetAdrLen(uint adress, int len)
        {
            AC2PMessage msg = new();
            msg.PGN = 100;
            msg.TransmitterAddress = 6;
            msg.TransmitterType = 126;
            msg.ReceiverAddress = 0;
            msg.ReceiverType = 123;
            msg.Data[0] = 2;
            msg.Data[1] = (byte)((adress >> 0) & 0xFF);
            msg.Data[2] = (byte)((adress >> 8) & 0xFF);
            msg.Data[3] = (byte)((adress >> 16) & 0xFF);
            msg.Data[4] = (byte)((len >> 0) & 0xFF);
            msg.Data[5] = (byte)((len >> 8) & 0xFF);
            msg.Data[6] = (byte)((len >> 16) & 0xFF);
            msg.Data[7] = (byte)((len >> 24) & 0xFF);
            VM.canAdapter.Transmit(msg);

        }
        private void writeFragmentToRam(CodeFragment f)
        {
            AC2PMessage msg = new();
            msg.PGN = 101;
            msg.TransmitterAddress = 6;
            msg.TransmitterType = 126;
            msg.ReceiverId = VM.SelectedConnectedDevice.ID;
            for (int i = 0; i < (f.Length + 7) / 8; i++)
            {
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

        }
        private void updateFirmware(List<CodeFragment> fragments)
        {
            VM.AC2PInstance.CurrentTask.Capture("Memory Erasing");
            eraseFlash();
            WaitFor(ref VM.SelectedConnectedDevice.eraseDone, 10);
            VM.AC2PInstance.CurrentTask.onDone();
            VM.AC2PInstance.CurrentTask.Capture("Programming...");
            bootloaderSetAdrLen(0x8008000, 128);
            WaitFor(ref VM.SelectedConnectedDevice.setAdrDone, 2);
            int cnt = 0;
            foreach (var f in fragments)
            {
                flashFragment(f);
                VM.AC2PInstance.CurrentTask.PercentComplete = cnt++ * 100 / fragments.Count;
                if (VM.AC2PInstance.CurrentTask.CTS.IsCancellationRequested) return;

            }
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
        }
    }
}
