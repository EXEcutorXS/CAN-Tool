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
    /// Логика взаимодействия для HeaterControl.xaml
    /// </summary>
    public partial class HeaterControl : UserControl
    {
        DeviceViewModel Vm => (DeviceViewModel)DataContext;

        //public DeviceViewModel Vm => (DeviceViewModel)DataContext;
        //private DispatcherTimer logTimer = new();

        public HeaterControl()
        {
            InitializeComponent();
        }

        private void FuelPumpMouseWheel(object sender, MouseWheelEventArgs e)
        {
            int newFrequency = Vm.OverrideState.FuelPumpOverridenFrequencyX100;
            if (Vm == null || !Vm.OverrideState.FuelPumpOverriden) return;
            int k = Keyboard.IsKeyDown(Key.LeftShift) ? 100 : 10;
            newFrequency += Math.Sign(e.Delta) * k;
            if (newFrequency < 0) newFrequency = 0;
            if (newFrequency > 1000) newFrequency = 1000;
            byte overrideByte1 = 0;
            byte overrideByte2 = 0;
            byte overrideStatesByte = 0;
            overrideByte1 |= 3;
            overrideByte1 |= 3 << 2;
            overrideByte1 |= 3 << 4;
            overrideByte1 |= 3 << 6;
            overrideByte2 |= 3;
            overrideStatesByte |= 3;
            overrideStatesByte |= 3 << 2;
            byte[] data = { overrideByte1, overrideByte2, overrideStatesByte, 0xFF, 0xFF, (byte)(newFrequency>>8), (byte)(newFrequency & 0xFF), 0xFF };
            OmniMessage msg = new() { Pgn = 47, ReceiverId = Vm.Id, Data = data };
            Vm.Transmit(msg.ToCanMessage());
        }

        private void GlowPlugMouseWheel(object sender, MouseWheelEventArgs e)
        {
            int newPower = Vm.OverrideState.GlowPlugOverridenPower;
            if (Vm == null || !Vm.OverrideState.GlowPlugOverriden) return;
            int k = Keyboard.IsKeyDown(Key.LeftShift) ? 10 : 1;
            newPower += Math.Sign(e.Delta) * k;
            if (newPower < 0) newPower = 0;
            if (newPower > 100) newPower = 100;
            byte overrideByte1 = 0;
            byte overrideByte2 = 0;
            byte overrideStatesByte = 0;
            overrideByte1 |= 3;
            overrideByte1 |= 3 << 2;
            overrideByte1 |= 3 << 4;
            overrideByte1 |= 3 << 6;
            overrideByte2 |= 3;
            overrideStatesByte |= 3;
            overrideStatesByte |= 3 << 2;
            byte[] data = { overrideByte1, overrideByte2, overrideStatesByte, 0xFF, (byte)newPower, 0xFF, 0xFF, 0xFF };
            OmniMessage msg = new() { Pgn = 47, ReceiverId = Vm.Id, Data = data };
            Vm.Transmit(msg.ToCanMessage());
        }


        private void SetBlowerOverrideVal(int newRevs)
        {
            if (newRevs < 0) newRevs = 0;
            if (newRevs > 200) newRevs = 200;
            byte overrideByte1 = 0;
            byte overrideByte2 = 0;
            byte overrideStatesByte = 0;
            overrideByte1 |= 3;
            overrideByte1 |= 3 << 2;
            overrideByte1 |= 3 << 4;
            overrideByte1 |= 3 << 6;
            overrideByte2 |= 3;
            overrideStatesByte |= 3;
            overrideStatesByte |= 3 << 2;
            byte[] data = { overrideByte1, overrideByte2, overrideStatesByte, (byte)newRevs, 0xFF, 0xFF, 0xFF, 0xFF };
            OmniMessage msg = new() { Pgn = 47, ReceiverId = Vm.Id, Data = data };
            Vm.Transmit(msg.ToCanMessage());
        }

        private void BlowerMouseWheel(object sender, MouseWheelEventArgs e)
        {
            int newRevs = Vm.OverrideState.BlowerOverridenRevs;

            if (Vm == null || !Vm.OverrideState.BlowerOverriden) return;
            int k = Keyboard.IsKeyDown(Key.LeftShift) ? 10 : 1;
            newRevs += Math.Sign(e.Delta) * k;
            SetBlowerOverrideVal(newRevs);
            /*
            if (newRevs < 0) newRevs = 0;
            if (newRevs > 200) newRevs = 200;
            byte overrideByte1 = 0;
            byte overrideByte2 = 0;
            byte overrideStatesByte = 0;
            overrideByte1 |= 3;
            overrideByte1 |= 3 << 2;
            overrideByte1 |= 3 << 4;
            overrideByte1 |= 3 << 6;
            overrideByte2 |= 3;
            overrideStatesByte |= 3;
            overrideStatesByte |= 3 << 2;
            byte[] data = { overrideByte1, overrideByte2, overrideStatesByte, (byte)newRevs, 0xFF, 0xFF, 0xFF, 0xFF };
            OmniMessage msg = new() { Pgn = 47, ReceiverId = Vm.Id, Data = data };
            Vm.Transmit(msg.ToCanMessage());
            */
        }

        private void BlowerOverrideClick(object sender, RoutedEventArgs e)
        {
            byte overrideByte1 = 0;
            byte overrideByte2 = 0;
            byte overrideStatesByte = 0;
            overrideByte1 |= 3;
            overrideByte1 |= 3 << 2;
            overrideByte1 |= 3 << 4;
            overrideByte1 |= 3 << 6;
            if (!Vm.OverrideState.BlowerOverriden) overrideByte2 |= 1;
            overrideStatesByte |= 3;
            overrideStatesByte |= 3 << 2;
            byte[] data = { overrideByte1, overrideByte2, overrideStatesByte, (byte)Vm.OverrideState.BlowerOverridenRevs, 0xFF, 0xFF, 0xFF, 0xFF };
            OmniMessage msg = new() { Pgn = 47, ReceiverId = Vm.Id, Data = data };
            Vm.Transmit(msg.ToCanMessage());
        }

        private void FuelPumpClick(object sender, RoutedEventArgs e)
        {
            byte overrideByte1 = 0;
            byte overrideByte2 = 0;
            byte overrideStatesByte = 0;
            if (!Vm.OverrideState.FuelPumpOverriden) overrideByte1 |= 1;
            overrideByte1 |= 3 << 2;
            overrideByte1 |= 3 << 4;
            overrideByte1 |= 3 << 6;
            overrideByte2 |= 3;
            overrideStatesByte |= 3;
            overrideStatesByte |= 3 << 2;
            byte[] data = { overrideByte1, overrideByte2, overrideStatesByte, 0xFF, 0xFF, 0xFF, 0xFF, 0xFF };
            OmniMessage msg = new() { Pgn = 47, ReceiverId = Vm.Id, Data = data };
            Vm.Transmit(msg.ToCanMessage());
        }

        private void GlowPlugClick(object sender, RoutedEventArgs e)
        {

            byte overrideByte1 = 0;
            byte overrideByte2 = 0;
            byte overrideStatesByte = 0;
            overrideByte1 |= 3;
            overrideByte1 |= 3 << 2;
            if (!Vm.OverrideState.GlowPlugOverriden) overrideByte1 |= 1 << 4;
            overrideByte1 |= 3 << 6;
            overrideByte2 |= 3;
            overrideStatesByte |= 3;
            overrideStatesByte |= 3 << 2;
            byte[] data = { overrideByte1, overrideByte2, overrideStatesByte, 0xFF, 0xFF, 0xFF, 0xFF, 0xFF };
            OmniMessage msg = new() { Pgn = 47, ReceiverId = Vm.Id, Data = data };
            Vm.Transmit(msg.ToCanMessage());

        }

        private void ReduceOverridenRevsButtonClick(object sender, RoutedEventArgs e)
        {
            SetBlowerOverrideVal(Keyboard.IsKeyDown(Key.LeftShift)? Vm.OverrideState.BlowerOverridenRevs-10: Vm.OverrideState.BlowerOverridenRevs-1);
        }

        private void IncreaseOverridenRevsButtonClick(object sender, RoutedEventArgs e)
        {
            SetBlowerOverrideVal(Keyboard.IsKeyDown(Key.LeftShift) ? Vm.OverrideState.BlowerOverridenRevs + 10 : Vm.OverrideState.BlowerOverridenRevs + 1);
        }
    }
}
