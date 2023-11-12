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
    /// Логика взаимодействия для TimberlineRvcControl.xaml
    /// </summary>
    public partial class TimberlineRvcControl : UserControl
    {

        public Timberline20Handler vm;

        public TimberlineRvcControl()
        {
            InitializeComponent();
            vm = (Timberline20Handler)DataContext;
        }

        private void ToggleHeaterClick(object sender, RoutedEventArgs e)
        {
            vm.ToggleHeater();
            e.Handled = true;
        }
    }
}
