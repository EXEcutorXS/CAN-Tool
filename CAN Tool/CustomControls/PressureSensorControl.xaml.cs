using CAN_Tool.ViewModels;
using OmniProtocol;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading;
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
    /// Логика взаимодействия для PressureSensorControl.xaml
    /// </summary>
    public partial class PressureSensorControl : UserControl
    {

        DispatcherTimer RenderTimer = new DispatcherTimer();

        public DeviceViewModel vm => (DeviceViewModel)DataContext;

        double[] dataToShow = new double[2000];

        int arraySize = 100;

        double maximum;
        double minimum;
        double amplitude;
        double average;
        double median;

        public PressureSensorControl()
        {
            InitializeComponent();
            RenderTimer.Interval = new TimeSpan(250000);
            RenderTimer.Tick += Render;
            PressurePlot.Plot.AddDataStreamer(dataToShow);
        }

        public static double GetMedian(double[] arrSource)
        {
            // Check if the array has values        
            if (arrSource == null || arrSource.Length == 0)
                throw new ArgumentException("Array is empty.");

            // Sort the array
            double[] arrSorted = (double[])arrSource.Clone();
            Array.Sort(arrSorted);

            // Calculate the median
            int size = arrSorted.Length;
            int mid = size / 2;

            if (size % 2 != 0)
                return arrSorted[mid];

            dynamic value1 = arrSorted[mid];
            dynamic value2 = arrSorted[mid - 1];
            return (value1 + value2) / 2;
        }

        private void Render(object sender, EventArgs e)
        {
            if (RenderTimer.IsEnabled)
            {
                for (int i = 0; i < arraySize; i++)
                    if ((vm.PressureLogPointer - i) > 0)
                        dataToShow[arraySize - i - 1] = vm.PressureLog[vm.PressureLogPointer - i-1];
                PressurePlot.Plot.SetAxisLimitsX(0, arraySize);
                var arr = dataToShow.Take(arraySize);
                minimum = arr.Min();
                maximum = arr.Max();
                amplitude = maximum - minimum;
                average = arr.Average();
                median = GetMedian(arr.ToArray());
                minimumPregress.Value = minimum;
                maximumPregress.Value = maximum;
                averageProgress.Value = average;
                medianProgress.Value = median;
                amplitudeProgress.Value = amplitude;
                minimumTextBlock.Text = minimum.ToString("F2");
                maximumTextBlock.Text = maximum.ToString("F2");
                averageTextBlock.Text = average.ToString("F2");
                medianTextBlock.Text = median.ToString("F2");
                amplitudeTextBlock.Text = amplitude.ToString("F2");
                if (minimum<maximum)
                    PressurePlot.Plot.SetAxisLimitsY(minimum, maximum );
                PressurePlot.Refresh();
            }
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
            var path = System.IO.Path.GetDirectoryName(System.Diagnostics.Process.GetCurrentProcess().MainModule.FileName) + "\\" + "PressureLog" + "_" + DateTime.Now.ToString("HH-mm-ss_dd-MM-yy") + ".txt";

            using (var sw = new StreamWriter(path))
            {
                for (var i = 0; i < vm.PressureLogPointer; i++)
                {
                    sw.Write($"{vm.PressureLog[i]:F3}" + Environment.NewLine);
                }
                sw.Flush();
                sw.Close();
            }
        }

        private void CheckBox_Checked(object sender, RoutedEventArgs e)
        {
            vm.PressureLogWriting = true;
            RenderTimer.IsEnabled = true;
        }

        private void CheckBox_Unchecked(object sender, RoutedEventArgs e)
        {
            RenderTimer.IsEnabled = false;
        }

        private void Slider_ValueChanged(object sender, RoutedPropertyChangedEventArgs<double> e)
        {
            arraySize = (int)Math.Round((sender as Slider).Value);
            if (SizeTextBlock != null)
                SizeTextBlock.Text = arraySize.ToString();
            if (PressurePlot != null)
                PressurePlot.Plot.SetAxisLimitsX(0, arraySize);
        }
    }
}
