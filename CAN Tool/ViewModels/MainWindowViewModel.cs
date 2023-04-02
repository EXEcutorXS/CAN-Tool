using OmniProtocol;
using Can_Adapter;
using CAN_Tool.Infrastructure.Commands;
using CAN_Tool.ViewModels.Base;
using ScottPlot;
using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.IO;
using System.IO.Ports;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using System.Windows.Input;
using System.Windows.Media;
using Xceed.Words.NET;
using Xceed.Document.NET;
using Alignment = Xceed.Document.NET.Alignment;
using CAN_Tool.Libs;
using static CAN_Tool.Libs.Helper;
using RVC;
using System.Windows.Markup;
using System.Security.AccessControl;
using System.Drawing.Text;

namespace CAN_Tool.ViewModels
{
    public enum WorkMode_t { Omni, Rvc, RegularCan }

    internal class MainWindowViewModel : ViewModel
    {
        private SynchronizationContext UIContext = SynchronizationContext.Current;

        public WorkMode_t[] WorkModes => new WorkMode_t[] { WorkMode_t.Omni, WorkMode_t.Rvc, WorkMode_t.RegularCan };

        private WorkMode_t mode;
        public WorkMode_t Mode { set => Set(ref mode, value); get => mode; }

        private readonly List<SolidColorBrush> brushes = new();

        public List<SolidColorBrush> Brushes => brushes;

        public FirmwarePageViewModel FirmwarePage { set; get; }
        public ManualPageViewModel ManualPage { set; get; }
        public RvcPageViewModel RvcPage { set; get; }
        public CanPageViewModel CanPage { set; get; }

        public bool OmniEnabled { set; get; }
        public bool RvcEnabled { set; get; }

        public bool AutoRedraw { set; get; } = true;

        private bool canAdapterSettings = false;
        public bool CanAdapterSettings { set=>Set(ref canAdapterSettings,value); get=>canAdapterSettings; } 

        CanAdapter canAdapter;
        public CanAdapter CanAdapter { get => canAdapter; }

        public Omni OmniInstance { set; get; }

        ConnectedDevice selectedConnectedDevice;
        public ConnectedDevice SelectedConnectedDevice
        {
            set => Set(ref selectedConnectedDevice, value);
            get => selectedConnectedDevice;
        }

        public Dictionary<int, OmniCommand> Commands => Omni.commands;

        public WpfPlot myChart;

        

        private OmniMessage selectedMessage;

        public OmniMessage SelectedMessage
        {
            get => selectedMessage;
            set => Set(ref selectedMessage, value);
        }
        

        OmniMessage customMessage = new OmniMessage() { PGN=1};

        public Dictionary<int, OmniCommand> CommandList { get; } = Omni.commands;

        public OmniMessage CustomMessage { get => customMessage; set => customMessage.Update(value); }

        public double[] CommandParametersArray;



        


        private string portName = "";
        public string PortName
        {
            get => portName;
            set => Set(ref portName, value);
        }



        private BindingList<string> _PortList = new BindingList<string>();
        public BindingList<string> PortList
        {
            get => _PortList;
            set => Set(ref _PortList, value);
        }



        public ICommand SetAdapterNormalModeCommand { get; }

        private void OnSetAdapterNormalModeCommandExecuted(object Parameter) => CanAdapter.StartNormal();
        private bool CanSetAdapterNormalModeCommandExecute(object Parameter) => CanAdapter.PortOpened;

        public ICommand SetAdapterListedModeCommand { get; }

        private void OnSetAdapterListedModeCommandExecuted(object Parameter) => CanAdapter.StartListen();
        private bool CanSetAdapterListedModeCommandExecute(object Parameter) => CanAdapter.PortOpened;


        public ICommand SetAdapterSelfReceptionModeCommand { get; }

        private void OnSetAdapterSelfReceptionModeCommandExecuted(object Parameter) => CanAdapter.StartSelfReception();
        private bool CanSetAdapterSelfReceptionModeCommandExecute(object Parameter) => CanAdapter.PortOpened;


        public ICommand StopCanAdapterCommand { get; }

        private void OnStopCanAdapterCommandExecuted(object Parameter) => CanAdapter.Stop();
        private bool CanStopCanAdapterCommandExecute(object Parameter) => CanAdapter.PortOpened;



