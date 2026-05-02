using CAN_Tool.ViewModels;
using System;
using System.Windows.Controls;
using System.Windows.Input;

namespace CAN_Tool.CustomControls
{
    public partial class CanModeView : UserControl
    {
        private MainWindowViewModel Vm => (MainWindowViewModel)DataContext;

        public CanModeView() => InitializeComponent();

        private void CanListDoubleClick(object sender, MouseButtonEventArgs e)
        {
            if (CanMessageList.SelectedItem != null)
                Vm.CanPage.ConstructedMessage.Update(CanMessageList.SelectedItem as CanMessage);
        }

        private void AddRandomMessageClick(object sender, System.Windows.RoutedEventArgs e)
        {
            CanMessage m = new();
            Random r = new(DateTime.Now.Millisecond);
            m.Ide = (r.Next(0, 255) % 2) == 0;
            m.Rtr = (r.Next(0, 255) % 2) == 0;
            m.Id = m.Ide ? r.Next(0, 0x1FFFFFFF) : r.Next(0, 0x7FF);
            m.Dlc = (byte)r.Next(1, 9);
            for (int i = 0; i < m.Dlc; i++)
                m.Data[i] = (byte)r.Next(0, 256);
            Vm.CanPage.MessageList.TryToAdd(m);
        }
    }
}
