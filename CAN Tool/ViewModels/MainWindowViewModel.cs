using OmniProtocol;
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
using System.Windows.Markup;
using System.Security.AccessControl;
using System.Drawing.Text;
using System.Windows.Threading;
using Microsoft.Win32;
using System.Windows;
using System.Text;
using CommunityToolkit.Mvvm.ComponentModel;
using System.Reflection;
using CommunityToolkit.Mvvm.Input;

namespace CAN_Tool.ViewModels
{

    public partial class MainWindowViewModel : ObservableObject
    {
        private SynchronizationContext UIContext = SynchronizationContext.Current;

        public string Title => "CAN Tool";

        [ObservableProperty] private List<SolidColorBrush> brushes = new();
        [ObservableProperty] public bool autoRedraw = true;

        public FirmwarePageViewModel FirmwarePage { set; get; }
        public ManualPageViewModel ManualPage { set; get; }
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

        private FileStream canLogStream;

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
                canLogStream = File.Create(path);
                canLogFileName = canLogStream.Name;
            }
        }

        [ObservableProperty] int messageDelay = 100;


        [RelayCommand]
        private void RefreshPortList(object Parameter)
        {
            PortList.Clear();
            foreach (var port in SerialPort.GetPortNames())
                PortList.Add(port);
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
                        case 0: CanAdapter.PortOpenNormal(); break;
                        case 1: CanAdapter.PortOpenListenOnly(); break;
                        case 2: CanAdapter.PortOpenSelfReception(); break;
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
            var plt = myChart.Plot;

            plt.Clear();
            if (OmniInstance == null) return;
            if (OmniInstance.SelectedConnectedDevice == null) return;

            foreach (var v in OmniInstance.SelectedConnectedDevice.Status)
                if (v.Display)
                {
                    var sig = plt.AddSignalConst(OmniInstance.SelectedConnectedDevice.LogData[v.Id].Take(OmniInstance.SelectedConnectedDevice.LogCurrentPos).ToArray(), color: v.Color, label: v.Name);
                    sig.UseParallel = false;
                    sig.LineWidth = v.LineWidth;
                    sig.LineStyle = v.LineStyle;
                    sig.MarkerShape = v.MarkShape;

                    if (v.Id == 17 || v.Id == 18) //ТН проецируется на правую ось
                        sig.YAxisIndex = 2;
                    plt.Grid(color: System.Drawing.Color.FromArgb(50, 200, 200, 200));
                    plt.Grid(lineStyle: LineStyle.Dot);
                    if (App.Settings.IsDark)
                        plt.Style(dataBackground: System.Drawing.Color.FromArgb(255, 40, 40, 40), figureBackground: System.Drawing.Color.DimGray);
                    else
                        plt.Style(dataBackground: System.Drawing.Color.WhiteSmoke, figureBackground: System.Drawing.Color.White);
                    plt.Legend(true, ScottPlot.Alignment.UpperLeft);


                }
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
            var path = Environment.GetFolderPath(Environment.SpecialFolder.Desktop) + "\\" + OmniInstance.SelectedConnectedDevice.Id.Type + "_" + DateTime.Now.ToString("HH-mm-ss_dd-MM-yy") + ".csv";

            using (var sw = new StreamWriter(path))
            {
                foreach (var v in OmniInstance.SelectedConnectedDevice.Status)
                    sw.Write(GetString($"vars_{v.Id}") + ";");
                sw.WriteLine();
                for (var i = 0; i < OmniInstance.SelectedConnectedDevice.LogCurrentPos; i++)
                {
                    foreach (var v in OmniInstance.SelectedConnectedDevice.Status)
                        sw.Write(OmniInstance.SelectedConnectedDevice.LogData[v.Id][i].ToString(v.AssignedParameter.OutputFormat) + ";");
                    sw.WriteLine();
                }
                sw.Flush();
                sw.Close();
            }
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
                    default:
                        v.Display = false;
                        break;

                }
            }

        }





        private void RefreshTimerTick(object sender, EventArgs e)
        {
            foreach (var m in OmniInstance.Messages)
                m.FreshCheck();
        }

        private void TimerTick(object sender, EventArgs e)
        {
            foreach (var d in OmniInstance.ConnectedDevices) //Источник тиков для ведения лога
            {
                d.LogTick();
            }

            if (AutoRedraw)                                 //Перерисовка графиков
                if (OmniInstance.SelectedConnectedDevice?.LogCurrentPos < 600)
                    ChartDraw(null);
                else if (DateTime.Now.Second % 10 == 0)
                    ChartDraw(null);


            foreach (var d in OmniInstance.ConnectedDevices.Where(d => d.SecondMessages)) //Поддержание связи только для котлов
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
            return (CanAdapter.PortOpened && OmniInstance.SelectedConnectedDevice != null && OmniInstance.SelectedConnectedDevice.ManualMode);
        }

        public void NewMessgeReceived(object sender, EventArgs e)
        {
            UIContext.Send(x => OmniInstance.ProcessCanMessage((e as GotCanMessageEventArgs).receivedMessage), null);
        }

        public MainWindowViewModel()
        {

            canAdapter = new();

            OmniInstance = new Omni(CanAdapter);

            OmniInstance.plot = myChart;
            FirmwarePage = new(this);
            ManualPage = new(this);
            CanPage = new(this);

            CanAdapter.GotNewMessage += NewMessgeReceived;



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
            brushes.Add(new SolidColorBrush(Colors.Red));
            brushes.Add(new SolidColorBrush(Colors.DeepPink));
            brushes.Add(new SolidColorBrush(Colors.MediumPurple));
            brushes.Add(new SolidColorBrush(Colors.BlueViolet));
            brushes.Add(new SolidColorBrush(Colors.DarkSlateBlue));
        }
    }
}
