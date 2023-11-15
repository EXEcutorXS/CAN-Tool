﻿using CAN_Tool.ViewModels;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Navigation;
using System.Windows.Shapes;

namespace CAN_Tool.Views
{
    /// <summary>
    /// Логика взаимодействия для TimberlineRvcControl.xaml
    /// </summary>
    public partial class TimberlineRvcControl : UserControl
    {

        public Timberline20Handler vm => (Timberline20Handler)DataContext;


        public TimberlineRvcControl()
        {
            InitializeComponent();
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

        private void Button_Click(object sender, RoutedEventArgs e)
        {
            vm?.SetTime(DateTime.Now);
        }

        private void SystemLimitChanged(object sender, RoutedPropertyChangedEventArgs<double> e)
        {
            vm?.SetSystemDuration((int)(sender as Slider).Value);
        }

        private void PumpDurationChanged(object sender, RoutedPropertyChangedEventArgs<double> e)
        {
            vm?.SetPumpOverrideDuration((int)(sender as Slider).Value);
        }

        private void DayStartTimeChanged(object sender, RoutedPropertyChangedEventArgs<DateTime?> e)
        {
            vm?.SetDayStart(e.NewValue.Value.Hour, e.NewValue.Value.Minute);
        }

        private void NightStartTimeChanged(object sender, RoutedPropertyChangedEventArgs<DateTime?> e)
        {
            vm?.SetNightStart(e.NewValue.Value.Hour, e.NewValue.Value.Minute);
        }

        private void ToggleSelectedZone(object sender, RoutedEventArgs e)
        {
            vm?.ToggleZoneState(vm.SelectedZoneNumber);
        }

        private void RadioChecked(object sender, RoutedEventArgs e)
        {
            vm.SelectedZone = vm.Zones.FirstOrDefault(x => x.Selected);
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
    }
}
