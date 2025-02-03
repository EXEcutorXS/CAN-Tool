using CAN_Tool;
using CAN_Tool.Libs;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using OmniProtocol;
using ScottPlot;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
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
using static Omni;
using static CAN_Tool.Libs.Helper;


namespace OmniProtocol
{

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
                    if (a >= 1)
                        return "0";
                    else if (a >= 0.1)
                        return "0.0";
                    else if (a >= 0.01)
                        return "0.00";
                    else
                        return "0.000";
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

    public partial class GenericLoadTrippleViewModel : ObservableObject
    {


        public GenericLoadTrippleViewModel()
        {

        }

        [ObservableProperty] public LoadMode_t loadMode1;
        [ObservableProperty] public LoadMode_t loadMode2;
        [ObservableProperty] public LoadMode_t loadMode3;
        [ObservableProperty] public int pwmLevel1;
        [ObservableProperty] public int pwmLevel2;
        [ObservableProperty] public int pwmLevel3;
    }

    public partial class ACInverterViewModel : ObservableObject
    {
        [ObservableProperty] public int compressorRevsSet;
        [ObservableProperty] public int compressorRevsMeasured;
        [ObservableProperty] public float compressorCurrent;
        [ObservableProperty] public float condensorCurrent;
        [ObservableProperty] public float condensorPwmSet;
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

        [NotifyPropertyChangedFor(nameof(DataAsText), nameof(VerboseInfo))]
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
                case 0: ret = 0; break; //Usually used Custom decoder
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

            var pgn = Pgns[Pgn];
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
            if (Pgns.ContainsKey(Pgn) && Pgns[Pgn].multiPack && Data[0] != m.Data[0]) //Другой номер мультипакета
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

    public class DeviceTemplate
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

public partial class CommonParameters : ObservableObject, ICloneable
{
    [ObservableProperty] private int revMeasured;
    [ObservableProperty] private int revSet;
    [ObservableProperty] private double fuelPumpMeasured;
    [ObservableProperty] private int glowPlug;
    [ObservableProperty] private double voltage;
    [ObservableProperty] private int stageTime;
    [ObservableProperty] private int modeTime;
    [ObservableProperty] private int setPowerLevel;

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
    [ObservableProperty] private float exPressure;
    [ObservableProperty] private float pcbTemp;
    [ObservableProperty] private float mcuTemp;


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
        if (obj is not DeviceId)
            return false;
        return GetHashCode() == obj.GetHashCode();
    }
}

public partial class Omni : ObservableObject
{

    public static Dictionary<int, DeviceTemplate> Devices { set; get; }
    public static Dictionary<int, PgnClass> Pgns { get; }
    public static Dictionary<int, OmniCommand> Commands { get; }

    partial void SeedStaticData();

    public Omni(CanAdapter canAdapter)
    {
        ArgumentNullException.ThrowIfNull(canAdapter);
        this.canAdapter = canAdapter;
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



    public bool UseImperial { set; get; }




    [ObservableProperty] private ObservableCollection<DeviceViewModel> connectedDevices = new();

    [NotifyPropertyChangedFor(nameof(AvailableModels), nameof(AvailableVendors))]
    [ObservableProperty] private DeviceViewModel selectedConnectedDevice;

    public UpdatableList<OmniMessage> Messages { get; } = new();

    private readonly CanAdapter canAdapter;

    [ObservableProperty] private bool readingBbErrorsMode = false;
    [NotifyPropertyChangedFor(nameof(AvailableModels), nameof(AvailableVendors))]
    [ObservableProperty] public static BindingList<ConfigPreset> allPresets = new();
    [NotifyPropertyChangedFor(nameof(AvailableModels))]
    [ObservableProperty] private string selectedVendor;
    [ObservableProperty] private string selectedModel;

    [RelayCommand]
    void ClearDevices()
    {
        ConnectedDevices.Clear();
    }

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
                Task.Run(() => RequestSerial(id));
        }

