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
using System.Windows.Threading;
using Microsoft.Win32;
using System.Windows;
using System.Text;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using System.Reflection;

namespace CAN_Tool.ViewModels
{
    public enum WorkMode_t { Omni, Rvc, RegularCan }
    public enum PhyProt_t { CAN, UART }

    public partial class MainWindowViewModel : ObservableObject
    {
        private SynchronizationContext UIContext = SynchronizationContext.Current;

        

        public WorkMode_t[] WorkModes => new WorkMode_t[] { WorkMode_t.Omni, WorkMode_t.Rvc, WorkMode_t.RegularCan };
        public PhyProt_t[] PhyProtocols => new PhyProt_t[2] { PhyProt_t.CAN, PhyProt_t.UART };

        public string Title => "CAN Tool";
        
        [ObservableProperty] private List<SolidColorBrush> brushes = new();
        [ObservableProperty] private WorkMode_t mode;
        [ObservableProperty] private PhyProt_t selectedProtocol;
        [ObservableProperty] public bool autoRedraw = true;

        public FirmwarePageViewModel FirmwarePage { set; get; }
        public ManualPageViewModel ManualPage { set; get; }
        public RvcPageViewModel RvcPage { set; get; }
        public CanPageViewModel CanPage { set; get; }


        [ObservableProperty] private bool canAdapterSettings = false;
        [ObservableProperty] private string portButtonString = GetString("b_open");
        [ObservableProperty] private int selectedCanBitrate = 5;
        [ObservableProperty] private CanAdapter canAdapter;
        [ObservableProperty] UartAdapter uartAdapter;
        [ObservableProperty] public Omni omniInstance;

        public Dictionary<int, OmniCommand> Commands => Omni.Commands;

        public WpfPlot myChart;

        [ObservableProperty]      private OmniMessage selectedMessage;
        [ObservableProperty] OmniMessage customMessage = new OmniMessage() { Pgn = 0, ReceiverId = new(27, 0), };

        public Dictionary<int, OmniCommand> CommandList { get; } = Omni.Commands;

        public double[] CommandParametersArray;
        [ObservableProperty]    private string portName = "";
        

        [ObservableProperty] private BindingList<string> portList = new();
        [ObservableProperty] private string toggleCanLogButtonName = GetString("b_start_can_log");

        private bool canLogging = false;

        private string canLogFileName = "";

        [ObservableProperty] private string toggleUartLogButtonName = GetString("b_start_uart_log");
        

        private bool uartLogging = false;

        private string uartLogFileName = "";



        public RelayCommand ToggleCanLogCommand { get; }

        private FileStream canLogStream;

        private void OnToggleCanLogCommandExecuted()
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


        private bool CanToggleCanLogCommandExecute() => CanAdapter.PortOpened;


        public RelayCommand ToggleUartLogCommand { get; }

        private FileStream uartLogStream;

        private void OnToggleUartLogCommandExecuted()
        {
            if (uartLogging)
            {
                uartLogging = false;
                ToggleUartLogButtonName = GetString("b_start_uart_log");
                uartLogStream.Flush();
                uartLogStream.Close();
            }
            else
            {
                uartLogging = true;
                ToggleCanLogButtonName = GetString("b_stop_uart_log");
                var path = Environment.GetFolderPath(Environment.SpecialFolder.Desktop) + "\\UAR Log_" + DateTime.Now.ToLongTimeString().Replace(':', '.') + ".txt";
                uartLogStream = File.Create(path);
                uartLogFileName = uartLogStream.Name;
            }
        }

        private bool CanToggleUartLogCommandExecute() => UartAdapter.SelectedPort.IsOpen;

        [ObservableProperty] int messageDelay = 100;

        public RelayCommand SetAdapterNormalModeCommand { get; }

        private void OnSetAdapterNormalModeCommandExecuted() => CanAdapter.StartNormal();
        private bool CanSetAdapterNormalModeCommandExecute() => CanAdapter.PortOpened;

        public RelayCommand SetAdapterListedModeCommand { get; }

        private void OnSetAdapterListedModeCommandExecuted() => CanAdapter.StartListen();
        private bool CanSetAdapterListedModeCommandExecute() => CanAdapter.PortOpened;


        public RelayCommand SetAdapterSelfReceptionModeCommand { get; }

        private void OnSetAdapterSelfReceptionModeCommandExecuted() => CanAdapter.StartSelfReception();
        private bool CanSetAdapterSelfReceptionModeCommandExecute() => CanAdapter.PortOpened;


