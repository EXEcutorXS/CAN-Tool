using CAN_Tool.ViewModels;
using OmniProtocol;
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
using System.Windows.Threading;

namespace CAN_Tool.CustomControls
{
    /// <summary>
    /// Логика взаимодействия для BootloaderControl.xaml
    /// </summary>
    public partial class AcPanelControl : UserControl
    {
       
        public DeviceViewModel vm => (DeviceViewModel)DataContext;

        public AcPanelControl()
        {
            InitializeComponent();
        }

        private void CondPwmChanged(object sender, RoutedPropertyChangedEventArgs<double> e)
        {
            OmniMessage m = new();
            m.ReceiverId.Type = vm.Id.Type;
            m.ReceiverId.Address = vm.Id.Address;
            m.Pgn = 50;
            m.Data = new byte[] { 4, 0, 0xFF, 0xFF, 0xFF, 0xFF, 0xFF, 0xFF };
            m.Data[4] = (byte)((uint)((sender as Slider).Value * 100) >> 8);
            m.Data[5] = (byte)((uint)((sender as Slider).Value * 100));
            vm.Transmit(m.ToCanMessage());
        }

        private void CompressorPwmChanged(object sender, RoutedPropertyChangedEventArgs<double> e)
        {
            OmniMessage m = new();
            m.ReceiverId.Type = vm.Id.Type;
            m.ReceiverId.Address = vm.Id.Address;
            m.Pgn = 50;
            m.Data = new byte[] { 1, 0, 0xFF, 0xFF, 0xFF, 0xFF, 0xFF, 0xFF };
            m.Data[4] = (byte)((uint)((sender as Slider).Value * 100) >> 8);
            m.Data[5] = (byte)((uint)((sender as Slider).Value * 100));
            vm.Transmit(m.ToCanMessage());
        }


        private void Button_Click(object sender, RoutedEventArgs e)
        {
            OmniMessage m = new();
            m.ReceiverId.Type = vm.Id.Type;
            m.ReceiverId.Address = vm.Id.Address;
            m.Pgn = 51;
            m.Data = new byte[] { 0x1, 0x0, 0xFF, 0xFF, 0xFF, 0xFF, 0xFF, 0xFF };
            vm.Transmit(m.ToCanMessage());
        }

        private void Button_Click_1(object sender, RoutedEventArgs e)
        {
            OmniMessage m = new();
            m.ReceiverId.Type = vm.Id.Type;
            m.ReceiverId.Address = vm.Id.Address;
            m.Pgn = 51;
            m.Data = new byte[] { 0x1, 0x1, 0xFF, 0xFF, 0xFF, 0xFF, 0xFF, 0xFF };
            vm.Transmit(m.ToCanMessage());
        }

        private void Button_Click_2(object sender, RoutedEventArgs e)
        {
            OmniMessage m = new();
            m.ReceiverId.Type = vm.Id.Type;
            m.ReceiverId.Address = vm.Id.Address;
            m.Pgn = 51;
            m.Data = new byte[] { 0x1, 0x2, 0xFF, 0xFF, 0xFF, 0xFF, 0xFF, 0xFF };
            vm.Transmit(m.ToCanMessage());
        }

        private void Button_Click_3(object sender, RoutedEventArgs e)
        {
            OmniMessage m = new();
            m.ReceiverId.Type = vm.Id.Type;
            m.ReceiverId.Address = vm.Id.Address;
            m.Pgn = 51;
            m.Data = new byte[] { 0x1, 0x3, 0xFF, 0xFF, 0xFF, 0xFF, 0xFF, 0xFF };
            vm.Transmit(m.ToCanMessage());
        }

        private void Button_Click_4(object sender, RoutedEventArgs e)
        {
            OmniMessage m = new();
            m.ReceiverId.Type = vm.Id.Type;
            m.ReceiverId.Address = vm.Id.Address;
            m.Pgn = 51;
            m.Data = new byte[] { 0x1, 0x4, 0xFF, 0xFF, 0xFF, 0xFF, 0xFF, 0xFF };
            vm.Transmit(m.ToCanMessage());
        }

        private void Button_Click_5(object sender, RoutedEventArgs e)
        {
            OmniMessage m = new();
            m.ReceiverId.Type = vm.Id.Type;
            m.ReceiverId.Address = vm.Id.Address;
            m.Pgn = 51;
            m.Data = new byte[] { 0x1, 0x5, 0xFF, 0xFF, 0xFF, 0xFF, 0xFF, 0xFF };
            vm.Transmit(m.ToCanMessage());
        }

