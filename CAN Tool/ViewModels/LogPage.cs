using Can_Adapter;
using CAN_Tool.Infrastructure.Commands;
using LiveCharts.Wpf;
using LiveCharts;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Input;
using AdversCan;

namespace CAN_Tool.ViewModels
{
    internal class LogPage
    {

        MainWindowViewModel vm;

        public SeriesCollection SeriesCollection { get; set; }
        public string[] Labels { get; set; }
        public Func<double, string> YFormatter { get; set; }

        public MainWindowViewModel VM => vm;

        #region LogStartCommand
        public ICommand LogStartCommand { get; }
        private void OnLogStartCommandExecuted(object parameter)
        {
            VM.SelectedConnectedDevice.LogStart();
        }
        private bool CanLogStartCommandExecute(object parameter) => (VM.SelectedConnectedDevice != null && VM.CanAdapter.PortOpened);
        #endregion

        #region LogStopCommand
        public ICommand LogStopCommand { get; }
        private void OnLogStopCommandExecuted(object parameter)
        {
            VM.SelectedConnectedDevice.LogStop();
        }
        private bool CanLogStopCommandExecute(object parameter) => (VM.SelectedConnectedDevice != null && VM.CanAdapter.PortOpened && VM.SelectedConnectedDevice.IsLogWriting);
        #endregion


        #region LogClearCommand
        public ICommand LogClearCommand { get; }
        private void OnLogClearCommandExecuted(object parameter)
        {
            VM.SelectedConnectedDevice.LogClear();
        }
        private bool CanLogClearCommandExecute(object parameter) => (VM.SelectedConnectedDevice != null);
        #endregion

        public ICommand ChartDrawCommand { get; }
        public void OnChartDrawCommandExecuted(object parameter)
        {

        }
        public bool CanChartDrawCommandExecute(object parameter) => (VM.SelectedConnectedDevice != null);

        public ICommand SaveLogCommand { get; }

        private void OnSaveLogCommandExecuted(object parameter)
        {
            string path = Environment.GetFolderPath(Environment.SpecialFolder.Desktop) + "\\" + vm.SelectedConnectedDevice.ID.Type + "_" + DateTime.Now.ToString("HH-mm-ss_dd-MM-yy") + ".csv";

            using (StreamWriter sw = new StreamWriter(path))
            {

                foreach (var v in vm.SelectedConnectedDevice.Status)
                    sw.Write(AC2P.Variables[v.Id].ShortName + ";");
                sw.WriteLine();

                if (vm.SelectedConnectedDevice.Status.Count == 0) return;

                for (int i = 0; i < vm.SelectedConnectedDevice.Status[0].Log.Count; i++)
                {
                    foreach (var v in vm.SelectedConnectedDevice.Status)
                        sw.Write(v.Log[i].ToString(v.AssignedParameter.OutputFormat) + ";");
                    sw.WriteLine();
                }
                sw.Flush();
                sw.Close();

            }
        }
        public LogPage(MainWindowViewModel vm)
        {
            this.vm = vm;

            SeriesCollection = new SeriesCollection
            {
                new LineSeries
                {
                    Title = "Cool",
                    Values = new ChartValues<double> { 4, 6, 5, 2 ,4 }
                },
                new LineSeries
                {
                    Title = "Very cool",
                    Values = new ChartValues<double> { 6, 7, 3, 4 ,6 },
                    PointGeometry = null
                },
                new LineSeries
                {
                    Title = "Awesome",
                    Values = new ChartValues<double> { 4,2,7,2,7 },
                    PointGeometry = DefaultGeometries.Square,
                    PointGeometrySize = 15
                }
            };

            Labels = new[] { "1", "2", "Mar", "Apr", "May" };


            LogStartCommand = new LambdaCommand(OnLogStartCommandExecuted, CanLogStartCommandExecute);
            LogStopCommand = new LambdaCommand(OnLogStopCommandExecuted, CanLogStopCommandExecute);
            LogClearCommand = new LambdaCommand(OnLogClearCommandExecuted, CanLogClearCommandExecute);
            SaveLogCommand = new LambdaCommand(OnSaveLogCommandExecuted, vm.deviceSelected);
            ChartDrawCommand = new LambdaCommand(OnChartDrawCommandExecuted, CanChartDrawCommandExecute);

        }
    }
}
