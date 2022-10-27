using Can_Adapter;
using CAN_Tool.Infrastructure.Commands;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Input;

namespace CAN_Tool.ViewModels
{
    internal class LogPage
    {

        MainWindowViewModel vm;

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

        #region ChartDrawCommand
        public ICommand ChartDrawCommand { get; }
        public void OnChartDrawCommandExecuted(object parameter)
        {

        }
        public bool CanChartDrawCommandExecute(object parameter) => (VM.SelectedConnectedDevice != null && VM.SelectedConnectedDevice.Log.Count > 0);
        #endregion
        public LogPage(MainWindowViewModel vm)
        {
            this.vm = vm;

            LogStartCommand = new LambdaCommand(OnLogStartCommandExecuted, CanLogStartCommandExecute);
            LogStopCommand = new LambdaCommand(OnLogStopCommandExecuted, CanLogStopCommandExecute);
            ChartDrawCommand = new LambdaCommand(OnChartDrawCommandExecuted, CanChartDrawCommandExecute);
        }
    }
}
