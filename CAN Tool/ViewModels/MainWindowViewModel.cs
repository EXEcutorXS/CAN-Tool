using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using CAN_Tool.ViewModels.Base;
using System.ComponentModel;
using System.Windows.Input;
using System.Windows;
using CAN_Tool.Infrastructure.Commands;
using System.IO.Ports;
using Can_Adapter;
using AdversCan;
using System.Windows.Threading;

namespace CAN_Tool.ViewModels
{
    internal class MainWindowViewModel : ViewModel
    {
        public int[] Bitrates = new int[] { 20, 50, 125, 250, 500, 800, 1000 };
        

        private CanAdapter _canAdapter;
        public CanAdapter canAdapter { get => _canAdapter;}

        public AC2P AC2PInstance;
        
        #region Title property
        /// <summary>
        /// MainWindow title
        /// </summary>
        private string _Title = "Advers CAN Tool";

        public string Title
        {
            get => _Title;
            set => Set(ref _Title, value);
        }
        #endregion

        #region SelectedPortIndex;
        private int _SelectedPortIndex = -1;
        public int SelectedPortIndex
        {
            get => _SelectedPortIndex;
            set => Set(ref _SelectedPortIndex, value);
        }
        #endregion

        #region PortList
        private BindingList<string> _PortList = new BindingList<string>();
        public BindingList<string> PortList
        {
            get => _PortList;
            set => Set(ref _PortList, value);
        }
        #endregion

        #region ApplicationCloseCommand
        public ICommand ApplicationCloseCommand;
        private void OnApplicationCloseCommandExecuted(object parameter) => Application.Current.Shutdown();
        private bool CanApplicationCloseCommandExecute(object parameter) => true;

        #endregion

        #region RefreshPortsCommand
        public ICommand RefreshPortListCommand { get; }
        private void OnRefreshPortsCommandExecuted(object Parameter)
        {
            PortList.Clear();
            foreach (var port in SerialPort.GetPortNames())
                PortList.Add(port);
        }

        #endregion

        #region OpenPortCommand
        public ICommand OpenPortCommand { get; }
        private void OnOpenPortCommandExecuted(object parameter)
        {
            canAdapter.PortName = _PortList[SelectedPortIndex];
            canAdapter.PortOpen();
        }
        private bool CanOpenPortCommandExecute(object parameter) => (_SelectedPortIndex >= 0 && _PortList[_SelectedPortIndex].StartsWith("COM") && !canAdapter.PortOpened);
        #endregion

        #region ClosePortCommand
        public ICommand ClosePortCommand { get; }
        private void OnClosePortCommandExecuted(object parameter)
        {
            canAdapter.PortClose();
        }
        private bool CanClosePortCommandExecute(object parameter) => (canAdapter.PortOpened);
        #endregion

        public MainWindowViewModel()
        {
            _canAdapter = new CanAdapter();
            AC2PInstance = new AC2P(canAdapter);
            ApplicationCloseCommand = new LambdaCommand(OnApplicationCloseCommandExecuted, CanApplicationCloseCommandExecute);
            OpenPortCommand = new LambdaCommand(OnOpenPortCommandExecuted, CanOpenPortCommandExecute);
            ClosePortCommand = new LambdaCommand(OnClosePortCommandExecuted, CanClosePortCommandExecute);
            RefreshPortListCommand = new LambdaCommand(OnRefreshPortsCommandExecuted);
        }


    }
}
