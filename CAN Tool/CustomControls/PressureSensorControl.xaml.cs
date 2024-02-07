using CAN_Tool.ViewModels;
using OmniProtocol;
using System;
using System.Collections.Generic;
using System.IO;
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

namespace CAN_Tool.CustomControls
{
    /// <summary>
    /// Логика взаимодействия для PressureSensorControl.xaml
    /// </summary>
    public partial class PressureSensorControl : UserControl
    {
        public DeviceViewModel vm => (DeviceViewModel)DataContext;

        public PressureSensorControl()
        {
            InitializeComponent();
        }

        private void Button_Click(object sender, RoutedEventArgs e)
        {
            vm.PressureLogWriting = true;
            vm.PressureLogPointer = 0;
            
        }

        private void Button_Click_1(object sender, RoutedEventArgs e)
        {
            vm.PressureLogWriting = false;
        }

        private void Button_Click_2(object sender, RoutedEventArgs e)
        {
            //var path = Environment.GetFolderPath(Environment.SpecialFolder.Desktop) + "\\" + "PressureLog" + "_" + DateTime.Now.ToString("HH-mm-ss_dd-MM-yy") + ".txt";
            var path = System.IO.Path.GetDirectoryName(System.Diagnostics.Process.GetCurrentProcess().MainModule.FileName) + "\\" + "PressureLog" + "_" + DateTime.Now.ToString("HH-mm-ss_dd-MM-yy") + ".txt";

            using (var sw = new StreamWriter(path))
            {
                for (var i = 0; i < vm.PressureLogPointer; i++)
                {
                    sw.Write($"{vm.PressureLog[i]:F3}"+Environment.NewLine);
                }
                sw.Flush();
                sw.Close();
            }
        }

        private void Button_Click_3(object sender, RoutedEventArgs e)
        {
            float[] renderArray = vm.PressureLog.Take(vm.PressureLogPointer).ToArray();
            PressurePlot.Plot.Clear();
            PressurePlot.Plot.AddSignal(renderArray);
            PressurePlot.Plot.Render();
            PressurePlot.Refresh();
        }
    }
}
