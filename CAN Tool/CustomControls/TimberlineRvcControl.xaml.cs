using CAN_Tool.ViewModels;
using System;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using System.Windows.Threading;

namespace CAN_Tool.CustomControls
{
    /// <summary>
    /// Логика взаимодействия для TimberlineRvcControl.xaml
    /// </summary>
    public partial class TimberlineRvcControl : UserControl
    {

        public Timberline20RvcViewModel vm => (Timberline20RvcViewModel)DataContext;

        DispatcherTimer sliderUpdateTimer;


        public TimberlineRvcControl()
        {
            InitializeComponent();
            sliderUpdateTimer = new();
            sliderUpdateTimer.Interval = new TimeSpan(0,0,0,0,500);
            sliderUpdateTimer.Tick += SliderUpdateTimer_Tick;
            sliderUpdateTimer.Start();

        }

        private void SliderUpdateTimer_Tick(object sender, EventArgs e)
        {
            if (vm==null) return;
            if (vm.DayStartMinutes != DayStartSlider.Value)
                DayStartSlider.SilentChange(vm.DayStartMinutes);
            if (vm.NightStartMinutes != NightStartSlider.Value)
                NightStartSlider.SilentChange(vm.NightStartMinutes);
            if (vm.SystemDuration!= SystemLimitSlider.Value)
                SystemLimitSlider.SilentChange(vm.SystemDuration);
            if (vm.PumpDuration != PumpLimitSlider.Value)
                PumpLimitSlider.SilentChange(vm.PumpDuration);
            if (EngineSetpointSlider.Value != vm.EnginePreheatSetpoint)
                EngineSetpointSlider.SilentChange(vm.EnginePreheatSetpoint);
            if (EngineDurationSlider.Value != vm.EnginePreheatDuration)
                EngineDurationSlider.SilentChange(vm.EnginePreheatDuration);
            if (EngineDurationSlider.Value != vm.EnginePreheatDuration)
                EngineDurationSlider.SilentChange(vm.EnginePreheatDuration);
            if (FloorSetpointSlider.Value != vm.UnderfloorSetpoint)
                FloorSetpointSlider.SilentChange(vm.UnderfloorSetpoint);
            if (FloorHysteresisSlider.Value!=vm.UnderfloorHysteresis)
                FloorHysteresisSlider.SilentChange(vm.UnderfloorHysteresis);
        }

        private void ToggleHeaterClick(object sender, RoutedEventArgs e)
        {
            vm?.ToggleHeater();
        }

        private void ElementToggleButtonClick(object sender, RoutedEventArgs e)
        {
            vm?.ToggleElement();
        }

        private void FloorButtonClick(object sender, RoutedEventArgs e)
        {
            vm?.ToggleUnderfloorHeating();
        }

        private void HeaterPumpButtonClick(object sender, RoutedEventArgs e)
        {
            vm?.ToggleHeaterPumpOverride();
        }

        private void Pump1ButtonClick(object sender, RoutedEventArgs e)
        {
            vm?.TogglePump1Override();
        }

        private void Pump2ButtonClick(object sender, RoutedEventArgs e)
        {
            vm?.TogglePump2Override();
        }

        private void AuxPump1ButtonClick(object sender, RoutedEventArgs e)
        {
            vm?.ToggleAuxPump1Override();
        }

        private void AuxPump2ButtonClick(object sender, RoutedEventArgs e)
        {
            vm?.ToggleAuxPump2Override();
        }

        private void AuxPump3ButtonClick(object sender, RoutedEventArgs e)
        {
            vm?.ToggleAuxPump3Override();
        }

        private void SystemLimitChanged(object sender, RoutedPropertyChangedEventArgs<double> e)
        {
            vm?.SetSystemDuration((int)(sender as Slider).Value);
        }

        private void PumpDurationChanged(object sender, RoutedPropertyChangedEventArgs<double> e)
        {
            vm?.SetPumpOverrideDuration((int)(sender as Slider).Value);
        }


        private void ToggleSelectedZone(object sender, RoutedEventArgs e)
        {
            vm?.ToggleZoneState(vm.SelectedZoneNumber);
        }

        private void ToggleFanAuto(object sender, RoutedEventArgs e)
        {
            vm?.ToggleFanManualMode(vm.SelectedZoneNumber);
        }

        private void ManualPercentChanged(object sender, RoutedPropertyChangedEventArgs<double> e)
        {
            vm?.SetFanManualSpeed((byte)vm.SelectedZoneNumber, (byte)(sender as Slider).Value);
        }

        private void DaySetpointChanged(object sender, RoutedPropertyChangedEventArgs<double> e)
        {
            vm?.SetDaySetpoint(vm.SelectedZoneNumber, (int)(sender as Slider).Value);
        }

        private void NightSetpointChanged(object sender, RoutedPropertyChangedEventArgs<double> e)
        {
            vm?.SetNightSetpoint(vm.SelectedZoneNumber, (int)(sender as Slider).Value);
        }

        private void Button_Click_1(object sender, RoutedEventArgs e)
        {
            vm?.ToggleEnginePreheat();
        }

        private void NightSetpointChanged(object sender, MouseButtonEventArgs e)
        {
            vm?.SetNightSetpoint(vm.SelectedZoneNumber, (int)(sender as Slider).Value);
        }

        private void DaySetpointChanged(object sender, MouseButtonEventArgs e)
        {
            vm?.SetDaySetpoint(vm.SelectedZoneNumber, (int)(sender as Slider).Value);
        }

        private void EngineDurationChanged(object sender, RoutedPropertyChangedEventArgs<double> e)
        {
            vm?.SetEngineDuration((int)(sender as Slider).Value);
        }

        private void EngineSetpointChanged(object sender, RoutedPropertyChangedEventArgs<double> e)
        {
            vm?.SetEngineSetpoint((int)(sender as Slider).Value);
        }

        private void FloorSetpointChanged(object sender, RoutedPropertyChangedEventArgs<double> e)
        {
            vm?.SetUnderfloorSetpoint((int)(sender as Slider).Value);
        }

        private void FloorHysteresisCHanged(object sender, RoutedPropertyChangedEventArgs<double> e)
        {
            vm?.SetUnderfloorHysteresis((float)(sender as Slider).Value);
        }

        private void DayStartChanged(object sender, RoutedPropertyChangedEventArgs<double> e)
        {
            int hours = (int)(sender as Slider).Value/60;
            int minutes  = (int)(sender as Slider).Value%60;

            vm?.SetDayStart(hours, minutes);
        }

        private void NightStartChanged(object sender, RoutedPropertyChangedEventArgs<double> e)
        {
            int hours = (int)(sender as Slider).Value / 60;
            int minutes = (int)(sender as Slider).Value % 60;

            vm?.SetNightStart(hours, minutes);
        }


        private void SelectedZoneChanged(object sender, SelectionChangedEventArgs e)
        {
            vm.SelectedZoneNumber = (sender as ListBox).SelectedIndex;
            vm.SelectedZone = vm.Zones[vm.SelectedZoneNumber];

            
            NightSetpointSlider.SilentChange(vm.SelectedZone.TempSetpointNight);
            DaySetpointSlider.SilentChange(vm.SelectedZone.TempSetpointDay);
            RvcTempSlider.Value = vm.SelectedZone.RvcTemperature;
            ManualSlider.SilentChange(vm.SelectedZone.ManualPercent);

        }

        private void SyncTimeClick(object sender, RoutedEventArgs e)
        {
            vm?.SetTime(DateTime.Now);
        }
    }
}