        public ICommand RefreshPortListCommand { get; }
        private void OnRefreshPortsCommandExecuted(object Parameter)
        {
            PortList.Clear();
            foreach (var port in SerialPort.GetPortNames())
                PortList.Add(port);
            if (PortList.Count > 0)
                PortName = PortList[^1];
        }




        public ICommand OpenPortCommand { get; }
        private void OnOpenPortCommandExecuted(object parameter)
        {
            CanAdapter.PortName = PortName;
            CanAdapter.PortOpen();
            Thread.Sleep(20);
            CanAdapter.StartNormal();
            Thread.Sleep(20);
            if (Mode == WorkMode_t.Omni)
                ExecuteCommand(1, new byte[8]); // Request devices on the bus
        }
        private bool CanOpenPortCommandExecute(object parameter) => (PortName.StartsWith("COM") && !CanAdapter.PortOpened);



        public ICommand ClosePortCommand { get; }
        private void OnClosePortCommandExecuted(object parameter)
        {
            CanAdapter.PortClose();
            OmniInstance.ConnectedDevices.Clear();
        }
        private bool CanClosePortCommandExecute(object parameter) => (CanAdapter.PortOpened);



        public ICommand StartHeaterCommand { get; }
        private void OnStartHeaterCommandExecuted(object parameter)
        {
            ExecuteCommand(1, 0xff, 0xff);
        }



        public ICommand StopHeaterCommand { get; }
        private void OnStopHeaterCommandExecuted(object parameter)
        {
            ExecuteCommand(3);
        }



        public ICommand StartPumpCommand { get; }
        private void OnStartPumpCommandExecuted(object parameter)
        {
            ExecuteCommand(4, 0, 0);
        }

        public ICommand ClearErrorsCommand { get; }
        private void OnClearErrorsCommandExecuted(object parameter)
        {
            ExecuteCommand(5);
        }


        public ICommand StartVentCommand { get; }
        private void OnStartVentCommandExecuted(object parameter)
        {
            ExecuteCommand(10);
        }


        public ICommand CalibrateTermocouplesCommand { get; }
        private void OnCalibrateTermocouplesCommandExecuted(object parameter)
        {
            ExecuteCommand(20);
        }


        public bool DeviceConnectedAndNotInManual(object parameter) => CanAdapter.PortOpened && SelectedConnectedDevice != null && !SelectedConnectedDevice.ManualMode;


        public ICommand LogStartCommand { get; }
        private void OnLogStartCommandExecuted(object parameter)
        {
            SelectedConnectedDevice.LogStart();
        }
        private bool CanLogStartCommandExecute(object parameter) => (SelectedConnectedDevice != null && CanAdapter.PortOpened);



        public ICommand LogStopCommand { get; }
        private void OnLogStopCommandExecuted(object parameter)
        {
            SelectedConnectedDevice.LogStop();
        }
        private bool CanLogStopCommandExecute(object parameter) => (SelectedConnectedDevice != null && CanAdapter.PortOpened && SelectedConnectedDevice.IsLogWriting);



        public ICommand ChartDrawCommand { get; }
        private void OnChartDrawCommandExecuted(object parameter)
        {
            Plot plt = myChart.Plot;

            plt.Clear();

            foreach (var v in SelectedConnectedDevice.Status)
                if (v.Display)
                {
                    var sig = plt.AddSignalConst(SelectedConnectedDevice.LogData[v.Id].Take(SelectedConnectedDevice.LogCurrentPos).ToArray(), color: v.Color, label: v.Name);
                    sig.UseParallel = false;
                    sig.LineWidth = v.LineWidth;
                    sig.LineStyle = v.LineStyle;
                    sig.MarkerShape = v.MarkShape;

                    if (v.Id == 17 || v.Id == 18) //ТН проецируется на правую ось
                        sig.YAxisIndex = 2;
                    plt.Grid(color: System.Drawing.Color.FromArgb(50, 200, 200, 200));
                    plt.Grid(lineStyle: LineStyle.Dot);
                    if (App.Settings.isDark)
                        plt.Style(dataBackground: System.Drawing.Color.FromArgb(255, 40, 40, 40), figureBackground: System.Drawing.Color.DimGray);
                    else
                        plt.Style(dataBackground: System.Drawing.Color.WhiteSmoke, figureBackground: System.Drawing.Color.White);
                    plt.Legend();


                }
            myChart.Refresh();

        }
        private bool CanChartDrawCommandExecute(object parameter) => (SelectedConnectedDevice != null && SelectedConnectedDevice.LogCurrentPos > 0);