        public RelayCommand StopCanAdapterCommand { get; }

        private void OnStopCanAdapterCommandExecuted() => CanAdapter.Stop();
        private bool CanStopCanAdapterCommandExecute() => CanAdapter.PortOpened;



        public RelayCommand RefreshPortListCommand { get; }
        private void OnRefreshPortsCommandExecuted()
        {
            PortList.Clear();
            foreach (var port in SerialPort.GetPortNames())
                PortList.Add(port);
            if (PortList.Count > 0)
                PortName = PortList[^1];
        }


        public RelayCommand TogglePortCommand { get; }
        private void OnTogglePortCommandExecuted()
        {
            try
            {

                if (SelectedProtocol == PhyProt_t.CAN)
                {
                    if (!CanAdapter.PortOpened)
                    {
                        CanAdapter.PortName = PortName;
                        CanAdapter.PortOpen();
                        Thread.Sleep(100);
                        PortButtonString = GetString("b_close");
                        CanAdapter.StartNormal();
                    }
                    else
                    {
                        CanAdapter.Stop();
                        Thread.Sleep(100);
                        PortButtonString = GetString("b_open");
                        CanAdapter.PortClose();
                    }
                }
                if (SelectedProtocol == PhyProt_t.UART)
                {
                    if (!UartAdapter.SelectedPort.IsOpen)
                    {
                        try
                        {
                            UartAdapter.SelectedPort.Open();
                            PortButtonString = GetString("b_close");
                        }
                        catch { }
                    }
                    else
                    {
                        UartAdapter.SelectedPort.Close();
                        PortButtonString = GetString("b_open");
                    }
                }
            }
            catch
            (Exception ex)
            {
                MessageBox.Show(ex.Message);
            }
        }
        private bool CanTogglePortCommandExecute() => (PortName.StartsWith("COM") || CanAdapter.PortOpened || UartAdapter.SelectedPort.IsOpen);


        public RelayCommand LoadFromLogCommand { get; }

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

        private async void OnLoadFromLogCommandExecuted()
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

        public RelayCommand SendFromLogCommand { get; }

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

        private async void OnSendFromLogCommandExecuted()
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




        public RelayCommand LogStartCommand { get; }
        private void OnLogStartCommandExecuted()
        {
            OmniInstance.SelectedConnectedDevice.LogStart();
        }
        private bool CanLogStartCommandExecute() => (OmniInstance.SelectedConnectedDevice != null && CanAdapter.PortOpened);



        public RelayCommand LogStopCommand { get; }
        private void OnLogStopCommandExecuted()
        {
            OmniInstance.SelectedConnectedDevice.LogStop();
        }
        private bool CanLogStopCommandExecute() => (OmniInstance.SelectedConnectedDevice != null && CanAdapter.PortOpened && OmniInstance.SelectedConnectedDevice.IsLogWriting);



