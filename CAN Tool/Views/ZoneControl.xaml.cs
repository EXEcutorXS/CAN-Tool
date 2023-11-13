using CAN_Tool.ViewModels;
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

namespace CAN_Tool.Views
{
    /// <summary>
    /// Логика взаимодействия для ZoneControl.xaml
    /// </summary>
    public partial class ZoneControl : UserControl
    {
        ZoneHandler zoneHandler;
        
        public ZoneControl()
        {
            zoneHandler = (DataContext as ZoneHandler);
            InitializeComponent();

        }

        private void ZoneModeButtonClick(object sender, RoutedEventArgs e)
        {
            zoneHandler.ToggleState();
        }

        private void DaySetpointChanged(object sender, RoutedPropertyChangedEventArgs<double> e)
        {

        }

        private void Slider_MouseWheel(object sender, MouseWheelEventArgs e)
        {

        }
    }
}
