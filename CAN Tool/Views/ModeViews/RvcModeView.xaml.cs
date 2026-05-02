using CAN_Tool.ViewModels;
using MaterialDesignThemes.Wpf;
using RVC;
using System;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Controls.Primitives;

namespace CAN_Tool.CustomControls
{
    public partial class RvcModeView : UserControl
    {
        private RvcPageViewModel Vm => (RvcPageViewModel)DataContext;

        public RvcModeView() => InitializeComponent();

        private void RVCMessageList_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            try { Vm.SelectedMessage = (RvcMessage)(sender as DataGrid).SelectedItems[(sender as DataGrid).SelectedItems.Count - 1]; }
            catch { }
        }

        private void SetTimeButtonPressed(object sender, RoutedEventArgs e) => Vm.Timberline15.SetTime(DateTime.Now);
        private void ToggleHeaterButtonPressed(object sender, RoutedEventArgs e) => Vm.Timberline15.ToggleHeater();
        private void ToggleElementButtonPressed(object sender, RoutedEventArgs e) => Vm.Timberline15.ToggleElement();
        private void ToggleWaterButtonPressed(object sender, RoutedEventArgs e) => Vm.Timberline15.ToggleWater();
        private void ToggleZoneButtonPressed(object sender, RoutedEventArgs e) => Vm.Timberline15.ToggleZone();
        private void TogglePumpButtonPressed(object sender, RoutedEventArgs e) => Vm.Timberline15.TogglePump();
        private void ToggleFanManualModeButtonPressed(object sender, RoutedEventArgs e) => Vm.Timberline15.ToggleFanManualMode();
        private void ToggleScheduleModeButtonPressed(object sender, RoutedEventArgs e) => Vm.Timberline15.ToggleScheduleMode();
        private void ClearErrorsButtonPressed(object sender, RoutedEventArgs e) => Vm.Timberline15.ClearErrors();

        private void DaySetPointValueChanged(object sender, RoutedEventArgs e) =>
            Vm?.Timberline15.SetDaySetpoint((int)(sender as ScrollBar).Value);
        private void NightSetPointValueChanged(object sender, RoutedEventArgs e) =>
            Vm?.Timberline15.SetNightSetpoint((int)(sender as ScrollBar).Value);
        private void ManualFanSpeedValueChanged(object sender, RoutedEventArgs e) =>
            Vm?.Timberline15.SetFanManualSpeed((byte)(sender as ScrollBar).Value);
        private void SystemDurationValueChanged(object sender, RoutedPropertyChangedEventArgs<double> e) =>
            Vm?.Timberline15.SetSystemDuration((int)(sender as ScrollBar).Value);
        private void WaterDurationValueChanged(object sender, RoutedPropertyChangedEventArgs<double> e) =>
            Vm?.Timberline15.SetWaterDuration((int)(sender as ScrollBar).Value);
        private void NightTimeChanged(object sender, RoutedPropertyChangedEventArgs<DateTime?> e)
        {
            if ((sender as TimePicker).SelectedTime.HasValue)
                Vm?.Timberline15.SetNightStart((sender as TimePicker).SelectedTime.Value.Hour, (sender as TimePicker).SelectedTime.Value.Minute);
        }
        private void DayStartChanged(object sender, RoutedPropertyChangedEventArgs<DateTime?> e)
        {
            if ((sender as TimePicker).SelectedTime.HasValue)
                Vm?.Timberline15.SetDayStart((sender as TimePicker).SelectedTime.Value.Hour, (sender as TimePicker).SelectedTime.Value.Minute);
        }
    }
}
