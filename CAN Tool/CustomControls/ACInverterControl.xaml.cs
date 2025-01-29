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
    public partial class AcInverterControl : UserControl
    {
       
        public DeviceViewModel vm => (DeviceViewModel)DataContext;

        public AcInverterControl()
        {
            InitializeComponent();
        }

        private void CondPwmChanged(object sender, RoutedPropertyChangedEventArgs<double> e)
        {
            OmniMessage m = new();
            m.ReceiverId.Type = vm.Id.Type;
            m.ReceiverId.Address = vm.Id.Address;
            m.Pgn = 50;
            m.Data = new byte[] { 1, 0xFF, 0xFF, 0xFF, 0xFF, 0xFF, 0xFF, 0xFF };
            m.Data[3] = (byte)((uint)((sender as Slider).Value * 100) >> 8);
            m.Data[4] = (byte)((uint)((sender as Slider).Value * 100));
            vm.Transmit(m.ToCanMessage());
        }

        private void CompressorRevsChanged(object sender, RoutedPropertyChangedEventArgs<double> e)
        {
            OmniMessage m = new();
            m.ReceiverId.Type = vm.Id.Type;
            m.ReceiverId.Address = vm.Id.Address;
            m.Pgn = 50;
            m.Data = new byte[] { 0x1, 0xFF, 0xFF, 0xFF, 0xFF, 0xFF, 0xFF, 0xFF };
            m.Data[1] = (byte)Math.Round((sender as Slider).Value);
            vm.Transmit(m.ToCanMessage());
        }
    }
}
