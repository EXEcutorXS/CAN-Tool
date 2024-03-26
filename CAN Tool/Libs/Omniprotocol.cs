using CAN_Tool.Libs;
using CAN_Tool.ViewModels;
using CAN_Tool;
using CommunityToolkit.Mvvm.ComponentModel;
using OmniProtocol;
using ScottPlot;
using System.Collections.Generic;
using System;
using System.ComponentModel;
using System.Diagnostics;
using System.Diagnostics.CodeAnalysis;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Threading;
using System.Windows.Media;
using static CAN_Tool.Libs.Helper;
using static Omni;
using System.Windows;
using CommunityToolkit.Mvvm.Input;
using System.IO;
using Newtonsoft;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using System.Collections.Specialized;
using ScottPlot.WPF;


namespace OmniProtocol
{

    public enum UnitType { None, Temp, Volt, Current, Pressure, Flow, Rpm, Rps, Percent, Second, Minute, Hour, Day, Month, Year, Frequency }

    public enum DeviceType { Binar, Planar, Hcu, ValveControl, BootLoader, CookingPanel, ExtensionBoard, PressureSensor }

    public class GotOmniMessageEventArgs : EventArgs
    {
        public OmniMessage receivedMessage;
    }

    public class PgnClass
    {
        public int id;
        public string name = "";
        public bool multiPack;
        public List<OmniPgnParameter> parameters = new();
    }

    public class ConfigPreset : ObservableObject
    {
        public int DeviceType;
        public string VendorName;
        public string ModelName;
        public BindingList<Tuple<int, uint>> ParamList = new();

    }


    public class OmniPgnParameter
    {
        internal int StartByte;   //Начальный байт в пакете
        internal int StartBit;    //Начальный бит в байте
        internal int BitLength;   //Длина параметра в битах
        internal bool Signed; //Число со знаком
        public string Name { set; get; }     //Имя параметра
        internal double a = 1;         //коэффициент приведения
        internal double b = 0;         //смещение

        public UnitType UnitT { get; set; } = UnitType.None;

        internal Dictionary<int, string> Meanings { set; get; } = new();
        internal Func<int, string> GetMeaning; //Принимает на вход сырое значение, возвращает строку с расшифровкой значения параметра
        internal Func<byte[], string> CustomDecoder; //Если для декодирования нужен весь пакет данных
        internal int PackNumber; //Номер пакета в мультипакете
        internal int Var; //Соответствующая переменная из paramsName.h
        public double DefaultValue; //Для конструктора комманд
        public bool AnswerOnly; //Присутствует только в ответе, не задаётся в комманде

        public string OutputFormat
        {
            get
            {
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
                            return GetString("u_kpa");
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
                    var value = ImperialConverter(rawDouble * a + b, UnitT);


                    retString.Append(value.ToString(OutputFormat) + " " + Unit);
                }
            }
            return retString.ToString();
        }

        public override string ToString() => Name;
    }

    public partial class OmniCommand : ObservableObject
    {
        [ObservableProperty] private int id;

        public string Name => GetString("c_" + Id.ToString());

        public List<OmniPgnParameter> Parameters { get; } = new();

        public override string ToString() => Name;

    }

    public partial class OmniZoneHandler : ObservableObject
    {

        [ObservableProperty] private int tempSetPointDay = 22;

        [ObservableProperty] private int tempSetPointNight = 20;

        [ObservableProperty] private int currentTemperature;

        [NotifyPropertyChangedFor(nameof(ManualMode))]
        [ObservableProperty] private zoneType_t connected = zoneType_t.Disconnected;

        [ObservableProperty]
        private zoneState_t state = zoneState_t.Off;

        [ObservableProperty] private bool manualMode;

        [ObservableProperty] private int manualPercent = 40;

        [ObservableProperty] private int setPwmPercent = 50;

        [ObservableProperty] private int fanStage = 2;

        [ObservableProperty] private int currentPwm = 50;
    }

    public partial class Timberline20OmniViewModel : ObservableObject
    {
        //private void ZoneChanged(OmniZoneHandler newSelectedZone) => SelectedZone = newSelectedZone;

        public Timberline20OmniViewModel()
        {
            for (var i = 0; i < 5; i++)
                Zones.Add(new OmniZoneHandler());

            SelectedZone = zones[0];
        }

        [ObservableProperty] private int tankTemperature;

        [ObservableProperty] private int outsideTemperature;

        [ObservableProperty] private int liquidLevel;

        [ObservableProperty] private bool heaterEnabled;

        [ObservableProperty] private bool elementEnabled;

        [ObservableProperty] private bool domesticWaterFlow;

        [ObservableProperty] private DateTime time;

        [ObservableProperty] private OmniZoneHandler selectedZone;

        [ObservableProperty] private BindingList<OmniZoneHandler> zones = new();
    }

    public partial class OmniMessage : ObservableObject, IUpdatable<OmniMessage>, IComparable
    {
        public OmniMessage()
        {
            Fresh = true;
            updateTick = DateTime.Now.Ticks;
            TransmitterId = new(126, 6);
            ReceiverId = new(0, 0);
            Data = new byte[8];
            for (var i = 0; i < 8; i++)
                Data[i] = 0xff;
        }
        public OmniMessage(CanMessage m) : this()
        {
            if (m.Dlc != 8 || m.Rtr || !m.Ide)
                throw new ArgumentException("CAN message is not compliant with OmniProtocol");
            Data = m.Data;
            Pgn = (m.Id >> 20) & 0b111111111;
            ReceiverId.Type = (m.Id >> 13) & 0b1111111;
            ReceiverId.Address = (m.Id >> 10) & 0b111;
            TransmitterId.Type = (m.Id >> 3) & 0b1111111;
            TransmitterId.Address = m.Id & 0b111;
            return;
        }

        public long updateTick;

        [ObservableProperty] private bool fresh;

        [NotifyPropertyChangedFor(nameof(DataAsText), nameof(DataAsULong), nameof(VerboseInfo))]
        [ObservableProperty] private byte[] data = new byte[8];

        public CanMessage ToCanMessage()
        {
            CanMessage ret = new();
            ret.Data = Data;
            ret.Dlc = 8;
            ret.Ide = true;
            ret.Rtr = false;
            ret.Id = (Pgn << 20) + (ReceiverId.Type << 13) + (ReceiverId.Address << 10) + (TransmitterId.Type << 3) + TransmitterId.Address;
            return ret;
        }

        public ulong DataAsULong
        {
            get
            {
                var bytes = new byte[8];
                Data.CopyTo(bytes, 0);
                Array.Reverse(bytes);
                return BitConverter.ToUInt64(bytes, 0);

            }
            set
            {
                var tempArr = BitConverter.GetBytes(value);
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
                for (var i = 0; i < 8; i++)
                    sb.Append($"{Data[i]:X02} ");
                return sb.ToString();
            }
        }
        public void FreshCheck()
        {
            if (Fresh && (DateTime.Now.Ticks - updateTick > 3000000))
                Fresh = false;
        }

        [NotifyPropertyChangedFor(nameof(VerboseInfo))]
        [ObservableProperty] private int pgn;
        [ObservableProperty] private DeviceId transmitterId;
        [ObservableProperty] private DeviceId receiverId;

        public static long GetRawValue(byte[] data, int bitLength, int startBit, int startByte, bool signed)
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
                        ret = BitConverter.ToUInt16(new[] { data[startByte + 1], data[startByte] });
                    else
                        ret = BitConverter.ToInt16(new[] { data[startByte + 1], data[startByte] });
                    break;


                case 24: ret = data[startByte] * 65536 + data[startByte + 1] * 256 + data[startByte + 2]; break;

                case 32:
                    if (!signed)
                        ret = BitConverter.ToUInt32(new[] { data[startByte + 3], data[startByte + 2], data[startByte + 1], data[startByte] });
                    else
                        ret = BitConverter.ToInt32(new[] { data[startByte + 3], data[startByte + 2], data[startByte + 1], data[startByte] });
                    break;
                default: throw new Exception("Bad parameter size");
            }
            return ret;
        }

        public string PrintParameter(OmniPgnParameter p)
        {
            StringBuilder retString = new();
            var rawValue = GetRawValue(Data, p.BitLength, p.StartBit, p.StartByte, p.Signed);
            retString.Append(GetString(p.Name) + ": ");
            if (p.CustomDecoder != null)
                return p.CustomDecoder(Data);

            if (p.GetMeaning != null)
                return p.GetMeaning((int)rawValue);
            if (p.Meanings != null && p.Meanings.ContainsKey((int)rawValue))
                retString.Append(rawValue + " - " + GetString(p.Meanings[(int)rawValue]));
            else
            {
                if (Math.Abs(rawValue - (Math.Pow(2, p.BitLength) - 1)) < 0.3)
                    retString.Append($"{GetString("t_no_data")}({rawValue})");
                else
                {
                    double rawDouble = rawValue;
                    var value = ImperialConverter(rawDouble * p.a + p.b, p.UnitT);
                    retString.Append(value.ToString(p.OutputFormat) + p.Unit);
                }
            }
            retString.Append(';');
            return retString.ToString();
        }

        public string GetVerboseInfo()
        {
            var retString = new StringBuilder();
            if (!Pgns.ContainsKey(this.Pgn))
                return "Pgn not found";
            var pgn = Pgns[this.Pgn];
            var sender = Devices.ContainsKey(TransmitterId.Type) ? Devices[TransmitterId.Type].Name : $"({GetString("t_unknown_device")} №{TransmitterId.Type})";
            var receiver = Devices.ContainsKey(ReceiverId.Type) ? Devices[ReceiverId.Type].Name : $"({GetString("t_unknown_device")} №{ReceiverId.Type})";
            retString.Append($"{sender}({TransmitterId.Address})->{receiver}({ReceiverId.Address});;");


            retString.Append(GetString(pgn.name) + ";;");
            if (pgn.multiPack)
                retString.Append($"{GetString("t_multipack")} №{Data[0]};");
            if (Pgn == 1 && Commands.ContainsKey(Data[1] + Data[0] * 256))
            {
                var cmd = Commands[Data[1] + Data[0] * 256];
                retString.Append(GetString(cmd.Name) + ";");
                if (cmd.Parameters != null)
                    foreach (var p in cmd.Parameters)
                        retString.Append(PrintParameter(p));
            }

            if (pgn.parameters == null) return retString.ToString();
            {
                foreach (var p in pgn.parameters.Where(p => !pgn.multiPack || Data[0] == p.PackNumber))
                    retString.Append(PrintParameter(p));
            }
            return retString.ToString();
        }

        public void Update(OmniMessage item)
        {
            Pgn = item.Pgn;
            TransmitterId.Address = item.TransmitterId.Address;
            TransmitterId.Type = item.TransmitterId.Type;
            ReceiverId.Address = item.ReceiverId.Address;
            ReceiverId.Type = item.ReceiverId.Type;
            Data = item.Data;
            Fresh = true;
            updateTick = DateTime.Now.Ticks;
        }

        public bool IsSimiliarTo(OmniMessage m)
        {
            if (Pgn != m.Pgn)
                return false;
            if (Pgn == 1 || Pgn == 2)
                if (Data[1] != m.Data[1])
                    return false;
            if (Pgns[Pgn].multiPack && Data[0] != m.Data[0]) //Другой номер мультипакета
                return false;
            return true;
        }

        public int CompareTo(object other)
        {
            return Pgn.CompareTo((other as OmniMessage).Pgn);
        }

        public string VerboseInfo => GetVerboseInfo().Replace(';', '\n');

        public override string ToString()
        {
            StringBuilder retString = new();
            retString.Append($"<{Pgn:D02}>[{TransmitterId.Type}]({TransmitterId.Address})->[{ReceiverId.Type}]({ReceiverId.Address}):");
            foreach (var b in Data)
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
    }

    public class Device
    {
        public int Id;
        public string Name => GetString($"d_{Id}");
        public DeviceType DevType { set; get; }

        public int MaxBlower { get; set; } = 130; //Максимальное значение скорости нагнетателя
        public double MaxFuelPump { get; set; } = 4; //Максимальное значение ТН
        public int BBErrorsLen { get; set; } = 512; //Длина ЧЯ для ошибок

        public override string ToString() => Name;
    }

    public partial class ReadedBlackBoxValue : ObservableObject, IUpdatable<ReadedBlackBoxValue>, IComparable
    {
        [ObservableProperty] private int id;

        [ObservableProperty] private uint value;

        public void Update(ReadedBlackBoxValue item) => Value = item.Value;

        public bool IsSimiliarTo(ReadedBlackBoxValue item) => Id == item.Id;

        public int CompareTo(object obj) => Id - (obj as ReadedBlackBoxValue).Id;

        public string Description => GetString($"bb_{Id}");
    }

}

public partial class ReadedParameter : ObservableObject, IUpdatable<ReadedParameter>, IComparable
{
    [ObservableProperty] private int id;

    [ObservableProperty] private uint value;

    public string Name => GetString($"par_{Id}");

    public void Update(ReadedParameter item) => Value = item.Value;

    public bool IsSimiliarTo(ReadedParameter item) => (Id == item.Id);

    public int CompareTo(object obj) => Id - (obj as ReadedParameter).Id;
}

public partial class StatusVariable : ObservableObject, IUpdatable<StatusVariable>, IComparable
{
    public int[] LineWidthes => new int[] { 1, 2, 3, 4, 5 };

    public StatusVariable(int var) : base()
    {
        Id = var;
        Display = App.Settings.ShowFlag[Id];
        chartBrush = new SolidColorBrush(App.Settings.Colors[Id]);
        lineWidth = App.Settings.LineWidthes[Id];
        LineStyle = App.Settings.LineStyles[Id];
        markShape = App.Settings.MarkShapes[Id];

    }

    [ObservableProperty] public int id;

