using Can_Adapter;
using CAN_Tool.ViewModels.Base;
using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Diagnostics;
using System.Diagnostics.CodeAnalysis;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Media;
using CAN_Tool.Libs;
using CAN_Tool;
using ScottPlot;
using static CAN_Tool.Libs.Helper;

namespace OmniProtocol
{

    public class PGN
    {
        public int id;
        public string name = "";
        public bool multipack;
        public List<OmniPgnParameter> parameters = new();
    }
    public class OmniPgnParameter : ViewModel
    {
        public string ParamsName = "";//from ParamsName.h
        internal int StartByte;   //Начальный байт в пакете
        internal int StartBit;    //Начальный бит в байте
        internal int BitLength;   //Длина параметра в битах
        internal bool Signed = false; //Число со знаком
        public string Name { set; get; }     //Имя параметра
        internal double a = 1;         //коэффициент приведения
        internal double b = 0;         //смещение
        //value = rawData*a+b
        public string Unit { get; set; } = "";//Единица измерения

        private string format;

        internal Dictionary<int, string> Meanings { set; get; } = new();
        internal Func<int, string> GetMeaning; //Принимает на вход сырое значение, возвращает строку с расшифровкой значения параметра
        internal Func<byte[], string> CustomDecoder; //Если для декодирования нужен весь пакет данных
        internal int PackNumber; //Номер пакета в мультипакете
        internal int Var; //Соответствующая переменная из paramsName.h
        public double DefaultValue; //Для конструктора комманд
        public bool AnswerOnly = false;
        private bool id = false;

        public string OutputFormat
        {
            get
            {
                if (format != null)
                    return format;
                else
                {
                    if (a == 1)
                        return "";
                    else if (a >= 0.09)
                        return "0.0";
                    else
                        return "0.00";
                }
            }
            set => Set(ref format, value);
        }


        public string Decode(long rawValue)
        {
            StringBuilder retString = new();
            retString.Append(Name + ": ");
            if (CustomDecoder != null)
                return "Custom decoders not supported";

            if (GetMeaning != null)
                return GetMeaning((int)rawValue);
            if (Meanings != null && Meanings.ContainsKey((int)rawValue))
                retString.Append(rawValue.ToString() + " - " + Meanings[(int)rawValue]);
            else
            {
                if (rawValue == Math.Pow(2, BitLength) - 1)
                    retString.Append($"Нет данных({rawValue})");
                else
                {

                    double rawDouble = (double)rawValue;
                    if (Signed && rawValue > Math.Pow(2, BitLength - 1))
                        rawDouble *= -1;
                    double value = rawDouble * a + b;
                    retString.Append(value.ToString(OutputFormat) + " " + Unit);
                }
            }
            return retString.ToString();
        }

        public override string ToString()
        {
            return Name;
        }
    }
    public class OmniCommand : ViewModel
    {
        int id;
        public int Id
        {
            get => id;
            internal set => Set(ref id, value);
        }

        public string Name { internal set; get; }

        private readonly List<OmniPgnParameter> _Parameters = new();
        public List<OmniPgnParameter> Parameters => _Parameters;
        public override string ToString() => Name;

    }

    public class ZoneHandler : ViewModel
    {
        public ZoneHandler(Action<ZoneHandler> NotifierAction)
        {
            selectedZoneChanged = NotifierAction;
        }
        private Action<ZoneHandler> selectedZoneChanged;

        private int tempSetpointDay = 22;
        public int TempSetpointDay { set => Set(ref tempSetpointDay, value); get => tempSetpointDay; }

        private int tempSetpointNight = 20;
        public int TempSetpointNight { set => Set(ref tempSetpointNight, value); get => tempSetpointNight; }

        private int currentTemperature = 0;
        public int CurrentTemperature { set => Set(ref currentTemperature, value); get => currentTemperature; }

        private bool connected = false;
        public bool Connected { set => Set(ref connected, value); get => connected; }

        private bool state = false;
        public bool State { set => Set(ref state, value); get => state; }

        private bool selected = false;
        public bool Selected { set { Set(ref selected, value); if (value) selectedZoneChanged(this); } get => selected; }

        private bool manualMode = false;
        public bool ManualMode { set => Set(ref manualMode, value); get => manualMode; }

        private int manualPercent = 40;
        public int ManualPercent { set => Set(ref manualPercent, value); get => manualPercent; }

        private int settedPwmPercent = 50;
        public int SettedPwmPercent { set => Set(ref settedPwmPercent, value); get => settedPwmPercent; }

        private int fanStage = 2;
        public int FanStage { set => Set(ref fanStage, value); get => fanStage; }

        private int currentPwm = 50;
        public int CurrentPwm { set => Set(ref currentPwm, value); get => currentPwm; }

        public Task ManualPercentChangeTask;

        public bool GotChange = false;

    }
    public class TimberlineHandler : ViewModel
    {
        private void ZoneChanged(ZoneHandler newSelectedZone)
        {
            SelectedZone = newSelectedZone;
        }
        public TimberlineHandler()
        {
            for (int i = 0; i < 5; i++)
            {
                Zones.Add(new ZoneHandler(ZoneChanged));
            }

            SelectedZone = zones[0];
            zones[0].Selected = true;
        }

        private int tankTemperature;
        public int TankTempereature { set => Set(ref tankTemperature, value); get => tankTemperature; }

        private int outsideTemperature;
        public int OutsideTemperature { set => Set(ref outsideTemperature, value); get => outsideTemperature; }

        private int liquidLevel;
        public int LiquidLevel { set => Set(ref liquidLevel, value); get => liquidLevel; }

        private bool heaterEnabled;
        public bool HeaterEnabled { set => Set(ref heaterEnabled, value); get => heaterEnabled; }

        private bool elementEbabled;
        public bool ElementEbabled { set => Set(ref elementEbabled, value); get => elementEbabled; }

        private bool domesticWaterFlow;
        public bool DomesticWaterFlow { set => Set(ref domesticWaterFlow, value); get => domesticWaterFlow; }

        public DateTime time;

        public DateTime Time { set => Set(ref time, value); get => time; }

        public BindingList<ZoneHandler> Zones => zones;

        private BindingList<ZoneHandler> zones = new();

        private ZoneHandler selectedZone;
        public ZoneHandler SelectedZone { set => Set(ref selectedZone, value); get => selectedZone; }


    }
    public class OmniMessage : CanMessage, IUpdatable<OmniMessage>
    {
        public OmniMessage() : base()
        {
            Fresh = true;
            Task.Run(() => { Task.Delay(300); Fresh = false; });
            DLC = 8;
            RTR = false;
            IDE = true;
            TransmitterType = 126;
            TransmitterAddress = 6;
            ReceiverAddress = 0;

        }
        public OmniMessage(CanMessage m) : this()
        {
            if (m.DLC != 8 || m.RTR || !m.IDE)
                throw new ArgumentException("CAN message is not compliant with OmniProtocol");
            Data = m.Data;
            Id = m.Id;
            return;
        }

        private bool fresh;

        public bool Fresh { get => fresh; set => Set(ref fresh, value); }

        [AffectsTo(nameof(VerboseInfo))]
        public int PGN
        {
            get { return (Id >> 20) & 0x1FF; }
            set
            {
                if (value > 511)
                    throw new ArgumentException("PGN can't be over 511");
                if (Id == value)
                    return;
                int temp = Id;
                temp &= ~(0x1FF << 20);
                temp |= value << 20;
                Id = temp;

            }
        }
        [AffectsTo(nameof(VerboseInfo))]
        public int ReceiverType
        {
            get { return (Id >> 13) & 0b1111111; }
            set
            {
                if (value > 127)
                    throw new ArgumentException("ReceiverType can't be over 127");
                if (ReceiverType == value)
                    return;
                int temp = Id;
                temp &= ~(0x7F << 13);
                temp |= value << 13;
                Id = temp;
            }
        }
        [AffectsTo(nameof(VerboseInfo))]
        public int ReceiverAddress
        {
            get { return (Id >> 10) & 0b111; }
            set
            {
                if (value > 7)
                    throw new ArgumentException("ReceiverAddress can't be over 7");
                if (ReceiverAddress == value)
                    return;
                int temp = Id;
                temp &= ~(0x7 << 10);
                temp |= value << 10;
                Id = temp;
            }
        }
        [AffectsTo(nameof(VerboseInfo))]
        public int TransmitterType
        {
            get { return (Id >> 3) & 0x7F; }
            set
            {
                if (value > 127)
                    throw new ArgumentException("TransmitterType can't be over 127");
                if (TransmitterType == value)
                    return;
                int temp = Id;
                temp &= ~(0x7F << 3);
                temp |= value << 3;
                Id = temp;
            }
        }
        [AffectsTo(nameof(VerboseInfo))]
        public int TransmitterAddress
        {
            get { return Id & 0b111; }
            set
            {
                if (value > 7)
                    throw new ArgumentException("TransmitterAddress can't be over 7");
                if (TransmitterAddress == value)
                    return;
                int temp = Id;
                temp &= ~(0x3);
                temp |= value;
                Id = temp;
            }
        }

