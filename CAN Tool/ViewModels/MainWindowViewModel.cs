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
using System.Windows.Media;
using ScottPlot.Renderable;
using System.Windows.Markup;
using Microsoft.Win32;
using System.IO;
using System.Text;
using System.Drawing;
using System.Windows;
using System.Threading;
using System.Windows.Media.Animation;

namespace CAN_Tool.ViewModels
{
    internal class MainWindowViewModel : ViewModel
    {

        private FirmwarePage firmwarePage;

        public FirmwarePage FirmwarePage
        {
            get { return firmwarePage; }
            set { Set(ref firmwarePage, value); }
        }

        int[] _Bitrates => new int[] { 20, 50, 125, 250, 500, 800, 1000 };

        private int manualAirBlower;
        public int ManualAirBlower { get { return manualAirBlower; } set { Set(ref manualAirBlower, value); } }

        private int manualFuelPump;
        public int ManualFuelPump { set => Set(ref manualFuelPump, value); get => manualFuelPump; }

        private int manualGlowPlug;
        public int ManualGlowPlug { set => Set(ref manualGlowPlug, value); get => manualGlowPlug; }

        private bool manualWaterPump;
        public bool ManualWaterPump { set => Set(ref manualWaterPump, value); get => manualWaterPump; }

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
            canAdapter.StartNormal();

            CustomMessage.TransmitterType = 126;
            CustomMessage.TransmitterAddress = 6;
            CustomMessage.ReceiverAddress = 7;
            CustomMessage.ReceiverType = 127;
            CustomMessage.PGN = 1;
            CustomMessage.Data = new byte[8];
            canAdapter.Transmit(customMessage);
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

                    plt.Style(ScottPlot.Style.Gray1);
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
            Task.Run(() => AC2PInstance.EraseCommonBlackBox(_connectedDevice.ID));
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
            AC2PInstance.SendMessage(CustomMessage);
        }
        private bool CanSendCustomMessageCommandExecute(object parameter)
        {
            if (!canAdapter.PortOpened) return false;
            return true;
        }

        #endregion

        #region EnterManualModeCommand
        public ICommand EnterManualModeCommand { get; }
        private void OnEnterManualModeCommandExecuted(object parameter)
        {
            executeCommand(67, new byte[] { 1, 0, 0, 0, 0, 0 });
        }

        #endregion


        #region ExitManualModeCommand
        public ICommand ExitManualModeCommand { get; }
        private void OnExitManualModeCommandExecuted(object parameter)
        {
            ManualAirBlower = 0;
            ManualFuelPump = 0;
            ManualGlowPlug = 0;
            executeCommand(67, new byte[] { 0, 0, 0, 0, 0, 0 });
        }
        #endregion

        #region IncreaceManualAirBlower
        public ICommand IncreaceManualAirBlowerCommand { get; }
        private void OnIncreaceManualAirBlowerCommandExecuted(object parameter)
        {
            if (ManualAirBlower < 100)
                ManualAirBlower++;
            updateManualMode();
        }
        #endregion

        #region DecreaseManualAirBlower
        public ICommand DecreaseManualAirBlowerCommand { get; }
        private void OnDecreaseManualAirBlowerCommandExecuted(object parameter)
        {
            if (ManualAirBlower > 0)
                ManualAirBlower--;
            updateManualMode();
        }
        #endregion

        #region IncreaseManualFuelPump
        public ICommand IncreaseManualFuelPumpCommand { get; }
        private void OnIncreaseManualFuelPumpCommandExecuted(object parameter)
        {
            ManualFuelPump += 5;
            if (ManualFuelPump > 700) ManualFuelPump = 700;
            updateManualMode();
        }
        #endregion



        #region DecreaseManualFuelPump
        public ICommand DecreaseFuelPumpCommand { get; }
        private void OnDecreaseFuelPumpCommandExecuted(object parameter)
        {
            ManualFuelPump -= 5;
            if (ManualFuelPump < 0) ManualFuelPump = 0;
            updateManualMode();
        }
        #endregion

        #region IncreaseManualGlowPlug
        public ICommand IncreaseGlowPlugCommand { get; }
        private void OnIncreaseGlowPlugCommandExecuted(object parameter)
        {
            ManualGlowPlug += 5;
            if (ManualGlowPlug > 100) ManualGlowPlug = 100;
            updateManualMode();
        }
        #endregion

        #region DecreaseManualGlowPlug
        public ICommand DecreaseGlowPlugCommand { get; }
        private void OnDecreaseGlowPlugCommandExecuted(object parameter)
        {
            ManualGlowPlug -= 5;
            if (ManualGlowPlug < 0) ManualGlowPlug = 0;
            updateManualMode();
        }
        #endregion

        #region TurnOnWaterPumpCommand
        public ICommand TurnOnWaterPumpCommand { get; }
        private void OnTurnOnWaterPumpCommandExecuted(object parameter)
        {
            manualWaterPump = true;
            updateManualMode();
        }
        #endregion

        #region TurnOffWaterPumpCommand
        public ICommand TurnOffWaterPumpCommand { get; }
        private void OnTurnOffWaterPumpCommandExecuted(object parameter)
        {
            manualWaterPump = false;
            updateManualMode();
        }
        #endregion

        #region SaveLogCommand

        public ICommand SaveLogCommand { get; }

        private void OnSaveLogCommandExecuted(object parameter)
        {
            string path = Environment.GetFolderPath(Environment.SpecialFolder.Desktop) + "\\" + SelectedConnectedDevice.ID.Type + "_" + DateTime.Now.ToString("HH-mm-ss_dd-MM-yy") + ".csv";

            using (StreamWriter sw = new StreamWriter(path))
            {
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
            }
        }

