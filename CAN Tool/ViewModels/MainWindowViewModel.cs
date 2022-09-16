﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using CAN_Tool.ViewModels.Base;
using System.ComponentModel;
using System.Windows.Input;
using CAN_Tool.Infrastructure.Commands;
using System.IO.Ports;
using Can_Adapter;
using AdversCan;
using ScottPlot;
using System.Windows.Media;
using ScottPlot.Renderable;

namespace CAN_Tool.ViewModels
{
    internal class MainWindowViewModel : ViewModel
    {

        int[] _Bitrates => new int[] { 20, 50, 125, 250, 500, 800, 1000 };

        public int ManualAirBlower { set; get; }
        public int ManualFuelPump { set; get; }
        public int ManualGlowPlug { set; get; }
        public bool ManualPump { set; get; }

        public bool AutoRedraw { set; get; } = true;
        public int[] Bitrates => _Bitrates;

        CanAdapter _canAdapter;
        public CanAdapter canAdapter { get => _canAdapter; }

        AC2P _AC2PInstance;
        public AC2P AC2PInstance => _AC2PInstance;

        ConnectedDevice _connectedDevice;
        public ConnectedDevice SelectedConnectedDevice
        {
            set =>
                Set(ref _connectedDevice, value);
            get => _connectedDevice;
        }

        public Dictionary<CommandId, AC2PCommand> Commands => AC2P.commands;
        #region SelectedMessage
        private AC2PMessage selectedMessage;

        public WpfPlot myChart;
        public AC2PMessage SelectedMessage
        {
            get => selectedMessage;
            set => Set(ref selectedMessage, value);
        }
        #endregion

        #region  CustomMessage

        AC2PMessage customMessage = new AC2PMessage();

        public Dictionary<CommandId, AC2PCommand> CommandList { get; } = AC2P.commands;

        public AC2PParameter SelectedParameter { set; get; } = new();
        public AC2PMessage CustomMessage { get => customMessage; set => customMessage.Update(value); }

        public double[] CommandParametersArray;


        #endregion

        #region ErrorString
        private string error;

        public string Error
        {
            get { return error; }
            set { Set(ref error, value); }
        }
        #endregion

