using CAN_Adapter;
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
using CAN_Tool.Infrastructure.Commands;
using CAN_Tool.ViewModels;
using System.Windows.Input;

namespace OmniProtocol
{
    
    public enum UnitType { None, Temp, Volt, Current, Pressure, Flow, Rpm, Rps, Percent, Second, Minute, Hour, Day, Month, Year, Frequency }

    public enum DeviceType {Binar, Planar, HCU, ValveControl, Bootloader, CookingPanel, ExtensionBoard }

    public class GotOmniMessageEventArgs : EventArgs
    {
        public OmniMessage receivedMessage;
    }

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
        //public string Unit { get; set; } = "";//Единица измерения

        public UnitType UnitT { get; set; } = UnitType.None;

        //private string format;

        internal Dictionary<int, string> Meanings { set; get; } = new();
        internal Func<int, string> GetMeaning; //Принимает на вход сырое значение, возвращает строку с расшифровкой значения параметра
        internal Func<byte[], string> CustomDecoder; //Если для декодирования нужен весь пакет данных
        internal int PackNumber; //Номер пакета в мультипакете
        internal int Var; //Соответствующая переменная из paramsName.h
        public double DefaultValue; //Для конструктора комманд
        public bool AnswerOnly = false;
        //private bool id = false;

        public string OutputFormat
        {
            get
            {
                //if (format != null)
                //    return format;
                if (UnitT == UnitType.Temp && App.Settings.UseImperial)
                    return "0.0"; // For correct farenheit display
                else
                {
                    if (a == 1)
                        return "0";
                    else if (a >= 0.09)
                        return "0.0";
                    else
                        return "0.00";
                }
            }
            //set => Set(ref format, value);
        }

