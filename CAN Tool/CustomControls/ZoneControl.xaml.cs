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

        public static readonly DependencyProperty ZoneNumberProperty;
        public int ZoneNumber
        {
            get { return (int)GetValue(ZoneNumberProperty); }
            set { SetValue(ZoneNumberProperty, value); }
        }

        static ZoneControl()
            {
            // Using a DependencyProperty as the backing store for ZoneNumber.  This enables animation, styling, binding, etc...
            ZoneNumberProperty = DependencyProperty.Register("ZoneNumber", typeof(int), typeof(ZoneControl), new PropertyMetadata(0));
    }
       

        public ZoneControl()
        {
            InitializeComponent();

            zoneHandler = (ZoneHandler)DataContext;

        }

        private void ZoneModeButtonClick(object sender, RoutedEventArgs e)
        {
            zoneHandler?.ToggleState(ZoneNumber);
        }

        private void NightSetpointChanged(object sender, RoutedPropertyChangedEventArgs<double> e)
        {
            zoneHandler?.SetNightSetpoint(ZoneNumber, (int)(sender as Slider).Value);
        }

        private void DaySetpointChanged(object sender, RoutedPropertyChangedEventArgs<double> e)
        {
            zoneHandler?.SetDaySetpoint(ZoneNumber, (int)(sender as Slider).Value);
        }
    }
}
