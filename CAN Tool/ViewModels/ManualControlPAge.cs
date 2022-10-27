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
using Can_Adapter;

namespace CAN_Tool.ViewModels
{
    internal class ManualControlPage:ViewModel
    {
        MainWindowViewModel VM { set; get; }

        private int manualAirBlower;
        public int ManualAirBlower { set => Set(ref manualAirBlower, value); get => manualAirBlower; }

        private int manualFuelPump;
        public int ManualFuelPump { set => Set(ref manualFuelPump, value); get => manualFuelPump; }

        private int manualGlowPlug;
        public int ManualGlowPlug { set => Set(ref manualGlowPlug, value); get => manualGlowPlug; }

        private bool manualWaterPump;
        public bool ManualWaterPump { set => Set(ref manualWaterPump, value); get => manualWaterPump; }

        #region EnterManualModeCommand
        public ICommand EnterManualModeCommand { get; }
        private void OnEnterManualModeCommandExecuted(object parameter)
        {
            VM.ExecuteCommand(67, new byte[] { 1, 0, 0, 0, 0, 0 });
        }

        #endregion

        private void updateManualMode()
        {
            AC2PMessage msg = new();
            msg.TransmitterType = 126;
            msg.TransmitterAddress = 6;
            msg.ReceiverId = VM.SelectedConnectedDevice.ID;
            msg.PGN = 1;
            msg.Data = new byte[8];
            msg.Data[0] = 0;
            msg.Data[1] = 68;

            if (ManualWaterPump)
                msg.Data[2] = 1;
            else
                msg.Data[2] = 0;
            msg.Data[3] = (byte)ManualAirBlower;
            msg.Data[4] = (byte)ManualGlowPlug;
            msg.Data[5] = (byte)(ManualFuelPump / 256);
            msg.Data[6] = (byte)ManualFuelPump;
            VM.CanAdapter.Transmit(msg);
        }
        #region ExitManualModeCommand
        public ICommand ExitManualModeCommand { get; }
        private void OnExitManualModeCommandExecuted(object parameter)
        {
            ManualAirBlower = 0;
            ManualFuelPump = 0;
            ManualGlowPlug = 0;
            VM.ExecuteCommand(67, new byte[] { 0, 0, 0, 0, 0, 0 });
        }
        #endregion

        #region IncreaceManualAirBlower
        public ICommand IncreaceManualAirBlowerCommand { get; }
        private void OnIncreaceManualAirBlowerCommandExecuted(object parameter)
        {
            if (ManualAirBlower < 100)
                ManualAirBlower++;
            updateManualMode();
        }
        #endregion

        #region DecreaseManualAirBlower
        public ICommand DecreaseManualAirBlowerCommand { get; }
        private void OnDecreaseManualAirBlowerCommandExecuted(object parameter)
        {
            if (ManualAirBlower > 0)
                ManualAirBlower--;
            updateManualMode();
        }
        #endregion

        #region IncreaseManualFuelPump
        public ICommand IncreaseManualFuelPumpCommand { get; }
        private void OnIncreaseManualFuelPumpCommandExecuted(object parameter)
        {
            ManualFuelPump += 5;
            if (ManualFuelPump > 700) ManualFuelPump = 700;
            updateManualMode();
        }
        #endregion



        #region DecreaseManualFuelPump
        public ICommand DecreaseFuelPumpCommand { get; }
        private void OnDecreaseFuelPumpCommandExecuted(object parameter)
        {
            ManualFuelPump -= 5;
            if (ManualFuelPump < 0) ManualFuelPump = 0;
            updateManualMode();
        }
        #endregion

        #region IncreaseManualGlowPlug
        public ICommand IncreaseGlowPlugCommand { get; }
        private void OnIncreaseGlowPlugCommandExecuted(object parameter)
        {
            ManualGlowPlug += 5;
            if (ManualGlowPlug > 100) ManualGlowPlug = 100;
            updateManualMode();
        }
        #endregion

        #region DecreaseManualGlowPlug
        public ICommand DecreaseGlowPlugCommand { get; }
        private void OnDecreaseGlowPlugCommandExecuted(object parameter)
        {
            ManualGlowPlug -= 5;
            if (ManualGlowPlug < 0) ManualGlowPlug = 0;
            updateManualMode();
        }
        #endregion

        #region TurnOnWaterPumpCommand
        public ICommand TurnOnWaterPumpCommand { get; }
        private void OnTurnOnWaterPumpCommandExecuted(object parameter)
        {
            manualWaterPump = true;
            updateManualMode();
        }
        #endregion

        #region TurnOffWaterPumpCommand
        public ICommand TurnOffWaterPumpCommand { get; }
        private void OnTurnOffWaterPumpCommandExecuted(object parameter)
        {
            manualWaterPump = false;
            updateManualMode();
        }
        public bool deviceInManualMode(object parameter)
        {
            return (VM.CanAdapter.PortOpened && VM.SelectedConnectedDevice != null && VM.SelectedConnectedDevice.ManualMode);
        }
            #endregion
            public ManualControlPage(MainWindowViewModel vm)
        {
            VM = vm;

            EnterManualModeCommand = new LambdaCommand(OnEnterManualModeCommandExecuted, VM.DeviceConnectedAndNotInManual);
            ExitManualModeCommand = new LambdaCommand(OnExitManualModeCommandExecuted, deviceInManualMode);
            IncreaceManualAirBlowerCommand = new LambdaCommand(OnIncreaceManualAirBlowerCommandExecuted, deviceInManualMode);
            DecreaseManualAirBlowerCommand = new LambdaCommand(OnDecreaseManualAirBlowerCommandExecuted, deviceInManualMode);
            IncreaseManualFuelPumpCommand = new LambdaCommand(OnIncreaseManualFuelPumpCommandExecuted, deviceInManualMode);
            DecreaseFuelPumpCommand = new LambdaCommand(OnDecreaseFuelPumpCommandExecuted, deviceInManualMode);
            IncreaseGlowPlugCommand = new LambdaCommand(OnIncreaseGlowPlugCommandExecuted, deviceInManualMode);
            DecreaseGlowPlugCommand = new LambdaCommand(OnDecreaseGlowPlugCommandExecuted, deviceInManualMode);
            TurnOnWaterPumpCommand = new LambdaCommand(OnTurnOnWaterPumpCommandExecuted, deviceInManualMode);
            TurnOffWaterPumpCommand = new LambdaCommand(OnTurnOffWaterPumpCommandExecuted, deviceInManualMode);
        }
    }
}

