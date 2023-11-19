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

namespace CAN_Tool.CustomControls
{
    /// <summary>
    /// Логика взаимодействия для HcuOmniControl.xaml
    /// </summary>
    public partial class HcuOmniControl : UserControl
    {

        enum ZoneState_t {Off,Heat,Fan };

        MainWindowViewModel vm => (MainWindowViewModel)DataContext;

        public HcuOmniControl()
        {
            InitializeComponent();
            
        }


        private void Zone1Field_MouseLeftButtonDown(object sender, MouseButtonEventArgs e)
        {
            ZoneControlArea.DataContext = vm.SelectedConnectedDevice.Timber.Zones[0];
            vm.SelectedConnectedDevice.Timber.SelectedZone = vm.SelectedConnectedDevice.Timber.Zones[0];

        }

        private void Zone2Field_MouseLeftButtonDown(object sender, MouseButtonEventArgs e)
        {
            ZoneControlArea.DataContext = vm.SelectedConnectedDevice.Timber.Zones[1];
            vm.SelectedConnectedDevice.Timber.SelectedZone = vm.SelectedConnectedDevice.Timber.Zones[1];
        }

        private void Zone3Field_MouseLeftButtonDown(object sender, MouseButtonEventArgs e)
        {
            ZoneControlArea.DataContext = vm.SelectedConnectedDevice.Timber.Zones[2];
            vm.SelectedConnectedDevice.Timber.SelectedZone = vm.SelectedConnectedDevice.Timber.Zones[2];
        }

        private void Zone4Field_MouseLeftButtonDown(object sender, MouseButtonEventArgs e)
        {
            ZoneControlArea.DataContext = vm.SelectedConnectedDevice.Timber.Zones[3];
            vm.SelectedConnectedDevice.Timber.SelectedZone = vm.SelectedConnectedDevice.Timber.Zones[3];
        }

        private void Zone5Field_MouseLeftButtonDown(object sender, MouseButtonEventArgs e)
        {
            ZoneControlArea.DataContext = vm.SelectedConnectedDevice.Timber.Zones[4];
            vm.SelectedConnectedDevice.Timber.SelectedZone = vm.SelectedConnectedDevice.Timber.Zones[4];
        }

        private void RadioChecked(object sender, RoutedEventArgs e)
        {
            DaytimeScroll.Value = vm.SelectedConnectedDevice.Timber.SelectedZone.TempSetpointDay;
            NightTimeScroll.Value = vm.SelectedConnectedDevice.Timber.SelectedZone.TempSetpointNight;
            ManualScroll.Value = vm.SelectedConnectedDevice.Timber.SelectedZone.ManualPercent;

        }


        private void HeaterButton_Click(object sender, RoutedEventArgs e)
        {
            OmniMessage m = new();
            m.ReceiverId = vm.SelectedConnectedDevice.ID;
            m.PGN = 22;
            m.Data = new byte[] { 0xFF, 0xFF, 0xFF, 0xFF, 0xFF, 0xFF, 0xFF, 0xFF };
            if (vm.SelectedConnectedDevice.Timber.HeaterEnabled)
                m.Data[7] = 0b11111100;
            else
                m.Data[7] = 0b11111101;
            vm.CanAdapter.Transmit(m.ToCanMessage());
        }

        private void ElementButton_Click(object sender, RoutedEventArgs e)
        {
            OmniMessage m = new();
            m.ReceiverId = vm.SelectedConnectedDevice.ID;
            m.PGN = 22;
            m.Data = new byte[] { 0xFF, 0xFF, 0xFF, 0xFF, 0xFF, 0xFF, 0xFF, 0xFF };
            if (vm.SelectedConnectedDevice.Timber.ElementEbabled)
                m.Data[7] = 0b11110011;
            else
                m.Data[7] = 0b11110111;
            vm.CanAdapter.Transmit(m.ToCanMessage());
        }


        private void DayTimeChanged(object sender, RoutedEventArgs e)
        {
            if (vm?.SelectedConnectedDevice != null)
            {
                OmniMessage m = new();
                m.ReceiverId = vm.SelectedConnectedDevice.ID;
                m.PGN = 25;
                m.Data = new byte[] { 0xFF, 0xFF, 0xFF, 0xFF, 0xFF, 0xFF, 0xFF, 0xFF };
                m.Data[vm.SelectedConnectedDevice.Timber.Zones.IndexOf(vm.SelectedConnectedDevice.Timber.SelectedZone)] = Convert.ToByte((sender as ScrollBar).Value + 75);
                vm.CanAdapter.Transmit(m.ToCanMessage());
            }
        }