        if (Pgns.ContainsKey(m.Pgn))
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
                        if (!senderDevice.OverrideState.BlowerOverriden)
                            senderDevice.OverrideState.BlowerOverridenRevs = senderDevice.Parameters.RevSet;
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
                    case 59:
                        senderDevice.Parameters.McuTemp = (int)(rawValue * sv.AssignedParameter.a + sv.AssignedParameter.b);
                        break;
                    case 60:
                        senderDevice.Parameters.Pressure = (float)ImperialConverter(rawValue * sv.AssignedParameter.a + sv.AssignedParameter.b, sv.AssignedParameter.UnitT);
                        if (senderDevice.PressureLogWriting)
                            senderDevice.PressureLog[senderDevice.PressureLogPointer++] = senderDevice.Parameters.Pressure;
                        break;
                    case 131:
                        senderDevice.Parameters.ExPressure = (float)ImperialConverter(rawValue * sv.AssignedParameter.a + sv.AssignedParameter.b, sv.AssignedParameter.UnitT);
                        break;
                    case 132:
                        senderDevice.Parameters.SetPowerLevel = (int)ImperialConverter(rawValue * sv.AssignedParameter.a + sv.AssignedParameter.b, sv.AssignedParameter.UnitT);
                        break;
                    case 134:
                        senderDevice.ACInverterParams.CompressorRevsSet = (int)(rawValue * sv.AssignedParameter.a + sv.AssignedParameter.b);
                        break;
                    case 135:
                        senderDevice.ACInverterParams.CompressorRevsMeasured = (int)(rawValue * sv.AssignedParameter.a + sv.AssignedParameter.b);
                        break;
                    case 136:
                        senderDevice.ACInverterParams.CondensorPwmSet = (int)(rawValue * sv.AssignedParameter.a + sv.AssignedParameter.b);
                        break;
                    case 138:
                        senderDevice.ACInverterParams.CompressorCurrent = (int)(rawValue * sv.AssignedParameter.a + sv.AssignedParameter.b);
                        break;
                    case 139:
                        senderDevice.ACInverterParams.CondensorCurrent = (int)(rawValue * sv.AssignedParameter.a + sv.AssignedParameter.b);
                        break;
                    case 145:
                        senderDevice.Parameters.PcbTemp = (int)(rawValue * sv.AssignedParameter.a + sv.AssignedParameter.b);
                        break;
                }
            }

        switch (m.Pgn)
        {
            case 2://Command ack
                switch (m.Data[1])
                {
                    case 0:
                        senderDevice.Firmware[0] = m.Data[2];
                        senderDevice.Firmware[1] = m.Data[3];
                        senderDevice.Firmware[2] = m.Data[4];
                        senderDevice.Firmware[3] = m.Data[5];
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
                            case 12: senderDevice.Serial[0] = (int)parameterValue; break;
                            case 13: senderDevice.Serial[1] = (int)parameterValue; break;
                            case 14: senderDevice.Serial[2] = (int)parameterValue; break;
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
                    if (m.Data[0] != 123)
                    {
                        senderDevice.Firmware[0] = m.Data[0];
                        senderDevice.Firmware[1] = m.Data[1];
                        senderDevice.Firmware[2] = m.Data[2];
                        senderDevice.Firmware[3] = m.Data[3];
                    }
                    else
                    {
                        senderDevice.BootFirmware[0] = m.Data[0];
                        senderDevice.BootFirmware[1] = m.Data[1];
                        senderDevice.BootFirmware[2] = m.Data[2];
                        senderDevice.BootFirmware[3] = m.Data[3];
                    }
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
                        senderDevice.Serial[0] = (int)(m.Data[1] * 0x1000000 + m.Data[2] * 0x10000 + m.Data[3] * 0x100 + m.Data[4]);
                        break;
                    case 2:
                        senderDevice.Serial[1] = (int)(m.Data[1] * 0x1000000 + m.Data[2] * 0x10000 + m.Data[3] * 0x100 + m.Data[4]);
                        break;
                    case 3:
                        senderDevice.Serial[2] = (int)(m.Data[1] * 0x1000000 + m.Data[2] * 0x10000 + m.Data[3] * 0x100 + m.Data[4]);
                        break;
                    default:
                        break;
                }

                break;
            case 47:
                if ((m.Data[0] & 3) < 2) senderDevice.OverrideState.FuelPumpOverriden = (m.Data[0] & 3) > 0;
                if (((m.Data[0] >> 2) & 3) < 2) senderDevice.OverrideState.RelayOverriden = ((m.Data[0] >> 2) & 3) > 0;
                if (((m.Data[0] >> 4) & 3) < 2) senderDevice.OverrideState.GlowPlugOverriden = ((m.Data[0] >> 4) & 3) > 0;
                if (((m.Data[0] >> 6) & 3) < 2) senderDevice.OverrideState.PumpOverriden = ((m.Data[0] >> 6) & 3) > 0;
                if ((m.Data[1] & 3) < 2) senderDevice.OverrideState.BlowerOverriden = (m.Data[1] & 3) > 0;

                if ((m.Data[2] & 3) < 2 && senderDevice.OverrideState.PumpOverriden) senderDevice.OverrideState.PumpOverridenState = (m.Data[1] & 3) > 0;
                if (((m.Data[2] >> 2) & 3) < 2 && senderDevice.OverrideState.RelayOverriden) senderDevice.OverrideState.RelayOverridenState = ((m.Data[2] >> 2) & 3) > 0;
                if (m.Data[3] != 255 && senderDevice.OverrideState.BlowerOverriden) senderDevice.OverrideState.BlowerOverridenRevs = m.Data[3];
                if (m.Data[4] != 255 && senderDevice.OverrideState.GlowPlugOverriden) senderDevice.OverrideState.GlowPlugOverridenPower = m.Data[4];
                if (m.Data[5] != 255 || m.Data[6] != 255 && senderDevice.OverrideState.FuelPumpOverriden) senderDevice.OverrideState.FuelPumpOverridenFrequencyX100 = m.Data[5] * 256 + m.Data[6];
                break;


            case 49:
                if ((m.Data[0] & 3) < 3)
                    senderDevice.GenericLoadTripple.LoadMode1 = (LoadMode_t)(m.Data[0] & 3);
                if (((m.Data[0] >> 2) & 3) < 3)
                    senderDevice.GenericLoadTripple.LoadMode2 = (LoadMode_t)((m.Data[0] >> 2) & 3);
                if (((m.Data[0] >> 4) & 3) < 3)
                    senderDevice.GenericLoadTripple.LoadMode3 = (LoadMode_t)((m.Data[0] >> 4) & 3);

                if (m.Data[1] <= 100)
                    senderDevice.GenericLoadTripple.PwmLevel1 = m.Data[1];
                if (m.Data[2] <= 100)
                    senderDevice.GenericLoadTripple.PwmLevel2 = m.Data[2];
                if (m.Data[3] <= 100)
                    senderDevice.GenericLoadTripple.PwmLevel3 = m.Data[3];
                break;

            case 50:
                switch (m.Data[0])
                {
                    case 0:
                        if (m.Data[1] != 255)
                            senderDevice.ACInverterParams.CompressorRevsSet = m.Data[1];
                        if (m.Data[2] != 255)
                            senderDevice.ACInverterParams.CompressorRevsMeasured = m.Data[2];
                        break;

                }
                break;

            case 100:
                if (m.Data[0] == 1 && m.Data[1] == 1)
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

    public async void RequestSerial(DeviceId id)
    {
        for (byte i = 1; i < 4; i++)
        {
            RequestPgn(id, 33, i);
            await Task.Delay(100);
        }
        RequestPgn(id, 18);
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

    public void RequestPgn(DeviceId device, int pgn, byte? multiPack = null)
    {
        var message = new OmniMessage();
        message.Pgn = 6;
        message.TransmitterId.Address = 6;
        message.TransmitterId.Type = 126;
        message.ReceiverId.Address = device.Address;
        message.ReceiverId.Type = device.Type;
        message.Data[0] = (byte)(pgn >> 8);
        message.Data[1] = (byte)(pgn & 0xFF);
        if (multiPack != null)
            message.Data[2] = (byte)multiPack;
        else
            message.Data[2] = 255;
        SendMessage(message);
    }

    


}