        [AffectsTo(nameof(VerboseInfo))]
        public DeviceId TransmitterId
        {
            get { return new DeviceId(TransmitterType, TransmitterAddress); }
            set { TransmitterType = value.Type; TransmitterAddress = value.Address; }
        }

        [AffectsTo(nameof(VerboseInfo))]
        public DeviceId ReceiverId
        {
            get { return new DeviceId(ReceiverType, ReceiverAddress); }
            set
            {
                ReceiverType = value.Type;
                ReceiverAddress = value.Address;
            }
        }


        public static long getRaw(byte[] data, int bitLength, int startBit, int startByte, bool signed)
        {
            long ret;
            switch (bitLength)
            {
                case 1: ret = data[startByte] >> startBit & 0b1; break;
                case 2: ret = data[startByte] >> startBit & 0b11; break;
                case 3: ret = data[startByte] >> startBit & 0b111; break;
                case 4: ret = data[startByte] >> startBit & 0b1111; break;
                case 8: ret = data[startByte]; break;
                case 16:
                    if (!signed)
                        ret = BitConverter.ToUInt16(new byte[] { data[startByte + 1], data[startByte] });
                    else
                        ret = BitConverter.ToInt16(new byte[] { data[startByte + 1], data[startByte] });
                    break;


                case 24: ret = data[startByte] * 65536 + data[startByte + 1] * 256 + data[startByte + 2]; break;

                case 32:
                    if (!signed)
                        ret = BitConverter.ToUInt32(new byte[] { data[startByte + 3], data[startByte + 2], data[startByte + 1], data[startByte] });
                    else
                        ret = BitConverter.ToInt32(new byte[] { data[startByte + 3], data[startByte + 2], data[startByte + 1], data[startByte] });
                    break;
                default: throw new Exception("Bad parameter size");
            }
            return ret;
        }
        public string PrintParameter(OmniPgnParameter p)
        {
            StringBuilder retString = new();
            long rawValue = getRaw(Data, p.BitLength, p.StartBit, p.StartByte, p.Signed);
            retString.Append(p.Name + ": ");
            if (p.CustomDecoder != null)
                return p.CustomDecoder(Data);

            if (p.GetMeaning != null)
                return p.GetMeaning((int)rawValue);
            if (p.Meanings != null && p.Meanings.ContainsKey((int)rawValue))
                retString.Append(rawValue.ToString() + " - " + GetString(p.Meanings[(int)rawValue]));
            else
            {
                if (rawValue == Math.Pow(2, p.BitLength) - 1)
                    retString.Append($"Нет данных({rawValue})");
                else
                {
                    double rawDouble = (double)rawValue;
                    double value = rawDouble * p.a + p.b;
                    retString.Append(value.ToString(p.OutputFormat) + p.Unit);
                }
            }
            retString.Append(';');
            return retString.ToString();
        }
        public override string ToString()
        {
            StringBuilder retString = new();
            retString.Append($"<{PGN:D02}>[{TransmitterType}]({TransmitterAddress})->[{ReceiverType}]({ReceiverAddress}):");
            foreach (byte b in Data)
                retString.Append($"{b:X02} ");
            retString.Append("\n");
            return retString.ToString();
        }
        public override bool Equals(object obj)
        {
            return base.Equals(obj);
        }
        public override int GetHashCode()
        {
            return base.GetHashCode();
        }
        public string GetVerboseInfo()
        {
            StringBuilder retString = new StringBuilder();
            if (!Omni.PGNs.ContainsKey(this.PGN))
                return "PGN not found";
            PGN pgn = Omni.PGNs[this.PGN];
            string sender, receiver;
            if (Omni.Devices.ContainsKey(TransmitterType))
                sender = Omni.Devices[TransmitterType].Name;
            else
                sender = $"(неизвестное устройство №{TransmitterType})";
            if (Omni.Devices.ContainsKey(ReceiverType))
                receiver = Omni.Devices[ReceiverType].Name;
            else
                receiver = $"(неизвестное устройство №{ReceiverType})";
            retString.Append($"{sender}({TransmitterAddress})->{receiver}({ReceiverAddress});;");


            retString.Append(pgn.name + ";;");
            if (pgn.multipack)
                retString.Append($"Мультипакет №{Data[0]};");
            if (PGN == 1 && Omni.commands.ContainsKey(Data[1]))
            {
                OmniCommand cmd = Omni.commands[Data[1]];
                retString.Append(cmd.Name + ";");
                if (cmd.Parameters != null)
                    foreach (OmniPgnParameter p in cmd.Parameters)
                        retString.Append(PrintParameter(p));
            }
            if (pgn.parameters != null)
                foreach (OmniPgnParameter p in pgn.parameters)
                    if (!pgn.multipack || Data[0] == p.PackNumber)
                        retString.Append(PrintParameter(p));
            return retString.ToString();
        }
        public override string VerboseInfo => GetVerboseInfo().Replace(';', '\n');

        public void Update(OmniMessage item)
        {
            PGN = item.PGN;
            TransmitterId = item.TransmitterId;
            ReceiverId = item.ReceiverId;
            Data = item.Data;
            Fresh = true;
            Task.Run(() => { Thread.Sleep(300); Fresh = false; });
        }


        public bool IsSimmiliarTo(OmniMessage m)
        {
            if (PGN != m.PGN)
                return false;
            if (PGN == 1 || PGN == 2)
                if (Data[1] != m.Data[1])
                    return false;
            if (Omni.PGNs[PGN].multipack && Data[0] != m.Data[0]) //Другой номер мультипакета
                return false;
            return true;
        }
    }
    public enum DeviceType
    {
        Binar, Planar, HCU, ValveControl, Bootloader, CookingPanel
    }
    public class Device
    {
        public int ID;
        public string Name => GetString($"d_{ID}");
        public DeviceType DevType { set; get; }

        public int MaxBlower { get; set; } = 130;
        public double MaxFuelPump { get; set; } = 4;
        public int BBErrorsLen { get; set; } = 512;

        public override string ToString()
        {
            return Name;
        }

    }
    public class ReadedBlackBoxValue : ViewModel, INotifyPropertyChanged, IUpdatable<ReadedBlackBoxValue>, IComparable
    {
        private int id;

        public int Id
        {
            get => id;
            set => Set(ref id, value);
        }
        private uint val;

        public uint Value
        {
            get => val;
            set => Set(ref val, value);

        }

        public void Update(ReadedBlackBoxValue item)
        {
            Value = item.Value;
        }

        public bool IsSimmiliarTo(ReadedBlackBoxValue item)
        {
            return (id == item.id);
        }

        public int CompareTo(object obj)
        {
            return id - (obj as ReadedBlackBoxValue).id;
        }

        public string Description
        {
            get => GetString($"bb_{Id}");

        }

    }
    public class ReadedParameter : ViewModel, INotifyPropertyChanged, IUpdatable<ReadedParameter>, IComparable
    {

        private int id;

        public int Id
        {
            get => id;
            set => Set(ref id, value);
        }

        private uint _value;

        public uint Value
        {
            get => _value;
            set => Set(ref _value, value);

        }

        public void Update(ReadedParameter item)
        {
            Value = item.Value;
        }

        public bool IsSimmiliarTo(ReadedParameter item)
        {
            return (id == item.id);
        }

        public int CompareTo(object obj)
        {
            return id - (obj as ReadedParameter).id;
        }

        public string Name => GetString($"par_{id}"); //Omni.configParameters[Id]?.NameRu;
    }
    public class StatusVariable : ViewModel, IUpdatable<StatusVariable>, IComparable
    {