        #region PortName;
        private string portName = "";
        public string PortName
        {
            get => portName;
            set => Set(ref portName, value);
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

        #region Commands

        #region CanAdapterCommands

        #region SetAdapterNormalModeCommand

        public ICommand SetAdapterNormalModeCommand { get; }

        private void OnSetAdapterNormalModeCommandExecuted(object Parameter) => canAdapter.StartNormal();
        private bool CanSetAdapterNormalModeCommandExecute(object Parameter) => canAdapter.PortOpened;
        #endregion

        #region SetAdapterListedModeCommand

        public ICommand SetAdapterListedModeCommand { get; }

        private void OnSetAdapterListedModeCommandExecuted(object Parameter) => canAdapter.StartListen();
        private bool CanSetAdapterListedModeCommandExecute(object Parameter) => canAdapter.PortOpened;
        #endregion

        #region SetAdapterSelfReceptionModeCommand

        public ICommand SetAdapterSelfReceptionModeCommand { get; }

        private void OnSetAdapterSelfReceptionModeCommandExecuted(object Parameter) => canAdapter.StartSelfReception();
        private bool CanSetAdapterSelfReceptionModeCommandExecute(object Parameter) => canAdapter.PortOpened;
        #endregion

        #region StopCanAdapterCommand

        public ICommand StopCanAdapterCommand { get; }

        private void OnStopCanAdapterCommandExecuted(object Parameter) => canAdapter.Stop();
        private bool CanStopCanAdapterCommandExecute(object Parameter) => canAdapter.PortOpened;
        #endregion

        #region RefreshPortsCommand
        public ICommand RefreshPortListCommand { get; }
        private void OnRefreshPortsCommandExecuted(object Parameter)
        {
            PortList.Clear();
            foreach (var port in SerialPort.GetPortNames())
                PortList.Add(port);
            if (PortList.Count > 0)
                PortName = PortList[0];
        }

        #endregion

        #region OpenPortCommand
        public ICommand OpenPortCommand { get; }
        private void OnOpenPortCommandExecuted(object parameter)
        {
            canAdapter.PortName = PortName;
            canAdapter.PortOpen();
        }
        private bool CanOpenPortCommandExecute(object parameter) => (PortName.StartsWith("COM") && !canAdapter.PortOpened);
        #endregion

        #region ClosePortCommand
        public ICommand ClosePortCommand { get; }
        private void OnClosePortCommandExecuted(object parameter)
        {
            canAdapter.PortClose();
        }
        private bool CanClosePortCommandExecute(object parameter) => (canAdapter.PortOpened);
        #endregion
        #endregion

        #region CloseApplicationCommand
        public ICommand CloseApplicationCommand { get; }
        private void OnCloseApplicationCommandExecuted(object parameter)
        {
            App.Current.Shutdown();
        }
        private bool CanCloseApplicationCommandExecute(object parameter) => true;
        #endregion

        #region HeaterCommands

        #region StartHeaterCommand
        public ICommand StartHeaterCommand { get; }
        private void OnStartHeaterCommandExecuted(object parameter)
        {
            executeCommand(1, 0xff, 0xff);
        }
        private bool CanStartHeaterCommandExecute(object parameter) => canAdapter.PortOpened && SelectedConnectedDevice != null;
        #endregion

        #region StopHeaterCommand
        public ICommand StopHeaterCommand { get; }
        private void OnStopHeaterCommandExecuted(object parameter)
        {
            executeCommand(3);
        }
        private bool CanStopHeaterCommandExecute(object parameter) => canAdapter.PortOpened && SelectedConnectedDevice != null;
        #endregion

        #region StartPumpCommand
        public ICommand StartPumpCommand { get; }
        private void OnStartPumpCommandExecuted(object parameter)
        {
            executeCommand(4, 0, 0);
        }
        private bool CanStartPumpCommandExecute(object parameter) => canAdapter.PortOpened && SelectedConnectedDevice != null;
        #endregion

        #region ClearErrorsCommand
        public ICommand ClearErrorsCommand { get; }
        private void OnClearErrorsCommandExecuted(object parameter)
        {
            executeCommand(5);
        }
        private bool CanClearErrorsCommandExecute(object parameter) => canAdapter.PortOpened && SelectedConnectedDevice != null;
        #endregion

        #region StartVentCommand
        public ICommand StartVentCommand { get; }
        private void OnStartVentCommandExecuted(object parameter)
        {
            executeCommand(10);
        }
        private bool CanStartVentCommandExecute(object parameter) => canAdapter.PortOpened && SelectedConnectedDevice != null;
        #endregion

        #region CalibrateTermocouplesCommand

        public ICommand CalibrateTermocouplesCommand { get; }
        private void OnCalibrateTermocouplesCommandExecuted(object parameter)
        {
            executeCommand(20);
        }


        private bool CanCalibrateTermocouplesCommandExecute(object parameter) => canAdapter.PortOpened && SelectedConnectedDevice != null;
        #endregion

        #endregion

        #region LogCommands

        #region LogStartCommand
        public ICommand LogStartCommand { get; }
        private void OnLogStartCommandExecuted(object parameter)
        {
            SelectedConnectedDevice.LogStart();
        }
        private bool CanLogStartCommandExecute(object parameter) => (SelectedConnectedDevice != null && canAdapter.PortOpened);
        #endregion

        #region LogStopCommand
        public ICommand LogStopCommand { get; }
        private void OnLogStopCommandExecuted(object parameter)
        {
            SelectedConnectedDevice.LogStop();
        }
        private bool CanLogStopCommandExecute(object parameter) => (SelectedConnectedDevice != null && canAdapter.PortOpened && SelectedConnectedDevice.IsLogWriting);
        #endregion

        #region ChartDrawCommand
        public ICommand ChartDrawCommand { get; }
        private void OnChartDrawCommandExecuted(object parameter)
        {
            Plot plt = myChart.Plot;

            plt.Clear();

            foreach (var v in SelectedConnectedDevice.Status)
                if (v.Display)
                {
                    ArraySegment<Double> dataToDisplay = new ArraySegment<Double>(SelectedConnectedDevice.LogData[v.Id], 0, SelectedConnectedDevice.LogCurrentPos);
                    var sig = plt.AddSignal(dataToDisplay.ToArray(), color: v.Color, label: v.Name);

                    plt.Style(Style.Gray1);
                    plt.Legend();
                }

            plt.Palette = Palette.OneHalfDark;

            myChart.Refresh();

        }
        private bool CanChartDrawCommandExecute(object parameter) => (SelectedConnectedDevice != null && SelectedConnectedDevice.LogCurrentPos > 0);
        #endregion

        #endregion

        #region CancelOperationCommand
        public ICommand CancelOperationCommand { get; }
        private void OnCancelOperationCommandExecuted(object parameter)
        {
            AC2PInstance.CurrentTask.onCancel();
        }
        private bool CanCancelOperationCommandExecute(object parameter) => (AC2PInstance.CurrentTask.Occupied);
        #endregion

        #region ConfigCommands
        #region ReadConfigCommand
        public ICommand ReadConfigCommand { get; }
        private void OnReadConfigCommandExecuted(object parameter)
        {
            AC2PInstance.ReadAllParameters(_connectedDevice.ID);
        }
        private bool CanReadConfigCommandExecute(object parameter) =>
            (canAdapter.PortOpened && SelectedConnectedDevice != null && !AC2PInstance.CurrentTask.Occupied);
        #endregion

        #region SaveConfigCommand
        public ICommand SaveConfigCommand { get; }
        private void OnSaveConfigCommandExecuted(object parameter)
        {
            AC2PInstance.SaveParameters(_connectedDevice.ID);
        }
        private bool CanSaveConfigCommandExecute(object parameter) =>
            (canAdapter.PortOpened && SelectedConnectedDevice != null && !AC2PInstance.CurrentTask.Occupied && SelectedConnectedDevice.readedParameters.Count > 0);
        #endregion

        #region ResetConfigCommand
        public ICommand ResetConfigCommand { get; }
        private void OnResetConfigCommandExecuted(object parameter)
        {
            AC2PInstance.ResetParameters(_connectedDevice.ID);
        }
        private bool CanResetConfigCommandExecute(object parameter) =>
            (canAdapter.PortOpened && SelectedConnectedDevice != null && !AC2PInstance.CurrentTask.Occupied);
        #endregion
        #endregion

        #region BlackBoxCommands
        #region ReadBlackBoxDataCommand
        public ICommand ReadBlackBoxDataCommand { get; }
        private void OnReadBlackBoxDataCommandExecuted(object parameter)
        {
            AC2PInstance.ReadBlackBoxData(_connectedDevice.ID);
        }
        private bool CanReadBlackBoxDataExecute(object parameter) =>
            (canAdapter.PortOpened && SelectedConnectedDevice != null && !AC2PInstance.CurrentTask.Occupied);
        #endregion

        #region ReadBlackBoxErrorsCommand
        public ICommand ReadBlackBoxErrorsCommand { get; }
        private void OnReadBlackBoxErrorsCommandExecuted(object parameter)
        {
            Task.Run(() => AC2PInstance.ReadErrorsBlackBox(_connectedDevice.ID));
        }
        private bool CanReadBlackBoxErrorsExecute(object parameter) =>
            (canAdapter.PortOpened && SelectedConnectedDevice != null && !AC2PInstance.CurrentTask.Occupied);
        #endregion

        #region EraseBlackBoxErrorsCommand
        public ICommand EraseBlackBoxErrorsCommand { get; }
        private void OnEraseBlackBoxErrorsCommandExecuted(object parameter)
        {
            Task.Run(() => AC2PInstance.EraseErrorsBlackBox(_connectedDevice.ID));
        }
        private bool CanEraseBlackBoxErrorsExecute(object parameter) =>
            (canAdapter.PortOpened && SelectedConnectedDevice != null && !AC2PInstance.CurrentTask.Occupied);
        #endregion

        #region EraseBlackBoxDataCommand
        public ICommand EraseBlackBoxDataCommand { get; }
        private void OnEraseBlackBoxDataCommandExecuted(object parameter)
        {
            Task.Run(() => AC2PInstance.EraseErrorsBlackBox(_connectedDevice.ID));
        }
        private bool CanEraseBlackBoxDataExecute(object parameter) =>
            (canAdapter.PortOpened && SelectedConnectedDevice != null && !AC2PInstance.CurrentTask.Occupied);
        #endregion
        #endregion

        #region SendCustomMessageCommand
        public ICommand SendCustomMessageCommand { get; }
        private void OnSendCustomMessageCommandExecuted(object parameter)
        {
            CustomMessage.TransmitterId = new DeviceId(126, 6);
            CustomMessage.ReceiverId = SelectedConnectedDevice.ID;
            AC2PInstance.SendMessage(CustomMessage);
        }
        private bool CanSendCustomMessageCommandExecute(object parameter)
        {
            if (!canAdapter.PortOpened || SelectedConnectedDevice == null) return false;
            return true;
        }

        #endregion

        #region EnterManualModeCommand
        public ICommand EnterManualModeCommand { get; }
        private void OnEnterManualModeCommandExecuted(object parameter)
        {
            executeCommand(67, new byte[] { 1, 0, 0, 0, 0, 0 });
        }
        private bool CanEnterManualModeCommandExecute(object parameter)
        {
            if (!canAdapter.PortOpened || SelectedConnectedDevice == null) return false;
            return true;
        }

        #endregion



        #region ExitManualModeCommand
        public ICommand ExitManualModeCommand { get; }
        private void OnExitManualModeCommandExecuted(object parameter)
        {
            executeCommand(67, new byte[] { 0, 0, 0, 0, 0, 0 });
        }
        private bool CanExitManualModeCommandExecute(object parameter)
        {
            if (!canAdapter.PortOpened || SelectedConnectedDevice == null) return false;
            return true;
        }

        #endregion

        #region IncreaceManualAirBlower
        public ICommand IncreaceManualAirBlowerCommand { get; }
        private void OnIncreaceManualAirBlowerCommandExecuted(object parameter)
        {
            if (ManualAirBlower < 100)
                ManualAirBlower++;
        }
        private bool CanIncreaceManualAirBlowerExecute(object parameter)
        {
            if (!canAdapter.PortOpened || SelectedConnectedDevice == null) return false;
            return true;
        }
        #endregion
        private void executeCommand(byte num, params byte[] data)
        {
            CustomMessage.TransmitterType = 126;
            CustomMessage.TransmitterAddress = 6;
            CustomMessage.ReceiverId = SelectedConnectedDevice.ID;
            CustomMessage.PGN = 1;
            CustomMessage.Data = new byte[8];
            for (int i = 0; i < data.Length; i++)
                customMessage.Data[i + 2] = data[i];
            CustomMessage.Data[1] = num;
            canAdapter.Transmit(customMessage);
        }

        #region Chart

        private void TimerTick(object sender, EventArgs e)
        {
            foreach (var d in AC2PInstance.ConnectedDevices)
            {
                d.LogTick();
            }

            if (AutoRedraw)
                if (CanChartDrawCommandExecute(null))
                    OnChartDrawCommandExecuted(null);

        }

        #endregion
        #endregion

        public void NewDeviceHandler(object sender, EventArgs e)
        {
            if (SelectedConnectedDevice == null || SelectedConnectedDevice.ID.Type == 126) //Котлы имеют приоритет над HCU в этом плане...
                SelectedConnectedDevice = AC2PInstance.ConnectedDevices[0];
        }
        public MainWindowViewModel()
        {

            _canAdapter = new CanAdapter();
            _AC2PInstance = new AC2P(canAdapter);

            System.Windows.Threading.DispatcherTimer timer = new System.Windows.Threading.DispatcherTimer();

            timer.Tick += new EventHandler(TimerTick);
            timer.Interval = new TimeSpan(0, 0, 1);
            timer.Start();

            AC2PInstance.NewDeviveAquired += NewDeviceHandler;


            OpenPortCommand = new LambdaCommand(OnOpenPortCommandExecuted, CanOpenPortCommandExecute);
            ClosePortCommand = new LambdaCommand(OnClosePortCommandExecuted, CanClosePortCommandExecute);
            RefreshPortListCommand = new LambdaCommand(OnRefreshPortsCommandExecuted);
            ReadConfigCommand = new LambdaCommand(OnReadConfigCommandExecuted, CanReadConfigCommandExecute);
            ReadBlackBoxDataCommand = new LambdaCommand(OnReadBlackBoxDataCommandExecuted, CanReadBlackBoxDataExecute);
            ReadBlackBoxErrorsCommand = new LambdaCommand(OnReadBlackBoxErrorsCommandExecuted, CanReadBlackBoxErrorsExecute);
            EraseBlackBoxErrorsCommand = new LambdaCommand(OnEraseBlackBoxErrorsCommandExecuted, CanEraseBlackBoxErrorsExecute);
            EraseBlackBoxDataCommand = new LambdaCommand(OnEraseBlackBoxDataCommandExecuted, CanEraseBlackBoxDataExecute);
            SendCustomMessageCommand = new LambdaCommand(OnSendCustomMessageCommandExecuted, CanSendCustomMessageCommandExecute);
            CancelOperationCommand = new LambdaCommand(OnCancelOperationCommandExecuted, CanCancelOperationCommandExecute);
            SaveConfigCommand = new LambdaCommand(OnSaveConfigCommandExecuted, CanSaveConfigCommandExecute);
            ResetConfigCommand = new LambdaCommand(OnResetConfigCommandExecuted, CanResetConfigCommandExecute);
            SetAdapterNormalModeCommand = new LambdaCommand(OnSetAdapterNormalModeCommandExecuted, CanSetAdapterNormalModeCommandExecute);
            SetAdapterListedModeCommand = new LambdaCommand(OnSetAdapterListedModeCommandExecuted, CanSetAdapterListedModeCommandExecute);
            SetAdapterSelfReceptionModeCommand = new LambdaCommand(OnSetAdapterSelfReceptionModeCommandExecuted, CanSetAdapterSelfReceptionModeCommandExecute);
            StopCanAdapterCommand = new LambdaCommand(OnStopCanAdapterCommandExecuted, CanStopCanAdapterCommandExecute);
            CloseApplicationCommand = new LambdaCommand(OnCloseApplicationCommandExecuted, CanCloseApplicationCommandExecute);
            StartHeaterCommand = new LambdaCommand(OnStartHeaterCommandExecuted, CanStartHeaterCommandExecute);
            StopHeaterCommand = new LambdaCommand(OnStopHeaterCommandExecuted, CanStopHeaterCommandExecute);
            StartPumpCommand = new LambdaCommand(OnStartPumpCommandExecuted, CanStartPumpCommandExecute);
            StartVentCommand = new LambdaCommand(OnStartVentCommandExecuted, CanStartVentCommandExecute);
            ClearErrorsCommand = new LambdaCommand(OnClearErrorsCommandExecuted, CanClearErrorsCommandExecute);
            CalibrateTermocouplesCommand = new LambdaCommand(OnCalibrateTermocouplesCommandExecuted, CanCalibrateTermocouplesCommandExecute);
            LogStartCommand = new LambdaCommand(OnLogStartCommandExecuted, CanLogStartCommandExecute);
            LogStopCommand = new LambdaCommand(OnLogStopCommandExecuted, CanLogStopCommandExecute);
            ChartDrawCommand = new LambdaCommand(OnChartDrawCommandExecuted, CanChartDrawCommandExecute);
            CustomMessage.TransmitterAddress = 6;
            CustomMessage.TransmitterType = 126;

        }
    }
}
