using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using Microsoft.Win32;
using OmniProtocol;
using ScottPlot;
using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.IO;
using System.IO.Ports;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Media;
using System.Windows.Threading;
using Xceed.Words.NET;
using static CAN_Tool.Libs.Helper;
using Alignment = Xceed.Document.NET.Alignment;
using RVC;

namespace CAN_Tool.ViewModels
{

    public enum WorkMode_t { Omni, Rvc, RegularCan }

    public partial class MainWindowViewModel : ObservableObject
    {
        private SynchronizationContext UIContext = SynchronizationContext.Current;

        public WorkMode_t[] WorkModes => new WorkMode_t[] { WorkMode_t.Omni, WorkMode_t.Rvc, WorkMode_t.RegularCan };

        public string Title => "CAN Tool";

        [ObservableProperty] private List<SolidColorBrush> brushes = new();
        [ObservableProperty] public bool autoRedraw = true;

        [ObservableProperty] private WorkMode_t mode;

        public ManualPageViewModel ManualPage { set; get; }
        public RvcPageViewModel RvcPage { set; get; }
        public CanPageViewModel CanPage { set; get; }


        [ObservableProperty] private bool canAdapterSettings = false;
        [ObservableProperty] private string portButtonString = GetString("b_open");
        [ObservableProperty] private int selectedCanBitrate = 5;
        [ObservableProperty] private int selectedCanMode = 0;
        [ObservableProperty] private CanAdapter canAdapter;
        [ObservableProperty] public Omni omniInstance;

        public Dictionary<int, OmniCommand> Commands => Omni.Commands;

        public WpfPlot myChart;

        [ObservableProperty] private OmniMessage selectedMessage;
        [ObservableProperty] OmniMessage customMessage = new OmniMessage() { Pgn = 0, ReceiverId = new(27, 0), };

        public Dictionary<int, OmniCommand> CommandList { get; } = Omni.Commands;

        public double[] CommandParametersArray;
        [ObservableProperty] private string portName = "";


        [ObservableProperty] private BindingList<string> portList = new();
        [ObservableProperty] private string toggleCanLogButtonName = GetString("b_start_can_log");

        private bool canLogging = false;

        private string canLogFileName = "";

        private StreamWriter canLogStream;

        [RelayCommand]
        private void ToggleCanLog(object Parameter)
        {
            if (canLogging)
            {
                canLogging = false;
                ToggleCanLogButtonName = GetString("b_start_can_log");
                canLogStream.Flush();
                canLogStream.Close();
            }
            else
            {
                canLogging = true;
                ToggleCanLogButtonName = GetString("b_stop_can_log");
                var path = Environment.GetFolderPath(Environment.SpecialFolder.Desktop) + "\\CAN Log_" + DateTime.Now.ToLongTimeString().Replace(':', '.') + ".txt";
                canLogStream = new StreamWriter(path, false, System.Text.Encoding.UTF8);
                canLogFileName = path;
            }
        }

        [ObservableProperty] int messageDelay = 100;


        // How long a BLE "refresh ports" scan runs before settling on whatever it found -
        // advertisements arrive roughly a few times a second, so this is comfortably long
        // enough to catch a nearby PU-28 without making the button feel unresponsive.
        private static readonly TimeSpan BleScanDuration = TimeSpan.FromSeconds(4);

        [RelayCommand]
        private async Task RefreshPortList(object Parameter)
        {
            PortList.Clear();

            if (CanAdapter.DriverSupportsDeviceScan)
            {
                CanAdapter.StartDeviceScan(name =>
                    UIContext.Post(_ =>
                    {
                        if (!PortList.Contains(name)) PortList.Add(name);
                    }, null));
                await Task.Delay(BleScanDuration);
                CanAdapter.StopDeviceScan();
            }
            else
            {
                foreach (var port in SerialPort.GetPortNames())
                    PortList.Add(port);
            }

            if (PortList.Count > 0)
                PortName = PortList[^1];
        }


