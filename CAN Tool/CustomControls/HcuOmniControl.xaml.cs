using CAN_Tool.ViewModels;
using OmniProtocol;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Controls.Primitives;
using System.Windows.Data;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Navigation;
using System.Windows.Shapes;
using System.Windows.Threading;

namespace CAN_Tool.CustomControls
{
    /// <summary>
    /// Логика взаимодействия для HcuOmniControl.xaml
    /// </summary>
    public partial class HcuOmniControl : UserControl
    {

        enum ZoneState_t { Off, Heat, Fan };

        DispatcherTimer SliderUpdateTimer = new();

        DeviceViewModel vm => (DeviceViewModel)DataContext;

        public HcuOmniControl()
        {
            InitializeComponent();

            SliderUpdateTimer.Interval = new TimeSpan(0, 0, 1);
            SliderUpdateTimer.Start();
            SliderUpdateTimer.Tick += SliderUpdateTimer_Tick;



        }

        private void SliderUpdateTimer_Tick(object sender, EventArgs e)
        {
            if (vm != null && DaytimeScroll.Value != vm.TimberlineParams.SelectedZone.TempSetpointDay)
                DaytimeScroll.SilentChange(vm.TimberlineParams.SelectedZone.TempSetpointDay);
            if (vm != null && NightTimeScroll.Value != vm.TimberlineParams.SelectedZone.TempSetpointNight)
                NightTimeScroll.SilentChange(vm.TimberlineParams.SelectedZone.TempSetpointNight);
            if (vm != null && ManualScroll.Value != vm.TimberlineParams.SelectedZone.ManualPercent)
                ManualScroll.SilentChange(vm.TimberlineParams.SelectedZone.ManualPercent);
        }

        private void Zone1Field_MouseLeftButtonDown(object sender, MouseButtonEventArgs e)
        {
            ZoneControlArea.DataContext = vm.TimberlineParams.Zones[0];
            vm.TimberlineParams.SelectedZone = vm.TimberlineParams.Zones[0];

        }

        private void Zone2Field_MouseLeftButtonDown(object sender, MouseButtonEventArgs e)
        {
            ZoneControlArea.DataContext = vm.TimberlineParams.Zones[1];
            vm.TimberlineParams.SelectedZone = vm.TimberlineParams.Zones[1];
        }

        private void Zone3Field_MouseLeftButtonDown(object sender, MouseButtonEventArgs e)
        {
            ZoneControlArea.DataContext = vm.TimberlineParams.Zones[2];
            vm.TimberlineParams.SelectedZone = vm.TimberlineParams.Zones[2];
        }

        private void Zone4Field_MouseLeftButtonDown(object sender, MouseButtonEventArgs e)
        {
            ZoneControlArea.DataContext = vm.TimberlineParams.Zones[3];
            vm.TimberlineParams.SelectedZone = vm.TimberlineParams.Zones[3];
        }

        private void Zone5Field_MouseLeftButtonDown(object sender, MouseButtonEventArgs e)
        {
            ZoneControlArea.DataContext = vm.TimberlineParams.Zones[4];
            vm.TimberlineParams.SelectedZone = vm.TimberlineParams.Zones[4];
        }


        private void HeaterButton_Click(object sender, RoutedEventArgs e)
        {
            OmniMessage m = new();
            m.ReceiverId = vm.Id;
            m.PGN = 22;
            m.Data = new byte[] { 0xFF, 0xFF, 0xFF, 0xFF, 0xFF, 0xFF, 0xFF, 0xFF };
            if (vm.TimberlineParams.HeaterEnabled)
                m.Data[7] = 0b11111100;
            else
                m.Data[7] = 0b11111101;

            vm.Transmit(m.ToCanMessage());
        }

        private void ElementButton_Click(object sender, RoutedEventArgs e)
        {
            OmniMessage m = new();
            m.ReceiverId = vm.Id;
            m.PGN = 22;
            m.Data = new byte[] { 0xFF, 0xFF, 0xFF, 0xFF, 0xFF, 0xFF, 0xFF, 0xFF };
            if (vm.TimberlineParams.ElementEbabled)
                m.Data[7] = 0b11110011;
            else
                m.Data[7] = 0b11110111;
            vm.Transmit(m.ToCanMessage());
        }