        private void Button_Click_6(object sender, RoutedEventArgs e)
        {
            OmniMessage m = new();
            m.ReceiverId.Type = vm.Id.Type;
            m.ReceiverId.Address = vm.Id.Address;
            m.Pgn = 51;
            m.Data = new byte[] { 0x1, 0x6, 0xFF, 0xFF, 0xFF, 0xFF, 0xFF, 0xFF };
            vm.Transmit(m.ToCanMessage());
        }

        private void Button_Click_7(object sender, RoutedEventArgs e)
        {
            OmniMessage m = new();
            m.ReceiverId.Type = vm.Id.Type;
            m.ReceiverId.Address = vm.Id.Address;
            m.Pgn = 51;
            m.Data = new byte[] { 0x1, 0xFF, 0xFF, 0x0, 0xFF, 0xFF, 0xFF, 0xFF };
            vm.Transmit(m.ToCanMessage());
        }

        private void Button_Click_8(object sender, RoutedEventArgs e)
        {
            OmniMessage m = new();
            m.ReceiverId.Type = vm.Id.Type;
            m.ReceiverId.Address = vm.Id.Address;
            m.Pgn = 51;
            m.Data = new byte[] { 0x1, 0xFF, 0xFF, 0x1, 0xFF, 0xFF, 0xFF, 0xFF };
            vm.Transmit(m.ToCanMessage());
        }

        private void Button_Click_9(object sender, RoutedEventArgs e)
        {
            OmniMessage m = new();
            m.ReceiverId.Type = vm.Id.Type;
            m.ReceiverId.Address = vm.Id.Address;
            m.Pgn = 51;
            m.Data = new byte[] { 0x1, 0xFF, 0xFF, 0x2, 0xFF, 0xFF, 0xFF, 0xFF };
            vm.Transmit(m.ToCanMessage());
        }

        private void Button_Click_10(object sender, RoutedEventArgs e)
        {
            OmniMessage m = new();
            m.ReceiverId.Type = vm.Id.Type;
            m.ReceiverId.Address = vm.Id.Address;
            m.Pgn = 51;
            m.Data = new byte[] { 0x1, 0xFF, 0xFF, 0x3, 0xFF, 0xFF, 0xFF, 0xFF };
            vm.Transmit(m.ToCanMessage());
        }

        private void Button_Click_11(object sender, RoutedEventArgs e)
        {
            OmniMessage m = new();
            m.ReceiverId.Type = vm.Id.Type;
            m.ReceiverId.Address = vm.Id.Address;
            m.Pgn = 51;
            m.Data = new byte[] { 0x1, 0xFF, 0xFF, 0x4, 0xFF, 0xFF, 0xFF, 0xFF };
            vm.Transmit(m.ToCanMessage());
        }

        private void Button_Click_12(object sender, RoutedEventArgs e)
        {
            OmniMessage m = new();
            m.ReceiverId.Type = vm.Id.Type;
            m.ReceiverId.Address = vm.Id.Address;
            m.Pgn = 51;
            m.Data = new byte[] { 0x1, 0xFF, 0xFF, 0x5, 0xFF, 0xFF, 0xFF, 0xFF };
            vm.Transmit(m.ToCanMessage());
        }

        private void SetpointChanged(object sender, RoutedPropertyChangedEventArgs<double> e)
        {
            if (vm == null) return;
            OmniMessage m = new();
            m.ReceiverId.Type = vm.Id.Type;
            m.ReceiverId.Address = vm.Id.Address;
            m.Pgn = 51;
            m.Data = new byte[] { 0x1, 0xFF, (byte)((sender as Slider).Value+75), 0xFF, 0xFF, 0xFF, 0xFF, 0xFF };
            vm.Transmit(m.ToCanMessage());
        }

        private void Button_Click_13(object sender, RoutedEventArgs e)
        {
            if (vm == null) return;
            OmniMessage m = new();
            m.ReceiverId.Type = vm.Id.Type;
            m.ReceiverId.Address = vm.Id.Address;
            m.Pgn = 1;
            m.Data = new byte[] { 0x0, 0x5, 0xFF, 0xFF, 0xFF, 0xFF, 0xFF, 0xFF };
            vm.Transmit(m.ToCanMessage());
        }
    }
}
