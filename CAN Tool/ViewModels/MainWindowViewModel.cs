using System;
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
using System.IO;
using System.Threading;
using System.Security.Principal;
using System.Windows.Media;
using LiveCharts.Wpf;
using LiveCharts;
using System.Windows.Controls;

namespace CAN_Tool.ViewModels
{

    internal class MainWindowViewModel : ViewModel
    {
        private FirmwarePage firmwarePage;
        public FirmwarePage FirmwarePage { get => firmwarePage; set => Set(ref firmwarePage, value); }

        private ManualControlPage manualControlPage;
        public ManualControlPage ManualControlPage { get => manualControlPage; set => Set(ref manualControlPage, value); }

        private LogPage logPage;
        public LogPage LogPage { get => logPage; set => Set(ref logPage, value); }
        public int[] Bitrates => new int[] { 20, 50, 125, 250, 500, 800, 1000 };

        List<Brush> brushes = new() {
        new SolidColorBrush(Colors.Red),
        new SolidColorBrush(Colors.DeepPink),
        new SolidColorBrush(Colors.MediumPurple),
        new SolidColorBrush(Colors.BlueViolet),
        new SolidColorBrush(Colors.DarkSlateBlue),
        new SolidColorBrush(Colors.PowderBlue),
        new SolidColorBrush(Colors.LightSkyBlue),
        new SolidColorBrush(Colors.Cyan),
        new SolidColorBrush(Colors.Teal),
        new SolidColorBrush(Colors.Green),
        new SolidColorBrush(Colors.LightGreen),
        new SolidColorBrush(Colors.YellowGreen),
        new SolidColorBrush(Colors.Yellow),
        new SolidColorBrush(Colors.Gold),
        new SolidColorBrush(Colors.Orange),
        new SolidColorBrush(Colors.OrangeRed),
        new SolidColorBrush(Colors.Peru),
        new SolidColorBrush(Colors.Gray),
        new SolidColorBrush(Colors.SlateGray) 
        };
        List<Brush> Brushes => brushes;

        public bool AutoRedraw { set; get; } = true;


        CanAdapter _canAdapter;
        public CanAdapter CanAdapter { get => _canAdapter; }

        AC2P _AC2PInstance;
        public AC2P AC2PInstance => _AC2PInstance;

        ConnectedDevice selectedConnectedDevice;
        public ConnectedDevice SelectedConnectedDevice
        {
            set => Set(ref selectedConnectedDevice, value);
            get => selectedConnectedDevice;
        }

        public Dictionary<int, AC2PCommand> Commands => AC2P.commands;

        #region SelectedMessage

        private AC2PMessage selectedMessage;

        public AC2PMessage SelectedMessage
        {
            get => selectedMessage;
            set => Set(ref selectedMessage, value);
        }
        #endregion

        #region  CustomMessage

        AC2PMessage customMessage = new AC2PMessage();

        public Dictionary<int, AC2PCommand> CommandList { get; } = AC2P.commands;

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

        private void OnSetAdapterNormalModeCommandExecuted(object Parameter) => CanAdapter.StartNormal();
        private bool CanSetAdapterNormalModeCommandExecute(object Parameter) => CanAdapter.PortOpened;
        #endregion

        #region SetAdapterListedModeCommand

        public ICommand SetAdapterListedModeCommand { get; }

        private void OnSetAdapterListedModeCommandExecuted(object Parameter) => CanAdapter.StartListen();
        private bool CanSetAdapterListedModeCommandExecute(object Parameter) => CanAdapter.PortOpened;
        #endregion

        #region SetAdapterSelfReceptionModeCommand

        public ICommand SetAdapterSelfReceptionModeCommand { get; }

        private void OnSetAdapterSelfReceptionModeCommandExecuted(object Parameter) => CanAdapter.StartSelfReception();
        private bool CanSetAdapterSelfReceptionModeCommandExecute(object Parameter) => CanAdapter.PortOpened;
        #endregion

        #region StopCanAdapterCommand

        public ICommand StopCanAdapterCommand { get; }

        private void OnStopCanAdapterCommandExecuted(object Parameter) => CanAdapter.Stop();
        private bool CanStopCanAdapterCommandExecute(object Parameter) => CanAdapter.PortOpened;
        #endregion

        #region RefreshPortsCommand
        public ICommand RefreshPortListCommand { get; }
        private void OnRefreshPortsCommandExecuted(object Parameter)
        {
            PortList.Clear();
            foreach (var port in SerialPort.GetPortNames())
                PortList.Add(port);
            if (PortList.Count > 0)
                PortName = PortList[^1];
        }

        #endregion

