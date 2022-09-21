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

namespace CAN_Tool.ViewModels
{
    internal partial class MainWindowViewModel:ViewModel
    {

        public ICommand SwitchToBootloaderCommand { get; }

        private void OnSwitchToBootloaderCommandExecuted(Object parameter)
        {

            AC2PMessage msg = new();
            msg.PGN = 1;
            msg.TransmitterAddress = 6;
            msg.TransmitterType = 126;
            msg.ReceiverId = SelectedConnectedDevice.ID;
            msg.Data[0] = 0;
            msg.Data[1] = 22;
            msg.Data[3] = 0;
            canAdapter.Transmit(msg);
        }


        public ICommand LoadHexCommand { get; }

        private List<CodeFragment> fragments;
        private void OnLoadHexCommandExecuted(object parameter)
        {
            OpenFileDialog dialog = new OpenFileDialog();
            dialog.Filter = "Hex Files|*.hex";
            dialog.ShowDialog();
            fragments = FirmwareUpdateTaskConstructor(dialog.FileName, 128);
            MessageBox.Show($"Hex is loaded, contains {fragments.Count} fragments.");
        }

        private bool CanLoadHexCommandExecute(object parameter)
        {
            return true;
        }

        private void booltloaderEraseFlash()
        {
            AC2PMessage msg = new();
            msg.PGN = 100;
            msg.TransmitterAddress = 6;
            msg.TransmitterType = 126;
            msg.ReceiverAddress = 0;
            msg.ReceiverType = 123;
            msg.Data[0] = 1;
            canAdapter.Transmit(msg);
        }

        private void booltloaderStartFlashProgram()
        {
            AC2PMessage msg = new();
            msg.PGN = 100;
            msg.TransmitterAddress = 6;
            msg.TransmitterType = 126;
            msg.ReceiverAddress = 0;
            msg.ReceiverType = 123;
            msg.Data[0] = 3;
            canAdapter.Transmit(msg);
        }

        private void bootloaderWrite()
        {
            foreach (CodeFragment f in fragments)
            {
                bootloaderSetAdrLen(f.StartAdress, f.Length);
                Thread.Sleep(50);
                bootloaderWriteDataToRam(f);
                SelectedConnectedDevice.ProgramDone = false;
                booltloaderStartFlashProgram();
                while (!SelectedConnectedDevice.ProgramDone) ;
            }

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

        }

        private void bootloaderWriteDataToRam(CodeFragment f)
        {
            AC2PMessage msg = new();
            msg.PGN = 101;
            msg.TransmitterAddress = 6;
            msg.TransmitterType = 126;
            msg.ReceiverId = SelectedConnectedDevice.ID;
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
                canAdapter.Transmit(msg);
            }

        }
        public ICommand UpdateFirmwareCommand { get; }

        private class CodeFragment
        {
            public CodeFragment(int len)
            {
                Data = new byte[len];
            }
            public uint StartAdress;
            public int Length;
            public byte[] Data;
        }
        private List<CodeFragment> FirmwareUpdateTaskConstructor(string path, int maxFragment)
        {
            List<CodeFragment> fragments = new List<CodeFragment>();
            CodeFragment current = new(maxFragment);
            uint pageAdress = 0;
            uint localAdress = 0;


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
        }
        private async void UpdateFirmware(List<CodeFragment> fragments)
        {

        }
        private void OnUpdateFirmwareCommandExecuted(object parameter)
        {


        }
    }
}