        public ICommand CancelOperationCommand { get; }
        private void OnCancelOperationCommandExecuted(object parameter)
        {
            OmniInstance.CurrentTask.onCancel();
        }
        private bool CanCancelOperationCommandExecute(object parameter) => (OmniInstance.CurrentTask.Occupied);




        public ICommand ReadConfigCommand { get; }
        private void OnReadConfigCommandExecuted(object parameter)
        {
            OmniInstance.ReadAllParameters(selectedConnectedDevice.ID);
        }
        private bool CanReadConfigCommandExecute(object parameter) =>
            (CanAdapter.PortOpened && SelectedConnectedDevice != null && !OmniInstance.CurrentTask.Occupied && SelectedConnectedDevice.Parameters.Stage == 0);



        public ICommand SaveConfigCommand { get; }
        private void OnSaveConfigCommandExecuted(object parameter)
        {
            OmniInstance.SaveParameters(selectedConnectedDevice.ID);
        }
        private bool CanSaveConfigCommandExecute(object parameter) =>
            (CanAdapter.PortOpened && SelectedConnectedDevice != null && !OmniInstance.CurrentTask.Occupied && SelectedConnectedDevice.readedParameters.Count > 0 && SelectedConnectedDevice.Parameters.Stage == 0);



        public ICommand ResetConfigCommand { get; }
        private void OnResetConfigCommandExecuted(object parameter)
        {
            OmniInstance.ResetParameters(selectedConnectedDevice.ID);
        }
        private bool CanResetConfigCommandExecute(object parameter) =>
            (CanAdapter.PortOpened && SelectedConnectedDevice != null && !OmniInstance.CurrentTask.Occupied && SelectedConnectedDevice.Parameters.Stage == 0);






        public ICommand ReadBlackBoxDataCommand { get; }
        private void OnReadBlackBoxDataCommandExecuted(object parameter)
        {
            OmniInstance.ReadBlackBoxData(selectedConnectedDevice.ID);
        }
        private bool CanReadBlackBoxDataExecute(object parameter) =>
            (CanAdapter.PortOpened && SelectedConnectedDevice != null && !OmniInstance.CurrentTask.Occupied && SelectedConnectedDevice.Parameters.Stage == 0);
 


        public ICommand ReadBlackBoxErrorsCommand { get; }
        private void OnReadBlackBoxErrorsCommandExecuted(object parameter)
        {
            Task.Run(() => OmniInstance.ReadErrorsBlackBox(selectedConnectedDevice.ID));
        }
        private bool CanReadBlackBoxErrorsExecute(object parameter) =>
            (CanAdapter.PortOpened && SelectedConnectedDevice != null && !OmniInstance.CurrentTask.Occupied && SelectedConnectedDevice.Parameters.Stage == 0);



        public ICommand EraseBlackBoxErrorsCommand { get; }
        private void OnEraseBlackBoxErrorsCommandExecuted(object parameter)
        {
            Task.Run(() => OmniInstance.EraseErrorsBlackBox(selectedConnectedDevice.ID));
        }
        private bool CanEraseBlackBoxErrorsExecute(object parameter) =>
            (CanAdapter.PortOpened && SelectedConnectedDevice != null && !OmniInstance.CurrentTask.Occupied && SelectedConnectedDevice.Parameters.Stage == 0);
    