        [RelayCommand]
        private void TogglePort(object parameter)
        {
            try
            {
                if (!CanAdapter.PortOpened)
                {
                    switch (SelectedCanMode)
                    {
                        case 0: CanAdapter.PortOpenNormal(PortName); break;
                        case 1: CanAdapter.PortOpenListenOnly(PortName); break;
                        case 2: CanAdapter.PortOpenSelfReception(PortName); break;
                    }
                    PortButtonString = GetString("b_close");
                }
                else
                {
                    CanAdapter.PortClose();
                    PortButtonString = GetString("b_open");
                }
            }
            catch
            (Exception ex)
            {
                MessageBox.Show(ex.Message);
            }
        }

        [RelayCommand]
        private void StartListenOnly(object parameter)
        {
            try
            {
                if (!CanAdapter.PortOpened)
                {
                    CanAdapter.PortOpenListenOnly();
                    Thread.Sleep(10);
                    PortButtonString = GetString("b_close");
                }
                else
                {
                    CanAdapter.PortClose();
                    Thread.Sleep(10);
                    PortButtonString = GetString("b_open");
                    CanAdapter.PortClose();
                }
            }
            catch
            (Exception ex)
            {
                MessageBox.Show(ex.Message);
            }
        }


        private async Task loadLogAsync(string path)
        {
            var lines = File.ReadAllLines(path);
            foreach (var line in lines)
            {
                var parts = line.Split(' ');
                CanMessage m = new();
                m.Id = Convert.ToInt32(parts[0], 16);
                m.Dlc = Convert.ToInt32(parts[1], 16);
                m.Ide = true;
                m.Rtr = false;

                for (var i = 2; i < parts.Length; i++)
                    m.Data[i - 2] = Convert.ToByte(parts[i], 16);

                CanAdapter.InjectMessage(m);
                await Task.Delay(MessageDelay);

            }
        }

        [RelayCommand]
        private async Task LoadFromLog(object parameter)
        {
            var dialog = new OpenFileDialog();
            dialog.DefaultExt = ".txt"; // Default file extension
            dialog.Filter = "Text documents (.txt)|*.txt"; // Filter files by extension

            var result = dialog.ShowDialog();

            // Process open file dialog box results
            if (result == true)
            {
                // Open document
                var filename = dialog.FileName;

                await loadLogAsync(filename);
            }
        }

        private async Task sendLogAsync(string path)
        {
            var lines = File.ReadAllLines(path);
            foreach (var line in lines)
            {
                var parts = line.Split(' ');
                CanMessage m = new();
                m.Id = Convert.ToInt32(parts[0], 16);
                m.Dlc = Convert.ToInt32(parts[1], 16);
                m.Ide = true;
                m.Rtr = false;

                for (var i = 2; i < parts.Length; i++)
                    m.Data[i - 2] = Convert.ToByte(parts[i], 16);

                CanAdapter.Transmit(m);
                await Task.Delay(MessageDelay);

            }
        }

        [RelayCommand]
        private async Task SendFromLog(object parameter)
        {
            var dialog = new OpenFileDialog();
            dialog.DefaultExt = ".txt"; // Default file extension
            dialog.Filter = "Text documents (.txt)|*.txt"; // Filter files by extension

            var result = dialog.ShowDialog();

            // Process open file dialog box results
            if (result == true)
            {
                // Open document
                var filename = dialog.FileName;

                await sendLogAsync(filename);
            }
        }

        public void ExecuteCommand(int cmdNum, params byte[] data)
        {
            if (OmniInstance?.SelectedConnectedDevice == null) return;
            OmniMessage msg = new();
            msg.TransmitterId.Type = 126;
            msg.TransmitterId.Address = 6;
            msg.ReceiverId.Address = OmniInstance.SelectedConnectedDevice.Id.Address;
            msg.ReceiverId.Type = OmniInstance.SelectedConnectedDevice.Id.Type;
            msg.Pgn = 1;
            msg.Data = new byte[8];
            msg.Data[0] = (byte)(cmdNum >> 8);
            msg.Data[1] = (byte)(cmdNum & 0xFF);
            for (var i = 0; i < data.Length; i++)
                msg.Data[i + 2] = data[i];
            CanAdapter.Transmit(msg.ToCanMessage());
        }