        public StatusVariable(int var) : base()
        {
            Id = var;
            Display = App.Settings.ShowFlag[Id];
            chartBrush = new SolidColorBrush(App.Settings.Colors[Id]);
            lineWidth = App.Settings.LineWidthes[Id];
            LineStyle = App.Settings.LineStyles[Id];
            markShape = App.Settings.MarkShapes[Id];

        }
        public StatusVariable()
        {

        }
        public int Id { get; set; }

        private long rawVal;

        [AffectsTo(nameof(VerboseInfo), nameof(Value), nameof(FormattedValue))]
        public long RawValue
        {
            get { return rawVal; }
            set { Set(ref rawVal, value); }
        }


        private OmniPgnParameter assignedParameter;

        public OmniPgnParameter AssignedParameter
        {
            get { return assignedParameter; }
            set { assignedParameter = value; }
        }

        public string VerboseInfo => AssignedParameter.Decode(RawValue);

        private bool display = false;

        public bool Display { get => display; set => Set(ref display, value); }

        public double Value
        {
            get => rawVal * assignedParameter.a + assignedParameter.b;
        }

        public string FormattedValue
        {
            get => Value.ToString(assignedParameter.OutputFormat);
        }

        public string Name => GetString($"var_{Id}");

        public string ShortName => GetString($"vars_{Id}");

        public System.Drawing.Color Color => System.Drawing.Color.FromArgb(255, (ChartBrush as SolidColorBrush).Color.R, (ChartBrush as SolidColorBrush).Color.G, (ChartBrush as SolidColorBrush).Color.B);

        private Brush chartBrush;

        [AffectsTo(nameof(Color))]
        public Brush ChartBrush
        {
            get => chartBrush;
            set => Set(ref chartBrush, value);
        }

        private int lineWidth;

        public int LineWidth
        {
            get => lineWidth;
            set => Set(ref lineWidth, value);
        }

        public int[] LineWidthes => new int[] { 1, 2, 3, 4, 5 };

        private LineStyle lineStyle;

        public LineStyle LineStyle
        {
            get => lineStyle;
            set => Set(ref lineStyle, value);
        }

        private MarkerShape markShape;

        public MarkerShape MarkShape
        {
            get => markShape;
            set => Set(ref markShape, value);
        }


        public bool IsSimmiliarTo(StatusVariable item)
        {
            return (Id == item.Id);
        }

        public void Update(StatusVariable item)
        {
            RawValue = item.RawValue;
        }

        public int CompareTo(object obj)
        {
            return Id - (obj as StatusVariable).Id;
        }
    }
    public class BBCommonVariable : ViewModel, IUpdatable<BBCommonVariable>, IComparable
    {
        int id;
        public int Id { get => id; set => id = value; }

        int _value;
        public int Value
        {
            get => _value;
            set => Set(ref _value, value);

        }

        public void Update(BBCommonVariable item)
        {
            Set(ref _value, item.Value);
        }

        public bool IsSimmiliarTo(BBCommonVariable item)
        {
            return id == item.id;
        }

        public string Name => GetString($"var_{Id}");

        public string Description => ToString();
        public override string ToString()
        {
            return $"{Name}: {Value}";
        }

        public int CompareTo(object obj)
        {
            return id - (obj as BBCommonVariable).id;
        }
    }
    public class BBError : IUpdatable<BBError>, INotifyPropertyChanged, IComparable
    {
        private readonly UpdatableList<BBCommonVariable> variables = new();
        public UpdatableList<BBCommonVariable> Variables { get => variables; }

        public event PropertyChangedEventHandler PropertyChanged;

        public bool IsSimmiliarTo(BBError item)
        {
            return false;
        }

        public int Id { get => 0; }
        public BBError()
        {
            variables.ListChanged += (s, a) => onChange("Name");
            variables.AddingNew += (s, a) => onChange("Name");
            variables.ListChanged += (s, a) => onChange("Description");
            variables.AddingNew += (s, a) => onChange("Description");
        }
        public void Update(BBError item)
        {
            throw new NotImplementedException();
        }
        public string Description => ToString();
        public override string ToString()
        {
            StringBuilder retString = new("");
            foreach (var v in Variables)
                retString.Append(v.ToString() + ";");
            return retString.ToString();
        }

        public string Name
        {
            get
            {
                BBCommonVariable error = Variables.FirstOrDefault(v => v.Id == 24); //24 - paramsname.h error code

                if (error == null)
                    return GetString("t_no_error_code");
                else
                    return GetString($"e_{error.Value}");
            }
        }