        public ICommand EraseBlackBoxDataCommand { get; }
        private void OnEraseBlackBoxDataCommandExecuted(object parameter)
        {
            Task.Run(() => OmniInstance.EraseCommonBlackBox(selectedConnectedDevice.ID));
        }
        private bool CanEraseBlackBoxDataExecute(object parameter) =>
            (CanAdapter.PortOpened && SelectedConnectedDevice != null && !OmniInstance.CurrentTask.Occupied && SelectedConnectedDevice.Parameters.Stage == 0);


        
        public ICommand SaveReportCommand { get; }
        private void OnSaveReportCommandExecuted(object parameter)
        {
            string path = Environment.GetFolderPath(Environment.SpecialFolder.Desktop) + '\\' + selectedConnectedDevice.Name + " " + DateTime.Now.ToShortDateString() + " " + DateTime.Now.ToShortTimeString().Replace(':', '-') + ".docx";
            DocX doc = DocX.Create(path);
            Paragraph headParagraph = doc.InsertParagraph();
            headParagraph.AppendLine(GetString("t_device_report") + ": ").Append(selectedConnectedDevice.Name).Bold();
            headParagraph.AppendLine(GetString("t_serial_number") + ": ").Append(selectedConnectedDevice.SerialAsString).Bold();
            headParagraph.AppendLine(GetString("t_manufacturing_date") + ": ").Append(selectedConnectedDevice.ProductionDate.ToString()).Bold();
            headParagraph.AppendLine(GetString("t_formed") + ": ").Append(DateTime.Now.ToLocalTime().ToString()).Bold();
            headParagraph.AppendLine();
            headParagraph.AppendLine(GetString("t_common_black_box_data") + ":").FontSize(18);
            headParagraph.Alignment = Alignment.center;
            Paragraph dataParagraph = doc.InsertParagraph();
            foreach (var p in selectedConnectedDevice.BBValues)
            {
                dataParagraph.Append(GetString($"bb_{p.Id}") + ": ");
                dataParagraph.Append(p.Value.ToString()).Bold();
                dataParagraph.AppendLine();
            }
            dataParagraph.AppendLine();


            if (selectedConnectedDevice.BBErrors.Count > 0)
            {
                var errorHeader = doc.InsertParagraph();
                errorHeader.AppendLine($"{GetString("t_errors_found") + ": "} {selectedConnectedDevice.BBErrors.Count}").FontSize(17);
                errorHeader.AppendLine();
                errorHeader.Alignment = Alignment.center;
                var errorParagraph = doc.InsertParagraph();

                foreach (var e in selectedConnectedDevice.BBErrors)
                {

                    errorParagraph.AppendLine(e.Name).Bold();
                    errorParagraph.AppendLine();
                    foreach (var v in e.Variables)
                        errorParagraph.AppendLine('\t' + v.Name + ": ").Append(v.Value.ToString()).Bold();
                    errorParagraph.AppendLine();
                }
            }

            doc.Save();

        }
        private bool CanSaveReportCommandExecute(object parameter) =>
        (selectedConnectedDevice != null && (SelectedConnectedDevice.BBErrors.Count > 0 || SelectedConnectedDevice.BBValues.Count > 0));
        


        public ICommand SendCustomMessageCommand { get; }
        private void OnSendCustomMessageCommandExecuted(object parameter)
        {
            CustomMessage.TransmitterId = new DeviceId(126, 6);
            OmniInstance.SendMessage(CustomMessage);
        }
        private bool CanSendCustomMessageCommandExecute(object parameter)
        {
            if (!CanAdapter.PortOpened) return false;
            return true;
        }






        public ICommand SaveLogCommand { get; }