        [RelayCommand]
        private void LogStart(object parameter) => OmniInstance.SelectedConnectedDevice.LogStart();

        [RelayCommand]
        private void LogStop(object parameter) => OmniInstance.SelectedConnectedDevice.LogStop();

        [RelayCommand]
        private void ChartDraw(object parameter)
        {
            if (myChart == null) return;
            var plt = myChart.Plot;

            plt.Clear();

            var device = OmniInstance?.SelectedConnectedDevice;
            if (device == null || device.LogData.Count == 0) return;

            int logLen = device.LogCurrentPos;
            bool hasRightAxis = false;
            foreach (var v in device.Status)
            {
                if (!v.Display || v.Id >= device.LogData.Count) continue;

                var sig = plt.AddSignalConst(
                    device.LogData[v.Id].Take(logLen).ToArray(),
                    color: v.Color, label: v.Name);
                sig.UseParallel = false;
                sig.LineWidth   = v.LineWidth;
                sig.LineStyle   = v.LineStyle;
                sig.MarkerShape = v.MarkShape;

                if (v.Id == 17 || v.Id == 18)
                {
                    sig.YAxisIndex = 1;
                    hasRightAxis = true;
                }
            }

            plt.YAxis2.Ticks(hasRightAxis);
            plt.YAxis2.AutomaticTickPositions();

            plt.Grid(color: System.Drawing.Color.FromArgb(50, 200, 200, 200));
            plt.Grid(lineStyle: LineStyle.Dot);
            if (App.Settings.IsDark)
                plt.Style(dataBackground: System.Drawing.Color.FromArgb(255, 40, 40, 40), figureBackground: System.Drawing.Color.DimGray);
            else
                plt.Style(dataBackground: System.Drawing.Color.WhiteSmoke, figureBackground: System.Drawing.Color.White);
            plt.Legend(true, ScottPlot.Alignment.UpperLeft);

            myChart.Refresh();
        }

        [RelayCommand]
        private void OnCancelOperation(object parameter) => OmniInstance.CurrentTask.OnCancel();

        [RelayCommand]
        private void ReadConfig(object parameter) => OmniInstance.ReadAllParameters(OmniInstance.SelectedConnectedDevice.Id);

        [RelayCommand]
        private void SaveConfig(object parameter) => OmniInstance.SaveParameters(OmniInstance.SelectedConnectedDevice.Id);

        [RelayCommand]
        private void ResetConfig(object parameter) => OmniInstance.ResetParameters(OmniInstance.SelectedConnectedDevice.Id);

        [RelayCommand]
        private void ReadBlackBoxData(object parameter) => OmniInstance.ReadBlackBoxData(OmniInstance.SelectedConnectedDevice.Id);

        [RelayCommand]
        private void ReadBlackBoxErrors(object parameter) => Task.Run(() => OmniInstance.ReadErrorsBlackBox(OmniInstance.SelectedConnectedDevice.Id));

        [RelayCommand]
        private void EraseBlackBoxErrors(object parameter) => Task.Run(() => OmniInstance.EraseErrorsBlackBox(OmniInstance.SelectedConnectedDevice.Id));

        [RelayCommand]
        private void EraseBlackBoxData(object parameter) => Task.Run(() => OmniInstance.EraseCommonBlackBox(OmniInstance.SelectedConnectedDevice.Id));

