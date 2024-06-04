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
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;

namespace CAN_Tool.ViewModels
{

    public partial class ManualPageViewModel : ObservableObject
    {
        public MainWindowViewModel Vm { set; get; }

        [ObservableProperty] private int manualAirBlower;
        [ObservableProperty] private int manualFuelPump;
        [ObservableProperty] private int manualGlowPlug;
        [ObservableProperty] private bool manualWaterPump;


        [RelayCommand]
        private void EnterManualMode(object parameter) => Vm.ExecuteCommand(67, new byte[] { 1, 0, 0, 0, 0, 0 });

        [RelayCommand]
        private void ExitManualMode(object parameter)
        {
            ManualAirBlower = 0;
            ManualFuelPump = 0;
            ManualGlowPlug = 0;
            Vm.ExecuteCommand(67, new byte[] { 0, 0, 0, 0, 0, 0 });
        }

        [RelayCommand]
        private void PumpCheck(object parameter)
        {
            Task.Run(() => Vm.OmniInstance.CheckPump(Vm.OmniInstance.SelectedConnectedDevice));
        }

        [RelayCommand]
        private void ChangeManualAirBlower(object parameter)
        {
            ManualAirBlower += (parameter == null) ? 1 : int.Parse(parameter as string);
            ManualAirBlower = Math.Clamp(ManualAirBlower, 0, 200);

            updateManualMode();
        }

        [RelayCommand]
        private void ChangeManualFuelPump(object parameter)
        {
            ManualFuelPump += (parameter == null) ? 5 : int.Parse(parameter as string);
            ManualFuelPump = Math.Clamp(ManualFuelPump, 0, 700);
            updateManualMode();
        }

        [RelayCommand]
        private void ChangeGlowPlug(object parameter)
        {
            ManualGlowPlug += (parameter == null) ? 1 : int.Parse(parameter as string);
            ManualGlowPlug = Math.Clamp(ManualGlowPlug, 0, 100);
            updateManualMode();
        }

        [RelayCommand]
        private void ToggleGlowPlug(object parameter)
        {
            if (ManualGlowPlug > 0)
                ManualGlowPlug = 0;
            else
                ManualGlowPlug = 100;
            updateManualMode();
        }


        [RelayCommand]
        private void ToggleWaterPump(object parameter)
        {
            ManualWaterPump = !ManualWaterPump;
            updateManualMode();
        }

        private void updateManualMode()
        {
            if (Vm.OmniInstance.SelectedConnectedDevice == null) return;
            OmniMessage msg = new();
            msg.TransmitterId.Type = 126;
            msg.TransmitterId.Address = 6;
            msg.ReceiverId.Type = Vm.OmniInstance.SelectedConnectedDevice.Id.Type;
            msg.ReceiverId.Address = 7;
            msg.Pgn = 1;
            msg.Data = new byte[8];
            msg.Data[0] = 0;
            msg.Data[1] = 68;
            msg.Data[2] = (byte)(ManualWaterPump ? 1 : 0);
            msg.Data[3] = (byte)ManualAirBlower;
            msg.Data[4] = (byte)ManualGlowPlug;
            msg.Data[5] = (byte)(ManualFuelPump / 256);
            msg.Data[6] = (byte)ManualFuelPump;
            Task.Run(() => Vm.OmniInstance.SendMessage(msg));
        }

        public ManualPageViewModel(MainWindowViewModel vm)
        {
            Vm = vm;
        }
    }
}