        private void OnSaveLogCommandExecuted(object parameter)
        {
            string path = Environment.GetFolderPath(Environment.SpecialFolder.Desktop) + "\\" + SelectedConnectedDevice.ID.Type + "_" + DateTime.Now.ToString("HH-mm-ss_dd-MM-yy") + ".csv";

            using (StreamWriter sw = new StreamWriter(path))
            {
                foreach (var v in SelectedConnectedDevice.Status)
                    sw.Write(GetString($"vars_{v.Id}") + ";");
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



        public void ExecuteCommand(int cmdNum, params byte[] data)
        {
            OmniMessage msg = new();
            msg.TransmitterType = 126;
            msg.TransmitterAddress = 6;
            msg.ReceiverId = SelectedConnectedDevice.ID;
            msg.PGN = 1;
            msg.Data = new byte[8];
            msg.Data[0] = (byte)(cmdNum >> 8);
            msg.Data[1] = (byte)(cmdNum & 0xFF);
            for (int i = 0; i < data.Length; i++)
                msg.Data[i + 2] = data[i];
            OmniInstance.SendMessage(msg);
        }


        

        private void TimerTick(object sender, EventArgs e)
        {
            foreach (var d in OmniInstance.ConnectedDevices) //Источник тиков для ведения лога
            {
                d.LogTick();
            }

            if (AutoRedraw)                                 //Перерисовка графиков
                if (CanChartDrawCommandExecute(null))
                {
                    if (SelectedConnectedDevice.LogCurrentPos < 600)
                        OnChartDrawCommandExecuted(null);
                    else if (DateTime.Now.Second % 10 == 0)
                        OnChartDrawCommandExecuted(null);
                }

            foreach (ConnectedDevice d in OmniInstance.ConnectedDevices) //Поддержание связи
            {
                OmniMessage msg = new();
                msg.TransmitterAddress = 6;
                msg.TransmitterType = 126;
                msg.PGN = 0;
                msg.ReceiverId = d.ID;
                OmniInstance.SendMessage(msg);
                Task.Delay(50);
            }

        }


        public void NewDeviceHandler(object sender, EventArgs e)
        {
            SelectedConnectedDevice = OmniInstance.ConnectedDevices[^1];
        }


        public bool portOpened(object parameter)
        {
            return CanAdapter.PortOpened;
        }
        public bool deviceSelected(object parameter)
        {
            return CanAdapter.PortOpened && SelectedConnectedDevice != null;
        }

        public bool deviceInManualMode(object parameter)
        {
            return (CanAdapter.PortOpened && SelectedConnectedDevice != null && SelectedConnectedDevice.ManualMode);

        }

        public void NewMessgeReceived(object sender, EventArgs e)
        { 
            switch(Mode)
            {
                case WorkMode_t.Omni: UIContext.Send(x => OmniInstance.ProcessCanMessage((e as GotMessageEventArgs).receivedMessage), null); break;
                case WorkMode_t.Rvc: UIContext.Send(x => RvcPage.ProcessMessage ((e as GotMessageEventArgs).receivedMessage), null); break;
                case WorkMode_t.RegularCan: UIContext.Send(x => CanPage.ProcessMessage((e as GotMessageEventArgs).receivedMessage), null); break;
            }
        }

        public MainWindowViewModel()
        {
            

            canAdapter = new();
            OmniInstance = new Omni(CanAdapter);

            OmniInstance.plot = myChart;
            FirmwarePage = new(this);
            RvcPage = new(this);
            ManualPage = new(this);
            CanPage = new(this);

            CanAdapter.GotNewMessage += NewMessgeReceived;
            var timer = new System.Windows.Threading.DispatcherTimer();

            timer.Tick += TimerTick;
            timer.Interval = new TimeSpan(0, 0, 1);
            timer.Start();

            OmniInstance.NewDeviceAquired += NewDeviceHandler;

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
            StartHeaterCommand = new LambdaCommand(OnStartHeaterCommandExecuted, DeviceConnectedAndNotInManual);
            StopHeaterCommand = new LambdaCommand(OnStopHeaterCommandExecuted, DeviceConnectedAndNotInManual);
            StartPumpCommand = new LambdaCommand(OnStartPumpCommandExecuted, DeviceConnectedAndNotInManual);
            StartVentCommand = new LambdaCommand(OnStartVentCommandExecuted, DeviceConnectedAndNotInManual);
            ClearErrorsCommand = new LambdaCommand(OnClearErrorsCommandExecuted, deviceSelected);
            CalibrateTermocouplesCommand = new LambdaCommand(OnCalibrateTermocouplesCommandExecuted, DeviceConnectedAndNotInManual);
            LogStartCommand = new LambdaCommand(OnLogStartCommandExecuted, CanLogStartCommandExecute);
            LogStopCommand = new LambdaCommand(OnLogStopCommandExecuted, CanLogStopCommandExecute);
            ChartDrawCommand = new LambdaCommand(OnChartDrawCommandExecuted, CanChartDrawCommandExecute);

            SaveLogCommand = new LambdaCommand(OnSaveLogCommandExecuted, CanSaveLogCommandExecuted);
            SaveReportCommand = new LambdaCommand(OnSaveReportCommandExecuted, CanSaveReportCommandExecute);


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
