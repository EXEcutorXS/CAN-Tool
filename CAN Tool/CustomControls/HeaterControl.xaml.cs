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

        //public DeviceViewModel Vm => (DeviceViewModel)DataContext;
        //private DispatcherTimer logTimer = new();

        public HeaterControl()
        {
            InitializeComponent();

        }

        private void ProgressBar_MouseWheel(object sender, MouseWheelEventArgs e)
        {

        }

        private void ProgressBar_MouseWheel_1(object sender, MouseWheelEventArgs e)
        {

        }
    }
}