    [NotifyPropertyChangedFor(nameof(VerboseInfo), nameof(Value), nameof(FormattedValue))]
    [ObservableProperty] private long rawValue;
    [ObservableProperty] private bool display = false;
    [ObservableProperty] private OmniPgnParameter assignedParameter;
    [NotifyPropertyChangedFor(nameof(Color))]
    [ObservableProperty] private Brush chartBrush;
    [ObservableProperty] private int lineWidth;
    [ObservableProperty] private LineStyle lineStyle;
    [ObservableProperty] private MarkerShape markShape;

    public string VerboseInfo => AssignedParameter.Decode(RawValue);

    public double Value => ImperialConverter(RawValue * AssignedParameter.a + AssignedParameter.b, AssignedParameter.UnitT);

    public string FormattedValue => Value.ToString(AssignedParameter.OutputFormat);

    public string Name => GetString($"var_{Id}");

    public string ShortName => GetString($"vars_{Id}");

    public System.Drawing.Color Color => System.Drawing.Color.FromArgb(255, (ChartBrush as SolidColorBrush).Color.R, (ChartBrush as SolidColorBrush).Color.G, (ChartBrush as SolidColorBrush).Color.B);

    public bool IsSimiliarTo(StatusVariable item) => Id == item.Id;

    public void Update(StatusVariable item) => RawValue = item.RawValue;

    public int CompareTo(object obj) => Id - (obj as StatusVariable).Id;
}

public partial class BbCommonVariable : ObservableObject, IUpdatable<BbCommonVariable>, IComparable
{
    [ObservableProperty] private int id;

    [ObservableProperty] public int value;

    public string Name => GetString($"var_{Id}");

    public string Description => ToString();

    public int CompareTo(object obj) => Id - ((BbCommonVariable)obj).Id;

    public void Update(BbCommonVariable item) => Value = item.Value;
    public bool IsSimiliarTo(BbCommonVariable item) => Id == item.Id;
    public override string ToString() => $"{Name}: {Value}";
}

public partial class BbError : ObservableObject
{
    public UpdatableList<BbCommonVariable> Variables { get; } = new();

    public override string ToString()
    {
        var retString = new StringBuilder("");
        foreach (var v in Variables)
            retString.Append(v + ";");
        return retString.ToString();
    }

    public string Name
    {
        get
        {
            var error = Variables.FirstOrDefault(v => v.Id == 24); //24 - paramsname.h error code

            return GetString(error == null ? "t_no_error_code" : $"e_{error.Value}");
        }
    }
}

public partial class OmniTask : ObservableObject
{
    [NotifyPropertyChangedFor(nameof(TaskStatus))]
    [ObservableProperty]
    private int percentComplete;

    private DateTime capturedTime;

    [NotifyPropertyChangedFor(nameof(TaskStatus))]
    [ObservableProperty] private string name;

    public CancellationTokenSource Cts { get; set; } = new CancellationTokenSource();

    [NotifyPropertyChangedFor(nameof(TaskStatus))]
    [ObservableProperty]
    private bool done;

    [NotifyPropertyChangedFor(nameof(TaskStatus))]
    [ObservableProperty]
    private bool cancelled;

    public event EventHandler TaskDone;
    public event EventHandler TaskCancelled;

    [ObservableProperty] private TimeSpan? lastOperationDuration;

    public void OnDone()
    {
        LastOperationDuration = DateTime.Now - capturedTime;
        Occupied = false;
        PercentComplete = 100;
        Done = true;
        TaskDone?.Invoke(null, null!);
    }

    public void OnCancel()
    {
        Cts.Cancel();
        Occupied = false;
        Cancelled = true;
        TaskCancelled?.Invoke(null, null!);
    }

    public void OnFail(string reason = "")
    {
        FailReason = reason;
        Cts.Cancel();
        Occupied = false;
        Failed = true;
        TaskCancelled?.Invoke(null, null!);
    }


    [NotifyPropertyChangedFor(nameof(TaskStatus))]
    [ObservableProperty] private bool occupied;

    [NotifyPropertyChangedFor(nameof(TaskStatus))]
    [ObservableProperty] private string failReason;

    [NotifyPropertyChangedFor(nameof(TaskStatus))]
    [ObservableProperty] private bool failed;