        public RelayCommand ChartDrawCommand { get; }
        private void OnChartDrawCommandExecuted()
        {
            var plt = myChart.Plot;

            plt.Clear();

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
        private bool CanChartDrawCommandExecute() => (OmniInstance.SelectedConnectedDevice != null && OmniInstance.SelectedConnectedDevice.LogCurrentPos > 0);


        public RelayCommand CancelOperationCommand { get; }
        private void OnCancelOperationCommandExecuted()
        {
            OmniInstance.CurrentTask.OnCancel();
        }
        private bool CanCancelOperationCommandExecute() => (OmniInstance.CurrentTask.Occupied);




        public RelayCommand ReadConfigCommand { get; }
        private void OnReadConfigCommandExecuted()
        {
            OmniInstance.ReadAllParameters(OmniInstance.SelectedConnectedDevice.Id);
        }

        public RelayCommand SaveConfigCommand { get; }
        private void OnSaveConfigCommandExecuted()
        {
            OmniInstance.SaveParameters(OmniInstance.SelectedConnectedDevice.Id);
        }
        private bool CanSaveConfigCommandExecute() =>
            (CanAdapter.PortOpened && OmniInstance.SelectedConnectedDevice != null && !OmniInstance.CurrentTask.Occupied && OmniInstance.SelectedConnectedDevice.ReadParameters.Count > 0 && OmniInstance.SelectedConnectedDevice.Parameters.Stage == 0);



        public RelayCommand ResetConfigCommand { get; }
        private void OnResetConfigCommandExecuted()
        {
            OmniInstance.ResetParameters(OmniInstance.SelectedConnectedDevice.Id);
        }
        private bool CanResetConfigCommandExecute() =>
            (CanAdapter.PortOpened && OmniInstance.SelectedConnectedDevice != null && !OmniInstance.CurrentTask.Occupied && OmniInstance.SelectedConnectedDevice.Parameters.Stage == 0);






        public RelayCommand ReadBlackBoxDataCommand { get; }
        private void OnReadBlackBoxDataCommandExecuted()
        {
            OmniInstance.ReadBlackBoxData(OmniInstance.SelectedConnectedDevice.Id);
        }
        private bool CanReadBlackBoxDataExecute() =>
            (CanAdapter.PortOpened && OmniInstance.SelectedConnectedDevice != null && !OmniInstance.CurrentTask.Occupied && OmniInstance.SelectedConnectedDevice.Parameters.Stage == 0);



        public RelayCommand ReadBlackBoxErrorsCommand { get; }
        private void OnReadBlackBoxErrorsCommandExecuted()
        {
            Task.Run(() => OmniInstance.ReadErrorsBlackBox(OmniInstance.SelectedConnectedDevice.Id));
        }
        private bool CanReadBlackBoxErrorsExecute() =>
            (CanAdapter.PortOpened && OmniInstance.SelectedConnectedDevice != null && !OmniInstance.CurrentTask.Occupied && OmniInstance.SelectedConnectedDevice.Parameters.Stage == 0);



        public RelayCommand EraseBlackBoxErrorsCommand { get; }
        private void OnEraseBlackBoxErrorsCommandExecuted()
        {
            Task.Run(() => OmniInstance.EraseErrorsBlackBox(OmniInstance.SelectedConnectedDevice.Id));
        }
        private bool CanEraseBlackBoxErrorsExecute() =>
            (CanAdapter.PortOpened && OmniInstance.SelectedConnectedDevice != null && !OmniInstance.CurrentTask.Occupied && OmniInstance.SelectedConnectedDevice.Parameters.Stage == 0);



        public RelayCommand EraseBlackBoxDataCommand { get; }
        private void OnEraseBlackBoxDataCommandExecuted()
        {
            Task.Run(() => OmniInstance.EraseCommonBlackBox(OmniInstance.SelectedConnectedDevice.Id));
        }
        private bool CanEraseBlackBoxDataExecute() =>
            (CanAdapter.PortOpened && OmniInstance.SelectedConnectedDevice != null && !OmniInstance.CurrentTask.Occupied && OmniInstance.SelectedConnectedDevice.Parameters.Stage == 0);



        public RelayCommand SaveReportCommand { get; }
        private void OnSaveReportCommandExecuted()
        {
            var dev  = OmniInstance.SelectedConnectedDevice;
            var path = Environment.GetFolderPath(Environment.SpecialFolder.Desktop) + '\\' +
                       dev.Name + " " + DateTime.Now.ToString("dd-MM-yyyy HH-mm") + ".docx";
            var doc  = DocX.Create(path);

            // ── Палитра ──────────────────────────────────────────────────────────
            var cDarkSlate  = System.Drawing.Color.FromArgb( 30,  45,  60);   // почти чёрный синий — шапки
            var cAccent     = System.Drawing.Color.FromArgb( 41,  98, 156);   // синий — заголовки секций
            var cHeaderBg   = System.Drawing.Color.FromArgb( 52, 117, 182);   // синий — строка-шапка таблиц
            var cRowOdd     = System.Drawing.Color.FromArgb(235, 240, 248);   // голубоватый — нечётные строки
            var cRowEven    = System.Drawing.Color.FromArgb(255, 255, 255);   // белый — чётные строки
            var cLabelBg    = System.Drawing.Color.FromArgb(225, 230, 238);   // серо-синий — колонка «параметр»
            var cWhite      = System.Drawing.Color.White;
            var cTextDark   = System.Drawing.Color.FromArgb( 30,  30,  30);
            var cErrDark    = System.Drawing.Color.FromArgb(164,   0,   0);
            var cErrHeader  = System.Drawing.Color.FromArgb(192,  40,  40);
            var cErrRowOdd  = System.Drawing.Color.FromArgb(255, 240, 240);

            // ── Вспомогательные методы ───────────────────────────────────────────
            Paragraph SectionBanner(string text, System.Drawing.Color bg, System.Drawing.Color fg, double size = 13)
            {
                var t = doc.AddTable(1, 1);
                t.AutoFit = AutoFit.Window;
                t.Design   = TableDesign.None;
                var cell = t.Rows[0].Cells[0];
                cell.FillColor = bg;
                var p = cell.Paragraphs[0];
                p.Append(text.ToUpper()).Bold().FontSize(size).Font("Calibri Light").Color(fg);
                p.Alignment = Alignment.left;
                doc.InsertTable(t);
                return doc.InsertParagraph().SpacingAfter(2);
            }

            void StyledHeaderCell(Cell cell, string text, System.Drawing.Color bg)
            {
                cell.FillColor = bg;
                var p = cell.Paragraphs[0];
                p.Append(text).Bold().FontSize(10).Font("Calibri").Color(cWhite);
                p.Alignment = Alignment.center;
            }

            void StyledDataCell(Cell cell, string text, System.Drawing.Color bg, bool bold = false)
            {
                cell.FillColor = bg;
                var p = cell.Paragraphs[0];
                var f = p.Append(text).FontSize(10).Font("Calibri").Color(cTextDark);
                if (bold) f.Bold();
            }

            // ══ ТИТУЛЬНЫЙ БЛОК ═══════════════════════════════════════════════════
            var titlePara = doc.InsertParagraph();
            titlePara.Append(GetString("t_device_report").ToUpper())
                     .Bold().FontSize(26).Font("Calibri Light").Color(cDarkSlate);
            titlePara.Alignment = Alignment.center;
            titlePara.SpacingAfter(2);

            // Цветная полоса с именем устройства
            var bannerT = doc.AddTable(1, 1);
            bannerT.AutoFit = AutoFit.Window;
            bannerT.Design = TableDesign.None;
            var bannerCell = bannerT.Rows[0].Cells[0];
            bannerCell.FillColor = cDarkSlate;
            bannerCell.Paragraphs[0]
                .Append(dev.Name).Bold().FontSize(16).Font("Calibri Light").Color(cWhite);
            bannerCell.Paragraphs[0].Alignment = Alignment.center;
            doc.InsertTable(bannerT);
            doc.InsertParagraph().SpacingAfter(6);

            // ══ ИНФОРМАЦИЯ ОБ УСТРОЙСТВЕ ══════════════════════════════════════════
            var infoTable = doc.AddTable(4, 2);
            infoTable.AutoFit = AutoFit.Window;
            infoTable.Design  = TableDesign.TableGrid;

            var serial = $"{dev.Serial[0]}.{dev.Serial[1]}.{dev.Serial[2]}";
            (string label, string value)[] infoRows =
            {
                (GetString("t_device_report"),       dev.Name),
                (GetString("t_serial_number"),        serial),
                (GetString("t_manufacturing_date"),   dev.ProductionDate.ToString()),
                (GetString("t_formed"),               DateTime.Now.ToString("dd.MM.yyyy  HH:mm:ss")),
            };

            for (var i = 0; i < infoRows.Length; i++)
            {
                var rowBg = i % 2 == 0 ? cRowEven : cRowOdd;
                StyledDataCell(infoTable.Rows[i].Cells[0], infoRows[i].label, cLabelBg, bold: true);
                StyledDataCell(infoTable.Rows[i].Cells[1], infoRows[i].value, rowBg);
            }

            doc.InsertTable(infoTable);
            doc.InsertParagraph().SpacingAfter(10);

            // ══ ДАННЫЕ ЧЁРНОГО ЯЩИКА ══════════════════════════════════════════════
            if (dev.BbValues.Count > 0)
            {
                SectionBanner(GetString("t_common_black_box_data"), cAccent, cWhite);

                var bbTable = doc.AddTable(dev.BbValues.Count + 1, 2);
                bbTable.AutoFit = AutoFit.Window;
                bbTable.Design  = TableDesign.TableGrid;

                StyledHeaderCell(bbTable.Rows[0].Cells[0], GetString("t_parameter"), cHeaderBg);
                StyledHeaderCell(bbTable.Rows[0].Cells[1], GetString("t_value"),     cHeaderBg);

                for (var i = 0; i < dev.BbValues.Count; i++)
                {
                    var p   = dev.BbValues[i];
                    var rowBg = i % 2 == 0 ? cRowEven : cRowOdd;
                    StyledDataCell(bbTable.Rows[i + 1].Cells[0], GetString($"bb_{p.Id}"),  cLabelBg);
                    StyledDataCell(bbTable.Rows[i + 1].Cells[1], p.Value.ToString(), rowBg, bold: true);
                }

                doc.InsertTable(bbTable);
                doc.InsertParagraph().SpacingAfter(10);
            }

            // ══ ОШИБКИ ════════════════════════════════════════════════════════════
            if (dev.BbErrors.Count > 0)
            {
                SectionBanner($"{GetString("t_errors_found")}: {dev.BbErrors.Count}",
                              cErrDark, cWhite);

                foreach (var e in dev.BbErrors)
                {
                    // Шапка ошибки — отдельная однострочная таблица-баннер
                    var errBannerT = doc.AddTable(1, 1);
                    errBannerT.AutoFit = AutoFit.Window;
                    errBannerT.Design  = TableDesign.None;
                    errBannerT.Rows[0].Cells[0].FillColor = cErrHeader;
                    errBannerT.Rows[0].Cells[0].Paragraphs[0]
                        .Append(e.Name).Bold().FontSize(11).Font("Calibri").Color(cWhite);
                    doc.InsertTable(errBannerT);

                    if (e.Variables.Count == 0)
                    {
                        doc.InsertParagraph().SpacingAfter(4);
                        continue;
                    }

                    var errTable = doc.AddTable(e.Variables.Count + 1, 2);
                    errTable.AutoFit = AutoFit.Window;
                    errTable.Design  = TableDesign.TableGrid;

                    StyledHeaderCell(errTable.Rows[0].Cells[0], GetString("t_parameter"), cErrHeader);
                    StyledHeaderCell(errTable.Rows[0].Cells[1], GetString("t_value"),     cErrHeader);

                    for (var i = 0; i < e.Variables.Count; i++)
                    {
                        var v     = e.Variables[i];
                        var rowBg = i % 2 == 0 ? cRowEven : cErrRowOdd;
                        StyledDataCell(errTable.Rows[i + 1].Cells[0], v.Name,             cLabelBg);
                        StyledDataCell(errTable.Rows[i + 1].Cells[1], v.Value.ToString(), rowBg, bold: true);
                    }

                    doc.InsertTable(errTable);
                    doc.InsertParagraph().SpacingAfter(8);
                }
            }

            doc.Save();
        }
        private bool CanSaveReportCommandExecute() =>
        (OmniInstance.SelectedConnectedDevice != null && (OmniInstance.SelectedConnectedDevice.BbErrors.Count > 0 || OmniInstance.SelectedConnectedDevice.BbValues.Count > 0));



        public RelayCommand SendCustomMessageCommand { get; }
        private void OnSendCustomMessageCommandExecuted()
        {
            CustomMessage.TransmitterId.Address = 6;
            CustomMessage.TransmitterId.Type = 126;
            OmniInstance.SendMessage(CustomMessage);
        }
        private bool CanSendCustomMessageCommandExecute()
        {
            if (!CanAdapter.PortOpened) return false;
            return true;
        }


        public RelayCommand SaveLogCommand { get; }

        private void OnSaveLogCommandExecuted()
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

        private bool CanSaveLogCommandExecuted()
        {
            return OmniInstance.SelectedConnectedDevice != null && OmniInstance.SelectedConnectedDevice.LogCurrentPos > 0;
        }

        public RelayCommand DefaultStyleCommand { get; }

        private void OnDefaultStyleExecuted()
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
                if (OmniInstance.SelectedConnectedDevice != null && OmniInstance.SelectedConnectedDevice.LogCurrentPos > 0)
                {
                    if (OmniInstance.SelectedConnectedDevice.LogCurrentPos < 600)
                        OnChartDrawCommandExecuted();
                    else if (DateTime.Now.Second % 10 == 0)
                        OnChartDrawCommandExecuted();
                }

            foreach (var d in OmniInstance.ConnectedDevices.Where(d=>d.SecondMessages)) //Поддержание связи только для котлов
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
            if (OmniInstance.ConnectedDevices.Count==1) //Select first device
                OmniInstance.SelectedConnectedDevice = OmniInstance.ConnectedDevices[0];
        }


