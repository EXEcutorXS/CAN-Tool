using OmniProtocol;
using System;
using System.Diagnostics;
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

        // "getlink"-ссылка модема (STRID_CONNECTION_LINK) - открываем в браузере по умолчанию.
        // UseShellExecute=true обязателен в .NET (Core) - без него Process.Start пытается
        // запустить url как исполняемый файл вместо передачи его системному обработчику протокола.
        private void ConnectionLink_Click(object sender, RoutedEventArgs e)
        {
            var url = vm?.ModemParams.ConnectionLink;
            if (string.IsNullOrWhiteSpace(url)) return;
            try
            {
                Process.Start(new ProcessStartInfo(url) { UseShellExecute = true });
            }
            catch (Exception ex)
            {
                MessageBox.Show(ex.Message);
            }
        }
    }
}