        private void ManualButtonClick(object sender, RoutedEventArgs e)
        {
            if (vm != null)
            {
                OmniMessage m = new();
                m.ReceiverId = vm.Id;
                m.PGN = 27;
                m.Data = new byte[] { 0xFF, 0xFF, 0xFF, 0xFF, 0xFF, 0xFF, 0xFF, 0xFF };
                int zoneNumber = vm.TimberlineParams.Zones.IndexOf(vm.TimberlineParams.SelectedZone);
                switch (zoneNumber)
                {
                    case 0: if (!vm.TimberlineParams.Zones[0].ManualMode) m.Data[5] = 0b11111101; else m.Data[5] = 0b11111100; break;
                    case 1: if (!vm.TimberlineParams.Zones[1].ManualMode) m.Data[5] = 0b11110111; else m.Data[5] = 0b11110011; break;
                    case 2: if (!vm.TimberlineParams.Zones[2].ManualMode) m.Data[5] = 0b11011111; else m.Data[5] = 0b11001111; break;
                    case 3: if (!vm.TimberlineParams.Zones[3].ManualMode) m.Data[5] = 0b01111111; else m.Data[5] = 0b00111111; break;
                    case 4: if (!vm.TimberlineParams.Zones[4].ManualMode) m.Data[6] = 0b11111101; else m.Data[6] = 0b11111100; break;
                }
                vm.Transmit(m.ToCanMessage());
            }
        }

        private void ToggleZoneModeClick(object sender, RoutedEventArgs e)
        {
            if (vm != null)
            {
                OmniMessage m = new();
                m.ReceiverId = vm.Id;
                m.PGN = 22;
                int zoneNumber = vm.TimberlineParams.Zones.IndexOf(vm.TimberlineParams.SelectedZone);
                int newState = (int)vm.TimberlineParams.SelectedZone.State + 1;
                if (newState > 2) newState = 0;
                byte newStateByte = (byte)(~(3 << (zoneNumber % 4)) | newState << (zoneNumber % 4));
                m.Data[zoneNumber / 4] = newStateByte;
                vm.Transmit(m.ToCanMessage());
            }
        }



        private void ZoneSelected(object sender, SelectionChangedEventArgs e)
        {
            int index = (sender as ListBox).SelectedIndex;
            vm.TimberlineParams.SelectedZone = vm.TimberlineParams.Zones[index];
            DaytimeScroll.SilentChange(vm.TimberlineParams.SelectedZone.TempSetpointDay);
            NightTimeScroll.SilentChange(vm.TimberlineParams.SelectedZone.TempSetpointNight);
            ManualScroll.SilentChange(vm.TimberlineParams.SelectedZone.ManualPercent);
        }

        private void ManualPercentChanged(object sender, RoutedPropertyChangedEventArgs<double> e)
        {
            if (vm != null && Convert.ToByte((sender as SilentSlider).Value) != vm.TimberlineParams.SelectedZone.ManualPercent)
            {
                var slider = (SilentSlider)sender;
                var newvalue = Math.Round(slider.Value, 0);
                if (newvalue > slider.Maximum)
                    newvalue = slider.Maximum;

                if (newvalue < slider.Minimum)
                    newvalue = slider.Minimum;
                // feel free to add code to test against the min, too.
                slider.Value = newvalue;
                OmniMessage m = new();
                m.ReceiverId = vm.Id;
                m.PGN = 27;
                m.Data[vm.TimberlineParams.Zones.IndexOf(vm.TimberlineParams.SelectedZone)] = Convert.ToByte((sender as SilentSlider).Value);
                vm.Transmit(m.ToCanMessage());
            }
        }

        private void NightTimeChanged(object sender, RoutedPropertyChangedEventArgs<double> e)
        {
            if (vm != null)
            {
                OmniMessage m = new();
                m.ReceiverId = vm.Id;
                m.PGN = 26;
                m.Data = new byte[] { 0xFF, 0xFF, 0xFF, 0xFF, 0xFF, 0xFF, 0xFF, 0xFF };
                m.Data[vm.TimberlineParams.Zones.IndexOf(vm.TimberlineParams.SelectedZone)] = Convert.ToByte((sender as SilentSlider).Value + 75);
                vm.Transmit(m.ToCanMessage());
            }
        }

        private void DayTimeChanged(object sender, RoutedPropertyChangedEventArgs<double> e)
        {
            if (vm?.TimberlineParams.SelectedZone != null)
            {
                OmniMessage m = new();
                m.ReceiverId = vm.Id;
                m.PGN = 25;
                m.Data[vm.TimberlineParams.Zones.IndexOf(vm.TimberlineParams.SelectedZone)] = Convert.ToByte((sender as SilentSlider).Value + 75);
                vm.Transmit(m.ToCanMessage());
            }
        }
    }


}