        public bool portOpened()
        {
            return CanAdapter.PortOpened;
        }
        public bool deviceSelected()
        {
            return CanAdapter.PortOpened && OmniInstance.SelectedConnectedDevice != null;
        }

        public bool deviceInManualMode()
        {
            return (CanAdapter.PortOpened && OmniInstance.SelectedConnectedDevice != null && OmniInstance.SelectedConnectedDevice.ManualMode);
        }

        public void NewMessgeReceived(object sender, EventArgs e)
        {
            switch (SelectedProtocol)
            {
                case PhyProt_t.CAN:
                    switch (Mode)
                    {
                        case 
                            WorkMode_t.Omni: 
                            
                            UIContext.Send(x => OmniInstance.ProcessCanMessage((e as GotCanMessageEventArgs).receivedMessage), null);
                            break;
                        case WorkMode_t.Rvc: UIContext.Send(x => RvcPage.ProcessMessage((e as GotCanMessageEventArgs).receivedMessage), null); break;
                        case WorkMode_t.RegularCan: UIContext.Send(x => CanPage.ProcessMessage((e as GotCanMessageEventArgs).receivedMessage), null); break;
                    }


                    if (canLogging && canLogStream != null && canLogStream.CanWrite)
                    {
                        canLogStream.Write(Encoding.ASCII.GetBytes((e as GotCanMessageEventArgs).receivedMessage.ToShortString() + Environment.NewLine));
                    }
                    break;
                case PhyProt_t.UART:
                    UIContext.Send(x => OmniInstance.ProcessOmniMessage((e as GotOmniMessageEventArgs).receivedMessage), null);
                    if (uartLogging && uartLogStream != null && uartLogStream.CanWrite)
                    {
                        canLogStream.Write(Encoding.ASCII.GetBytes((e as GotOmniMessageEventArgs).receivedMessage.ToString() + Environment.NewLine));
                    }
                    break;
            }
        }

