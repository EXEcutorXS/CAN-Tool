using OmniProtocol;
using System.Windows;
using System.Windows.Controls;

namespace CAN_Tool.CustomControls
{
    /// <summary>
    /// Логика взаимодействия для ModemControl.xaml
    /// </summary>
    public partial class ModemControl : UserControl
    {
        public ModemDeviceViewModel vm => DataContext as ModemDeviceViewModel;

        public ModemControl()
        {
            InitializeComponent();
        }

        private void OnlySmsModeClick(object sender, RoutedEventArgs e)
        {
            vm?.ToggleOnlySmsMode();
        }

        private void FaultReportClick(object sender, RoutedEventArgs e)
        {
            vm?.ToggleFaultReport();
        }

        private void CmdAckClick(object sender, RoutedEventArgs e)
        {
            vm?.ToggleCmdAck();
        }
    }
}