        private void NightTimeChanged(object sender, RoutedEventArgs e)
        {
            if (vm?.SelectedConnectedDevice != null)
            {
                OmniMessage m = new();
                m.ReceiverId = vm.SelectedConnectedDevice.ID;
                m.PGN = 26;
                m.Data = new byte[] { 0xFF, 0xFF, 0xFF, 0xFF, 0xFF, 0xFF, 0xFF, 0xFF };
                m.Data[vm.SelectedConnectedDevice.Timber.Zones.IndexOf(vm.SelectedConnectedDevice.Timber.SelectedZone)] = Convert.ToByte((sender as ScrollBar).Value + 75);
                vm.CanAdapter.Transmit(m.ToCanMessage());
            }
        }


        private void ManualButtonClick(object sender, RoutedEventArgs e)
        {
            if (vm?.SelectedConnectedDevice != null)
            {
                OmniMessage m = new();
                m.ReceiverId = vm.SelectedConnectedDevice.ID;
                m.PGN = 27;
                m.Data = new byte[] { 0xFF, 0xFF, 0xFF, 0xFF, 0xFF, 0xFF, 0xFF, 0xFF };
                int zoneNumber = vm.SelectedConnectedDevice.Timber.Zones.IndexOf(vm.SelectedConnectedDevice.Timber.SelectedZone);
                switch (zoneNumber)
                {
                    case 0: if (!vm.SelectedConnectedDevice.Timber.Zones[0].ManualMode) m.Data[5] = 0b11111101; else m.Data[5] = 0b11111100; break;
                    case 1: if (!vm.SelectedConnectedDevice.Timber.Zones[1].ManualMode) m.Data[5] = 0b11110111; else m.Data[5] = 0b11110011; break;
                    case 2: if (!vm.SelectedConnectedDevice.Timber.Zones[2].ManualMode) m.Data[5] = 0b11011111; else m.Data[5] = 0b11001111; break;
                    case 3: if (!vm.SelectedConnectedDevice.Timber.Zones[3].ManualMode) m.Data[5] = 0b01111111; else m.Data[5] = 0b00111111; break;
                    case 4: if (!vm.SelectedConnectedDevice.Timber.Zones[4].ManualMode) m.Data[6] = 0b11111101; else m.Data[6] = 0b11111100; break;
                }
                vm.CanAdapter.Transmit(m.ToCanMessage());
            }
        }

        private void ToggleZoneModeClick(object sender, RoutedEventArgs e)
        {
            if (vm?.SelectedConnectedDevice != null)
            {
                OmniMessage m = new();
                m.ReceiverId = vm.SelectedConnectedDevice.ID;
                m.PGN = 22;
                int zoneNumber = vm.SelectedConnectedDevice.Timber.Zones.IndexOf(vm.SelectedConnectedDevice.Timber.SelectedZone);
                int newState = (int)vm.SelectedConnectedDevice.Timber.SelectedZone.State + 1;
                if (newState > 2) newState = 0;
                byte newStateByte = (byte)(~(3 << (zoneNumber % 4)) | newState << (zoneNumber % 4));
                m.Data[zoneNumber/4] = newStateByte;
                vm.CanAdapter.Transmit(m.ToCanMessage());
            }
        }


        private void ManualPercentChanged(object sender, RoutedEventArgs e)
        {

            if (vm?.SelectedConnectedDevice != null && Convert.ToByte((sender as ScrollBar).Value) != vm.SelectedConnectedDevice.Timber.SelectedZone.ManualPercent)
            {
                var scrollBar = (ScrollBar)sender;
                var newvalue = Math.Round(scrollBar.Value, 0);
                if (newvalue > scrollBar.Maximum)
                    newvalue = scrollBar.Maximum;

                if (newvalue < scrollBar.Minimum)
                    newvalue = scrollBar.Minimum;
                // feel free to add code to test against the min, too.
                scrollBar.Value = newvalue;
                OmniMessage m = new();
                m.ReceiverId = vm.SelectedConnectedDevice.ID;
                m.PGN = 27;
                m.Data[vm.SelectedConnectedDevice.Timber.Zones.IndexOf(vm.SelectedConnectedDevice.Timber.SelectedZone)] = Convert.ToByte((sender as ScrollBar).Value);
                Thread.Sleep(80);
                vm.CanAdapter.Transmit(m.ToCanMessage());
            }
        }
    }


}