        public MainWindowViewModel()
        {

            canAdapter = new();
            uartAdapter = new();

            OmniInstance = new Omni(CanAdapter, uartAdapter);

            OmniInstance.plot = myChart;
            FirmwarePage = new(this);
            RvcPage = new(this);
            ManualPage = new(this);
            CanPage = new(this);

            CanAdapter.GotNewMessage += NewMessgeReceived;



            var timer = new DispatcherTimer();
            timer.Interval = new TimeSpan(0, 0, 0, 1 ,0);
            timer.Tick += TimerTick;
            timer.Start();

            var refreshTimer = new System.Timers.Timer(250);
            refreshTimer.Elapsed += RefreshTimerTick;
            refreshTimer.Start();

            OmniInstance.NewDeviceAcquired += NewDeviceHandler;

            TogglePortCommand = new RelayCommand(OnTogglePortCommandExecuted);
            RefreshPortListCommand = new RelayCommand(OnRefreshPortsCommandExecuted);
            ReadConfigCommand = new RelayCommand(OnReadConfigCommandExecuted);
            ReadBlackBoxDataCommand = new RelayCommand(OnReadBlackBoxDataCommandExecuted);
            ReadBlackBoxErrorsCommand = new RelayCommand(OnReadBlackBoxErrorsCommandExecuted);
            EraseBlackBoxErrorsCommand = new RelayCommand(OnEraseBlackBoxErrorsCommandExecuted);
            EraseBlackBoxDataCommand = new RelayCommand(OnEraseBlackBoxDataCommandExecuted);
            SendCustomMessageCommand = new RelayCommand(OnSendCustomMessageCommandExecuted);
            CancelOperationCommand = new RelayCommand(OnCancelOperationCommandExecuted);
            SaveConfigCommand = new RelayCommand(OnSaveConfigCommandExecuted);
            ResetConfigCommand = new RelayCommand(OnResetConfigCommandExecuted);
            SetAdapterNormalModeCommand = new RelayCommand(OnSetAdapterNormalModeCommandExecuted);
            SetAdapterListedModeCommand = new RelayCommand(OnSetAdapterListedModeCommandExecuted);
            SetAdapterSelfReceptionModeCommand = new RelayCommand(OnSetAdapterSelfReceptionModeCommandExecuted);
            StopCanAdapterCommand = new RelayCommand(OnStopCanAdapterCommandExecuted);

            LogStartCommand = new RelayCommand(OnLogStartCommandExecuted);
            LogStopCommand = new RelayCommand(OnLogStopCommandExecuted);
            ChartDrawCommand = new RelayCommand(OnChartDrawCommandExecuted);
            LoadFromLogCommand = new RelayCommand(OnLoadFromLogCommandExecuted);
            SendFromLogCommand = new RelayCommand(OnSendFromLogCommandExecuted);

            SaveLogCommand = new RelayCommand(OnSaveLogCommandExecuted);
            DefaultStyleCommand = new RelayCommand(OnDefaultStyleExecuted);
            SaveReportCommand = new RelayCommand(OnSaveReportCommandExecuted);

            ToggleCanLogCommand = new RelayCommand(OnToggleCanLogCommandExecuted);


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