    public bool Capture(string taskName)
    {
        if (Occupied) return false;
        Occupied = true;
        Name = taskName;
        FailReason = "";
        PercentComplete = 0;
        Done = false;
        Cancelled = false;
        Cts = new CancellationTokenSource();
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

public partial class MainParameters : ObservableObject, ICloneable
{
    [ObservableProperty] private int revMeasured;
    [ObservableProperty] private int revSet;
    [ObservableProperty] private double fuelPumpMeasured;
    [ObservableProperty] private int glowPlug;
    [ObservableProperty] private double voltage;
    [ObservableProperty] private int stageTime;
    [ObservableProperty] private int modeTime;

    [NotifyPropertyChangedFor(nameof(StageString))]
    [ObservableProperty] private int stage;

    [NotifyPropertyChangedFor(nameof(StageString))]
    [ObservableProperty] private int mode;

    [NotifyPropertyChangedFor(nameof(ErrorString))]
    [ObservableProperty] private int error;
    [ObservableProperty] private int workTime;
    [ObservableProperty] private int flameSensor;
    [ObservableProperty] private int bodyTemp;
    [ObservableProperty] private int liquidTemp;
    [ObservableProperty] private int overheatTemp;
    [ObservableProperty] private int panelTemp;
    [ObservableProperty] private int inletTemp;
    [ObservableProperty] private float pressure;


    public string StageString => GetString($"m_{Stage}-{Mode}");

    public string ErrorString => Error + " - " + GetString($"e_{Error}");

    public object Clone() => MemberwiseClone();
}



public partial class DeviceId : ObservableObject
{
    public DeviceId(int type, int adr)
    {
        if (type > 127 || adr > 7)
            throw new ArgumentException("Bad device config address must be below 7 and Type - below 127");
        Type = type;
        Address = adr;
    }

    [ObservableProperty] private int type;
    [ObservableProperty] private int address;

    public override string ToString() => Devices.ContainsKey(Type) ? $"{Type} - {Address} ({Devices[Type]})" : $"{Type} - {Address}";

    public override int GetHashCode() => Type << 3 + Address;

    public override bool Equals([NotNullWhen(true)] object obj)
    {
        if (obj is not DeviceId) return false;
        return GetHashCode() == obj.GetHashCode();
    }
}

public partial class Omni : ObservableObject
{
    public static void SeedStaticData()
    {

        #region Device names init
        Devices = new Dictionary<int, Device>() {
            { 0, new (){Id=0, } } ,
            { 1, new (){Id=1, DevType=DeviceType.Binar}} ,
            { 2, new (){Id=2, DevType=DeviceType.Planar}} ,
            { 3, new (){Id=3, DevType=DeviceType.Planar}} ,
            { 4, new (){Id=4, DevType=DeviceType.Binar}} ,
            { 5, new (){Id=5, DevType=DeviceType.Binar}} ,
            { 6, new (){Id=6, DevType=DeviceType.Binar,MaxBlower=200}} ,
            { 7, new (){Id=7, DevType=DeviceType.Planar}} ,
            { 8, new (){Id=8, DevType=DeviceType.Binar}} ,
            { 9, new (){Id=9, DevType=DeviceType.Planar }} ,
            { 10, new (){Id=10, DevType=DeviceType.Binar,MaxBlower=200}} ,
            { 11, new (){Id=11, DevType=DeviceType.Planar}} ,
            { 12, new (){Id=12, DevType=DeviceType.Planar}} ,
            { 13, new (){Id=13, DevType=DeviceType.Planar}} ,
            { 14, new (){Id=14, DevType=DeviceType.CookingPanel}} ,
            { 15, new (){Id=15, DevType=DeviceType.Planar}} ,
            { 16, new (){Id=16, DevType=DeviceType.Binar}} ,
            { 17, new (){Id=17, DevType=DeviceType.Binar}} ,
            { 18, new (){Id=18, DevType=DeviceType.Planar}} ,
            { 19, new (){Id=19, DevType=DeviceType.ValveControl}} ,
            { 20, new (){Id=20, DevType=DeviceType.Planar}} ,
            { 21, new (){Id=21, DevType=DeviceType.Binar}} ,
            { 22, new (){Id=22, DevType=DeviceType.Binar}} ,
            { 23, new (){Id=23, DevType=DeviceType.Binar,MaxBlower=90,MaxFuelPump=4}} ,
            { 25, new (){Id=25, DevType=DeviceType.Binar}} ,
            { 27, new (){Id=27, DevType=DeviceType.Binar, MaxBlower=90,MaxFuelPump=4}} ,
            { 29, new (){Id=29, DevType=DeviceType.Binar}} ,
            { 31, new (){Id=31, DevType=DeviceType.Binar}} ,
            { 32, new (){Id=32, DevType=DeviceType.Binar}} ,
            { 34, new (){Id=34, DevType=DeviceType.Binar, MaxBlower=150}} ,
            { 35, new (){Id=35, DevType=DeviceType.Binar, MaxBlower=150}} ,
            { 37, new (){Id=37, DevType=DeviceType.ExtensionBoard}} ,
            { 40, new (){Id=40, DevType=DeviceType.PressureSensor}} ,
            { 123, new (){Id=123, DevType=DeviceType.BootLoader }} ,
            { 126, new (){Id=126, DevType=DeviceType.Hcu }},
            { 255, new (){Id=255}}
        };
        #endregion

        #region Pgn names init
        Pgns.Add(0, new() { id = 0, name = "t_empty_command" });
        Pgns.Add(1, new() { id = 1, name = "t_control_command" });
        Pgns.Add(2, new() { id = 2, name = "t_received_command_ack" });
        Pgns.Add(3, new() { id = 3, name = "t_spn_request" });
        Pgns.Add(4, new() { id = 4, name = "t_spn_answer" });
        Pgns.Add(5, new() { id = 5, name = "t_parameter_write" });
        Pgns.Add(6, new() { id = 6, name = "t_pgn_request" });
        Pgns.Add(7, new() { id = 7, name = "t_flash_conf_read_write" });
        Pgns.Add(8, new() { id = 8, name = "t_black_box_operation" });
        Pgns.Add(10, new() { id = 10, name = "t_stage_mode_failures" });
        Pgns.Add(11, new() { id = 11, name = "t_voltage_pressure_current" });
        Pgns.Add(12, new() { id = 12, name = "t_blower_fp_plug_relay" });
        Pgns.Add(13, new() { id = 13, name = "t_liquid_heater_temperatures" });
        Pgns.Add(14, new() { id = 14, name = "t_flame_process" });
        Pgns.Add(15, new() { id = 15, name = "t_adc0-3" });
        Pgns.Add(16, new() { id = 16, name = "t_adc4-7" });
        Pgns.Add(17, new() { id = 17, name = "t_adc8-11" });
        Pgns.Add(18, new() { id = 18, name = "t_firmware_version" });
        Pgns.Add(19, new() { id = 19, name = "t_hcu_parameters", multiPack = true });
        Pgns.Add(20, new() { id = 20, name = "t_failures" });
        Pgns.Add(21, new() { id = 21, name = "t_hcu_status" });
        Pgns.Add(22, new() { id = 22, name = "t_zone_control" });
        Pgns.Add(23, new() { id = 23, name = "t_fan_setpoints" });
        Pgns.Add(24, new() { id = 24, name = "t_fan_current_speed" });
        Pgns.Add(25, new() { id = 25, name = "t_daytime_setpoins" });
        Pgns.Add(26, new() { id = 26, name = "t_nighttime_setpoints" });
        Pgns.Add(27, new() { id = 27, name = "t_fan_manual_control" });
        Pgns.Add(28, new() { id = 28, name = "t_total_working_time" });
        Pgns.Add(29, new() { id = 29, name = "t_Параметры давления", multiPack = true });
        Pgns.Add(30, new() { id = 30, name = "t_remote_wire_engine_air_temp" });
        Pgns.Add(31, new() { id = 31, name = "t_working_time" });
        Pgns.Add(32, new() { id = 32, name = "t_liquid_heater_setup" });
        Pgns.Add(33, new() { id = 33, name = "t_serial_number", multiPack = true });
        Pgns.Add(34, new() { id = 34, name = "t_read_flash_by_address_req" });
        Pgns.Add(35, new() { id = 35, name = "t_read_flash_by_address_ans" });
        Pgns.Add(36, new() { id = 36, name = "t_valves_status_probe_valve_failures" });
        Pgns.Add(37, new() { id = 37, name = "t_air_heater_temperatures", multiPack = true });
        Pgns.Add(38, new() { id = 38, name = "t_panel_temperature" });
        Pgns.Add(39, new() { id = 39, name = "t_drivers_status" });
        Pgns.Add(40, new() { id = 40, name = "t_date_time" });
        Pgns.Add(41, new() { id = 41, name = "t_day_night_backlight" });
        Pgns.Add(42, new() { id = 42, name = "t_pump_control" });
        Pgns.Add(43, new() { id = 43, name = "t_generic_board_pwm_command" });
        Pgns.Add(44, new() { id = 44, name = "t_generic_board_pwm_status" });
        Pgns.Add(45, new() { id = 45, name = "t_generic_board_temp" });
        Pgns.Add(46, new() { id = 46, name = "t_hcu_error_codes" });
        Pgns.Add(100, new() { id = 100, name = "t_memory_control_old", multiPack = true });
        Pgns.Add(101, new() { id = 101, name = "t_buffer_data_transmitting_old" });
        Pgns.Add(105, new() { id = 105, name = "t_memory_control" });
        Pgns.Add(106, new() { id = 106, name = "t_buffer_data_transmitting" });
        #endregion


        Commands.Add(0, new() { Id = 0 });
        Commands.Add(1, new() { Id = 1 });
        Commands.Add(3, new() { Id = 3 });
        Commands.Add(4, new() { Id = 4 });
        Commands.Add(5, new() { Id = 5 });
        Commands.Add(6, new() { Id = 6 });
        Commands.Add(7, new() { Id = 7 });
        Commands.Add(8, new() { Id = 8 });
        Commands.Add(9, new() { Id = 9 });
        Commands.Add(10, new() { Id = 10 });
        Commands.Add(20, new() { Id = 20 });
        Commands.Add(21, new() { Id = 21 });
        Commands.Add(22, new() { Id = 22 });
        Commands.Add(45, new() { Id = 45 });
        Commands.Add(65, new() { Id = 65 });
        Commands.Add(66, new() { Id = 66 });
        Commands.Add(67, new() { Id = 67 });
        Commands.Add(68, new() { Id = 68 });
        Commands.Add(69, new() { Id = 69 });
        Commands.Add(70, new() { Id = 70 });


        #region Command parameters init
        Commands[0].Parameters.Add(new() { StartByte = 2, BitLength = 8, GetMeaning = i => (GetString("t_device") + ": " + GetString($"d_{i}") + ";"), AnswerOnly = true }); ;
        Commands[0].Parameters.Add(new() { StartByte = 3, BitLength = 8, Meanings = { { 0, "t_12 volts" }, { 1, "t_24_volts" } }, AnswerOnly = true });
        Commands[0].Parameters.Add(new() { StartByte = 4, BitLength = 8, Name = "t_firmware", AnswerOnly = true });
        Commands[0].Parameters.Add(new() { StartByte = 5, BitLength = 8, Name = "t_modification", AnswerOnly = true });

        Commands[1].Parameters.Add(new() { StartByte = 2, BitLength = 16, Name = "t_working_time", UnitT = UnitType.Minute });

        Commands[4].Parameters.Add(new() { StartByte = 2, BitLength = 16, Name = "t_working_time", UnitT = UnitType.Minute });

        Commands[6].Parameters.Add(new() { StartByte = 4, BitLength = 4, Name = "t_working_mode", Meanings = { { 0, "t_regular" }, { 1, "t_eco" }, { 2, "t_additional_heater" }, { 3, "t_preheater" }, { 4, "t_heating_systems" } } });
        Commands[6].Parameters.Add(new() { StartByte = 4, StartBit = 4, BitLength = 4, Name = "t_additional_heater_mode", Meanings = { { 0, "t_off" }, { 1, "t_auto" }, { 2, "t_manual" } } });
        Commands[6].Parameters.Add(new() { StartByte = 5, BitLength = 16, Name = "t_temp_setpoint", UnitT = UnitType.Temp });
        Commands[6].Parameters.Add(new() { StartByte = 7, BitLength = 2, Name = "t_pump_in_idle", Meanings = DefMeaningsOnOff });
        Commands[6].Parameters.Add(new() { StartByte = 7, BitLength = 2, StartBit = 2, Name = "t_pump_while_engine_running", Meanings = DefMeaningsOnOff });

        Commands[7].Parameters.Add(new() { StartByte = 2, BitLength = 8, Name = "t_power_level" });

        Commands[7].Parameters.Add(new() { StartByte = 3, BitLength = 16, Name = "t_going_up_temperature", AnswerOnly = true });
        Commands[7].Parameters.Add(new() { StartByte = 4, BitLength = 16, Name = "t_going_down_temperature", AnswerOnly = true });

        Commands[8].Parameters.Add(new() { StartByte = 2, BitLength = 2, StartBit = 0, Name = "t_valve_1_state", Meanings = DefMeaningsOnOff });
        Commands[8].Parameters.Add(new() { StartByte = 2, BitLength = 2, StartBit = 2, Name = "t_valve_2_state", Meanings = DefMeaningsOnOff });
        Commands[8].Parameters.Add(new() { StartByte = 2, BitLength = 2, StartBit = 4, Name = "t_valve_3_state", Meanings = DefMeaningsOnOff });
        Commands[8].Parameters.Add(new() { StartByte = 2, BitLength = 2, StartBit = 6, Name = "t_valve_4_state", Meanings = DefMeaningsOnOff });
        Commands[8].Parameters.Add(new() { StartByte = 3, BitLength = 2, StartBit = 0, Name = "t_valve_5_state", Meanings = DefMeaningsOnOff });
        Commands[8].Parameters.Add(new() { StartByte = 3, BitLength = 2, StartBit = 2, Name = "t_valve_6_state", Meanings = DefMeaningsOnOff });
        Commands[8].Parameters.Add(new() { StartByte = 3, BitLength = 2, StartBit = 4, Name = "t_valve_7_state", Meanings = DefMeaningsOnOff });
        Commands[8].Parameters.Add(new() { StartByte = 3, BitLength = 2, StartBit = 6, Name = "t_valve_8_state", Meanings = DefMeaningsOnOff });
        Commands[8].Parameters.Add(new() { StartByte = 4, BitLength = 1, StartBit = 0, Meanings = { { 0, "t_do_not_clear_codes" }, { 1, "t_clear_error_codes" } } });

        Commands[9].Parameters.Add(new() { StartByte = 2, BitLength = 16, Name = "t_working_time", UnitT = UnitType.Minute });
        Commands[9].Parameters.Add(new() { StartByte = 4, BitLength = 4, Name = "t_working_mode", Meanings = { { 0, "t_not_used" }, { 1, "t_work_by_pcb_temp" }, { 2, "t_work_by_panel_sensor_temp" }, { 3, "t_work_by_external_sensor" }, { 4, "t_work_by_power" } } });
        Commands[9].Parameters.Add(new() { StartByte = 4, StartBit = 4, BitLength = 2, Name = "t_enable_idle_while_working_by_temp_sensor", Meanings = DefMeaningsAllow });
        Commands[9].Parameters.Add(new() { StartByte = 4, StartBit = 6, BitLength = 2, Name = "t_enable_blower_while_idle", Meanings = DefMeaningsAllow });
        Commands[9].Parameters.Add(new() { StartByte = 5, BitLength = 16, Name = "t_set_room_temperature", UnitT = UnitType.Temp });
        Commands[9].Parameters.Add(new() { StartByte = 7, BitLength = 4, Name = "t_power_setpoint" });

        Commands[10].Parameters.Add(new() { StartByte = 2, BitLength = 16, Name = "t_working_time", UnitT = UnitType.Temp });

        Commands[20].Parameters.Add(new() { StartByte = 2, BitLength = 16, Name = "t_1st_tcouple_cal", AnswerOnly = true });
        Commands[20].Parameters.Add(new() { StartByte = 4, BitLength = 16, Name = "t_2nd_tcouple_cal", AnswerOnly = true });

        Commands[21].Parameters.Add(new() { StartByte = 2, BitLength = 8, Name = "t_prescaler" });
        Commands[21].Parameters.Add(new() { StartByte = 3, BitLength = 8, Name = "t_pwm_period" });
        Commands[21].Parameters.Add(new() { StartByte = 5, BitLength = 8, Name = "t_required_freq", UnitT = UnitType.Frequency });

        Commands[22].Parameters.Add(new() { StartByte = 2, StartBit = 0, BitLength = 8, Name = "t_action_after_reset", Meanings = { { 0, "t_stay_in_boot" }, { 1, "t_to_main_program_without_delay" }, { 2, "t_5_sec_in_boot" } } });

        Commands[45].Parameters.Add(new() { StartByte = 2, StartBit = 0, BitLength = 2, Name = "t_mask_all", Meanings = DefMeaningsYesNo });
        Commands[45].Parameters.Add(new() { StartByte = 2, StartBit = 2, BitLength = 2, Name = "t_mask_fp_failures", Meanings = DefMeaningsYesNo });
        Commands[45].Parameters.Add(new() { StartByte = 2, StartBit = 4, BitLength = 2, Name = "t_mask_flamebreak_fails", Meanings = DefMeaningsYesNo });
        Commands[45].Parameters.Add(new() { StartByte = 2, StartBit = 6, BitLength = 2, Name = "t_mask_glow_plug_failures", Meanings = DefMeaningsYesNo });
        Commands[45].Parameters.Add(new() { StartByte = 3, StartBit = 0, BitLength = 2, Name = "t_mask_blower_failures", Meanings = DefMeaningsYesNo });
        Commands[45].Parameters.Add(new() { StartByte = 3, StartBit = 2, BitLength = 2, Name = "t_mask_sensors_failures", Meanings = DefMeaningsYesNo });
        Commands[45].Parameters.Add(new() { StartByte = 3, StartBit = 4, BitLength = 2, Name = "t_mask_pump_failures", Meanings = DefMeaningsYesNo });
        Commands[45].Parameters.Add(new() { StartByte = 3, StartBit = 6, BitLength = 2, Name = "t_mask_overheating_failures", Meanings = DefMeaningsYesNo });

        Commands[65].Parameters.Add(new() { StartByte = 2, StartBit = 0, BitLength = 8, Name = "", Meanings = { { 7, "t_liquid_temp" }, { 10, "t_overheat_temp" }, { 12, "t_flame_temperature" }, { 13, "t_body_temp" }, { 27, "t_air_temp" } } });
        Commands[65].Parameters.Add(new() { StartByte = 3, BitLength = 16, Name = "t_temp_value", UnitT = UnitType.Temp });

        Commands[67].Parameters.Add(new() { StartByte = 2, StartBit = 0, BitLength = 2, Name = "", Meanings = { { 0, "t_leave_m" }, { 1, "t_ener_m" } } });
        Commands[67].Parameters.Add(new() { StartByte = 2, StartBit = 2, BitLength = 2, Name = "", Meanings = { { 0, "t_leave_t" }, { 1, "t_enter_t" } } });

        Commands[68].Parameters.Add(new() { StartByte = 2, StartBit = 0, BitLength = 2, Name = "t_pump_state", Meanings = DefMeaningsOnOff });
        Commands[68].Parameters.Add(new() { StartByte = 3, StartBit = 0, BitLength = 8, Name = "t_blower_revs", UnitT = UnitType.Rps });
        Commands[68].Parameters.Add(new() { StartByte = 4, StartBit = 0, BitLength = 8, Name = "t_glow_plug", UnitT = UnitType.Percent });
        Commands[68].Parameters.Add(new() { StartByte = 5, StartBit = 0, BitLength = 16, Name = "t_fuel_pump_freq", a = 0.01, UnitT = UnitType.Frequency });

        Commands[69].Parameters.Add(new() { StartByte = 2, StartBit = 0, BitLength = 8, Name = "t_exec_dev_type", Meanings = { { 0, "t_fpx10" }, { 1, "t_relay01" }, { 2, "t_glow_plug_perc" }, { 3, "t_pump_perc" }, { 4, "t_blower_perc" }, { 23, "t_blower_revs_rps" } } });
        Commands[69].Parameters.Add(new() { StartByte = 3, StartBit = 0, BitLength = 16, Name = "Значение" });

        Commands[70].Parameters.Add(new() { StartByte = 2, StartBit = 0, BitLength = 2, Name = "t_fuel_pump_state", Meanings = DefMeaningsOnOff });
        Commands[70].Parameters.Add(new() { StartByte = 2, StartBit = 2, BitLength = 2, Name = "t_relay_state", Meanings = DefMeaningsOnOff });
        Commands[70].Parameters.Add(new() { StartByte = 2, StartBit = 4, BitLength = 2, Name = "t_glow_plug_state", Meanings = DefMeaningsOnOff });
        Commands[70].Parameters.Add(new() { StartByte = 2, StartBit = 6, BitLength = 2, Name = "t_pump_state", Meanings = DefMeaningsOnOff });
        Commands[70].Parameters.Add(new() { StartByte = 3, StartBit = 0, BitLength = 2, Name = "t_blower_state", Meanings = DefMeaningsOnOff });
        #endregion

        #region Pgn parameters initialise

        Pgns[3].parameters.Add(new() { Name = "SPN", BitLength = 16, StartByte = 0 });

        Pgns[4].parameters.Add(new() { Name = "SPN", BitLength = 16, StartByte = 0 });
        Pgns[4].parameters.Add(new() { Name = "Значение", BitLength = 32, StartBit = 0, StartByte = 2 });

        Pgns[5].parameters.Add(new() { Name = "SPN", BitLength = 16, StartByte = 0 });
        Pgns[5].parameters.Add(new() { Name = "Значение", BitLength = 32, StartBit = 0, StartByte = 2 });

        Pgns[6].parameters.Add(new() { Name = "Pgn", BitLength = 16, StartByte = 0, GetMeaning = x => { if (Pgns.ContainsKey(x)) return Pgns[x].name; else return "Нет такого Pgn"; } });

        Pgns[7].parameters.Add(new() { Name = "Команда", BitLength = 8, StartBit = 0, StartByte = 0, Meanings = { { 0, "Стереть конфигурацию" }, { 1, "Запись параметра в ОЗУ" }, { 2, "Запись всех параметров во Flash" }, { 3, "Чтение параметра по номеру" }, { 4, "Успешный ответ на запрос" }, { 5, "Невозможно выполнить" } } });
        Pgns[7].parameters.Add(new() { Name = "Запрошенная команда", BitLength = 8, StartBit = 0, StartByte = 1, Meanings = { { 0, "Стереть конфигурацию" }, { 1, "Запись параметра в ОЗУ" }, { 2, "Запись всех параметров во Flash" }, { 3, "Чтение параметра по номеру" }, { 255, "" } } });
        Pgns[7].parameters.Add(new() { Name = "Параметр", BitLength = 16, StartBit = 0, StartByte = 2, GetMeaning = x => GetString($"par_{x}") });
        Pgns[7].parameters.Add(new() { Name = "Value", BitLength = 32, StartBit = 0, StartByte = 4, AnswerOnly = true });

        Pgns[8].parameters.Add(new() { Name = "Команда", BitLength = 4, StartBit = 0, StartByte = 0, Meanings = { { 0, "Стереть ЧЯ" }, { 3, "Чтение ЧЯ" }, { 4, "Ответ" }, { 6, "Чтение параметра (из paramsname.h)" } } });
        Pgns[8].parameters.Add(new() { Name = "Тип:", BitLength = 2, StartBit = 4, StartByte = 0, Meanings = { { 0, "Общие данные" }, { 1, "Неисправности" } } });
        Pgns[8].parameters.Add(new() { Name = "Номер пары", CustomDecoder = d => { if ((d[0] & 0xF) == 3) return "Номер пары:" + (d[4] * 0x100 + d[5]).ToString() + ";"; else return ""; } });
        Pgns[8].parameters.Add(new() { Name = "Номер параметра", CustomDecoder = d => { if ((d[0] & 0xF) == 6) return "Номер параметра:" + (d[4] * 0x100 + d[5]).ToString() + ";"; else return ""; } });
        Pgns[8].parameters.Add(new() { Name = "Число пар", CustomDecoder = d => { if ((d[0] & 0xF) == 6) return "Запрошено пар:" + (d[6] * 0x100 + d[7]).ToString() + ";"; else return ""; } });
        Pgns[8].parameters.Add(new() { Name = "Номер параметра", CustomDecoder = d => { if (d[0] == 4) return "Параметр:" + (d[2] * 256 + d[3]).ToString() + ";"; else return ""; } });
        Pgns[8].parameters.Add(new() { Name = "Значение параметра", CustomDecoder = d => { if (d[0] == 4) return "Значение:" + (d[4] * 0x1000000 + d[5] * 0x10000 + d[6] * 0x100 + d[7]).ToString() + ";"; else return ""; }, AnswerOnly = true });


        Pgns[10].parameters.Add(new() { Name = "Стадия", BitLength = 8, StartByte = 0, Meanings = Stages, Var = 1 });
        Pgns[10].parameters.Add(new() { Name = "Режим", BitLength = 8, StartByte = 1, Var = 2 });
        Pgns[10].parameters.Add(new() { Name = "Код неисправности", BitLength = 8, StartByte = 2, Var = 24, GetMeaning = x => GetString($"e_{x}") });
        Pgns[10].parameters.Add(new() { Name = "Помпа неисправна", BitLength = 2, StartByte = 3, Meanings = DefMeaningsYesNo });
        Pgns[10].parameters.Add(new() { Name = "Код предупреждения", BitLength = 8, StartByte = 4 });
        Pgns[10].parameters.Add(new() { Name = "Количество морганий", BitLength = 8, StartByte = 5, Var = 25 });

        Pgns[11].parameters.Add(new() { Name = "Напряжение питания", BitLength = 16, StartByte = 0, a = 0.1, UnitT = UnitType.Volt, Var = 5 });
        Pgns[11].parameters.Add(new() { Name = "Атмосферное давление", BitLength = 8, StartByte = 2, UnitT = UnitType.Pressure });
        Pgns[11].parameters.Add(new() { Name = "Ток двигателя, значения АЦП", BitLength = 16, StartByte = 3 });
        Pgns[11].parameters.Add(new() { Name = "Ток двигателя, мА", BitLength = 16, StartByte = 5, UnitT = UnitType.Current, a = 0.001, Var = 128 });

        Pgns[12].parameters.Add(new() { Name = "Заданные обороты нагнетателя воздуха", BitLength = 8, StartByte = 0, UnitT = UnitType.Rps, Var = 15 });
        Pgns[12].parameters.Add(new() { Name = "Измеренные обороты нагнетателя воздуха,", BitLength = 8, StartByte = 1, UnitT = UnitType.Rps, Var = 16 });
        Pgns[12].parameters.Add(new() { Name = "Заданная частота ТН", BitLength = 16, StartByte = 2, a = 0.01, UnitT = UnitType.Frequency, Var = 17 });
        Pgns[12].parameters.Add(new() { Name = "Реализованная частота ТН", BitLength = 16, StartByte = 4, a = 0.01, UnitT = UnitType.Frequency, Var = 18 });
        Pgns[12].parameters.Add(new() { Name = "Мощность свечи", BitLength = 8, StartByte = 6, UnitT = UnitType.Percent, Var = 21 });
        Pgns[12].parameters.Add(new() { Name = "Состояние помпы", BitLength = 2, StartByte = 7, Meanings = DefMeaningsOnOff, Var = 46 });
        Pgns[12].parameters.Add(new() { Name = "Состояние реле печки кабины", BitLength = 2, StartByte = 7, StartBit = 2, Meanings = DefMeaningsOnOff, Var = 45 });
        Pgns[12].parameters.Add(new() { Name = "Состояние состояние канала сигнализации", BitLength = 2, StartByte = 7, StartBit = 4, Meanings = DefMeaningsOnOff, Var = 47 });

        Pgns[13].parameters.Add(new() { Name = "Температура ИП", BitLength = 16, StartByte = 0, UnitT = UnitType.Temp, Var = 6 });
        Pgns[13].parameters.Add(new() { Name = "Температура платы/процессора", BitLength = 8, StartByte = 2, b = -75, UnitT = UnitType.Temp, Var = 59 });
        Pgns[13].parameters.Add(new() { Name = "Температура жидкости", BitLength = 8, StartByte = 3, b = -75, UnitT = UnitType.Temp, Var = 40 });
        Pgns[13].parameters.Add(new() { Name = "Температура перегрева", BitLength = 8, StartByte = 4, b = -75, UnitT = UnitType.Temp, Var = 41 });

        Pgns[14].parameters.Add(new() { Name = "Минимальная температура пламени перед розжигом", BitLength = 16, StartByte = 0, UnitT = UnitType.Temp, Var = 36, Signed = true });
        Pgns[14].parameters.Add(new() { Name = "Граница срыва пламени", BitLength = 16, StartByte = 2, UnitT = UnitType.Temp, Var = 37, Signed = true });
        Pgns[14].parameters.Add(new() { Name = "Граница срыва пламени на прогреве", BitLength = 16, StartByte = 4, UnitT = UnitType.Temp, Signed = true });
        Pgns[14].parameters.Add(new() { Name = "Скорость изменения температуры ИП", BitLength = 16, StartByte = 6, UnitT = UnitType.Temp, Signed = true });


        Pgns[15].parameters.Add(new() { Name = "0 канал АЦП ", BitLength = 16, StartByte = 0, Var = 49 });
        Pgns[15].parameters.Add(new() { Name = "1 канал АЦП ", BitLength = 16, StartByte = 2, Var = 50 });
        Pgns[15].parameters.Add(new() { Name = "2 канал АЦП ", BitLength = 16, StartByte = 4, Var = 51 });
        Pgns[15].parameters.Add(new() { Name = "3 канал АЦП ", BitLength = 16, StartByte = 6, Var = 52 });

        Pgns[16].parameters.Add(new() { Name = "4 канал АЦП ", BitLength = 16, StartByte = 0, Var = 53 });
        Pgns[16].parameters.Add(new() { Name = "5 канал АЦП ", BitLength = 16, StartByte = 2, Var = 54 });
        Pgns[16].parameters.Add(new() { Name = "6 канал АЦП ", BitLength = 16, StartByte = 4, Var = 55 });
        Pgns[16].parameters.Add(new() { Name = "7 канал АЦП ", BitLength = 16, StartByte = 6, Var = 56 });

        Pgns[17].parameters.Add(new() { Name = "8 канал АЦП ", BitLength = 16, StartByte = 0, Var = 57 });
        Pgns[17].parameters.Add(new() { Name = "9 канал АЦП ", BitLength = 16, StartByte = 2, Var = 58 });
        Pgns[17].parameters.Add(new() { Name = "10 канал АЦП ", BitLength = 16, StartByte = 4 });
        Pgns[17].parameters.Add(new() { Name = "11 канал АЦП ", BitLength = 16, StartByte = 6 });

        Pgns[18].parameters.Add(new() { Name = "Вид изделия", BitLength = 8, StartByte = 0, GetMeaning = i => Devices[i]?.Name });
        Pgns[18].parameters.Add(new() { Name = "Напряжение питания", BitLength = 8, StartByte = 1, Meanings = { { 0, "12 Вольт" }, { 1, "24 Вольта" } } });
        Pgns[18].parameters.Add(new() { Name = "Версия ПО", BitLength = 8, StartByte = 2 });
        Pgns[18].parameters.Add(new() { Name = "Модификация ПО", BitLength = 8, StartByte = 3 });
        Pgns[18].parameters.Add(new() { Name = "Дата релиза", BitLength = 24, StartByte = 5, GetMeaning = v => $"{v >> 16}.{(v >> 8) & 0xF}.{v & 0xFF}" });
        //Pgns[18].parameters.Add(new () { Name = "День ", BitLength = 8, StartByte = 5 });    Не красиво выглядит...луше одной строкой
        //Pgns[18].parameters.Add(new () { Name = "Месяц", BitLength = 8, StartByte = 6 });
        //Pgns[18].parameters.Add(new () { Name = "Год", BitLength = 8, StartByte = 7 });

        Pgns[19].parameters.Add(new() { Name = "Подогреватель", BitLength = 2, StartBit = 0, StartByte = 1, PackNumber = 1, Meanings = DefMeaningsOnOff });
        Pgns[19].parameters.Add(new() { Name = "Помпа", BitLength = 2, StartBit = 2, StartByte = 1, PackNumber = 1, Meanings = DefMeaningsOnOff });
        Pgns[19].parameters.Add(new() { Name = "Вода", BitLength = 2, StartBit = 4, StartByte = 1, PackNumber = 1, Meanings = DefMeaningsOnOff });
        Pgns[19].parameters.Add(new() { Name = "Быстрый нагрев воды", BitLength = 2, StartBit = 6, StartByte = 1, PackNumber = 1, Meanings = DefMeaningsOnOff });
        Pgns[19].parameters.Add(new() { Name = "Помпа подогревателя статус", BitLength = 2, StartByte = 2, PackNumber = 1, Meanings = DefMeaningsOnOff });
        Pgns[19].parameters.Add(new() { Name = "Помпа 1 статус", BitLength = 2, StartByte = 2, StartBit = 2, PackNumber = 1, Meanings = DefMeaningsOnOff });
        Pgns[19].parameters.Add(new() { Name = "Помпа 2 статус", BitLength = 2, StartByte = 2, StartBit = 4, PackNumber = 1, Meanings = DefMeaningsOnOff });
        Pgns[19].parameters.Add(new() { Name = "Помпа 3 статус", BitLength = 2, StartByte = 2, StartBit = 6, PackNumber = 1, Meanings = DefMeaningsOnOff });
        Pgns[19].parameters.Add(new() { Name = "Температура бака", BitLength = 8, StartByte = 2, b = -75, UnitT = UnitType.Temp, PackNumber = 1 });
        Pgns[19].parameters.Add(new() { Name = "Атмосферное давление", BitLength = 8, StartByte = 3, UnitT = UnitType.Temp, PackNumber = 1 });
        Pgns[19].parameters.Add(new() { Name = "Сработал датчик бытовой воды", BitLength = 2, StartByte = 4, PackNumber = 1, Meanings = DefMeaningsYesNo, Var = 108 });
        Pgns[19].parameters.Add(new() { Name = "Доступен тёплый пол", BitLength = 2, StartByte = 5, StartBit = 6, PackNumber = 1, Meanings = DefMeaningsOnOff });
        Pgns[19].parameters.Add(new() { Name = "Доступен предпусковой подогрев", BitLength = 2, StartByte = 5, StartBit = 6, PackNumber = 1, Meanings = DefMeaningsOnOff });
        Pgns[19].parameters.Add(new() { Name = "Доп помпа 1 статус", BitLength = 2, StartByte = 6, StartBit = 0, PackNumber = 1, Meanings = DefMeaningsOnOff });
        Pgns[19].parameters.Add(new() { Name = "Доп помпа 2 статус", BitLength = 2, StartByte = 6, StartBit = 2, PackNumber = 1, Meanings = DefMeaningsOnOff });
        Pgns[19].parameters.Add(new() { Name = "Доп помпа 3 статус", BitLength = 2, StartByte = 7, StartBit = 4, PackNumber = 1, Meanings = DefMeaningsOnOff });

        Pgns[19].parameters.Add(new() { Name = "Уставка температуры жидкости подогревателя для перехода в ждущий.", BitLength = 8, StartByte = 1, b = -75, PackNumber = 2, UnitT = UnitType.Temp });
        Pgns[19].parameters.Add(new() { Name = "Уставка температуры жидкости подогревателя для выхода из ждущего.", BitLength = 8, StartByte = 2, b = -75, PackNumber = 2, UnitT = UnitType.Temp });
        Pgns[19].parameters.Add(new() { Name = "Уставка температуры жидкости подогревателя для выхода из ждущего при разборе воды.", BitLength = 8, StartByte = 3, b = -75, PackNumber = 2, UnitT = UnitType.Temp });
        Pgns[19].parameters.Add(new() { Name = "Уставка температуры бака для перехода в ждущий.", BitLength = 8, StartByte = 4, b = -75, PackNumber = 2, UnitT = UnitType.Temp });
        Pgns[19].parameters.Add(new() { Name = "Уставка температуры бака для выхода из ждущего.", BitLength = 8, StartByte = 5, b = -75, PackNumber = 2, UnitT = UnitType.Temp });
        Pgns[19].parameters.Add(new() { Name = "Уставка температуры бака для выхода из ждущего при разборе воды.", BitLength = 8, StartByte = 6, b = -75, PackNumber = 2, UnitT = UnitType.Temp });

        Pgns[19].parameters.Add(new() { Name = "Уставка температуры для тёплого пола", BitLength = 8, StartByte = 1, b = -75, PackNumber = 3, UnitT = UnitType.Temp });
        Pgns[19].parameters.Add(new() { Name = "Гистерезис работы тёплого пола ", BitLength = 8, StartByte = 2, PackNumber = 3, UnitT = UnitType.Temp });
        Pgns[19].parameters.Add(new() { Name = "Уставка предпускового подогрева", BitLength = 8, StartByte = 3, b = -75, PackNumber = 3, UnitT = UnitType.Temp });
        Pgns[19].parameters.Add(new() { Name = "Ограничение работы по времени предпускового подогрева, мин", BitLength = 16, StartByte = 4, PackNumber = 3, UnitT = UnitType.Minute });

        Pgns[19].parameters.Add(new() { Name = "Подключенная зона 1", BitLength = 8, StartByte = 1, PackNumber = 4, Meanings = { { 0, "Не подключена" }, { 1, "Зависимые отопители" }, { 2, "Защита от замерзания" }, { 3, "Пассивное отопление" } } });
        Pgns[19].parameters.Add(new() { Name = "Подключенная зона 2", BitLength = 8, StartByte = 2, PackNumber = 4, Meanings = { { 0, "Не подключена" }, { 1, "Зависимые отопители" }, { 2, "Защита от замерзания" }, { 3, "Пассивное отопление" } } });
        Pgns[19].parameters.Add(new() { Name = "Подключенная зона 3", BitLength = 8, StartByte = 3, PackNumber = 4, Meanings = { { 0, "Не подключена" }, { 1, "Зависимые отопители" }, { 2, "Защита от замерзания" }, { 3, "Пассивное отопление" } } });
        Pgns[19].parameters.Add(new() { Name = "Подключенная зона 4", BitLength = 8, StartByte = 4, PackNumber = 4, Meanings = { { 0, "Не подключена" }, { 1, "Зависимые отопители" }, { 2, "Защита от замерзания" }, { 3, "Пассивное отопление" } } });
        Pgns[19].parameters.Add(new() { Name = "Подключенная зона 5", BitLength = 8, StartByte = 5, PackNumber = 4, Meanings = { { 0, "Не подключена" }, { 1, "Зависимые отопители" }, { 2, "Защита от замерзания" }, { 3, "Пассивное отопление" } } });


        Pgns[20].parameters.Add(new() { Name = "Код неисправности", BitLength = 8, StartByte = 0 });
        Pgns[20].parameters.Add(new() { Name = "Количество морганий", BitLength = 8, StartByte = 1 });
        Pgns[20].parameters.Add(new() { Name = "Байт неисправностей 1", BitLength = 8, StartByte = 2 });
        Pgns[20].parameters.Add(new() { Name = "Байт неисправностей 2", BitLength = 8, StartByte = 3 });
        Pgns[20].parameters.Add(new() { Name = "Байт неисправностей 3", BitLength = 8, StartByte = 4 });
        Pgns[20].parameters.Add(new() { Name = "Байт неисправностей 4", BitLength = 8, StartByte = 5 });
        Pgns[20].parameters.Add(new() { Name = "Байт неисправностей 5", BitLength = 8, StartByte = 6 });
        Pgns[20].parameters.Add(new() { Name = "Байт неисправностей 6", BitLength = 8, StartByte = 7 });

        Pgns[21].parameters.Add(new() { Name = "Опорное напряжение процессора", BitLength = 8, StartByte = 0, UnitT = UnitType.Volt, a = 0.1 });
        Pgns[21].parameters.Add(new() { Name = "Температура процессора", BitLength = 8, StartByte = 1, UnitT = UnitType.Temp, b = -75 });
        Pgns[21].parameters.Add(new() { Name = "Температура бака", BitLength = 8, StartByte = 2, UnitT = UnitType.Temp, b = -75, Var = 106 });
        Pgns[21].parameters.Add(new() { Name = "Температура теплообменника", BitLength = 8, StartByte = 3, UnitT = UnitType.Temp, b = -75 });
        Pgns[21].parameters.Add(new() { Name = "Температура наружного воздуха", BitLength = 8, StartByte = 4, UnitT = UnitType.Temp, b = -75, Var = 107 });
        Pgns[21].parameters.Add(new() { Name = "Иконка подогревателя", BitLength = 8, StartByte = 5, Meanings = { { 0, "Ожидание" }, { 1, "Продувка" }, { 2, "Розжиг" }, { 3, "Работа на мощности" } } });
        Pgns[21].parameters.Add(new() { Name = "Уровень жидкости в баке", BitLength = 8, StartByte = 6, Var = 105 });
        Pgns[21].parameters.Add(new() { Name = "Режим хранения", BitLength = 2, StartByte = 7, Meanings = DefMeaningsYesNo });
        Pgns[21].parameters.Add(new() { Name = "ТЭН активен", StartBit = 2, BitLength = 2, StartByte = 7, Meanings = DefMeaningsYesNo });



        Pgns[22].parameters.Add(new() { Name = "Зона 1", BitLength = 2, StartByte = 0, StartBit = 0, Meanings = DefMeaningsOnOff, Var = 65 });
        Pgns[22].parameters.Add(new() { Name = "Зона 2", BitLength = 2, StartByte = 0, StartBit = 2, Meanings = DefMeaningsOnOff, Var = 66 });
        Pgns[22].parameters.Add(new() { Name = "Зона 3", BitLength = 2, StartByte = 0, StartBit = 4, Meanings = DefMeaningsOnOff, Var = 67 });
        Pgns[22].parameters.Add(new() { Name = "Зона 4", BitLength = 2, StartByte = 0, StartBit = 6, Meanings = DefMeaningsOnOff, Var = 68 });
        Pgns[22].parameters.Add(new() { Name = "Зона 5", BitLength = 2, StartByte = 1, StartBit = 0, Meanings = DefMeaningsOnOff, Var = 69 });
        Pgns[22].parameters.Add(new() { Name = "Температура зоны 1", BitLength = 8, StartByte = 2, UnitT = UnitType.Temp, b = -75, Var = 70 });
        Pgns[22].parameters.Add(new() { Name = "Температура зоны 2", BitLength = 8, StartByte = 3, UnitT = UnitType.Temp, b = -75, Var = 71 });
        Pgns[22].parameters.Add(new() { Name = "Температура зоны 3", BitLength = 8, StartByte = 4, UnitT = UnitType.Temp, b = -75, Var = 72 });
        Pgns[22].parameters.Add(new() { Name = "Температура зоны 4", BitLength = 8, StartByte = 5, UnitT = UnitType.Temp, b = -75, Var = 73 });
        Pgns[22].parameters.Add(new() { Name = "Температура зоны 5", BitLength = 8, StartByte = 6, UnitT = UnitType.Temp, b = -75, Var = 74 });
        Pgns[22].parameters.Add(new() { Name = "Кнопка Подогреватель", BitLength = 2, StartByte = 7, StartBit = 0, Meanings = DefMeaningsOnOff, Var = 119 });
        Pgns[22].parameters.Add(new() { Name = "Кнопка ТЭН", BitLength = 2, StartByte = 7, StartBit = 2, Meanings = DefMeaningsOnOff, Var = 120 });
        Pgns[22].parameters.Add(new() { Name = "Кнопка Тёплый пол", BitLength = 2, StartByte = 7, StartBit = 4, Meanings = DefMeaningsOnOff });
        Pgns[22].parameters.Add(new() { Name = "Кнопка Предпусковой подогрев", BitLength = 2, StartByte = 7, StartBit = 5, Meanings = DefMeaningsOnOff });

        Pgns[23].parameters.Add(new() { Name = "Зад. ШИМ вентилятора зоны 1", BitLength = 8, StartByte = 0, UnitT = UnitType.Percent, Var = 100 });
        Pgns[23].parameters.Add(new() { Name = "Зад. ШИМ вентилятора зоны 2", BitLength = 8, StartByte = 1, UnitT = UnitType.Percent, Var = 101 });
        Pgns[23].parameters.Add(new() { Name = "Зад. ШИМ вентилятора зоны 3", BitLength = 8, StartByte = 2, UnitT = UnitType.Percent, Var = 102 });
        Pgns[23].parameters.Add(new() { Name = "Зад. ШИМ вентилятора зоны 4", BitLength = 8, StartByte = 3, UnitT = UnitType.Percent, Var = 103 });
        Pgns[23].parameters.Add(new() { Name = "Зад. ШИМ вентилятора зоны 5", BitLength = 8, StartByte = 4, UnitT = UnitType.Percent, Var = 104 });

        Pgns[24].parameters.Add(new() { Name = "Ступень скорости вентилятора зоны 1", BitLength = 4, StartByte = 0, });
        Pgns[24].parameters.Add(new() { Name = "Ступень скорости вентилятора зоны 2", BitLength = 4, StartByte = 0, StartBit = 4 });
        Pgns[24].parameters.Add(new() { Name = "Ступень скорости вентилятора зоны 3", BitLength = 4, StartByte = 1, });
        Pgns[24].parameters.Add(new() { Name = "Ступень скорости вентилятора зоны 4", BitLength = 4, StartByte = 1, StartBit = 4 });
        Pgns[24].parameters.Add(new() { Name = "Ступень скорости вентилятора зоны 5", BitLength = 4, StartByte = 2, });
        Pgns[24].parameters.Add(new() { Name = "Тек. ШИМ вентилятора зоны 1", BitLength = 8, StartByte = 3, UnitT = UnitType.Percent, Var = 95 });
        Pgns[24].parameters.Add(new() { Name = "Тек. ШИМ вентилятора зоны 2", BitLength = 8, StartByte = 4, UnitT = UnitType.Percent, Var = 96 });
        Pgns[24].parameters.Add(new() { Name = "Тек. ШИМ вентилятора зоны 3", BitLength = 8, StartByte = 5, UnitT = UnitType.Percent, Var = 97 });
        Pgns[24].parameters.Add(new() { Name = "Тек. ШИМ вентилятора зоны 4", BitLength = 8, StartByte = 6, UnitT = UnitType.Percent, Var = 98 });
        Pgns[24].parameters.Add(new() { Name = "Тек. ШИМ вентилятора зоны 5", BitLength = 8, StartByte = 7, UnitT = UnitType.Percent, Var = 99 });

        Pgns[25].parameters.Add(new() { Name = "Дневная уставка зоны 1", BitLength = 8, StartByte = 0, UnitT = UnitType.Temp, b = -75, Var = 75 });
        Pgns[25].parameters.Add(new() { Name = "Дневная уставка зоны 2", BitLength = 8, StartByte = 1, UnitT = UnitType.Temp, b = -75, Var = 76 });
        Pgns[25].parameters.Add(new() { Name = "Дневная уставка зоны 3", BitLength = 8, StartByte = 2, UnitT = UnitType.Temp, b = -75, Var = 77 });
        Pgns[25].parameters.Add(new() { Name = "Дневная уставка зоны 4", BitLength = 8, StartByte = 3, UnitT = UnitType.Temp, b = -75, Var = 78 });
        Pgns[25].parameters.Add(new() { Name = "Дневная уставка зоны 5", BitLength = 8, StartByte = 4, UnitT = UnitType.Temp, b = -75, Var = 79 });

        Pgns[26].parameters.Add(new() { Name = "Ночная уставка зоны 1", BitLength = 8, StartByte = 0, UnitT = UnitType.Temp, b = -75, Var = 80 });
        Pgns[26].parameters.Add(new() { Name = "Ночная уставка зоны 2", BitLength = 8, StartByte = 1, UnitT = UnitType.Temp, b = -75, Var = 81 });
        Pgns[26].parameters.Add(new() { Name = "Ночная уставка зоны 3", BitLength = 8, StartByte = 2, UnitT = UnitType.Temp, b = -75, Var = 82 });
        Pgns[26].parameters.Add(new() { Name = "Ночная уставка зоны 4", BitLength = 8, StartByte = 3, UnitT = UnitType.Temp, b = -75, Var = 83 });
        Pgns[26].parameters.Add(new() { Name = "Ночная уставка зоны 5", BitLength = 8, StartByte = 4, UnitT = UnitType.Temp, b = -75, Var = 84 });


        Pgns[27].parameters.Add(new() { Name = "Ручная уставка ШИМ зоны 1", BitLength = 8, StartByte = 0, UnitT = UnitType.Percent, Var = 90 });
        Pgns[27].parameters.Add(new() { Name = "Ручная уставка ШИМ зоны 2", BitLength = 8, StartByte = 1, UnitT = UnitType.Percent, Var = 91 });
        Pgns[27].parameters.Add(new() { Name = "Ручная уставка ШИМ зоны 3", BitLength = 8, StartByte = 2, UnitT = UnitType.Percent, Var = 92 });
        Pgns[27].parameters.Add(new() { Name = "Ручная уставка ШИМ зоны 4", BitLength = 8, StartByte = 3, UnitT = UnitType.Percent, Var = 93 });
        Pgns[27].parameters.Add(new() { Name = "Ручная уставка ШИМ зоны 5", BitLength = 8, StartByte = 4, UnitT = UnitType.Percent, Var = 94 });

        Pgns[27].parameters.Add(new() { Name = "Зона 1 Ручной режим", BitLength = 2, StartByte = 5, StartBit = 0, Meanings = DefMeaningsOnOff, Var = 85 });
        Pgns[27].parameters.Add(new() { Name = "Зона 2 Ручной режим", BitLength = 2, StartByte = 5, StartBit = 2, Meanings = DefMeaningsOnOff, Var = 86 });
        Pgns[27].parameters.Add(new() { Name = "Зона 3 Ручной режим", BitLength = 2, StartByte = 5, StartBit = 4, Meanings = DefMeaningsOnOff, Var = 87 });
        Pgns[27].parameters.Add(new() { Name = "Зона 4 Ручной режим", BitLength = 2, StartByte = 5, StartBit = 6, Meanings = DefMeaningsOnOff, Var = 88 });
        Pgns[27].parameters.Add(new() { Name = "Зона 5 Ручной режим", BitLength = 2, StartByte = 6, StartBit = 0, Meanings = DefMeaningsOnOff, Var = 89 });

        Pgns[28].parameters.Add(new() { Name = "Общее время на всех режимах", BitLength = 32, StartByte = 0, UnitT = UnitType.Second });
        Pgns[28].parameters.Add(new() { Name = "Общее время работы (кроме ожидания команды)", BitLength = 32, StartByte = 4, UnitT = UnitType.Second });

        Pgns[29].parameters.Add(new() { Name = "Атмосферное давление", BitLength = 8, StartByte = 1, UnitT = UnitType.Pressure, PackNumber = 1 });
        Pgns[29].parameters.Add(new() { Name = "Среднее максимальное значение давления", BitLength = 24, StartByte = 2, UnitT = UnitType.Pressure, a = 0.001, PackNumber = 1 });
        Pgns[29].parameters.Add(new() { Name = "Среднее минимальное значение давления", BitLength = 24, StartByte = 4, UnitT = UnitType.Pressure, a = 0.001, PackNumber = 1 });

        Pgns[29].parameters.Add(new() { Name = "Разница между max и min  значениями", BitLength = 16, StartByte = 1, a = 0.01, UnitT = UnitType.Pressure, PackNumber = 2 });
        Pgns[29].parameters.Add(new() { Name = "Флаг появления пламени по пульсации давления", BitLength = 2, StartByte = 3, Meanings = DefMeaningsYesNo, PackNumber = 2 });
        Pgns[29].parameters.Add(new() { Name = "Атмосферное давление", BitLength = 24, StartByte = 4, UnitT = UnitType.Pressure, a = 0.001, PackNumber = 2, Var = 60 });

        Pgns[31].parameters.Add(new() { Name = "Время работы", BitLength = 32, StartByte = 0, UnitT = UnitType.Second, Var = 3 });
        Pgns[31].parameters.Add(new() { Name = "Время работы на режиме", BitLength = 32, StartByte = 4, UnitT = UnitType.Second, Var = 4 });

        Pgns[40].parameters.Add(new() { Name = "t_year", BitLength = 8, StartByte = 0, UnitT = UnitType.Year, Var = 118 });
        Pgns[40].parameters.Add(new() { Name = "t_month", BitLength = 8, StartByte = 1, UnitT = UnitType.Month, Var = 117 });
        Pgns[40].parameters.Add(new() { Name = "t_day", BitLength = 8, StartByte = 2, UnitT = UnitType.Day, Var = 116 });
        Pgns[40].parameters.Add(new() { Name = "t_hour", BitLength = 8, StartByte = 3, UnitT = UnitType.Hour, Var = 115 });
        Pgns[40].parameters.Add(new() { Name = "t_minute", BitLength = 8, StartByte = 4, UnitT = UnitType.Minute, Var = 114 });
        Pgns[40].parameters.Add(new() { Name = "t_second", BitLength = 8, StartByte = 5, UnitT = UnitType.Second, Var = 113 });

        Pgns[41].parameters.Add(new() { Name = "t_day_start_hour", BitLength = 8, StartByte = 0, UnitT = UnitType.Hour });
        Pgns[41].parameters.Add(new() { Name = "t_day_start_minute", BitLength = 8, StartByte = 1, UnitT = UnitType.Minute });
        Pgns[41].parameters.Add(new() { Name = "t_night_start_hour", BitLength = 8, StartByte = 2, UnitT = UnitType.Hour });
        Pgns[41].parameters.Add(new() { Name = "t_hight_start_minute", BitLength = 8, StartByte = 3, UnitT = UnitType.Minute });
        Pgns[41].parameters.Add(new() { Name = "t_daytime_backlight", BitLength = 8, StartByte = 4, UnitT = UnitType.Percent });
        Pgns[41].parameters.Add(new() { Name = "t_nighttime_backlight", BitLength = 8, StartByte = 5, UnitT = UnitType.Percent });
        Pgns[41].parameters.Add(new() { Name = "t_display_sleep_time", BitLength = 16, StartByte = 6, UnitT = UnitType.Second });

        Pgns[42].parameters.Add(new() { Name = "t_pump_heater_btn", BitLength = 8, StartByte = 0, Meanings = DefMeaningsOnOff });
        Pgns[42].parameters.Add(new() { Name = "t_pump_1_btn", BitLength = 8, StartByte = 1, Meanings = DefMeaningsOnOff });
        Pgns[42].parameters.Add(new() { Name = "t_pump_2_btn", BitLength = 8, StartByte = 2, Meanings = DefMeaningsOnOff });
        Pgns[42].parameters.Add(new() { Name = "t_pump_3_btn", BitLength = 8, StartByte = 3, Meanings = DefMeaningsOnOff });
        Pgns[42].parameters.Add(new() { Name = "t_pump_aux_1_btn", BitLength = 8, StartByte = 4, Meanings = DefMeaningsOnOff });
        Pgns[42].parameters.Add(new() { Name = "t_pump_aux_2_btn", BitLength = 8, StartByte = 5, Meanings = DefMeaningsOnOff });
        Pgns[42].parameters.Add(new() { Name = "t_pump_aux_3_btn", BitLength = 8, StartByte = 6, Meanings = DefMeaningsOnOff });

        Pgns[43].parameters.Add(new() { Name = "t_channel1_pwm_value", BitLength = 16, StartByte = 0 });
        Pgns[43].parameters.Add(new() { Name = "t_channel2_pwm_value", BitLength = 16, StartByte = 2 });
        Pgns[43].parameters.Add(new() { Name = "t_channel3_pwm_value", BitLength = 16, StartByte = 4 });
        Pgns[43].parameters.Add(new() { Name = "t_channel4_pwm_value", BitLength = 16, StartByte = 6 });

        Pgns[44].parameters.Add(new() { Name = "t_channel1_pwm_value", BitLength = 16, StartByte = 0 });
        Pgns[44].parameters.Add(new() { Name = "t_channel2_pwm_value", BitLength = 16, StartByte = 2 });
        Pgns[44].parameters.Add(new() { Name = "t_channel3_pwm_value", BitLength = 16, StartByte = 4 });
        Pgns[44].parameters.Add(new() { Name = "t_channel4_pwm_value", BitLength = 16, StartByte = 6 });

        Pgns[45].parameters.Add(new() { Name = "t_channel1_temperature", BitLength = 16, StartByte = 0, a = 0.1, UnitT = UnitType.Temp, Signed = true });
        Pgns[45].parameters.Add(new() { Name = "t_channel2_temperature", BitLength = 16, StartByte = 2, a = 0.1, UnitT = UnitType.Temp, Signed = true });
        Pgns[45].parameters.Add(new() { Name = "t_channel3_temperature", BitLength = 16, StartByte = 4, a = 0.1, UnitT = UnitType.Temp, Signed = true });
        Pgns[45].parameters.Add(new() { Name = "t_channel4_temperature", BitLength = 16, StartByte = 6, a = 0.1, UnitT = UnitType.Temp, Signed = true });

        Pgns[46].parameters.Add(new() { Name = "t_error_code1", BitLength = 8, StartByte = 0, GetMeaning = (x) => GetString($"e_{x}") });
        Pgns[46].parameters.Add(new() { Name = "t_error_code2", BitLength = 8, StartByte = 1, GetMeaning = (x) => GetString($"e_{x}") });
        Pgns[46].parameters.Add(new() { Name = "t_error_code3", BitLength = 8, StartByte = 2, GetMeaning = (x) => GetString($"e_{x}") });
        Pgns[46].parameters.Add(new() { Name = "t_error_code4", BitLength = 8, StartByte = 3, GetMeaning = (x) => GetString($"e_{x}") });
        Pgns[46].parameters.Add(new() { Name = "t_error_code5", BitLength = 8, StartByte = 4, GetMeaning = (x) => GetString($"e_{x}") });
        Pgns[46].parameters.Add(new() { Name = "t_error_code6", BitLength = 8, StartByte = 5, GetMeaning = (x) => GetString($"e_{x}") });
        Pgns[46].parameters.Add(new() { Name = "t_error_code7", BitLength = 8, StartByte = 6, GetMeaning = (x) => GetString($"e_{x}") });
        Pgns[46].parameters.Add(new() { Name = "t_error_code8", BitLength = 8, StartByte = 7, GetMeaning = (x) => GetString($"e_{x}") });


        Pgns[100].parameters.Add(new() { Name = "Начальный адрес", BitLength = 24, StartByte = 1, PackNumber = 2, GetMeaning = r => $"{GetString("t_starting_address")}: 0X{(r + 0x8000000):X}" });
        Pgns[100].parameters.Add(new() { Name = "Длина данных", BitLength = 32, StartByte = 4, PackNumber = 2 });
        Pgns[100].parameters.Add(new() { Name = "Длина данных", BitLength = 24, StartByte = 1, PackNumber = 4 });
        Pgns[100].parameters.Add(new() { Name = "CRC", BitLength = 32, StartByte = 4, PackNumber = 4, GetMeaning = r => $"CRC: 0X{(r):X}" });
        Pgns[100].parameters.Add(new() { Name = "Адрес фрагмента", BitLength = 32, StartByte = 2, PackNumber = 5, GetMeaning = r => $"{GetString("t_fragment_address")}: 0X{r:X}" });

        Pgns[101].parameters.Add(new() { Name = "Первое слово", BitLength = 32, StartByte = 0, GetMeaning = r => $"1st: 0X{(r):X}" });
        Pgns[101].parameters.Add(new() { Name = "Второе слово", BitLength = 32, StartByte = 4, GetMeaning = r => $"2nd: 0X{(r):X}" });


        #endregion
    }

    public Omni(CanAdapter canAdapter, UartAdapter uartAdapter)
    {
        ArgumentNullException.ThrowIfNull(canAdapter);
        ArgumentNullException.ThrowIfNull(uartAdapter);
        this.canAdapter = canAdapter;
        this.uartAdapter = uartAdapter;
        SeedStaticData();

        try
        {
            var str = File.ReadAllText("presets.json");
            JArray serialised = (JArray)JsonConvert.DeserializeObject(str);
            AllPresets = serialised.ToObject<BindingList<ConfigPreset>>();
        }
        catch
        {
            Debug.WriteLine("Can't load preset list, empty list initiated");
            //MessageBox.Show("Can't load preset list, empty list initiated");
        }

        allPresets.ListChanged += PresetCollectionChanged;
    }

    private void PresetCollectionChanged(object sender, ListChangedEventArgs e)
    {
        OnPropertyChanged(nameof(AvailableModels));
        OnPropertyChanged(nameof(AvailableVendors));
    }

    public event EventHandler NewDeviceAcquired;

    public WpfPlot plot;

    private static readonly Dictionary<int, string> DefMeaningsYesNo = new() { { 0, "t_no" }, { 1, "t_yes" }, { 2, "t_no_data" }, { 3, "t_no_data" } };
    private static readonly Dictionary<int, string> DefMeaningsOnOff = new() { { 0, "t_off" }, { 1, "t_on" }, { 2, "t_no_data" }, { 3, "t_no_data" } };
    private static readonly Dictionary<int, string> DefMeaningsAllow = new() { { 0, "t_disabled" }, { 1, "t_enabled" }, { 2, "t_no_data" }, { 3, "t_no_data" } };
    private static readonly Dictionary<int, string> Stages = new() { { 0, "STAGE_Z" }, { 1, "STAGE_P" }, { 2, "STAGE_H" }, { 3, "STAGE_W" }, { 4, "STAGE_F" }, { 5, "STAGE_T" }, { 6, "STAGE_M" } };

    public bool UseImperial { set; get; }

    public static Dictionary<int, PgnClass> Pgns { get; } = new();

    public static Dictionary<int, OmniCommand> Commands { get; } = new();

    public BindingList<DeviceViewModel> ConnectedDevices { get; } = new();
    [NotifyPropertyChangedFor(nameof(AvailableModels), nameof(AvailableVendors))]
    [ObservableProperty] private DeviceViewModel selectedConnectedDevice;

    public UpdatableList<OmniMessage> Messages { get; } = new();

    private readonly CanAdapter canAdapter;
    private readonly UartAdapter uartAdapter;

    [ObservableProperty] private bool readingBbErrorsMode = false;
    [NotifyPropertyChangedFor(nameof(AvailableModels), nameof(AvailableVendors))]
    [ObservableProperty] public static BindingList<ConfigPreset> allPresets = new();
    [NotifyPropertyChangedFor(nameof(AvailableModels))]
    [ObservableProperty] private string selectedVendor;
    [ObservableProperty] private string selectedModel;

    [RelayCommand]
    void LoadPreset()
    {
        SelectedConnectedDevice.ReadParameters.Clear();
        var Preset = AllPresets.FirstOrDefault(p => p.VendorName == SelectedVendor && p.ModelName == SelectedModel && SelectedConnectedDevice.Id.Type == p.DeviceType);
        if (Preset == null)
        {
            MessageBox.Show($"Can't find preset for {SelectedVendor}-{SelectedModel}");
            return;
        }
        foreach (var parameter in Preset.ParamList)
        {
            SelectedConnectedDevice.ReadParameters.TryToAdd(new ReadedParameter() { Id = parameter.Item1, Value = parameter.Item2 });
        }
    }

    [RelayCommand]
    void SavePreset()
    {
        if (SelectedModel.Length < 3 || SelectedModel.Length < 3)
        {
            MessageBox.Show("Model and vendor names must contain at least 3 characters");
            return;
        }
        var Preset = AllPresets.FirstOrDefault(p => p.VendorName == SelectedVendor && p.ModelName == SelectedModel && p.DeviceType == SelectedConnectedDevice.Id.Type);
        if (Preset != null)
        {
            MessageBox.Show($"Preset for \"{SelectedVendor} - {SelectedModel}\" already exists, delete it first");
            return;
        }
        ConfigPreset newPreset = new ConfigPreset() { ModelName = SelectedModel, VendorName = SelectedVendor, DeviceType = SelectedConnectedDevice.Id.Type };
        foreach (var parameter in SelectedConnectedDevice.ReadParameters)
        {
            if (parameter.Id < 12 || parameter.Id > 14) //Excluding serial number
                newPreset.ParamList.Add(new Tuple<int, uint>(parameter.Id, parameter.Value));
        }
        AllPresets.Add(newPreset);
        if (File.Exists("presets.json"))
            File.Delete("presets.json");
        string serialized = JsonConvert.SerializeObject(AllPresets);

        StreamWriter sw = new("presets.json", false);
        sw.Write(serialized);
        sw.Flush();
        sw.Dispose();

    }

    [RelayCommand]
    void DeletePreset()
    {
        if (AllPresets.Remove(AllPresets.FirstOrDefault(p => p.ModelName == SelectedModel && p.VendorName == SelectedVendor && p.DeviceType == SelectedConnectedDevice.Id.Type)))
        {
            try
            {
                if (File.Exists("presets.json"))
                    File.Delete("presets.json");
                string serialized = JsonConvert.SerializeObject(AllPresets);

                StreamWriter sw = new("presets.json", false);
                sw.Write(serialized);
                sw.Flush();
                sw.Dispose();
                MessageBox.Show($"Preset for {SelectedVendor}-{SelectedModel} successfully removed");
            }
            catch (Exception ex)
            {
                MessageBox.Show(ex.Message);
            }
        }
        else
        {
            MessageBox.Show($"Preset for {SelectedVendor}-{SelectedModel} not found");
        }

    }

    public BindingList<string> AvailableModels
    {
        get
        {
            var initialCollection = AllPresets;
            var distincted = initialCollection.Where(p => p.VendorName == SelectedVendor && p.DeviceType == SelectedConnectedDevice.Id.Type).ToList();
            var ret = new BindingList<string>(distincted.Select(p => p.ModelName).ToList());
            return ret;
        }
    }

    public BindingList<string> AvailableVendors
    {
        get
        {
            var initialCollection = AllPresets;
            var distincted = initialCollection.DistinctBy(p => p.VendorName).Where(p => p.DeviceType == SelectedConnectedDevice?.Id.Type).ToList();
            var ret = new BindingList<string>(distincted.Select(p => p.VendorName).ToList());
            return ret;
        }
    }


    private readonly SynchronizationContext uiContext = SynchronizationContext.Current;

    [ObservableProperty] private OmniTask currentTask = new();

    private bool CancellationRequested => CurrentTask.Cts.IsCancellationRequested;

    private bool Capture(string n) => CurrentTask.Capture(n);

    private void Done() => CurrentTask.OnDone();

    private void Cancel() => CurrentTask.OnCancel();

    private void Fail(string reason = "") => CurrentTask.OnFail(reason);

    private void UpdatePercent(int p) => CurrentTask.UpdatePercent(p);

    public void ProcessCanMessage(CanMessage m)
    {
        ProcessOmniMessage(new OmniMessage(m));
    }

    public void ProcessOmniMessage(OmniMessage m)
    {
        var id = m.TransmitterId;

        var senderDevice = ConnectedDevices.FirstOrDefault(d => d.Id.Equals(m.TransmitterId));

        if (senderDevice == null)
        {
            senderDevice = new DeviceViewModel(id);
            ConnectedDevices.Add(senderDevice);
            NewDeviceAcquired?.Invoke(this, null);
            if (senderDevice.Id.Type != 123)        //Requesting basic data, but not for bootloaders
                Task.Run(() => RequestBasicData(id));
        }

        if (!Pgns.ContainsKey(m.Pgn))
        {
            return; //There is no such PGN in dictionary
        }

        foreach (var p in Pgns[m.Pgn].parameters)
        {

            if (Pgns[m.Pgn].multiPack && p.PackNumber != m.Data[0]) continue;
            if (p.Var == 0) continue;
            var sv = new StatusVariable(p.Var);
            sv.AssignedParameter = p;
            var rawValue = OmniMessage.GetRawValue(m.Data, p.BitLength, p.StartBit, p.StartByte, p.Signed);
            if (Math.Abs(rawValue - (Math.Pow(2, p.BitLength) - 1)) < 0.3) continue; //Unsupported parameter
            sv.RawValue = rawValue;
            senderDevice.SupportedVariables[sv.Id] = true;
            senderDevice.Status.TryToAdd(sv);

            switch (sv.Id)
            {
                case 1:
                    senderDevice.Parameters.Stage = (int)(rawValue * sv.AssignedParameter.a + sv.AssignedParameter.b);
                    break;
                case 2:
                    senderDevice.Parameters.Mode = (int)(rawValue * sv.AssignedParameter.a + sv.AssignedParameter.b);
                    break;
                case 3:
                    senderDevice.Parameters.WorkTime = (int)(rawValue * sv.AssignedParameter.a + sv.AssignedParameter.b);
                    break;
                case 4:
                    senderDevice.Parameters.StageTime = (int)(rawValue * sv.AssignedParameter.a + sv.AssignedParameter.b);
                    break;
                case 5:
                    senderDevice.Parameters.Voltage = rawValue * sv.AssignedParameter.a + sv.AssignedParameter.b;
                    break;
                case 6:
                    senderDevice.Parameters.FlameSensor = (int)ImperialConverter(rawValue * sv.AssignedParameter.a + sv.AssignedParameter.b, sv.AssignedParameter.UnitT);
                    break;
                case 7:
                    senderDevice.Parameters.BodyTemp = (int)ImperialConverter(rawValue * sv.AssignedParameter.a + sv.AssignedParameter.b, sv.AssignedParameter.UnitT);
                    break;
                case 8:
                    senderDevice.Parameters.PanelTemp = (int)ImperialConverter(rawValue * sv.AssignedParameter.a + sv.AssignedParameter.b, sv.AssignedParameter.UnitT);
                    break;
                case 10:
                    senderDevice.Parameters.InletTemp = (int)ImperialConverter(rawValue * sv.AssignedParameter.a + sv.AssignedParameter.b, sv.AssignedParameter.UnitT);
                    break;
                case 15:
                    senderDevice.Parameters.RevSet = (int)(rawValue * sv.AssignedParameter.a + sv.AssignedParameter.b);
                    break;
                case 16:
                    senderDevice.Parameters.RevMeasured = (int)(rawValue * sv.AssignedParameter.a + sv.AssignedParameter.b);
                    break;
                case 18:
                    senderDevice.Parameters.FuelPumpMeasured = (rawValue * sv.AssignedParameter.a + sv.AssignedParameter.b);
                    break;
                case 21:
                    senderDevice.Parameters.GlowPlug = (int)(rawValue * sv.AssignedParameter.a + sv.AssignedParameter.b);
                    break;
                case 24:
                    senderDevice.Parameters.Error = (int)(rawValue * sv.AssignedParameter.a + sv.AssignedParameter.b);
                    break;
                case 40:
                    senderDevice.Parameters.LiquidTemp = (int)ImperialConverter(rawValue * sv.AssignedParameter.a + sv.AssignedParameter.b, sv.AssignedParameter.UnitT);
                    break;
                case 41:
                    senderDevice.Parameters.OverheatTemp = (int)ImperialConverter(rawValue * sv.AssignedParameter.a + sv.AssignedParameter.b, sv.AssignedParameter.UnitT);
                    break;
                case 60:
                    senderDevice.Parameters.Pressure = (float)ImperialConverter(rawValue * sv.AssignedParameter.a + sv.AssignedParameter.b, sv.AssignedParameter.UnitT);
                    if (senderDevice.PressureLogWriting)
                        senderDevice.PressureLog[senderDevice.PressureLogPointer++] = senderDevice.Parameters.Pressure;
                    break;
            }
        }

        switch (m.Pgn)
        {
            case 2://Command ack
                switch (m.Data[1])
                {
                    case 0:
                        senderDevice.Firmware = new[] { m.Data[2], m.Data[3], m.Data[4], m.Data[5] };
                        break;
                    case 67:
                        senderDevice.ManualMode = m.Data[2] == 1;
                        break;
                }

                break;
            case 7: //Answer for param request
                {
                    if (m.Data[0] == 4) // Processing only successful answers
                    {
                        var parameterId = m.Data[3] + m.Data[2] * 256;
                        var parameterValue = ((uint)m.Data[4] * 0x1000000) + ((uint)m.Data[5] * 0x10000) + ((uint)m.Data[6] * 0x100) + m.Data[7];
                        if (parameterValue != 0xFFFFFFFF)
                        {
                            senderDevice.ReadParameters.TryToAdd(new ReadedParameter { Id = parameterId, Value = parameterValue });
                            Debug.WriteLine($"{GetString($"par_{parameterId}")}={parameterValue}");
                        }
                        else
                        if (GotResource($"par_{parameterId}"))
                            Debug.WriteLine($"{GetString($"par_{parameterId}")} not supported");

                        switch (parameterId)//Serial num in separate var
                        {
                            case 12: senderDevice.Serial1 = parameterValue; break;
                            case 13: senderDevice.Serial2 = parameterValue; break;
                            case 14: senderDevice.Serial3 = parameterValue; break;
                        }

                        senderDevice.flagGetParamDone = true;
                    }

                    if (m.Data[0] == 5) // Parameter not supported
                        senderDevice.flagGetParamDone = true;
                    
                    break;
                }
            case 8: //Black box 
                {
                    if (m.Data[0] == 4) // Processing only successful answers
                    {
                        if (!ReadingBbErrorsMode)
                        {
                            var parameterId = m.Data[3] + m.Data[2] * 256;
                            var parameterValue = ((uint)m.Data[4] * 0x1000000) + ((uint)m.Data[5] * 0x10000) + ((uint)m.Data[6] * 0x100) + m.Data[7];
                            if (parameterValue != 0xFFFFFFFF)
                                senderDevice.BbValues.TryToAdd(new ReadedBlackBoxValue() { Id = parameterId, Value = parameterValue });

                        }
                        else
                        {
                            if (m.Data[2] == 0xFF && m.Data[3] == 0xFA) //Report header
                                senderDevice.BbErrors.AddNew();
                            else
                            {
                                BbCommonVariable v = new()
                                {
                                    Id = m.Data[2] * 256 + m.Data[3],
                                    Value = m.Data[4] * 0x1000000 + m.Data[5] * 0x10000 + m.Data[6] * 0x100 + m.Data[7]
                                };
                                if (v.Id != 65535)
                                    senderDevice.BbErrors.Last().Variables.TryToAdd(v);
                            }
                        }
                        senderDevice.flagGetBbDone = true;
                    }

                    break;
                }
            case 18: //Version
                {
                    senderDevice.flagGetVersionDone = true;
                    if (m.Data[0] != 123)
                        senderDevice.Firmware = m.Data[0..4];
                    else
                        senderDevice.BootloaderFirmware = m.Data[0..4];
                    if (m.Data[5] != 0xff && m.Data[6] != 0xff && m.Data[7] != 0xff)
                        try
                        {
                            senderDevice.ProductionDate = new DateOnly(m.Data[7] + 2000, m.Data[6], m.Data[5]);
                        }
                        catch {/*ignored*/}

                    break;
                }

            case 19:
                {
                    if (m.Data[0] == 4)
                    {
                        if (m.Data[1] < 4) senderDevice.TimberlineParams.Zones[0].Connected = (zoneType_t)m.Data[1];
                        if (m.Data[2] < 4) senderDevice.TimberlineParams.Zones[1].Connected = (zoneType_t)m.Data[2];
                        if (m.Data[3] < 4) senderDevice.TimberlineParams.Zones[2].Connected = (zoneType_t)m.Data[3];
                        if (m.Data[4] < 4) senderDevice.TimberlineParams.Zones[3].Connected = (zoneType_t)m.Data[4];
                        if (m.Data[5] < 4) senderDevice.TimberlineParams.Zones[4].Connected = (zoneType_t)m.Data[5];
                    }

                    break;
                }
            case 21:
                {
                    if (m.Data[2] != 255) senderDevice.TimberlineParams.TankTemperature = m.Data[2] - 75;
                    if (m.Data[4] != 255) senderDevice.TimberlineParams.OutsideTemperature = m.Data[4] - 75;
                    if (m.Data[6] != 255) senderDevice.TimberlineParams.LiquidLevel = m.Data[6];
                    if ((m.Data[7] & 3) != 3) senderDevice.TimberlineParams.DomesticWaterFlow = (m.Data[7] & 3) != 0;
                    break;
                }
            case 22:
                {
                    if ((m.Data[0] & 3) != 3)
                    {
                        if ((m.Data[0] & 3) == 0) senderDevice.TimberlineParams.Zones[0].State = zoneState_t.Off;
                        if ((m.Data[0] & 3) == 1) senderDevice.TimberlineParams.Zones[0].State = zoneState_t.Heat;
                        if ((m.Data[0] & 3) == 2) senderDevice.TimberlineParams.Zones[0].State = zoneState_t.Fan;
                    }

                    if (((m.Data[0] >> 2) & 3) != 3)
                    {
                        if (((m.Data[0] >> 2) & 3) == 0) senderDevice.TimberlineParams.Zones[1].State = zoneState_t.Off;
                        if (((m.Data[0] >> 2) & 3) == 1) senderDevice.TimberlineParams.Zones[1].State = zoneState_t.Heat;
                        if (((m.Data[0] >> 2) & 3) == 2) senderDevice.TimberlineParams.Zones[1].State = zoneState_t.Fan;
                    }

                    if (((m.Data[0] >> 4) & 3) != 3)
                    {
                        if (((m.Data[0] >> 4) & 3) == 0) senderDevice.TimberlineParams.Zones[2].State = zoneState_t.Off;
                        if (((m.Data[0] >> 4) & 3) == 1) senderDevice.TimberlineParams.Zones[2].State = zoneState_t.Heat;
                        if (((m.Data[0] >> 4) & 3) == 2) senderDevice.TimberlineParams.Zones[2].State = zoneState_t.Fan;
                    }
                    if (((m.Data[0] >> 6) & 3) != 3)
                    {
                        if (((m.Data[0] >> 6) & 3) == 0) senderDevice.TimberlineParams.Zones[3].State = zoneState_t.Off;
                        if (((m.Data[0] >> 6) & 3) == 1) senderDevice.TimberlineParams.Zones[3].State = zoneState_t.Heat;
                        if (((m.Data[0] >> 6) & 3) == 2) senderDevice.TimberlineParams.Zones[3].State = zoneState_t.Fan;
                    }
                    if ((m.Data[1] & 3) != 3)
                    {
                        if ((m.Data[1] & 3) == 0) senderDevice.TimberlineParams.Zones[4].State = zoneState_t.Off;
                        if ((m.Data[1] & 3) == 1) senderDevice.TimberlineParams.Zones[4].State = zoneState_t.Heat;
                        if ((m.Data[1] & 3) == 2) senderDevice.TimberlineParams.Zones[4].State = zoneState_t.Fan;
                    }

                    if (m.Data[2] != 255) senderDevice.TimberlineParams.Zones[0].CurrentTemperature = m.Data[2] - 75;
                    if (m.Data[3] != 255) senderDevice.TimberlineParams.Zones[1].CurrentTemperature = m.Data[3] - 75;
                    if (m.Data[4] != 255) senderDevice.TimberlineParams.Zones[2].CurrentTemperature = m.Data[4] - 75;
                    if (m.Data[5] != 255) senderDevice.TimberlineParams.Zones[3].CurrentTemperature = m.Data[5] - 75;
                    if (m.Data[6] != 255) senderDevice.TimberlineParams.Zones[4].CurrentTemperature = m.Data[6] - 75;

                    if ((m.Data[7] & 3) != 3) senderDevice.TimberlineParams.HeaterEnabled = (m.Data[7] & 3) != 0;
                    if (((m.Data[7] >> 2) & 3) != 3) senderDevice.TimberlineParams.ElementEnabled = ((m.Data[7] >> 2) & 3) != 0;
                    break;
                }
            case 23:
                {
                    if (m.Data[0] != 255) senderDevice.TimberlineParams.Zones[0].SetPwmPercent = m.Data[0];
                    if (m.Data[1] != 255) senderDevice.TimberlineParams.Zones[1].SetPwmPercent = m.Data[1];
                    if (m.Data[2] != 255) senderDevice.TimberlineParams.Zones[2].SetPwmPercent = m.Data[2];
                    if (m.Data[3] != 255) senderDevice.TimberlineParams.Zones[3].SetPwmPercent = m.Data[3];
                    if (m.Data[4] != 255) senderDevice.TimberlineParams.Zones[4].SetPwmPercent = m.Data[4];
                    break;
                }
            case 24:
                {
                    if ((m.Data[0] & 15) != 15) senderDevice.TimberlineParams.Zones[0].FanStage = m.Data[0] & 15;
                    if (((m.Data[0] >> 4) & 15) != 15) senderDevice.TimberlineParams.Zones[1].FanStage = (m.Data[0] >> 4) & 15;
                    if ((m.Data[1] & 15) != 15) senderDevice.TimberlineParams.Zones[2].FanStage = m.Data[1] & 15;
                    if (((m.Data[1] >> 4) & 15) != 15) senderDevice.TimberlineParams.Zones[3].FanStage = (m.Data[0] >> 4) & 15;
                    if ((m.Data[2] & 15) != 15) senderDevice.TimberlineParams.Zones[4].FanStage = m.Data[2] & 15;
                    if (m.Data[3] != 255) senderDevice.TimberlineParams.Zones[0].CurrentPwm = m.Data[3];
                    if (m.Data[4] != 255) senderDevice.TimberlineParams.Zones[1].CurrentPwm = m.Data[4];
                    if (m.Data[5] != 255) senderDevice.TimberlineParams.Zones[2].CurrentPwm = m.Data[5];
                    if (m.Data[6] != 255) senderDevice.TimberlineParams.Zones[3].CurrentPwm = m.Data[6];
                    if (m.Data[7] != 255) senderDevice.TimberlineParams.Zones[4].CurrentPwm = m.Data[7];
                    break;
                }
            case 25:
                {
                    if (m.Data[0] != 255) senderDevice.TimberlineParams.Zones[0].TempSetPointDay = m.Data[0] - 75;
                    if (m.Data[1] != 255) senderDevice.TimberlineParams.Zones[1].TempSetPointDay = m.Data[1] - 75;
                    if (m.Data[2] != 255) senderDevice.TimberlineParams.Zones[2].TempSetPointDay = m.Data[2] - 75;
                    if (m.Data[3] != 255) senderDevice.TimberlineParams.Zones[3].TempSetPointDay = m.Data[3] - 75;
                    if (m.Data[4] != 255) senderDevice.TimberlineParams.Zones[4].TempSetPointDay = m.Data[4] - 75;
                    break;
                }
            case 26:
                {
                    if (m.Data[0] != 255) senderDevice.TimberlineParams.Zones[0].TempSetPointNight = m.Data[0] - 75;
                    if (m.Data[1] != 255) senderDevice.TimberlineParams.Zones[1].TempSetPointNight = m.Data[1] - 75;
                    if (m.Data[2] != 255) senderDevice.TimberlineParams.Zones[2].TempSetPointNight = m.Data[2] - 75;
                    if (m.Data[3] != 255) senderDevice.TimberlineParams.Zones[3].TempSetPointNight = m.Data[3] - 75;
                    if (m.Data[4] != 255) senderDevice.TimberlineParams.Zones[4].TempSetPointNight = m.Data[4] - 75;
                    break;
                }
            case 27:
                {
                    if (m.Data[0] != 255) senderDevice.TimberlineParams.Zones[0].ManualPercent = m.Data[0];
                    if (m.Data[1] != 255) senderDevice.TimberlineParams.Zones[1].ManualPercent = m.Data[1];
                    if (m.Data[2] != 255) senderDevice.TimberlineParams.Zones[2].ManualPercent = m.Data[2];
                    if (m.Data[3] != 255) senderDevice.TimberlineParams.Zones[3].ManualPercent = m.Data[3];
                    if (m.Data[4] != 255) senderDevice.TimberlineParams.Zones[4].ManualPercent = m.Data[4];
                    if ((m.Data[5] & 3) != 3) senderDevice.TimberlineParams.Zones[0].ManualMode = (m.Data[5] & 3) != 0;
                    if (((m.Data[5] >> 2) & 3) != 3) senderDevice.TimberlineParams.Zones[1].ManualMode = ((m.Data[5] >> 2) & 3) != 0;
                    if (((m.Data[5] >> 4) & 3) != 3) senderDevice.TimberlineParams.Zones[2].ManualMode = ((m.Data[5] >> 4) & 3) != 0;
                    if (((m.Data[5] >> 6) & 3) != 3) senderDevice.TimberlineParams.Zones[3].ManualMode = ((m.Data[5] >> 6) & 3) != 0;
                    if ((m.Data[6] & 3) != 3) senderDevice.TimberlineParams.Zones[4].ManualMode = (m.Data[6] & 3) != 0;
                    break;
                }
            case 33:
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

                break;
            case 100:
                if (m.Data[0] == 1 && m.Data[1]==1)
                {
                    senderDevice.flagEraseDone = true;
                }
                if (m.Data[0] == 2 && m.Data[1] == 1)
                {
                    senderDevice.flagSetAdrDone = true;
                }
                if (m.Data[0] == 3 && m.Data[1] == 1)
                {
                    senderDevice.flagProgramDone = true;
                }
                break;
            case 105:
                {
                    if (m.Data[0] == 1)
                    {
                        senderDevice.fragmentAddress = (uint)(m.Data[1] * 0x1000000 + m.Data[2] * 0x10000 + m.Data[3] * 0x100 + m.Data[4]);
                        Debug.WriteLine($"Adress set to 0X{senderDevice.fragmentAddress:X}");
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

                    break;
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
        var currentDevice = ConnectedDevices.FirstOrDefault(d => d.Id == id);
        if (currentDevice == null) return;
        ReadingBbErrorsMode = false;
        OmniMessage msg = new()
        {
            Pgn = 8,
            TransmitterId = new(126, 6),
            ReceiverId = new(id.Type, id.Address),
            Data =
            {
                [0] = 6, //Read Single Param
                [1] = 0xFF //Read Param
            }
        };

        var counter = 0;
        var parameterCount = 64;
        for (var p = 0; p < parameterCount; p++)
        {
            msg.Data[4] = (byte)(p / 256);
            msg.Data[5] = (byte)(p % 256);
            currentDevice.flagGetBbDone = false;
            for (var t = 0; t < 7; t++)
            {
                SendMessage(msg);
                var success = false;
                for (var i = 0; i < 50; i++)
                {
                    if (currentDevice.flagGetBbDone)
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

        OmniMessage msg = new()
        {
            TransmitterId = new(126, 6),
            ReceiverId = new(cd.Id.Type, cd.Id.Address),
            Pgn = 1,
            Data = new byte[8]

        };
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
        ReadingBbErrorsMode = true;

        var currentDevice = ConnectedDevices.FirstOrDefault(i => i.Id.Equals(id));

        uiContext.Send(x =>
        {
            if (currentDevice == null) return;
            currentDevice.BbErrors.Clear();
        }, null);

        var msg = new OmniMessage
        {
            Pgn = 8,
            ReceiverId = new(id.Type, id.Address),
            Data =
            {
                [0] = 0x13, //Read Errors
                [1] = 0xFF
            }
        };
        if (currentDevice != null)
            for (var i = 0; i < currentDevice.DeviceReference.BBErrorsLen; i++)
            {
                msg.Data[4] = (byte)(i / 256); //Pair count
                msg.Data[5] = (byte)(i % 256); //Pair count
                msg.Data[6] = 0x00; //Pair count MSB
                msg.Data[7] = 0x01; //Pair count LSB

                currentDevice.flagGetBbDone = false;

                for (var t = 0; t < 7; t++)
                {
                    SendMessage(msg);
                    var success = false;
                    for (var j = 0; j < 50; j++)
                    {
                        if (currentDevice.flagGetBbDone)
                        {
                            success = true;
                            break;
                        }

                        await Task.Delay(1);
                        Debug.WriteLineIf(j == 49, $"Error reading BB address {i},attempt:{t + 1}");
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

        var msg = new OmniMessage
        {
            Pgn = 8,
            ReceiverId = new(id.Type, id.Address),
            Data =
            {
                [0] = 0x0, //Erase Common
                [1] = 0xFF,
                [4] = 0xFF,
                [5] = 0xFF,
                [6] = 0xFF,
                [7] = 0xFF
            }
        };
        SendMessage(msg);

        Done();
    }

    public void EraseErrorsBlackBox(DeviceId id)
    {
        if (!Capture("t_bb_errors_erasing")) return;

        var msg = new OmniMessage();
        msg.Pgn = 8;
        msg.TransmitterId.Address = 6;
        msg.TransmitterId.Type = 126;
        msg.ReceiverId.Address = id.Address;
        msg.ReceiverId.Type = id.Type;
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
        var cnt = 0;

        var currentDevice = ConnectedDevices.FirstOrDefault(i => i.Id.Equals(id));

        for (var parId = 0; parId < 700; parId++) //Currently we have 600 parameters, 100 just in case
        {
            //if (!GotResource($"par_{parId}")) // Requesting even if we know nothing about it
            //    continue;
            OmniMessage msg = new();
            msg.Pgn = 7;
            msg.TransmitterId.Address = 6;
            msg.TransmitterId.Type = 126;
            msg.ReceiverId.Address = id.Address;
            msg.ReceiverId.Type = id.Type;
            msg.Data[0] = 3; //Read Param
            msg.Data[1] = 0xFF; //Read Param
            msg.Data[2] = (byte)(parId / 256);
            msg.Data[3] = (byte)(parId % 256);

            currentDevice.flagGetParamDone = false;


            for (var t = 0; t < 7; t++)
            {
                SendMessage(msg);
                var success = false;

                for (var j = 0; j < 50; j++)
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
        var currentDevice = ConnectedDevices.FirstOrDefault(i => i.Id.Equals(id));

        for (var parId = 12; parId < 15; parId++) //ID1 ID2 ID3
        {
            OmniMessage msg = new();
            msg.Pgn = 7;
            msg.TransmitterId.Address = 6;
            msg.TransmitterId.Type = 126;
            msg.ReceiverId.Address = id.Address;
            msg.ReceiverId.Type = id.Type;
            msg.Data[0] = 3; //Read Param
            msg.Data[1] = 0xFF; //Read Param
            msg.Data[2] = (byte)(parId / 256);
            msg.Data[3] = (byte)(parId % 256);

            currentDevice.flagGetParamDone = false;

            for (var t = 0; t < 7; t++)
            {
                SendMessage(msg);
                var success = false;

                for (var j = 0; j < 50; j++)
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

            msg.Pgn = 6;
            msg.Data[0] = 0;
            msg.Data[1] = 18;
            for (var t = 0; t < 7; t++)
            {
                currentDevice.flagGetVersionDone = false;
                SendMessage(msg);
                var success = false;

                for (var j = 0; j < 50; j++)
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
        var dev = ConnectedDevices.FirstOrDefault(d => d.Id.Equals(id));
        if (dev == null) return;
        var msg = new OmniMessage();
        var tempCollection = new List<ReadedParameter>();
        foreach (var p in dev.ReadParameters)
            tempCollection.Add(p);
        var cnt = 0;
        foreach (var p in tempCollection)
        {
            msg.Pgn = 7;
            msg.TransmitterId.Address = 6;
            msg.TransmitterId.Type = 126;
            msg.ReceiverId.Address = id.Address;
            msg.ReceiverId.Type = id.Type;
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
        var dev = ConnectedDevices.FirstOrDefault(d => d.Id.Equals(id));
        if (dev == null) return;
        var msg = new OmniMessage();

        msg.Pgn = 7;
        msg.ReceiverId.Address = id.Address;
        msg.ReceiverId.Type = id.Type;
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
        var msg = new OmniMessage();
        msg.Pgn = pgn;
        msg.TransmitterId.Address = from.Address;
        msg.TransmitterId.Type = from.Type;
        msg.ReceiverId.Address = to.Address;
        msg.ReceiverId.Type = to.Type;
        data.CopyTo(msg.Data, 0);
        SendMessage(msg);
    }
    public void SendCommand(int com, DeviceId dev, byte[] data = null)
    {
        var message = new OmniMessage();
        message.Pgn = 1;
        message.TransmitterId.Address = 6;
        message.TransmitterId.Type = 126;
        message.ReceiverId.Address = dev.Address;
        message.ReceiverId.Type = dev.Type;
        message.Data[1] = (byte)com;
        for (var i = 0; i < 6; i++)
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