        #region OpenPortCommand
        public ICommand OpenPortCommand { get; }
        private void OnOpenPortCommandExecuted(object parameter)
        {
            CanAdapter.PortName = PortName;
            CanAdapter.PortOpen();
            Thread.Sleep(20);
            CanAdapter.StartNormal();
            Thread.Sleep(20);
            AC2PMessage msg = new();
            msg.TransmitterType = 126;
            msg.TransmitterAddress = 6;
            msg.ReceiverAddress = 7;
            msg.ReceiverType = 127;
            msg.PGN = 1;
            msg.Data = new byte[8];
            CanAdapter.Transmit(msg);
        }
        private bool CanOpenPortCommandExecute(object parameter) => (PortName.StartsWith("COM") && !CanAdapter.PortOpened);
        #endregion

        #region ClosePortCommand
        public ICommand ClosePortCommand { get; }
        private void OnClosePortCommandExecuted(object parameter)
        {
            CanAdapter.PortClose();
            AC2PInstance.ConnectedDevices.Clear();
        }
        private bool CanClosePortCommandExecute(object parameter) => (CanAdapter.PortOpened);
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
            ExecuteCommand(1, 0xff, 0xff);
        }
        #endregion

        #region StopHeaterCommand
        public ICommand StopHeaterCommand { get; }
        private void OnStopHeaterCommandExecuted(object parameter)
        {
            ExecuteCommand(3);
        }
        #endregion

        #region StartPumpCommand
        public ICommand StartPumpCommand { get; }
        private void OnStartPumpCommandExecuted(object parameter)
        {
            ExecuteCommand(4, 0, 0);
        }

        #endregion

        #region ClearErrorsCommand
        public ICommand ClearErrorsCommand { get; }
        private void OnClearErrorsCommandExecuted(object parameter)
        {
            ExecuteCommand(5);
        }
        #endregion

        #region StartVentCommand
        public ICommand StartVentCommand { get; }
        private void OnStartVentCommandExecuted(object parameter)
        {
            ExecuteCommand(10);
        }
        #endregion

        #region CalibrateTermocouplesCommand

        public ICommand CalibrateTermocouplesCommand { get; }
        private void OnCalibrateTermocouplesCommandExecuted(object parameter)
        {
            ExecuteCommand(20);
        }

        #endregion

        public bool DeviceConnectedAndNotInManual(object parameter) => CanAdapter.PortOpened && SelectedConnectedDevice != null && !SelectedConnectedDevice.ManualMode;
        #endregion

