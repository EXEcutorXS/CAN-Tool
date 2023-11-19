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

namespace CAN_Tool.ViewModels
{

    internal class ManualPageViewModel : ViewModel
    {
        public MainWindowViewModel Vm { set; get; }


        private int manualAirBlower;
        public int ManualAirBlower { set => Set(ref manualAirBlower, value); get => manualAirBlower; }

        private int manualFuelPump;
        public int ManualFuelPump { set => Set(ref manualFuelPump, value); get => manualFuelPump; }

        private int manualGlowPlug;
        public int ManualGlowPlug { set => Set(ref manualGlowPlug, value); get => manualGlowPlug; }

        private bool manualWaterPump;
        public bool ManualWaterPump { set => Set(ref manualWaterPump, value); get => manualWaterPump; }



        public ICommand EnterManualModeCommand { get; }
        private void OnEnterManualModeCommandExecuted(object parameter)
        {
            Vm.ExecuteCommand(67, new byte[] { 1, 0, 0, 0, 0, 0 });
        }


        public ICommand ExitManualModeCommand { get; }
        private void OnExitManualModeCommandExecuted(object parameter)
        {
            ManualAirBlower = 0;
            ManualFuelPump = 0;
            ManualGlowPlug = 0;
            Vm.ExecuteCommand(67, new byte[] { 0, 0, 0, 0, 0, 0 });
        }
        
        
        public ICommand PumpCheckCommand { get; }
        private void OnPumpCheckCommandExecuted(object parameter)
        {
            Task.Run(() => Vm.OmniInstance.CheckPump(Vm.SelectedConnectedDevice));
        }

        private bool CanPumpCheckCommandExecute(object parameter)
        {
            return (Vm.deviceSelected(null) && Vm.SelectedConnectedDevice.ManualMode && !Vm.OmniInstance.CurrentTask.Occupied);
        }
        
        public ICommand IncreaceManualAirBlowerCommand { get; }
        private void OnIncreaceManualAirBlowerCommandExecuted(object parameter)
        {
            int delta;

            if (parameter == null)
                delta = 1;
            else
                delta = (int)parameter;

            ManualAirBlower += delta;
            if (ManualAirBlower >= 200)
                manualAirBlower = 200;

            updateManualMode();
        }

        public ICommand DecreaseManualAirBlowerCommand { get; }
        private void OnDecreaseManualAirBlowerCommandExecuted(object parameter)
        {
            int delta;

            if (parameter == null)
                delta = -1;
            else
                delta = (int)parameter;

            ManualAirBlower += delta;
            if (ManualAirBlower <= 0)
                ManualAirBlower = 0;

            updateManualMode();
        }

        public ICommand IncreaseManualFuelPumpCommand { get; }
        private void OnIncreaseManualFuelPumpCommandExecuted(object parameter)
        {
            ManualFuelPump += 5;
            if (ManualFuelPump > 700) ManualFuelPump = 700;
            updateManualMode();
        }


        public ICommand DecreaseFuelPumpCommand { get; }
        private void OnDecreaseFuelPumpCommandExecuted(object parameter)
        {
            ManualFuelPump -= 5;
            if (ManualFuelPump < 0) ManualFuelPump = 0;
            updateManualMode();
        }

        public ICommand IncreaseGlowPlugCommand { get; }
        private void OnIncreaseGlowPlugCommandExecuted(object parameter)
        {
            ManualGlowPlug += 5;
            if (ManualGlowPlug > 100) ManualGlowPlug = 100;
            updateManualMode();
        }
        
        public ICommand DecreaseGlowPlugCommand { get; }
        private void OnDecreaseGlowPlugCommandExecuted(object parameter)
        {
            ManualGlowPlug -= 5;
            if (ManualGlowPlug < 0) ManualGlowPlug = 0;
            updateManualMode();
        }

        public ICommand TurnOnWaterPumpCommand { get; }
        private void OnTurnOnWaterPumpCommandExecuted(object parameter)
        {
            manualWaterPump = true;
            updateManualMode();
        }

        public ICommand TurnOffWaterPumpCommand { get; }
        private void OnTurnOffWaterPumpCommandExecuted(object parameter)
        {
            manualWaterPump = false;
            updateManualMode();
        }

        private void updateManualMode()
        {
            OmniMessage msg = new();
            msg.TransmitterType = 126;
            msg.TransmitterAddress = 6;
            msg.ReceiverType = Vm.SelectedConnectedDevice.ID.Type;
            msg.ReceiverAddress = 7;
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
            Task.Run(() => Vm.OmniInstance.SendMessage(msg));
        }

        public ManualPageViewModel(MainWindowViewModel vm)
        {
            Vm = vm;

            EnterManualModeCommand = new LambdaCommand(OnEnterManualModeCommandExecuted, null);
            ExitManualModeCommand = new LambdaCommand(OnExitManualModeCommandExecuted, Vm.deviceInManualMode);
            IncreaceManualAirBlowerCommand = new LambdaCommand(OnIncreaceManualAirBlowerCommandExecuted, Vm.deviceInManualMode);
            DecreaseManualAirBlowerCommand = new LambdaCommand(OnDecreaseManualAirBlowerCommandExecuted, Vm.deviceInManualMode);
            IncreaseManualFuelPumpCommand = new LambdaCommand(OnIncreaseManualFuelPumpCommandExecuted, Vm.deviceInManualMode);
            DecreaseFuelPumpCommand = new LambdaCommand(OnDecreaseFuelPumpCommandExecuted, Vm.deviceInManualMode);
            IncreaseGlowPlugCommand = new LambdaCommand(OnIncreaseGlowPlugCommandExecuted, Vm.deviceInManualMode);
            DecreaseGlowPlugCommand = new LambdaCommand(OnDecreaseGlowPlugCommandExecuted, Vm.deviceInManualMode);
            TurnOnWaterPumpCommand = new LambdaCommand(OnTurnOnWaterPumpCommandExecuted, Vm.deviceInManualMode);
            TurnOffWaterPumpCommand = new LambdaCommand(OnTurnOffWaterPumpCommandExecuted, Vm.deviceInManualMode);
            PumpCheckCommand = new LambdaCommand(OnPumpCheckCommandExecuted, CanPumpCheckCommandExecute);
        }
    }
}
