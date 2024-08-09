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
    public partial class GenericLoadTrippleControl : UserControl
    {

        
        public DeviceViewModel vm => (DeviceViewModel)DataContext;

        

        public GenericLoadTrippleControl()
        {
            InitializeComponent();

        }

        private void PWM1Changed(object sender, RoutedPropertyChangedEventArgs<double> e)
        {
            OmniMessage m = new();
            m.ReceiverId.Type = vm.Id.Type;
            m.ReceiverId.Address = vm.Id.Address;
            m.Pgn = 49;
            m.Data = new byte[] { 0xFF, 0xFF, 0xFF, 0xFF, 0xFF, 0xFF, 0xFF, 0xFF };
            m.Data[1] = (byte)(sender as Slider).Value;
            vm.Transmit(m.ToCanMessage());
        }

    

        private void PWM2Changed(object sender, RoutedPropertyChangedEventArgs<double> e)
        {
            OmniMessage m = new();
            m.ReceiverId.Type = vm.Id.Type;
            m.ReceiverId.Address = vm.Id.Address;
            m.Pgn = 49;
            m.Data = new byte[] { 0xFF, 0xFF, 0xFF, 0xFF, 0xFF, 0xFF, 0xFF, 0xFF };
            m.Data[2] = (byte)(sender as Slider).Value;
            vm.Transmit(m.ToCanMessage());
        }

        private void PWM3Changed(object sender, RoutedPropertyChangedEventArgs<double> e)
        {
            OmniMessage m = new();
            m.ReceiverId.Type = vm.Id.Type;
            m.ReceiverId.Address = vm.Id.Address;
            m.Pgn = 49;
            m.Data = new byte[] { 0xFF, 0xFF, 0xFF, 0xFF, 0xFF, 0xFF, 0xFF, 0xFF };
            m.Data[3] = (byte)(sender as Slider).Value;
            vm.Transmit(m.ToCanMessage());
        }

        private void Channel1ModeClick(object sender, RoutedEventArgs e)
        {
            OmniMessage m = new();
            m.ReceiverId.Type = vm.Id.Type;
            m.ReceiverId.Address = vm.Id.Address;
            m.Pgn = 49;
            m.Data = new byte[] { 0xFF, 0xFF, 0xFF, 0xFF, 0xFF, 0xFF, 0xFF, 0xFF };
            switch (vm.GenericLoadTripple.LoadMode1)
            {
                case LoadMode_t.Off: 
                    m.Data[0] = 0b11111101; break;
                case LoadMode_t.Toggle:
                    m.Data[0] = 0b11111110; break;
                case LoadMode_t.Pwm:
                    m.Data[0] = 0b11111100; break;
                default:
                    m.Data[0] = 0b11111100; break;
            }
            vm.Transmit(m.ToCanMessage());
        }

        private void Channel2ModeClick(object sender, RoutedEventArgs e)
        {
            OmniMessage m = new();
            m.ReceiverId.Type = vm.Id.Type;
            m.ReceiverId.Address = vm.Id.Address;
            m.Pgn = 49;
            m.Data = new byte[] { 0xFF, 0xFF, 0xFF, 0xFF, 0xFF, 0xFF, 0xFF, 0xFF };
            switch (vm.GenericLoadTripple.LoadMode2)
            {
                case LoadMode_t.Off:
                    m.Data[0] = 0b11110111; break;
                case LoadMode_t.Toggle:
                    m.Data[0] = 0b11111011; break;
                case LoadMode_t.Pwm:
                    m.Data[0] = 0b11110011; break;
                default:
                    m.Data[0] = 0b11110011; break;
            }
            vm.Transmit(m.ToCanMessage());
        }

        private void Channel3ModeClick(object sender, RoutedEventArgs e)
        {
            OmniMessage m = new();
            m.ReceiverId.Type = vm.Id.Type;
            m.ReceiverId.Address = vm.Id.Address;
            m.Pgn = 49;
            m.Data = new byte[] { 0xFF, 0xFF, 0xFF, 0xFF, 0xFF, 0xFF, 0xFF, 0xFF };
            switch (vm.GenericLoadTripple.LoadMode3)
            {
                case LoadMode_t.Off:
                    m.Data[0] = 0b11011111; break;
                case LoadMode_t.Toggle:
                    m.Data[0] = 0b11101111; break;
                case LoadMode_t.Pwm:
                    m.Data[0] = 0b11001111; break;
                default:
                    m.Data[0] = 0b11001111; break;
            }
            vm.Transmit(m.ToCanMessage());
        }

    }
}