        #region LogCommands


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
            AC2PInstance.ReadAllParameters(selectedConnectedDevice.ID);
        }
        private bool CanReadConfigCommandExecute(object parameter) =>
            (CanAdapter.PortOpened && SelectedConnectedDevice != null && !AC2PInstance.CurrentTask.Occupied && SelectedConnectedDevice.Parameters.Stage == 0);
        #endregion

        #region SaveConfigCommand
        public ICommand SaveConfigCommand { get; }
        private void OnSaveConfigCommandExecuted(object parameter)
        {
            AC2PInstance.SaveParameters(selectedConnectedDevice.ID);
        }
        private bool CanSaveConfigCommandExecute(object parameter) =>
            (CanAdapter.PortOpened && SelectedConnectedDevice != null && !AC2PInstance.CurrentTask.Occupied && SelectedConnectedDevice.readedParameters.Count > 0 && SelectedConnectedDevice.Parameters.Stage == 0);
        #endregion

        #region ResetConfigCommand
        public ICommand ResetConfigCommand { get; }
        private void OnResetConfigCommandExecuted(object parameter)
        {
            AC2PInstance.ResetParameters(selectedConnectedDevice.ID);
        }
        private bool CanResetConfigCommandExecute(object parameter) =>
            (CanAdapter.PortOpened && SelectedConnectedDevice != null && !AC2PInstance.CurrentTask.Occupied && SelectedConnectedDevice.Parameters.Stage == 0);
        #endregion
        #endregion

        #region BlackBoxCommands

        #region ReadBlackBoxDataCommand
        public ICommand ReadBlackBoxDataCommand { get; }
        private void OnReadBlackBoxDataCommandExecuted(object parameter)
        {
            AC2PInstance.ReadBlackBoxData(selectedConnectedDevice.ID);
        }
        private bool CanReadBlackBoxDataExecute(object parameter) =>
            (CanAdapter.PortOpened && SelectedConnectedDevice != null && !AC2PInstance.CurrentTask.Occupied && SelectedConnectedDevice.Parameters.Stage == 0);
        #endregion

        #region ReadBlackBoxErrorsCommand
        public ICommand ReadBlackBoxErrorsCommand { get; }
        private void OnReadBlackBoxErrorsCommandExecuted(object parameter)
        {
            Task.Run(() => AC2PInstance.ReadErrorsBlackBox(selectedConnectedDevice.ID));
        }
        private bool CanReadBlackBoxErrorsExecute(object parameter) =>
            (CanAdapter.PortOpened && SelectedConnectedDevice != null && !AC2PInstance.CurrentTask.Occupied && SelectedConnectedDevice.Parameters.Stage == 0);
        #endregion

        #region EraseBlackBoxErrorsCommand
        public ICommand EraseBlackBoxErrorsCommand { get; }
        private void OnEraseBlackBoxErrorsCommandExecuted(object parameter)
        {
            Task.Run(() => AC2PInstance.EraseErrorsBlackBox(selectedConnectedDevice.ID));
        }
        private bool CanEraseBlackBoxErrorsExecute(object parameter) =>
            (CanAdapter.PortOpened && SelectedConnectedDevice != null && !AC2PInstance.CurrentTask.Occupied && SelectedConnectedDevice.Parameters.Stage == 0);
        #endregion

        #region EraseBlackBoxDataCommand
        public ICommand EraseBlackBoxDataCommand { get; }
        private void OnEraseBlackBoxDataCommandExecuted(object parameter)
        {
            Task.Run(() => AC2PInstance.EraseCommonBlackBox(selectedConnectedDevice.ID));
        }
        private bool CanEraseBlackBoxDataExecute(object parameter) =>
            (CanAdapter.PortOpened && SelectedConnectedDevice != null && !AC2PInstance.CurrentTask.Occupied && SelectedConnectedDevice.Parameters.Stage == 0);
        #endregion
        #endregion

        #region SaveReportCommand
        public ICommand SaveReportCommand { get; }
        private void OnSaveReportCommandExecuted(object parameter)
        {
            Task.Run(() => AC2PInstance.EraseCommonBlackBox(selectedConnectedDevice.ID));
        }
        private bool CanSaveReportCommandExecute(object parameter) =>
            (SelectedConnectedDevice.BBErrors.Count > 0 || SelectedConnectedDevice.BBValues.Count > 0);
        #endregion

        #region SendCustomMessageCommand
        public ICommand SendCustomMessageCommand { get; }
        private void OnSendCustomMessageCommandExecuted(object parameter)
        {
            CustomMessage.TransmitterId = new DeviceId(126, 6);
            AC2PInstance.SendMessage(CustomMessage);
        }
        private bool CanSendCustomMessageCommandExecute(object parameter)
        {
            if (!CanAdapter.PortOpened) return false;
            return true;
        }

        #endregion

        #region SaveLogCommand

        public ICommand SaveLogCommand { get; }

        private void OnSaveLogCommandExecuted(object parameter)
        {
            string path = Environment.GetFolderPath(Environment.SpecialFolder.Desktop) + "\\" + SelectedConnectedDevice.ID.Type + "_" + DateTime.Now.ToString("HH-mm-ss_dd-MM-yy") + ".csv";

            using (StreamWriter sw = new StreamWriter(path))
            {
                /*
                foreach (var v in SelectedConnectedDevice.Status)
                    sw.Write(AC2P.Variables[v.Id].ShortName + ";");
                sw.WriteLine();
                for (int i = 0; i < SelectedConnectedDevice.LogCurrentPos; i++)
                {
                    foreach (var v in SelectedConnectedDevice.Status)
                        sw.Write(SelectedConnectedDevice.LogData[v.Id][i].ToString(v.AssignedParameter.OutputFormat) + ";");
                    sw.WriteLine();
                }
                sw.Flush();
                sw.Close();
                */
            }
        }

        private bool CanSaveLogCommandExecuted(object parameter)
        {
            return SelectedConnectedDevice != null;
        }
        #endregion


        public void ExecuteCommand(int cmdNum, params byte[] data)
        {
            AC2PMessage msg = new();
            msg.TransmitterType = 126;
            msg.TransmitterAddress = 6;
            msg.ReceiverId = SelectedConnectedDevice.ID;
            msg.PGN = 1;
            msg.Data = new byte[8];
            msg.Data[0] = (byte)(cmdNum >> 8);
            msg.Data[1] = (byte)(cmdNum & 0xFF);
            for (int i = 0; i < data.Length; i++)
                msg.Data[i + 2] = data[i];

            CanAdapter.Transmit(msg);
        }



        private async void requestSerial()
        {
            await Task.Delay(200);
            AC2PMessage m = new();
            m.TransmitterType = 126;
            m.TransmitterAddress = 6;
            m.ReceiverId = SelectedConnectedDevice.ID;
            m.PGN = 7;
            m.Data = new byte[8];
            m.Data[0] = 3;
            m.Data[1] = 0;
            m.Data[2] = 0;
            m.Data[3] = 12;
            CanAdapter.Transmit(m);
            await Task.Delay(200);
            m.Data[3] = 13;
            CanAdapter.Transmit(m);
            await Task.Delay(200);
            m.Data[3] = 14;
            CanAdapter.Transmit(m);
        }

        #region Chart

        private void TimerTick(object sender, EventArgs e)
        {
            foreach (var d in AC2PInstance.ConnectedDevices) //Источник токов для ведения лога
            {
                d.LogTick();
            }

            if (AutoRedraw)                                 //Перерисовк графиков
                if (LogPage.CanChartDrawCommandExecute(null))
                    LogPage.OnChartDrawCommandExecuted(null);

            foreach (ConnectedDevice d in AC2PInstance.ConnectedDevices) //Поддержание связи
            {
                if (d.ID.Type == 34)
                {
                    AC2PMessage msg = new();
                    msg.TransmitterAddress = 6;
                    msg.TransmitterType = 126;
                    msg.PGN = 0;
                    msg.ReceiverId = d.ID;
                    CanAdapter.Transmit(msg);
                }
            }
        }

        #endregion

        #endregion

        public void NewDeviceHandler(object sender, EventArgs e)
        {
            SelectedConnectedDevice = AC2PInstance.ConnectedDevices[^1];
            firmwarePage.GetVersionCommand.Execute(null);
            Task.Run(() => requestSerial());
        }


        public bool portOpened(object parameter)
        {
            return CanAdapter.PortOpened;
        }
        public bool deviceSelected(object parameter)
        {
            return CanAdapter.PortOpened && SelectedConnectedDevice != null;
        }



        public MainWindowViewModel()
        {

            _canAdapter = new CanAdapter();
            _AC2PInstance = new AC2P(CanAdapter);
            FirmwarePage = new(this);

            System.Windows.Threading.DispatcherTimer timer = new System.Windows.Threading.DispatcherTimer();

            timer.Tick += TimerTick;
            timer.Interval = new TimeSpan(0, 0, 1);
            timer.Start();

            AC2PInstance.NewDeviceAquired += NewDeviceHandler;


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
            StartHeaterCommand = new LambdaCommand(OnStartHeaterCommandExecuted, DeviceConnectedAndNotInManual);
            StopHeaterCommand = new LambdaCommand(OnStopHeaterCommandExecuted, DeviceConnectedAndNotInManual);
            StartPumpCommand = new LambdaCommand(OnStartPumpCommandExecuted, DeviceConnectedAndNotInManual);
            StartVentCommand = new LambdaCommand(OnStartVentCommandExecuted, DeviceConnectedAndNotInManual);
            ClearErrorsCommand = new LambdaCommand(OnClearErrorsCommandExecuted, deviceSelected);
            CalibrateTermocouplesCommand = new LambdaCommand(OnCalibrateTermocouplesCommandExecuted, DeviceConnectedAndNotInManual);


            SaveLogCommand = new LambdaCommand(OnSaveLogCommandExecuted, CanSaveLogCommandExecuted);

            CustomMessage.TransmitterAddress = 6;
            CustomMessage.TransmitterType = 126;

            brushes.Add(new SolidColorBrush(Colors.Red));
            brushes.Add(new SolidColorBrush(Colors.DeepPink));
            brushes.Add(new SolidColorBrush(Colors.MediumPurple));
            brushes.Add(new SolidColorBrush(Colors.BlueViolet));
            brushes.Add(new SolidColorBrush(Colors.DarkSlateBlue));
            brushes.Add(new SolidColorBrush(Colors.PowderBlue));
            brushes.Add(new SolidColorBrush(Colors.LightSkyBlue));
            brushes.Add(new SolidColorBrush(Colors.Cyan));
            brushes.Add(new SolidColorBrush(Colors.Teal));
            brushes.Add(new SolidColorBrush(Colors.Green));
            brushes.Add(new SolidColorBrush(Colors.LightGreen));
            brushes.Add(new SolidColorBrush(Colors.YellowGreen));
            brushes.Add(new SolidColorBrush(Colors.Yellow));
            brushes.Add(new SolidColorBrush(Colors.Gold));
            brushes.Add(new SolidColorBrush(Colors.Orange));
            brushes.Add(new SolidColorBrush(Colors.OrangeRed));
            brushes.Add(new SolidColorBrush(Colors.Peru));
            brushes.Add(new SolidColorBrush(Colors.Gray));
            brushes.Add(new SolidColorBrush(Colors.SlateGray));
        }
    }
}