        void onChange(string propName)
        {
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propName));
        }

        public int CompareTo(object obj)
        {
            return Id - (obj as BBError).Id;
        }
    }
    public class OmniTask : ViewModel
    {
        int percentComplete;

        private DateTime capturedTime;

        private TimeSpan? lastOperationDuration;
        public TimeSpan? LastOperationDuration
        {
            set => Set(ref lastOperationDuration, value);
            get => lastOperationDuration;
        }

        public int PercentComplete
        {
            get => percentComplete;
            set => Set(ref percentComplete, value);

        }

        string name;
        public string Name
        {
            get => name;
            set => Set(ref name, value);

        }
        public CancellationTokenSource CTS { get; set; } = new CancellationTokenSource();

        bool done = false;

        public bool Done
        {
            get { return done; }
            set { Set(ref done, value); }
        }

        bool cancelled;
        public bool Cancelled
        {
            get { return cancelled; }
            set { Set(ref cancelled, value); }
        }

        public event EventHandler TaskDone;
        public event EventHandler TaskCancelled;

        public void onDone()
        {
            LastOperationDuration = DateTime.Now - capturedTime;
            Occupied = false;
            PercentComplete = 100;
            Done = true;
            TaskDone?.Invoke(null, null);
        }

        public void onCancel()
        {
            CTS.Cancel();
            Occupied = false;
            Cancelled = true;
            TaskCancelled?.Invoke(null, null);
        }

        public void onFail(string reason = "")
        {
            FailReason = reason;
            CTS.Cancel();
            Occupied = false;
            Failed = true;
            TaskCancelled?.Invoke(null, null);
        }

        private bool occupied;

        public bool Occupied
        {
            get { return occupied; }
            set { Set(ref occupied, value); }
        }

        private string failReason;

        public string FailReason
        {
            get { return failReason; }
            set { Set(ref failReason, value); }
        }

        private bool failed;

        public bool Failed
        {
            get { return failed; }
            set { Set(ref failed, value); }
        }
        /// <summary>
        /// Captures current task instance
        /// </summary>
        /// <param name="TaskName"></param>
        /// <returns>true if capture was successful, false if it's not</returns>
        public bool Capture(string TaskName)
        {
            if (occupied) return false;
            Occupied = true;
            Name = TaskName;
            FailReason = "";
            PercentComplete = 0;
            Done = false;
            Cancelled = false;
            CTS = new CancellationTokenSource();
            capturedTime = DateTime.Now;
            return true;
        }
        public void UpdatePercent(int p)
        {
            PercentComplete = p;
        }

    }
    public class MainParameters : ViewModel, ICloneable
    {

        private int revMeasured = 0;
        public int RevMeasured
        {
            get { return revMeasured; }
            set { Set(ref revMeasured, value); }
        }

        private int revSet = 0;
        public int RevSet
        {
            get { return revSet; }
            set { Set(ref revSet, value); }
        }

        private double fuelPumpMeasured = 0;
        public double FuelPumpMeasured
        {
            get { return fuelPumpMeasured; }
            set { Set(ref fuelPumpMeasured, value); }
        }


        private int glowPlug = 0;
        public int GlowPlug
        {
            get { return glowPlug; }
            set { Set(ref glowPlug, value); }
        }


        private double voltage = 0;
        public double Voltage
        {
            get { return voltage; }
            set { Set(ref voltage, value); }
        }

        private int stageTime = 0;
        public int StageTime
        {
            get { return stageTime; }
            set { Set(ref stageTime, value); }
        }

        private int modeTime = 0;
        public int ModeTime
        {
            get { return modeTime; }
            set { Set(ref modeTime, value); }
        }

        private int stage;

        [AffectsTo(nameof(StageString))]
        public int Stage
        {
            get { return stage; }
            set { Set(ref stage, value); }
        }

        private int mode;

        [AffectsTo(nameof(StageString))]
        public int Mode
        {
            get { return mode; }
            set { Set(ref mode, value); }
        }

        private int error;

        [AffectsTo(nameof(ErrorString))]
        public int Error
        {
            get { return error; }
            set { Set(ref error, value); }
        }

        private int workTime;

        public int WorkTime
        {
            get { return workTime; }
            set { Set(ref workTime, value); }
        }

        private int flameSensor;
        public int FlameSensor
        {
            get { return flameSensor; }
            set { Set(ref flameSensor, value); }
        }

        private int bodyTemp;
        public int BodyTemp
        {
            get { return bodyTemp; }
            set { Set(ref bodyTemp, value); }
        }

        private int liquidTemp;
        public int LiquidTemp
        {
            get { return liquidTemp; }
            set { Set(ref liquidTemp, value); }
        }

        private int overheatTemp;
        public int OverheatTemp
        {
            get { return overheatTemp; }
            set { Set(ref overheatTemp, value); }
        }

        private int panelTemp;
        public int PanelTemp
        {
            get { return panelTemp; }
            set { Set(ref panelTemp, value); }
        }

        private int inletTemp;
        public int InletTemp
        {
            get { return inletTemp; }
            set { Set(ref inletTemp, value); }
        }

        public string StageString => GetString($"m_{Stage}-{Mode}");

        public string ErrorString => error.ToString() + " - " + GetString($"e_{error}");

        public object Clone()
        {
            return MemberwiseClone();
        }
    }
    public class ConnectedDevice : ViewModel
    {
        public bool WaitForFlag(ref bool flag, int delay)
        {
            int wd = 0;
            while (!flag && wd < delay)
            {
                wd++;
                Thread.Sleep(1);
            }
            if (!flag)
            {
                flag = false;
                return false;
            }
            else
            {
                flag = false;
                return true;
            }
        }

        public ConnectedDevice(DeviceId newId)
        {
            LogInit();
            id = newId;
            if (Omni.Devices.ContainsKey(ID.Type))
                deviceReference = Omni.Devices[ID.Type];
        }

        public MainParameters Parameters { get; set; } = new();

        private DeviceId id;

        public DeviceId ID
        {
            get { return id; }
            set { Set(ref id, value); }
        }

        private DateOnly prodDate;

        public DateOnly ProductionDate
        {
            get => prodDate;
            set => Set(ref prodDate, value);
        }

        private byte[] firmware;

        [AffectsTo(nameof(FirmwareAsText))]
        public byte[] Firmware
        {
            get { return firmware; }
            set { Set(ref firmware, value); }
        }

        public string FirmwareAsText
        {
            get
            {
                if (firmware != null)
                    return $"{firmware[0]}.{firmware[1]}.{firmware[2]}.{firmware[3]}";
                else
                    return GetString("t_no_formware_data");
            }
        }


        private uint serial1 = 0;

        [AffectsTo(nameof(SerialAsString))]
        public uint Serial1
        {
            get => serial1;
            set => Set(ref serial1, value);
        }

        private uint serial2 = 0;

        [AffectsTo(nameof(SerialAsString))]
        public uint Serial2
        {
            get => serial2;
            set => Set(ref serial2, value);
        }

        private uint serial3 = 0;

        [AffectsTo(nameof(SerialAsString))]
        public uint Serial3
        {
            get => serial3;
            set => Set(ref serial3, value);
        }

        public string SerialAsString => $"{serial1}.{serial2}.{serial3}";

        UpdatableList<StatusVariable> status = new();
        public UpdatableList<StatusVariable> Status => status;

        private readonly UpdatableList<ReadedParameter> _readedParameters = new();
        public UpdatableList<ReadedParameter> readedParameters => _readedParameters;

        private UpdatableList<ReadedBlackBoxValue> _bbValues = new();
        public UpdatableList<ReadedBlackBoxValue> BBValues => _bbValues;

        private UpdatableList<BBError> _BBErrors = new();
        public UpdatableList<BBError> BBErrors => _BBErrors;

        private readonly BindingList<MainParameters> log = new();
        public BindingList<MainParameters> Log => log;

        public TimberlineHandler Timber { set; get; } = new();

        private bool manualMode;

        public bool ManualMode
        {
            get { return manualMode; }
            set { Set(ref manualMode, value); }
        }

        public bool flagEraseDone = false;

        public bool flagSetAdrDone = false;

        public bool flagProgramDone = false;

        public bool flagTransmissionCheck = false;

        public bool flagCrcGetDone = false;

        public bool flagDataGetDone = false;

        public int receivedDataLength = 0;

        public uint receiverDataCrc = 0;

        public bool flagGetParamDone = false;

        public bool flagGetVersionDone = false;

        public bool flagGetBBDone = false;

        public bool waitForBB = false;

        public uint fragmentAdress = 0;

        public int receivedFragmentLength = 0;

        public uint receivedFragmentCrc = 0;

        public string Name => ToString();

        private Device deviceReference;

        public Device DeviceReference => deviceReference;
        public string Img => $"~\\..\\Images\\{id.Type}.jpg";
        public override string ToString()
        {
            if (deviceReference != null)
                return deviceReference.Name;
            else
                return $"No device <{ID.Type}> in list";

        }

        public override bool Equals(object obj)
        {
            if (obj == null) return false;
            if (obj is not ConnectedDevice) return false;
            return ID.Equals((obj as ConnectedDevice).ID);
        }

        public override int GetHashCode()
        {
            return ID.GetHashCode();
        }

        private bool isLogWriting = false;

        public bool IsLogWriting
        {
            get { return isLogWriting; }
            private set { Set(ref isLogWriting, value); }
        }

        public List<double[]> LogData = new List<double[]>();

        private int logCurrentPos;

        public int LogCurrentPos
        {
            get => logCurrentPos;
            private set => Set(ref logCurrentPos, value);
        }


        public void LogTick()
        {
            Log.Insert(0, (MainParameters)Parameters.Clone());

            if (!isLogWriting) return;

            if (LogCurrentPos < LogData[0].Length)
            {
                foreach (StatusVariable sv in Status)
                    LogData[sv.Id][LogCurrentPos] = sv.Value;
                LogCurrentPos++;

            }
            else
            {
                LogStop();
                LogDataOverrun?.Invoke(this, null);
            }
        }

        public void LogInit(int length = 86400)
        {
            LogCurrentPos = 0;
            LogData = new List<double[]>();
            for (int i = 0; i < 100; i++) //Переменных пока намного меньше, но поставим пока 100
            {
                LogData.Add(new double[length]);
            }
        }

        private bool[] supportedVariables = new bool[100];

        public bool[] SupportedVariables => supportedVariables;

        public int SupportedVariablesCount
        {
            get
            {
                int ret = 0;
                foreach (var s in supportedVariables)
                    if (s == true) ret++;
                return ret;
            }
        }
        public void LogStart()
        {
            LogInit();
            IsLogWriting = true;
        }
        public void LogClear()
        {
            LogInit();
        }

        public void LogStop()
        {
            IsLogWriting = false;
        }

        public void SaveReport()
        {

        }
        public event EventHandler LogDataOverrun;

    }
    public class DeviceId : ViewModel
    {
        private int type;

        public int Type
        {
            get { return type; }
            set { Set(ref type, value); }
        }

        private int adr;

        public int Address
        {
            get { return adr; }
            set { Set(ref adr, value); }
        }

        public DeviceId(int type, int adr)
        {
            if (type > 127 || adr > 7)
                throw new ArgumentException("Bad device config adress must be below 7 and Type - below 127");
            Type = type;
            Address = adr;
            return;
        }
        public override string ToString()
        {
            if (Omni.Devices.ContainsKey(Type))
                return $"{Type} - {Address} ({Omni.Devices[Type]})";
            else
                return $"{Type} - {Address}";
        }

        public override int GetHashCode()
        {
            return Type << 3 + Address;
        }
        public override bool Equals([NotNullWhen(true)] object obj)
        {
            if (!(obj is DeviceId)) return false;
            return GetHashCode() == obj.GetHashCode();
        }
    }
    public class ConfigParameter
    {
        public int Id;
        public string StringId;
        public string Name;
        public override string ToString()
        {
            return $"{Id} - {StringId}: {Name}";
        }
    }
    public class Variable
    {
        public int Id;
        public string StringId;
        public string VarType;
        public string Description;
        public string Formula;
        public string Format;
        public string ShortName;

        public override string ToString()
        {
            return $"{Id} - {StringId}: {Description}";
        }
    }
    public class BbParameter
    {
        public int Id;
        public string StringId;
        public string Description;

        public override string ToString()
        {
            return $"{Id} - {StringId}:{Description}";
        }
    }
    public partial class Omni : ViewModel
    {

        public event EventHandler NewDeviceAquired;

        private SynchronizationContext UIContext;

        public ScottPlot.WpfPlot plot;

        static readonly Dictionary<int, string> defMeaningsYesNo = new() { { 0, "Нет" }, { 1, "Да" }, { 2, "Нет данных" }, { 3, "Нет данных" } };
        static readonly Dictionary<int, string> defMeaningsOnOff = new() { { 0, "Выкл" }, { 1, "Вкл" }, { 2, "Нет данных" }, { 3, "Нет данных" } };
        static readonly Dictionary<int, string> defMeaningsAllow = new() { { 0, "Разрешено" }, { 1, "Запрещёно" }, { 2, "Нет данных" }, { 3, "Нет данных" } };
        static readonly Dictionary<int, string> Stages = new() { { 0, "STAGE_Z" }, { 1, "STAGE_P" }, { 2, "STAGE_H" }, { 3, "STAGE_W" }, { 4, "STAGE_F" }, { 5, "STAGE_T" }, { 6, "STAGE_M" } };



        public static Dictionary<int, PGN> PGNs = new();

        public static Dictionary<int, OmniCommand> commands = new();

        private readonly BindingList<ConnectedDevice> connectedDevices = new();
        public BindingList<ConnectedDevice> ConnectedDevices => connectedDevices;

        private readonly UpdatableList<OmniMessage> messages = new();
        public UpdatableList<OmniMessage> Messages => messages;

        private readonly CanAdapter canAdapter;

        public bool ReadingBBErrorsMode { get; set; } = false;

        OmniTask currentTask = new();

        public OmniTask CurrentTask
        {
            get => currentTask;
            set => Set(ref currentTask, value);
        }

        private bool CancellationRequested => CurrentTask.CTS.IsCancellationRequested;

        private bool Capture(string n) => CurrentTask.Capture(n);

        private void Done() => CurrentTask.onDone();

        private void Cancel() => CurrentTask.onCancel();

        private void Fail(string reason = "") => CurrentTask.onFail(reason);

        private void UpdatePercent(int p) => CurrentTask.UpdatePercent(p);

        private void ProcessCanMessage(CanMessage msg)
        {

            OmniMessage m = new OmniMessage(msg);
            DeviceId id = m.TransmitterId;

            ConnectedDevice senderDevice = ConnectedDevices.FirstOrDefault(d => d.ID.Equals(m.TransmitterId));

            if (senderDevice == null)
            {
                senderDevice = new ConnectedDevice(id);
                ConnectedDevices.Add(senderDevice);
                NewDeviceAquired?.Invoke(this, null);
                Task.Run(()=>RequestBasicData(id));
            }


            if (!PGNs.ContainsKey(m.PGN)) return; //Такого PGN нет в библиотеке

            

            foreach (OmniPgnParameter p in PGNs[m.PGN].parameters)
            {

                if (PGNs[m.PGN].multipack && p.PackNumber != m.Data[0]) continue;
                if (p.Var != 0)
                {
                    StatusVariable sv = new StatusVariable(p.Var);
                    sv.AssignedParameter = p;
                    long rawValue = OmniMessage.getRaw(m.Data, p.BitLength, p.StartBit, p.StartByte, p.Signed);
                    if (rawValue == Math.Pow(2, p.BitLength) - 1) return; //Неподдерживаемый параметр, ливаем
                    sv.RawValue = rawValue;
                    senderDevice.SupportedVariables[sv.Id] = true;
                    senderDevice.Status.TryToAdd(sv);

                    if (sv.Id == 1)
                        senderDevice.Parameters.Stage = (int)(rawValue * sv.AssignedParameter.a + sv.AssignedParameter.b);
                    if (sv.Id == 2)
                        senderDevice.Parameters.Mode = (int)(rawValue * sv.AssignedParameter.a + sv.AssignedParameter.b);
                    if (sv.Id == 3)
                        senderDevice.Parameters.WorkTime = (int)(rawValue * sv.AssignedParameter.a + sv.AssignedParameter.b);
                    if (sv.Id == 4)
                        senderDevice.Parameters.StageTime = (int)(rawValue * sv.AssignedParameter.a + sv.AssignedParameter.b);
                    if (sv.Id == 5)
                        senderDevice.Parameters.Voltage = rawValue * sv.AssignedParameter.a + sv.AssignedParameter.b;
                    if (sv.Id == 6)
                        senderDevice.Parameters.FlameSensor = (int)(rawValue * sv.AssignedParameter.a + sv.AssignedParameter.b);
                    if (sv.Id == 7)
                        senderDevice.Parameters.BodyTemp = (int)(rawValue * sv.AssignedParameter.a + sv.AssignedParameter.b);
                    if (sv.Id == 8)
                        senderDevice.Parameters.PanelTemp = (int)(rawValue * sv.AssignedParameter.a + sv.AssignedParameter.b);
                    if (sv.Id == 10)
                        senderDevice.Parameters.InletTemp = (int)(rawValue * sv.AssignedParameter.a + sv.AssignedParameter.b);
                    if (sv.Id == 15)
                        senderDevice.Parameters.RevSet = (int)(rawValue * sv.AssignedParameter.a + sv.AssignedParameter.b);
                    if (sv.Id == 16)
                        senderDevice.Parameters.RevMeasured = (int)(rawValue * sv.AssignedParameter.a + sv.AssignedParameter.b);
                    if (sv.Id == 18)
                        senderDevice.Parameters.FuelPumpMeasured = (rawValue * sv.AssignedParameter.a + sv.AssignedParameter.b);
                    if (sv.Id == 21)
                        senderDevice.Parameters.GlowPlug = (int)(rawValue * sv.AssignedParameter.a + sv.AssignedParameter.b);
                    if (sv.Id == 24)
                        senderDevice.Parameters.Error = (int)(rawValue * sv.AssignedParameter.a + sv.AssignedParameter.b);
                    if (sv.Id == 40)
                        senderDevice.Parameters.LiquidTemp = (int)(rawValue * sv.AssignedParameter.a + sv.AssignedParameter.b);
                    if (sv.Id == 41)
                        senderDevice.Parameters.OverheatTemp = (int)(rawValue * sv.AssignedParameter.a + sv.AssignedParameter.b);





                }
            }
            
            if (m.PGN == 2) //Подтверждение выполненной комманды
            {

                switch (m.Data[1])
                {
                    case 0:
                        senderDevice.Firmware = new byte[] { m.Data[2], m.Data[3], m.Data[4], m.Data[5] };
                        break;
                    case 67:                                               //Вход в ручной режим
                        if (m.Data[2] == 1)
                            senderDevice.ManualMode = true;
                        else
                            senderDevice.ManualMode = false;
                        break;
                }


            }
            if (m.PGN == 7) //Ответ на запрос параметра
            {
                if (m.Data[0] == 4) // Обрабатываем только упешные ответы на запросы
                {
                    int parameterId = m.Data[3] + m.Data[2] * 256;
                    uint parameterValue = ((uint)m.Data[4] * 0x1000000) + ((uint)m.Data[5] * 0x10000) + ((uint)m.Data[6] * 0x100) + (uint)m.Data[7];
                    if (parameterValue != 0xFFFFFFFF)
                    {
                        senderDevice.readedParameters.TryToAdd(new() { Id = parameterId, Value = parameterValue });
                        Debug.WriteLine($"{GetString($"par_{parameterId}")}={parameterValue}");
                    }
                    else
                        if (GotString($"par_{parameterId}"))
                        Debug.WriteLine($"{GetString($"par_{parameterId}")} not supported");
                    //Серийник в отдельной переменной
                    if (parameterId == 12)
                        senderDevice.Serial1 = parameterValue;
                    if (parameterId == 13)
                        senderDevice.Serial2 = parameterValue;
                    if (parameterId == 14)
                        senderDevice.Serial3 = parameterValue;

                    senderDevice.flagGetParamDone = true;
                }

            }
            if (m.PGN == 8) //Работа с ЧЯ
            {
                if (m.Data[0] == 4) // Обрабатываем только упешные ответы на запросы
                {
                    if (!ReadingBBErrorsMode)
                    {
                        int parameterId = m.Data[3] + m.Data[2] * 256;
                        uint parameterValue = ((uint)m.Data[4] * 0x1000000) + ((uint)m.Data[5] * 0x10000) + ((uint)m.Data[6] * 0x100) + (uint)m.Data[7];
                        if (parameterValue != 0xFFFFFFFF)
                            senderDevice.BBValues.TryToAdd(new ReadedBlackBoxValue() { Id = parameterId, Value = parameterValue });

                    }
                    else
                    {
                        if (m.Data[2] == 0xFF && m.Data[3] == 0xFA) //Заголовок отчёта
                            senderDevice.BBErrors.AddNew();
                        else
                        {
                            BBCommonVariable v = new();
                            v.Id = m.Data[2] * 256 + m.Data[3];
                            v.Value = m.Data[4] * 0x1000000 + m.Data[5] * 0x10000 + m.Data[6] * 0x100 + m.Data[7];
                            if (v.Id != 65535)
                                senderDevice.BBErrors.Last().Variables.TryToAdd(v);
                        }
                    }
                    senderDevice.flagGetBBDone = true;
                }
            }
            if (m.PGN == 18) //Версия
            {
                senderDevice.flagGetVersionDone = true;
                senderDevice.Firmware = m.Data[0..4];
                if (m.Data[5] != 0xff && m.Data[6] != 0xff && m.Data[7] != 0xff)
                    try
                    {
                        senderDevice.ProductionDate = new DateOnly(m.Data[7] + 2000, m.Data[6], m.Data[5]);
                    }
                    catch { }
            }
            if (m.PGN == 21)
            {
                if (m.Data[2] != 255) senderDevice.Timber.TankTempereature = m.Data[2] - 75;
                if (m.Data[4] != 255) senderDevice.Timber.OutsideTemperature = m.Data[4] - 75;
                if (m.Data[6] != 255) senderDevice.Timber.LiquidLevel = m.Data[6];
                if ((m.Data[7] & 3) != 3) senderDevice.Timber.DomesticWaterFlow = (m.Data[7] & 3) != 0;
            }
            if (m.PGN == 22)
            {
                if ((m.Data[0] & 3) != 3)
                {
                    senderDevice.Timber.Zones[0].State = (m.Data[0] & 3) != 0;
                    senderDevice.Timber.Zones[0].Connected = true;
                }
                else
                    senderDevice.Timber.Zones[0].Connected = false;
                if (((m.Data[0] >> 2) & 3) != 3)
                {
                    senderDevice.Timber.Zones[1].State = ((m.Data[0] >> 2) & 3) != 0;
                    senderDevice.Timber.Zones[1].Connected = true;
                }
                else
                    senderDevice.Timber.Zones[1].Connected = false;
                if (((m.Data[0] >> 4) & 3) != 3)
                {
                    senderDevice.Timber.Zones[2].State = ((m.Data[0] >> 4) & 3) != 0;
                    senderDevice.Timber.Zones[2].Connected = true;
                }
                else
                    senderDevice.Timber.Zones[2].Connected = false;
                if (((m.Data[0] >> 6) & 3) != 3)
                {
                    senderDevice.Timber.Zones[3].State = ((m.Data[0] >> 6) & 3) != 0;
                    senderDevice.Timber.Zones[3].Connected = true;
                }
                else
                    senderDevice.Timber.Zones[3].Connected = false;
                if ((m.Data[1] & 3) != 3)
                {
                    senderDevice.Timber.Zones[4].State = (m.Data[1] & 3) != 0;
                    senderDevice.Timber.Zones[4].Connected = true;
                }
                else
                    senderDevice.Timber.Zones[4].Connected = false;

                if (m.Data[2] != 255) senderDevice.Timber.Zones[0].CurrentTemperature = m.Data[2] - 75;
                if (m.Data[3] != 255) senderDevice.Timber.Zones[1].CurrentTemperature = m.Data[3] - 75;
                if (m.Data[4] != 255) senderDevice.Timber.Zones[2].CurrentTemperature = m.Data[4] - 75;
                if (m.Data[5] != 255) senderDevice.Timber.Zones[3].CurrentTemperature = m.Data[5] - 75;
                if (m.Data[6] != 255) senderDevice.Timber.Zones[4].CurrentTemperature = m.Data[6] - 75;

                if ((m.Data[7] & 3) != 3) senderDevice.Timber.HeaterEnabled = (m.Data[7] & 3) != 0;
                if (((m.Data[7] >> 2) & 3) != 3) senderDevice.Timber.ElementEbabled = ((m.Data[7] >> 2) & 3) != 0;

            }
            if (m.PGN == 23)
            {
                if (m.Data[0] != 255) senderDevice.Timber.Zones[0].SettedPwmPercent = m.Data[0];
                if (m.Data[1] != 255) senderDevice.Timber.Zones[1].SettedPwmPercent = m.Data[1];
                if (m.Data[2] != 255) senderDevice.Timber.Zones[2].SettedPwmPercent = m.Data[2];
                if (m.Data[3] != 255) senderDevice.Timber.Zones[3].SettedPwmPercent = m.Data[3];
                if (m.Data[4] != 255) senderDevice.Timber.Zones[4].SettedPwmPercent = m.Data[4];
            }
            if (m.PGN == 24)
            {
                if ((m.Data[0] & 15) != 15) senderDevice.Timber.Zones[0].FanStage = m.Data[0] & 15;
                if (((m.Data[0] >> 4) & 15) != 15) senderDevice.Timber.Zones[1].FanStage = (m.Data[0] >> 4) & 15;
                if ((m.Data[1] & 15) != 15) senderDevice.Timber.Zones[2].FanStage = m.Data[1] & 15;
                if (((m.Data[1] >> 4) & 15) != 15) senderDevice.Timber.Zones[3].FanStage = (m.Data[0] >> 4) & 15;
                if ((m.Data[2] & 15) != 15) senderDevice.Timber.Zones[4].FanStage = m.Data[2] & 15;
                if (m.Data[3] != 255) senderDevice.Timber.Zones[0].CurrentPwm = m.Data[3];
                if (m.Data[4] != 255) senderDevice.Timber.Zones[1].CurrentPwm = m.Data[4];
                if (m.Data[5] != 255) senderDevice.Timber.Zones[2].CurrentPwm = m.Data[5];
                if (m.Data[6] != 255) senderDevice.Timber.Zones[3].CurrentPwm = m.Data[6];
                if (m.Data[7] != 255) senderDevice.Timber.Zones[4].CurrentPwm = m.Data[7];
            }
            if (m.PGN == 25)
            {
                if (m.Data[0] != 255) senderDevice.Timber.Zones[0].TempSetpointDay = m.Data[0] - 75;
                if (m.Data[1] != 255) senderDevice.Timber.Zones[1].TempSetpointDay = m.Data[1] - 75;
                if (m.Data[2] != 255) senderDevice.Timber.Zones[2].TempSetpointDay = m.Data[2] - 75;
                if (m.Data[3] != 255) senderDevice.Timber.Zones[3].TempSetpointDay = m.Data[3] - 75;
                if (m.Data[4] != 255) senderDevice.Timber.Zones[4].TempSetpointDay = m.Data[4] - 75;
            }
            if (m.PGN == 26)
            {
                if (m.Data[0] != 255) senderDevice.Timber.Zones[0].TempSetpointNight = m.Data[0] - 75;
                if (m.Data[1] != 255) senderDevice.Timber.Zones[1].TempSetpointNight = m.Data[1] - 75;
                if (m.Data[2] != 255) senderDevice.Timber.Zones[2].TempSetpointNight = m.Data[2] - 75;
                if (m.Data[3] != 255) senderDevice.Timber.Zones[3].TempSetpointNight = m.Data[3] - 75;
                if (m.Data[4] != 255) senderDevice.Timber.Zones[4].TempSetpointNight = m.Data[4] - 75;
            }
            if (m.PGN == 27)
            {
                if (m.Data[0] != 255) senderDevice.Timber.Zones[0].ManualPercent = m.Data[0];
                if (m.Data[1] != 255) senderDevice.Timber.Zones[1].ManualPercent = m.Data[1];
                if (m.Data[2] != 255) senderDevice.Timber.Zones[2].ManualPercent = m.Data[2];
                if (m.Data[3] != 255) senderDevice.Timber.Zones[3].ManualPercent = m.Data[3];
                if (m.Data[4] != 255) senderDevice.Timber.Zones[4].ManualPercent = m.Data[4];
                if ((m.Data[5] & 3) != 3) senderDevice.Timber.Zones[0].ManualMode = (m.Data[5] & 3) != 0;
                if (((m.Data[5] >> 2) & 3) != 3) senderDevice.Timber.Zones[1].ManualMode = ((m.Data[5] >> 2) & 3) != 0;
                if (((m.Data[5] >> 4) & 3) != 3) senderDevice.Timber.Zones[2].ManualMode = ((m.Data[5] >> 4) & 3) != 0;
                if (((m.Data[5] >> 6) & 3) != 3) senderDevice.Timber.Zones[3].ManualMode = ((m.Data[5] >> 6) & 3) != 0;
                if ((m.Data[6] & 3) != 3) senderDevice.Timber.Zones[4].ManualMode = (m.Data[6] & 3) != 0;
            }
            if (m.PGN == 33)
            {
                switch (m.Data[0])
                {
                    case 1:
                        senderDevice.Serial1 = (uint)(m.Data[1] * 0x1000000 + m.Data[2] * 0x10000 + m.Data[3] * 0x100 + m.Data[4]);
                        break;
                    case 2:
                        senderDevice.Serial2 = (uint)(m.Data[1] * 0x1000000 + m.Data[2] * 0x10000 + m.Data[3] * 0x100 + m.Data[4]);
                        break;
                    case 3:
                        senderDevice.Serial3 = (uint)(m.Data[1] * 0x1000000 + m.Data[2] * 0x10000 + m.Data[3] * 0x100 + m.Data[4]);
                        break;
                    default:
                        break;
                }

            }
            if (m.PGN == 105)
            {
                if (m.Data[0] == 1)
                {
                    senderDevice.fragmentAdress = (uint)(m.Data[1] * 0x1000000 + m.Data[2] * 0x10000 + m.Data[3] * 0x100 + m.Data[4]);
                    Debug.WriteLine($"Adress set to 0X{senderDevice.fragmentAdress:X}");
                    senderDevice.flagSetAdrDone = true;
                }

                if (m.Data[0] == 3)
                {
                    senderDevice.receivedFragmentLength = m.Data[1] * 0x10000 + m.Data[2] * 0x100 + m.Data[3];
                    senderDevice.receivedFragmentCrc = m.Data[4] * 0x1000000U + m.Data[5] * 0x10000U + m.Data[6] * 0x100U + m.Data[7];
                    Debug.WriteLine($"Data fragment len:{senderDevice.receivedFragmentLength},CRC:{senderDevice.receivedFragmentCrc:X}");
                    senderDevice.flagDataGetDone = true;
                }

                if (m.Data[0] == 5)
                {
                    if (m.Data[1] == 0)
                    {
                        Debug.WriteLine("Flash fragment successed");
                        senderDevice.flagProgramDone = true;
                    }
                    else
                        Debug.WriteLine("Flash fragment failed");
                }

                if (m.Data[0] == 7)
                {
                    if (m.Data[1] == 0)
                    {
                        Debug.WriteLine("Memory erase confirmed");
                        senderDevice.flagEraseDone = true;
                    }
                    else
                        Debug.WriteLine("Memory erase fail");
                }
            }

            Messages.TryToAdd(m);
        }

        public async void ReadBlackBoxData(DeviceId id)
        {
            if (!Capture("Чтение параметров чёрного ящика")) return;
            ConnectedDevice currentDevice = ConnectedDevices.FirstOrDefault(d => d.ID == id);
            if (currentDevice == null) return;
            ReadingBBErrorsMode = false;
            OmniMessage msg = new()
            {
                PGN = 8,
                TransmitterAddress = 6,
                TransmitterType = 126,
                ReceiverAddress = id.Address,
                ReceiverType = id.Type,
            };
            msg.Data[0] = 6; //Read Single Param
            msg.Data[1] = 0xFF; //Read Param

            int counter = 0;
            int parameterCount = 64;
            for (int p = 0; p < parameterCount; p++)
            {
                msg.Data[4] = (byte)(p / 256);
                msg.Data[5] = (byte)(p % 256);
                currentDevice.flagGetBBDone = false;
                for (int t = 0; t < 7; t++)
                {
                    SendMessage(msg);
                    bool success = false;
                    for (int i = 0; i < 50; i++)
                    {
                        if (currentDevice.flagGetBBDone)
                        {
                            success = true;
                            break;
                        }
                        await Task.Delay(1);
                        Debug.WriteLineIf(i == 49, $"Error reading parameter {p} ({GetString($"bb_{p}")}), attempt:{t}");
                    }
                    if (success) break;
                    if (t == 6)
                        Fail("Can't read black box parameter");
                }

                if (CancellationRequested)
                {
                    Cancel();
                    return;
                }
                UpdatePercent(100 * counter++ / parameterCount);
            }
            Done();
        }

        public async void CheckPump(ConnectedDevice cd)
        {
            if (!Capture(GetString("b_1000_ticks"))) return;
            if (cd == null) return;

            OmniMessage msg = new();
            msg.TransmitterType = 126;
            msg.TransmitterAddress = 6;
            msg.ReceiverId = cd.ID;
            msg.PGN = 1;
            msg.Data = new byte[8];
            msg.Data[0] = 0;
            msg.Data[1] = 68;
            msg.Data[2] = 0;
            msg.Data[3] = 0;
            msg.Data[4] = 0;
            msg.Data[5] = 400 / 256;
            msg.Data[6] = 400 % 256;
            var startTime = DateTime.Now;
            SendMessage(msg);
            while (true)
            {
                UpdatePercent((int)((DateTime.Now - startTime).TotalSeconds / 2.5));
                await Task.Delay(100);
                if (CancellationRequested || (DateTime.Now - startTime).TotalSeconds > 250)
                    break;
            }
            msg.Data[5] = 0;
            msg.Data[6] = 0;
            SendMessage(msg);
            if (CancellationRequested)
                Cancel();
            else
                Done();
        }

        public async void ReadErrorsBlackBox(DeviceId id)
        {
            if (!Capture("Чтение ошибок из чёрного ящика")) return;
            ReadingBBErrorsMode = true;

            ConnectedDevice currentDevice = ConnectedDevices.FirstOrDefault(i => i.ID.Equals(id));

            UIContext.Send(x => currentDevice.BBErrors.Clear(), null);

            OmniMessage msg = new OmniMessage
            {
                PGN = 8,
                TransmitterAddress = 6,
                TransmitterType = 126,
                ReceiverAddress = id.Address,
                ReceiverType = id.Type
            };
            msg.Data[0] = 0x13; //Read Errors
            msg.Data[1] = 0xFF;
            for (int i = 0; i < currentDevice.DeviceReference.BBErrorsLen; i++)
            {
                msg.Data[4] = (byte)(i / 256);  //Pair count
                msg.Data[5] = (byte)(i % 256);  //Pair count
                msg.Data[6] = 0x00; //Pair count MSB
                msg.Data[7] = 0x01; //Pair count LSB

                currentDevice.flagGetBBDone = false;

                for (int t = 0; t < 7; t++)
                {
                    SendMessage(msg);
                    bool success = false;
                    for (int j = 0; j < 50; j++)
                    {
                        if (currentDevice.flagGetBBDone)
                        {
                            success = true;
                            break;
                        }
                        await Task.Delay(1);
                        Debug.WriteLineIf(j == 49, $"Error reading BB adress {i},attempt:{t + 1}");
                    }
                    if (success) break;
                    if (t == 6)
                        Fail("Can't read black box error");
                }
                if (CancellationRequested)
                {
                    Cancel();
                    return;
                }
                UpdatePercent(100 * i / 512);
            }
            Done();
        }

        public void EraseCommonBlackBox(DeviceId id)
        {
            if (!Capture("Стирание параметров чёрного ящика")) return;

            OmniMessage msg = new OmniMessage();
            msg.PGN = 8;
            msg.TransmitterAddress = 6;
            msg.TransmitterType = 126;
            msg.ReceiverAddress = id.Address;
            msg.ReceiverType = id.Type;
            msg.Data[0] = 0x0; //Erase Common
            msg.Data[1] = 0xFF;
            msg.Data[4] = 0xFF;
            msg.Data[5] = 0xFF;
            msg.Data[6] = 0xFF;
            msg.Data[7] = 0xFF;
            SendMessage(msg);

            Done();
        }

        public void EraseErrorsBlackBox(DeviceId id)
        {
            if (!Capture("Стирание ошибок чёрного ящика")) return;

            OmniMessage msg = new OmniMessage();
            msg.PGN = 8;
            msg.TransmitterAddress = 6;
            msg.TransmitterType = 126;
            msg.ReceiverAddress = id.Address;
            msg.ReceiverType = id.Type;
            msg.Data[0] = 0x10; //Erase Errors
            msg.Data[1] = 0xFF;
            msg.Data[4] = 0xFF;
            msg.Data[5] = 0xFF;
            msg.Data[6] = 0xFF;
            msg.Data[7] = 0xFF;
            SendMessage(msg);

            Done();
        }
        public async void ReadAllParameters(DeviceId id)
        {
            if (!Capture("Чтение параметров из Flash")) return;
            int cnt = 0;

            ConnectedDevice currentDevice = ConnectedDevices.FirstOrDefault(i => i.ID.Equals(id));

            for (int parId = 0; parId < 601; parId++) //Currently we have 600 parameters
            {
                if (!GotString($"par_{parId}")) // Если нет параметра в ресурсах - не запрашиваем ???
                    continue;
                OmniMessage msg = new();
                msg.PGN = 7;
                msg.TransmitterAddress = 6;
                msg.TransmitterType = 126;
                msg.ReceiverAddress = id.Address;
                msg.ReceiverType = id.Type;
                msg.Data[0] = 3; //Read Param
                msg.Data[1] = 0xFF; //Read Param
                msg.Data[2] = (byte)(parId / 256);
                msg.Data[3] = (byte)(parId % 256);

                currentDevice.flagGetParamDone = false;
                

                for (int t = 0; t < 7; t++)
                {
                    SendMessage(msg);
                    bool success = false;

                    for (int j = 0; j < 50; j++)
                    {
                        if (currentDevice.flagGetParamDone)
                        {
                            success = true;
                            break;
                        }
                        await Task.Delay(1);
                        Debug.WriteLineIf(j == 49, $"Error reading parameter {parId}, attempt {t + 1})");
                    }
                    if (success) break;
                    if (t == 6)
                        Fail("Can't read parameter");
                }
                UpdatePercent(cnt++ * 100 / 601);
                if (CancellationRequested)
                {
                    Cancel();
                    return;
                }
            }
            Done();
        }

        public async void RequestBasicData(DeviceId id)
        {
            ConnectedDevice currentDevice = ConnectedDevices.FirstOrDefault(i => i.ID.Equals(id));

            for (int parId = 12; parId < 15; parId++) //ID1 ID2 ID3
            {
                OmniMessage msg = new();
                msg.PGN = 7;
                msg.TransmitterAddress = 6;
                msg.TransmitterType = 126;
                msg.ReceiverAddress = id.Address;
                msg.ReceiverType = id.Type;
                msg.Data[0] = 3; //Read Param
                msg.Data[1] = 0xFF; //Read Param
                msg.Data[2] = (byte)(parId / 256);
                msg.Data[3] = (byte)(parId % 256);

                currentDevice.flagGetParamDone = false;

                for (int t = 0; t < 7; t++)
                {
                    SendMessage(msg);
                    bool success = false;

                    for (int j = 0; j < 50; j++)
                    {
                        if (currentDevice.flagGetParamDone)
                        {
                            success = true;
                            break;
                        }
                        await Task.Delay(1);
                        Debug.WriteLineIf(j == 49, $"Error reading parameter {parId}, attempt {t + 1})");
                    }
                    if (success) break;
                }
                await Task.Delay(100);

                msg.PGN = 6;
                msg.Data[0] = 0;
                msg.Data[1] = 18;
                for (int t = 0; t < 7; t++)
                {
                    currentDevice.flagGetVersionDone = false;
                    SendMessage(msg);
                    bool success = false;

                    for (int j = 0; j < 50; j++)
                    {
                        if (currentDevice.flagGetParamDone)
                        {
                            success = true;
                            break;
                        }
                        await Task.Delay(1);
                        Debug.WriteLineIf(j == 49, $"Error reading version for {currentDevice.ToString()}");
                    }
                    if (success) break;
                }
            }
        }

        public async void SaveParameters(DeviceId id)
        {
            if (!Capture("Сохранение параметров во Flash")) return;
            var dev = ConnectedDevices.FirstOrDefault(d => d.ID.Equals(id));
            if (dev == null) return;
            OmniMessage msg = new OmniMessage();
            List<ReadedParameter> tempCollection = new List<ReadedParameter>();
            foreach (var p in dev.readedParameters)
                tempCollection.Add(p);
            int cnt = 0;
            foreach (var p in tempCollection)
            {
                msg.PGN = 7;
                msg.TransmitterAddress = 6;
                msg.TransmitterType = 126;
                msg.ReceiverAddress = id.Address;
                msg.ReceiverType = id.Type;
                msg.Data[0] = 1; //Write Param to raw
                msg.Data[1] = 0xFF;
                msg.Data[2] = (byte)(p.Id / 256);
                msg.Data[3] = (byte)(p.Id % 256);
                msg.Data[4] = (byte)((p.Value >> 24) & 0xFF);
                msg.Data[5] = (byte)((p.Value >> 16) & 0xFF);
                msg.Data[6] = (byte)((p.Value >> 8) & 0xFF);
                msg.Data[7] = (byte)((p.Value) & 0xFF);
                SendMessage(msg);
                await Task.Run(() => Thread.Sleep(100));
                UpdatePercent(cnt++ * 100 / tempCollection.Count);
                if (CancellationRequested)
                {
                    Cancel();
                    return;
                }
            }
            await Task.Run(() => Thread.Sleep(100));
            msg.Data[0] = 2;
            SendMessage(msg);
            Done();
        }

        public void ResetParameters(DeviceId id)
        {
            if (!Capture("Стирание пареметров")) return;
            var dev = ConnectedDevices.FirstOrDefault(d => d.ID.Equals(id));
            if (dev == null) return;
            OmniMessage msg = new OmniMessage();

            msg.PGN = 7;
            msg.TransmitterAddress = 6;
            msg.TransmitterType = 126;
            msg.ReceiverAddress = id.Address;
            msg.ReceiverType = id.Type;
            msg.Data[0] = 0; //Erase 
            msg.Data[1] = 0xFF;
            SendMessage(msg);
            Done();
        }

        public void SendMessage(OmniMessage m)
        {
            //Debug.WriteLine("-> " + (new AC2PMessage(m)).ToString());
            canAdapter.Transmit(m);
        }
        public void SendMessage(DeviceId from, DeviceId to, int pgn, byte[] data)
        {
            OmniMessage msg = new OmniMessage();
            msg.PGN = pgn;
            msg.TransmitterAddress = from.Address;
            msg.TransmitterType = from.Type;
            msg.ReceiverAddress = to.Address;
            msg.ReceiverType = to.Type;
            data.CopyTo(msg.Data, 0);
            SendMessage(msg);
        }
        public void SendCommand(int com, DeviceId dev, byte[] data = null)
        {
            OmniMessage message = new OmniMessage();
            message.PGN = 1;
            message.TransmitterAddress = 6;
            message.TransmitterType = 126;
            message.ReceiverAddress = dev.Address;
            message.ReceiverType = dev.Type;
            message.Data[1] = (byte)com;
            for (int i = 0; i < 6; i++)
            {
                if (data != null)
                    message.Data[i + 2] = data[i];
                else
                    message.Data[i + 2] = 0xFF;
            }
            SendMessage(message);
        }

        public static Dictionary<int, Device> Devices;

        public Omni(CanAdapter adapter)
        {
            UIContext = SynchronizationContext.Current;
            if (adapter == null) throw new ArgumentNullException("Can Adapter reference can't be null");
            canAdapter = adapter;
            adapter.GotNewMessage += Adapter_GotNewMessage;
            SeedStaticData();

        }

        private void Adapter_GotNewMessage(object sender, EventArgs e)
        {
            UIContext.Send(x => ProcessCanMessage((e as GotMessageEventArgs).receivedMessage), null);
        }

    }

}