        public string Unit
        {
            get
            {
                switch (UnitT)
                {
                    case UnitType.None: return "";
                    case UnitType.Temp:
                        if (App.Settings.UseImperial)
                            return "°F";
                        else
                            return "°C";
                    case UnitType.Volt: return GetString("u_voltage");
                    case UnitType.Percent: return "%";
                    case UnitType.Flow:
                        if (App.Settings.UseImperial)
                            return GetString("u_hal_per_minute");
                        else
                            return GetString("u_litre_per_minute");
                    case UnitType.Current: return GetString("u_ampere");
                    case UnitType.Rpm: return GetString("u_rpm");
                    case UnitType.Pressure:
                        if (App.Settings.UseImperial)
                            return "PSI";
                        else
                            return GetString("u_kPa");
                    default: return "";
                }
            }
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
                retString.Append(rawValue.ToString() + " - " + GetString(Meanings[(int)rawValue]));
            else
            {
                if (rawValue == Math.Pow(2, BitLength) - 1)
                    retString.Append(GetString("t_no_data") + $" ({rawValue})");
                else
                {
                    double rawDouble = rawValue;
                    if (Signed && rawValue > Math.Pow(2, BitLength - 1))
                        rawDouble *= -1;
                    double value = ImperialConverter(rawDouble * a + b, UnitT);


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
        private string recourceId;

        public string Name { internal set => recourceId = value; get => GetString(recourceId); }

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

        private zoneType_t connected = zoneType_t.Disconnected;
        public  zoneType_t Connected {set => Set(ref connected, value); get => connected; }

        private zoneState_t state = zoneState_t.Off;
        public zoneState_t State { set => Set(ref state, value); get => state; }

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

    public class Timberline20OmniViewModel : ViewModel
    {
        private void ZoneChanged(ZoneHandler newSelectedZone)
        {
            SelectedZone = newSelectedZone;
        }
        public Timberline20OmniViewModel()
        {
            for (int i = 0; i < 5; i++)
            {
                Zones.Add(new ZoneHandler(ZoneChanged));
            }

            SelectedZone = zones[0];
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

    public class OmniMessage : ViewModel, IUpdatable<OmniMessage>, IComparable
    {

        private bool fresh;
        public bool Fresh { set => Set(ref fresh, value); get => fresh; }

        public long updatetick;

        private byte[] data = new byte[8];

        [AffectsTo(nameof(DataAsText), nameof(DataAsULong), nameof(VerboseInfo))]
        public byte[] Data { set => Set(ref data, value); get => data; }

        public OmniMessage()
        {
            Fresh = true;
            updatetick = DateTime.Now.Ticks;
            TransmitterType = 126;
            TransmitterAddress = 6;
            ReceiverAddress = 0;
            Data = new byte[8];
            for (var i = 0; i < 8; i++)
                Data[i] = 0xff;
        }

        public CanMessage ToCanMessage()
        {
            CanMessage ret = new();
            ret.Data = Data;
            ret.DLC = 8;
            ret.IDE = true;
            ret.RTR = false;
            ret.Id = (PGN << 20) + (receiverType << 13) + (receiverAddress << 10) + (transmitterType << 3) + transmitterAddress;
            return ret;
        }

        public OmniMessage(CanMessage m) : this()
        {
            if (m.DLC != 8 || m.RTR || !m.IDE)
                throw new ArgumentException("CAN message is not compliant with OmniProtocol");
            Data = m.Data;
            PGN = (m.Id >> 20) & 0b111111111;
            ReceiverType = (m.Id >> 13) & 0b1111111;
            ReceiverAddress = (m.Id >> 10) & 0b111;
            TransmitterType = (m.Id >> 3) & 0b1111111;
            TransmitterAddress = m.Id & 0b111;
            return;
        }

        public ulong DataAsULong
        {
            get
            {
                byte[] bytes = new byte[8];
                Data.CopyTo(bytes, 0);
                Array.Reverse(bytes);
                return BitConverter.ToUInt64(bytes, 0);

            }
            set
            {
                byte[] tempArr = BitConverter.GetBytes(value);
                Array.Reverse(tempArr);
                tempArr.CopyTo(Data, 0);
                OnPropertyChanged(nameof(Data));
                OnPropertyChanged(nameof(DataAsText));
                OnPropertyChanged(nameof(VerboseInfo));
            }
        }

        public string DataAsText
        {
            get
            {
                StringBuilder sb = new("");
                for (int i = 0; i < 8; i++)
                    sb.Append($"{Data[i]:X02} ");
                return sb.ToString();
            }
        }
        public void FreshCheck()
        {
            if (fresh && (DateTime.Now.Ticks - updatetick > 3000000))
                Fresh = false;
        }

        private int pgn;

        [AffectsTo(nameof(VerboseInfo))]
        public int PGN
        {
            get => pgn;
            set
            {
                if (value > 511)
                    throw new ArgumentException("PGN can't be over 511");
                if (PGN == value)
                    return;
                Set(ref pgn, value);
            }
        }
        int receiverType;

        [AffectsTo(nameof(VerboseInfo))]
        public int ReceiverType
        {
            get => receiverType;
            set
            {
                if (value > 127)
                    throw new ArgumentException("ReceiverType can't be over 127");
                Set(ref receiverType, value);
            }
        }
        int receiverAddress;

        [AffectsTo(nameof(VerboseInfo))]
        public int ReceiverAddress
        {
            get => receiverAddress;
            set
            {
                if (value > 7)
                    throw new ArgumentException("ReceiverAddress can't be over 7");
                Set(ref receiverAddress, value);
            }
        }

        private int transmitterType;

        [AffectsTo(nameof(VerboseInfo))]
        public int TransmitterType
        {
            get => transmitterType;
            set
            {
                if (value > 127)
                    throw new ArgumentException("TransmitterType can't be over 127");
                Set(ref transmitterType, value);
            }
        }

        int transmitterAddress;

        [AffectsTo(nameof(VerboseInfo))]
        public int TransmitterAddress
        {
            get => transmitterAddress;
            set
            {
                if (value > 7)
                    throw new ArgumentException("TransmitterAddress can't be over 7");
                Set(ref transmitterAddress, value);
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
            retString.Append(GetString(p.Name) + ": ");
            if (p.CustomDecoder != null)
                return p.CustomDecoder(Data);

            if (p.GetMeaning != null)
                return p.GetMeaning((int)rawValue);
            if (p.Meanings != null && p.Meanings.ContainsKey((int)rawValue))
                retString.Append(rawValue.ToString() + " - " + GetString(p.Meanings[(int)rawValue]));
            else
            {
                if (rawValue == Math.Pow(2, p.BitLength) - 1)
                    retString.Append($"{GetString("t_no_data")}({rawValue})");
                else
                {
                    double rawDouble = (double)rawValue;
                    double value = ImperialConverter(rawDouble * p.a + p.b, p.UnitT);
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


            retString.Append(GetString(pgn.name) + ";;");
            if (pgn.multipack)
                retString.Append($"Мультипакет №{Data[0]};");
            if (PGN == 1 && Omni.commands.ContainsKey(Data[1] + Data[0] * 256))
            {
                OmniCommand cmd = Omni.commands[Data[1] + Data[0] * 256];
                retString.Append(GetString(cmd.Name) + ";");
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
        public string VerboseInfo => GetVerboseInfo().Replace(';', '\n');

        public void Update(OmniMessage item)
        {
            PGN = item.PGN;
            TransmitterId = item.TransmitterId;
            ReceiverId = item.ReceiverId;
            Data = item.Data;
            Fresh = true;
            updatetick = DateTime.Now.Ticks;
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

        public int CompareTo(object other)
        {
            return pgn.CompareTo((other as OmniMessage).pgn);
        }
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
            get => ImperialConverter(rawVal * assignedParameter.a + assignedParameter.b, assignedParameter.UnitT);
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

        [AffectsTo(nameof(TaskStatus))]
        public int PercentComplete
        {
            get => percentComplete;
            set => Set(ref percentComplete, value);
        }

        string name;
        [AffectsTo(nameof(TaskStatus))]
        public string Name
        {
            get => name;
            set => Set(ref name, value);

        }
        public CancellationTokenSource CTS { get; set; } = new CancellationTokenSource();

        bool done = false;

        [AffectsTo(nameof(TaskStatus))]
        public bool Done
        {
            get { return done; }
            set { Set(ref done, value); }
        }

        bool cancelled;
        [AffectsTo(nameof(TaskStatus))]
        public bool Cancelled
        {
            get { return cancelled; }
            set { Set(ref cancelled, value); }
        }

        public event EventHandler TaskDone;
        public event EventHandler TaskCancelled;

        private TimeSpan? lastOperationDuration;
        public TimeSpan? LastOperationDuration
        {
            set => Set(ref lastOperationDuration, value);
            get => lastOperationDuration;
        }

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
        [AffectsTo(nameof(TaskStatus))]
        public bool Occupied
        {
            get { return occupied; }
            set { Set(ref occupied, value); }
        }

        private string failReason;
        [AffectsTo(nameof(TaskStatus))]
        public string FailReason
        {
            get { return failReason; }
            set { Set(ref failReason, value); }
        }

        private bool failed;
        [AffectsTo(nameof(TaskStatus))]
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

        public string TaskStatus
        {
            get
            {
                if (!Done && !Cancelled && !Failed && Occupied)
                    return GetString(Name) + " " + GetString("t_in_progress");
                if (Done)
                    return GetString(Name) + " " + GetString("t_done_in") + " " + $"{LastOperationDuration.Value.TotalSeconds:F3} " + GetString("u_s");
                if (Cancelled)
                    return GetString(Name) + " " + GetString("t_cancelled");
                if (Failed)
                    return GetString(Name) + " " + GetString("t_failed") + ", " + GetString(FailReason);
                return "";
            }
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

    public partial class Omni : ViewModel
    {

        public Omni(CanAdapter canAdapter, UartAdapter uartAdapter)
        {
            if (canAdapter == null) throw new ArgumentNullException("Can Adapter reference can't be null");
            if (uartAdapter == null) throw new ArgumentNullException("Uart Adapter reference can't be null");
            this.canAdapter = canAdapter;
            this.uartAdapter = uartAdapter;
            SeedStaticData();
        }

        public event EventHandler NewDeviceAquired;

        public ScottPlot.WpfPlot plot;

        static readonly Dictionary<int, string> defMeaningsYesNo = new() { { 0, "t_no" }, { 1, "t_yes" }, { 2, "t_no_data" }, { 3, "t_no_data" } };
        static readonly Dictionary<int, string> defMeaningsOnOff = new() { { 0, "t_off" }, { 1, "t_on" }, { 2, "t_no_data" }, { 3, "t_no_data" } };
        static readonly Dictionary<int, string> defMeaningsAllow = new() { { 0, "t_disabled" }, { 1, "t_enabled" }, { 2, "t_no_data" }, { 3, "t_no_data" } };
        static readonly Dictionary<int, string> Stages = new() { { 0, "STAGE_Z" }, { 1, "STAGE_P" }, { 2, "STAGE_H" }, { 3, "STAGE_W" }, { 4, "STAGE_F" }, { 5, "STAGE_T" }, { 6, "STAGE_M" } };

        public bool UseImperial { set; get; }

        public static Dictionary<int, PGN> PGNs = new();

        public static Dictionary<int, OmniCommand> commands = new();

        private readonly BindingList<DeviceViewModel> connectedDevices = new();
        public BindingList<DeviceViewModel> ConnectedDevices => connectedDevices;

        private readonly UpdatableList<OmniMessage> messages = new();
        public UpdatableList<OmniMessage> Messages => messages;

        private readonly CanAdapter canAdapter;
        private readonly UartAdapter uartAdapter;

        public bool ReadingBBErrorsMode { get; set; } = false;

        OmniTask currentTask = new();

        SynchronizationContext UIContext = SynchronizationContext.Current;

        public OmniTask CurrentTask {get => currentTask; set => Set(ref currentTask, value);}

        private bool CancellationRequested => CurrentTask.CTS.IsCancellationRequested;

        private bool Capture(string n) => CurrentTask.Capture(n);

        private void Done() => CurrentTask.onDone();

        private void Cancel() => CurrentTask.onCancel();

        private void Fail(string reason = "") => CurrentTask.onFail(reason);

        private void UpdatePercent(int p) => CurrentTask.UpdatePercent(p);

        public void ProcessCanMessage(CanMessage m)
        {
            ProcessOmniMessage(new OmniMessage(m));
        }

        public void ProcessOmniMessage(OmniMessage m)
        {
            DeviceId id = m.TransmitterId;

            DeviceViewModel senderDevice = ConnectedDevices.FirstOrDefault(d => d.ID.Equals(m.TransmitterId));

            if (senderDevice == null)
            {
                senderDevice = new DeviceViewModel(id);
                ConnectedDevices.Add(senderDevice);
                NewDeviceAquired?.Invoke(this, null);
                if (senderDevice.ID.Type != 123)        //Requesting basic data, but not for bootloaders
                    Task.Run(() => RequestBasicData(id));
            }


            if (!PGNs.ContainsKey(m.PGN))
            {
                Debug.WriteLine($"{m.PGN} {GetString("t_not supported")}");
                return; //Такого PGN нет в библиотеке
            }



            foreach (OmniPgnParameter p in PGNs[m.PGN].parameters)
            {

                if (PGNs[m.PGN].multipack && p.PackNumber != m.Data[0]) continue;
                if (p.Var != 0)
                {
                    StatusVariable sv = new StatusVariable(p.Var);
                    sv.AssignedParameter = p;
                    long rawValue = OmniMessage.getRaw(m.Data, p.BitLength, p.StartBit, p.StartByte, p.Signed);
                    if (rawValue == Math.Pow(2, p.BitLength) - 1) continue; //Неподдерживаемый параметр, ливаем
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
                        senderDevice.Parameters.FlameSensor = (int)ImperialConverter(rawValue * sv.AssignedParameter.a + sv.AssignedParameter.b, sv.AssignedParameter.UnitT);
                    if (sv.Id == 7)
                        senderDevice.Parameters.BodyTemp = (int)ImperialConverter(rawValue * sv.AssignedParameter.a + sv.AssignedParameter.b, sv.AssignedParameter.UnitT);
                    if (sv.Id == 8)
                        senderDevice.Parameters.PanelTemp = (int)ImperialConverter(rawValue * sv.AssignedParameter.a + sv.AssignedParameter.b, sv.AssignedParameter.UnitT);
                    if (sv.Id == 10)
                        senderDevice.Parameters.InletTemp = (int)ImperialConverter(rawValue * sv.AssignedParameter.a + sv.AssignedParameter.b, sv.AssignedParameter.UnitT);

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
                        senderDevice.Parameters.LiquidTemp = (int)ImperialConverter(rawValue * sv.AssignedParameter.a + sv.AssignedParameter.b, sv.AssignedParameter.UnitT);
                    if (sv.Id == 41)
                        senderDevice.Parameters.OverheatTemp = (int)ImperialConverter(rawValue * sv.AssignedParameter.a + sv.AssignedParameter.b, sv.AssignedParameter.UnitT);
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
                        senderDevice.ReadedParameters.TryToAdd(new() { Id = parameterId, Value = parameterValue });
                        Debug.WriteLine($"{GetString($"par_{parameterId}")}={parameterValue}");
                    }
                    else
                        if (GotResource($"par_{parameterId}"))
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

            if (m.PGN == 19) //Версия
            {

                if (m.Data[0] == 4)
                {
                    if (m.Data[1] < 4) senderDevice.Timber.Zones[0].Connected = (zoneType_t)m.Data[1];
                    if (m.Data[2] < 4) senderDevice.Timber.Zones[1].Connected = (zoneType_t)m.Data[2];
                    if (m.Data[3] < 4) senderDevice.Timber.Zones[2].Connected = (zoneType_t)m.Data[3];
                    if (m.Data[4] < 4) senderDevice.Timber.Zones[3].Connected = (zoneType_t)m.Data[4];
                    if (m.Data[5] < 4) senderDevice.Timber.Zones[4].Connected = (zoneType_t)m.Data[5];
                }
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
                    if ((m.Data[0] & 3) == 0) senderDevice.Timber.Zones[0].State = zoneState_t.Off;
                    if ((m.Data[0] & 3) == 1) senderDevice.Timber.Zones[0].State = zoneState_t.Heat;
                    if ((m.Data[0] & 3) == 2) senderDevice.Timber.Zones[0].State = zoneState_t.Fan;
                }
                
                if (((m.Data[0] >> 2) & 3) != 3)
                {
                    if (((m.Data[0] >> 2) & 3) == 0) senderDevice.Timber.Zones[1].State = zoneState_t.Off;
                    if (((m.Data[0] >> 2) & 3) == 1) senderDevice.Timber.Zones[1].State = zoneState_t.Heat;
                    if (((m.Data[0] >> 2) & 3) == 2) senderDevice.Timber.Zones[1].State = zoneState_t.Fan;
                }
                
                if (((m.Data[0] >> 4) & 3) != 3)
                {
                    if (((m.Data[0] >> 4) & 3) == 0) senderDevice.Timber.Zones[2].State = zoneState_t.Off;
                    if (((m.Data[0] >> 4) & 3) == 1) senderDevice.Timber.Zones[2].State = zoneState_t.Heat;
                    if (((m.Data[0] >> 4) & 3) == 2) senderDevice.Timber.Zones[2].State = zoneState_t.Fan;
                }
                if (((m.Data[0] >> 6) & 3) != 3)
                {
                    if (((m.Data[0] >> 6) & 3) == 0) senderDevice.Timber.Zones[3].State = zoneState_t.Off;
                    if (((m.Data[0] >> 6) & 3) == 1) senderDevice.Timber.Zones[3].State = zoneState_t.Heat;
                    if (((m.Data[0] >> 6) & 3) == 2) senderDevice.Timber.Zones[3].State = zoneState_t.Fan;
                }
                if ((m.Data[1] & 3) != 3)
                {
                    if ((m.Data[1] & 3) == 0) senderDevice.Timber.Zones[4].State = zoneState_t.Off;
                    if ((m.Data[1] & 3) == 1) senderDevice.Timber.Zones[4].State = zoneState_t.Heat;
                    if ((m.Data[1] & 3) == 2) senderDevice.Timber.Zones[4].State = zoneState_t.Fan;
                }

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

        public void ProcessUartMessage(byte[] buf)
        {

        }

        public void ProcessMessage(UInt16 pgn, byte[] data)
        {

        }
        public async void ReadBlackBoxData(DeviceId id)
        {
            if (!Capture("t_reading_bb_parameters")) return;
            DeviceViewModel currentDevice = ConnectedDevices.FirstOrDefault(d => d.ID == id);
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

        public async void CheckPump(DeviceViewModel cd)
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
            if (!Capture("t_reading_b_errors")) return;
            ReadingBBErrorsMode = true;

            DeviceViewModel currentDevice = ConnectedDevices.FirstOrDefault(i => i.ID.Equals(id));

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
            if (!Capture("t_bb_common_erasing")) return;

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
            if (!Capture("t_bb_errors_erasing")) return;

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
            if (!Capture("t_reading_flash_parameters")) return;
            int cnt = 0;

            DeviceViewModel currentDevice = ConnectedDevices.FirstOrDefault(i => i.ID.Equals(id));

            for (int parId = 0; parId < 601; parId++) //Currently we have 600 parameters
            {
                if (!GotResource($"par_{parId}")) // Если нет параметра в ресурсах - не запрашиваем ???
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
            DeviceViewModel currentDevice = ConnectedDevices.FirstOrDefault(i => i.ID.Equals(id));

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
            if (!Capture("t_saving_params_to_flash")) return;
            var dev = ConnectedDevices.FirstOrDefault(d => d.ID.Equals(id));
            if (dev == null) return;
            OmniMessage msg = new OmniMessage();
            List<ReadedParameter> tempCollection = new List<ReadedParameter>();
            foreach (var p in dev.ReadedParameters)
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
            if (!Capture("t_config_erase")) return;
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
            if ((bool)canAdapter?.PortOpened)
                canAdapter.Transmit(m.ToCanMessage());
            if (uartAdapter.SelectedPort != null && uartAdapter.SelectedPort.IsOpen)
                uartAdapter.Transmit(m);
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

        
    }

}