        private bool CanSaveLogCommandExecuted(object parameter)
        {
            return SelectedConnectedDevice != null && SelectedConnectedDevice.LogCurrentPos > 0;
        }
        #endregion


        private void executeCommand(int cmdNum, params byte[] data)
        {
            CustomMessage.TransmitterType = 126;
            CustomMessage.TransmitterAddress = 6;
            CustomMessage.ReceiverId = SelectedConnectedDevice.ID;
            CustomMessage.PGN = 1;
            CustomMessage.Data = new byte[8];
            for (int i = 0; i < data.Length; i++)
                customMessage.Data[i + 2] = data[i];
            CustomMessage.Data[0] = (byte)(cmdNum >> 8);
            CustomMessage.Data[1] = (byte)(cmdNum & 0xFF);
            canAdapter.Transmit(customMessage);
        }

        private void updateManualMode()
        {
            CustomMessage.TransmitterType = 126;
            CustomMessage.TransmitterAddress = 6;
            CustomMessage.ReceiverId = SelectedConnectedDevice.ID;
            CustomMessage.PGN = 1;
            CustomMessage.Data = new byte[8];
            CustomMessage.Data[0] = 0;
            CustomMessage.Data[1] = 68;

            if (ManualWaterPump)
                CustomMessage.Data[2] = 1;
            else
                CustomMessage.Data[2] = 0;
            CustomMessage.Data[3] = (byte)ManualAirBlower;
            CustomMessage.Data[4] = (byte)ManualGlowPlug;
            CustomMessage.Data[5] = (byte)(ManualFuelPump / 256);
            CustomMessage.Data[6] = (byte)ManualFuelPump;
            canAdapter.Transmit(customMessage);
        }

        private async void requestSerial()
        {
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
            canAdapter.Transmit(m);
            await Task.Delay(100);
            m.Data[3] = 13;
            canAdapter.Transmit(m);
            await Task.Delay(100);
            m.Data[3] = 14;
            canAdapter.Transmit(m);
        }

        #region Chart

        private void TimerTick(object sender, EventArgs e)
        {
            foreach (var d in AC2PInstance.ConnectedDevices) //Источник токов для ведения лога
            {
                d.LogTick();
            }

            if (AutoRedraw)                                 //Перерисовк графиков
                if (CanChartDrawCommandExecute(null))
                    OnChartDrawCommandExecuted(null);

            foreach (ConnectedDevice d in AC2PInstance.ConnectedDevices) //Поддержание связи
            {
                AC2PMessage msg = new();
                msg.TransmitterAddress = 6;
                msg.TransmitterType = 126;
                msg.PGN = 0;
                msg.ReceiverId = d.ID;
                canAdapter.Transmit(msg);
            }
        }

        #endregion
        #endregion

        public void NewDeviceHandler(object sender, EventArgs e)
        {
                SelectedConnectedDevice = AC2PInstance.ConnectedDevices[^1];
                firmwarePage.GetVersionCommand.Execute(null);
                Task.Run(()=> requestSerial());
        }


        public bool portOpened(object parameter)
        {
            return canAdapter.PortOpened;
        }
        public bool deviceSelected(object parameter)
        {
            return canAdapter.PortOpened && SelectedConnectedDevice != null;
        }

        public bool deviceInManualMode(object parameter)
        {
            return (canAdapter.PortOpened && SelectedConnectedDevice != null && SelectedConnectedDevice.ManualMode);

        }

        public MainWindowViewModel()
        {

            _canAdapter = new CanAdapter();
            _AC2PInstance = new AC2P(canAdapter);
            FirmwarePage = new(this);

            System.Windows.Threading.DispatcherTimer timer = new System.Windows.Threading.DispatcherTimer();

            timer.Tick += TimerTick;
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
            EnterManualModeCommand = new LambdaCommand(OnEnterManualModeCommandExecuted, deviceSelected);
            ExitManualModeCommand = new LambdaCommand(OnExitManualModeCommandExecuted, deviceInManualMode);
            IncreaceManualAirBlowerCommand = new LambdaCommand(OnIncreaceManualAirBlowerCommandExecuted, deviceInManualMode);
            DecreaseManualAirBlowerCommand = new LambdaCommand(OnDecreaseManualAirBlowerCommandExecuted, deviceInManualMode);
            IncreaseManualFuelPumpCommand = new LambdaCommand(OnIncreaseManualFuelPumpCommandExecuted, deviceInManualMode);
            DecreaseFuelPumpCommand = new LambdaCommand(OnDecreaseFuelPumpCommandExecuted, deviceInManualMode);
            IncreaseGlowPlugCommand = new LambdaCommand(OnIncreaseGlowPlugCommandExecuted, deviceInManualMode);
            DecreaseGlowPlugCommand = new LambdaCommand(OnDecreaseGlowPlugCommandExecuted, deviceInManualMode);
            TurnOnWaterPumpCommand = new LambdaCommand(OnTurnOnWaterPumpCommandExecuted, deviceInManualMode);
            TurnOffWaterPumpCommand = new LambdaCommand(OnTurnOffWaterPumpCommandExecuted, deviceInManualMode);
            SaveLogCommand = new LambdaCommand(OnSaveLogCommandExecuted, CanSaveLogCommandExecuted);

            CustomMessage.TransmitterAddress = 6;
            CustomMessage.TransmitterType = 126;

        }
    }
}