        [RelayCommand]
        private void SaveReport(object parameter)
        {
            var path = Environment.GetFolderPath(Environment.SpecialFolder.Desktop) + '\\' + OmniInstance.SelectedConnectedDevice.Name + " " + DateTime.Now.ToShortDateString() + " " + DateTime.Now.ToShortTimeString().Replace(':', '-') + ".docx";
            var doc = DocX.Create(path);
            var headParagraph = doc.InsertParagraph();
            headParagraph.AppendLine(GetString("t_device_report") + ": ").Append(OmniInstance.SelectedConnectedDevice.Name).Bold();
            headParagraph.AppendLine(GetString("t_serial_number") + ": ").Append(OmniInstance.SelectedConnectedDevice.Serial[0].ToString() + "." + OmniInstance.SelectedConnectedDevice.Serial[1].ToString() + "." + OmniInstance.SelectedConnectedDevice.Serial[2].ToString()).Bold();
            headParagraph.AppendLine(GetString("t_manufacturing_date") + ": ").Append(OmniInstance.SelectedConnectedDevice.ProductionDate.ToString()).Bold();
            headParagraph.AppendLine(GetString("t_formed") + ": ").Append(DateTime.Now.ToLocalTime().ToString()).Bold();
            headParagraph.AppendLine();
            headParagraph.AppendLine(GetString("t_common_black_box_data") + ":").FontSize(18);
            headParagraph.Alignment = Alignment.center;
            var dataParagraph = doc.InsertParagraph();
            foreach (var p in OmniInstance.SelectedConnectedDevice.BbValues)
            {
                dataParagraph.Append(GetString($"bb_{p.Id}") + ": ");
                dataParagraph.Append(p.Value.ToString()).Bold();
                dataParagraph.AppendLine();
            }
            dataParagraph.AppendLine();


            if (OmniInstance.SelectedConnectedDevice.BbErrors.Count > 0)
            {
                var errorHeader = doc.InsertParagraph();
                errorHeader.AppendLine($"{GetString("t_errors_found") + ": "} {OmniInstance.SelectedConnectedDevice.BbErrors.Count}").FontSize(17);
                errorHeader.AppendLine();
                errorHeader.Alignment = Alignment.center;
                var errorParagraph = doc.InsertParagraph();

                foreach (var e in OmniInstance.SelectedConnectedDevice.BbErrors)
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

        [RelayCommand]
        private void SendCustomMessage(object parameter)
        {
            CustomMessage.TransmitterId.Address = 6;
            CustomMessage.TransmitterId.Type = 126;
            OmniInstance.SendMessage(CustomMessage);
        }



        [RelayCommand]
        private void SaveLog(object parameter)
        {
            OmniInstance.SelectedConnectedDevice.saveLog();
        }


        [RelayCommand]
        private void DefaultStyle(object parameter)
        {
            foreach (var v in OmniInstance.SelectedConnectedDevice.Status)
            {
                v.LineStyle = LineStyle.Solid;
                v.MarkShape = MarkerShape.none;
                v.LineWidth = 1;
                switch (v.Id)
                {
                    case 6:
                        v.Display = true;
                        v.ChartBrush = new SolidColorBrush(Colors.Orange);

                        break;
                    case 7:
                        v.Display = true;
                        v.ChartBrush = new SolidColorBrush(Colors.OrangeRed); break;
                    case 15:
                        v.Display = true;
                        v.ChartBrush = new SolidColorBrush(Colors.LightBlue); break;
                    case 16:
                        v.Display = true;
                        v.ChartBrush = new SolidColorBrush(Colors.LightBlue);
                        v.LineStyle = LineStyle.DashDotDot; break;
                    case 17:
                        v.Display = true;
                        v.ChartBrush = new SolidColorBrush(Colors.Green);

                        break;
                    case 21:
                        v.Display = true;
                        v.ChartBrush = new SolidColorBrush(Colors.Red); break;
                    case 40:
                        v.Display = true;
                        v.ChartBrush = new SolidColorBrush(Colors.LightYellow);
                        v.LineStyle = LineStyle.DashDot; break;
                    case 41:
                        v.Display = true;
                        v.ChartBrush = new SolidColorBrush(Colors.Yellow); break;
                    case 135:
                        v.Display = true;
                        v.ChartBrush = new SolidColorBrush(Colors.White);
                        v.LineStyle = LineStyle.Dash; break;
                    case 136:
                        v.Display = true;
                        v.ChartBrush = new SolidColorBrush(Colors.LightBlue);
                        v.LineStyle = LineStyle.DashDot; break;
                    case 137:
                        v.Display = true;
                        v.ChartBrush = new SolidColorBrush(Colors.White);
                        v.LineStyle = LineStyle.DashDotDot; break;
                    case 146:
                        v.Display = true;
                        v.ChartBrush = new SolidColorBrush(Colors.Purple);
                        break;
                    case 148:
                        v.Display = true;
                        v.ChartBrush = new SolidColorBrush(Colors.Pink);
                        break;
                    case 149:
                        v.Display = true;
                        v.ChartBrush = new SolidColorBrush(Colors.Green);
                        v.LineWidth = 2;
                        break;

                    default:
                        v.Display = false;
                        break;

                }
            }

        }





        private void RefreshTimerTick(object sender, EventArgs e)
        {
        }

        private void TimerTick(object sender, EventArgs e)
        {
            foreach (var d in OmniInstance.ConnectedDevices) //Источник тиков для ведения лога
            {
                d.LogTick();
            }

            if (AutoRedraw && System.Windows.Input.Mouse.LeftButton != System.Windows.Input.MouseButtonState.Pressed)
                if (OmniInstance.SelectedConnectedDevice?.LogCurrentPos < 600)  //Перерисовка графиков
                    ChartDraw(null);
                else if (DateTime.Now.Second % 10 == 0)
                    ChartDraw(null);


            foreach (var d in OmniInstance.ConnectedDevices.OfType<HeaterDeviceViewModel>().Where(d => d.SecondMessages)) //Поддержание связи только для котлов
            {
                OmniMessage msg = new();
                msg.TransmitterId.Address = 6;
                msg.TransmitterId.Type = 126;
                msg.Pgn = 0;
                msg.ReceiverId.Address = d.Id.Address;
                msg.ReceiverId.Type = d.Id.Type;
                OmniInstance.SendMessage(msg);
                Task.Delay(150);
            }
        }


        public void NewDeviceHandler(object sender, EventArgs e)
        {
            if (OmniInstance.ConnectedDevices.Count == 1) //Select first device
                OmniInstance.SelectedConnectedDevice = OmniInstance.ConnectedDevices[0];
        }


        public bool portOpened(object parameter)
        {
            return CanAdapter.PortOpened;
        }
        public bool deviceSelected(object parameter)
        {
            return CanAdapter.PortOpened && OmniInstance.SelectedConnectedDevice != null;
        }

        public bool deviceInManualMode(object parameter)
        {
            return (CanAdapter.PortOpened && OmniInstance.SelectedConnectedDevice is HeaterDeviceViewModel h && h.ManualMode);
        }

        private void WriteToLog(CanMessage msg, string direction)
        {
            if (!canLogging || canLogStream == null) return;
            string line;
            if (Mode == WorkMode_t.Rvc && msg.RvcCompatible)
            {
                var rvcMsg = new RvcMessage(msg);
                line = rvcMsg.PrintLine(direction) + " | " + rvcMsg.PrintParameters();
            }
            else if (Mode == WorkMode_t.Omni && msg.RvcCompatible)
            {
                try
                {
                    var omniStr = new OmniMessage(msg).ToString().TrimEnd();
                    line = omniStr.Substring(0, 12) + " " + direction + omniStr.Substring(12);
                }
                catch { line = $"{DateTime.Now:HH:mm:ss.fff} {direction} {msg}"; }
            }
            else
                line = $"{DateTime.Now:HH:mm:ss.fff} {direction} {msg}";
            canLogStream.WriteLine(line);
        }

        public void NewMessgeReceived(object sender, EventArgs e)
        {
            var msg = (e as GotCanMessageEventArgs).receivedMessage;
            WriteToLog(msg, "←");
            // Post (не Send): адаптер шлёт кадры на фоновом потоке приёма, и синхронный Send
            // блокирует этот поток на время обработки каждого сообщения в UI-потоке. При
            // всплеске из сотен кадров подряд (например, потоковое чтение PGN109) это не
            // даёт потоку приёма вовремя вычитывать очередь драйвера/адаптера, и часть
            // кадров теряется на приёме ещё до того, как долетит до этого обработчика.
            switch (Mode)
            {
                case WorkMode_t.Omni:
                    UIContext.Post(x => OmniInstance.ProcessCanMessage(msg), null); break;
                case WorkMode_t.Rvc:
                    UIContext.Post(x => RvcPage.ProcessMessage(msg), null); break;
                case WorkMode_t.RegularCan:
                    UIContext.Post(x => CanPage.ProcessMessage(msg), null); break;
            }
        }

        public void MessageTransmittedHandler(object sender, EventArgs e)
        {
            var msg = (e as GotCanMessageEventArgs).receivedMessage;
            WriteToLog(msg, "→");
        }

        // Кэшированная ссылка на себя - используется DeviceViewModel.Transmit/Bus (в т.ч. из
        // фоновых потоков команд загрузчика, см. BootloaderDeviceViewModel), где обращение к
        // Application.Current.MainWindow.DataContext бросает "The calling thread cannot access
        // this object because a different thread owns it" (Window - DispatcherObject, его
        // свойства можно читать только из потока, которому он принадлежит). Инстанс создаётся
        // один раз на UI-потоке при старте приложения, поэтому кэшированную ссылку безопасно
        // читать из любого потока.
        public static MainWindowViewModel Instance { get; private set; }

        public MainWindowViewModel()
        {
            Instance = this;

            canAdapter = new();

            // Try to refresh omnidata.json from Google Sheets before loading static data.
            // Uses a built-in timeout; silently falls back to the local file on any failure.
            CAN_Tool.Libs.GoogleSheetsUpdater.TryUpdateAsync().GetAwaiter().GetResult();

            OmniInstance = new Omni(CanAdapter);

            OmniInstance.plot = myChart;
            ManualPage = new(this);
            CanPage = new(this);
            RvcPage = new(this);

            CanAdapter.GotNewMessage += NewMessgeReceived;
            CanAdapter.MessageTransmitted += MessageTransmittedHandler;



            var timer = new DispatcherTimer();
            timer.Interval = new TimeSpan(0, 0, 0, 1, 0);
            timer.Tick += TimerTick;
            timer.Start();

            var refreshTimer = new System.Timers.Timer(250);
            refreshTimer.Elapsed += RefreshTimerTick;
            refreshTimer.Start();

            OmniInstance.NewDeviceAcquired += NewDeviceHandler;

            CustomMessage.TransmitterId.Address = 6;
            CustomMessage.TransmitterId.Type = 126;

            brushes.Add(new SolidColorBrush(Color.FromRgb(63,  81,  181))); // Indigo
            brushes.Add(new SolidColorBrush(Color.FromRgb(33,  150, 243))); // Blue
            brushes.Add(new SolidColorBrush(Color.FromRgb(3,   169, 244))); // LightBlue
            brushes.Add(new SolidColorBrush(Colors.Cyan));                   // Cyan
            brushes.Add(new SolidColorBrush(Colors.Teal));                   // Teal
            brushes.Add(new SolidColorBrush(Colors.Green));                  // Green
            brushes.Add(new SolidColorBrush(Colors.LightGreen));             // LightGreen
            brushes.Add(new SolidColorBrush(Colors.Lime));                   // Lime
            brushes.Add(new SolidColorBrush(Colors.Yellow));                 // Yellow
            brushes.Add(new SolidColorBrush(Color.FromRgb(255, 193, 7)));   // Amber
            brushes.Add(new SolidColorBrush(Colors.Orange));                 // Orange
            brushes.Add(new SolidColorBrush(Colors.OrangeRed));              // DeepOrange
            brushes.Add(new SolidColorBrush(Colors.SaddleBrown));            // Brown
            brushes.Add(new SolidColorBrush(Colors.Gray));                   // Grey
            brushes.Add(new SolidColorBrush(Colors.SlateGray));              // BlueGrey
            brushes.Add(new SolidColorBrush(Colors.Red));                    // Red
            brushes.Add(new SolidColorBrush(Colors.HotPink));                // Pink
            brushes.Add(new SolidColorBrush(Colors.Purple));                 // Purple
            brushes.Add(new SolidColorBrush(Color.FromRgb(103, 58,  183))); // DeepPurple
        }
    }
}
