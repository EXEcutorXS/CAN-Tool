using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.IO;
using System.Threading;
using System.ComponentModel;
using Can_Adapter;
using System.Diagnostics.CodeAnalysis;
using System.Collections.ObjectModel;
using System.Data;
using CAN_Tool.ViewModels.Base;
using System.Windows.Markup;
using System.Diagnostics.Contracts;
using System.Reflection.Metadata.Ecma335;
using System.Diagnostics;
using System.Windows.Media;
using System.Security.Cryptography.Pkcs;
using LiveCharts;
using LiveCharts.Wpf;
using LiveCharts.Helpers;
using System.Windows.Data;

namespace AdversCan
{
    public enum DeviceType
    {
        Binar, Planar, HCU, ValveControl, Bootloader, Stove
    }
    public class PGN
    {
        public int id;
        public string name = "";
        public bool multipack;
        public List<AC2PParameter> parameters = new();
    }
    public class AC2PParameter : ViewModel
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
    public class AC2PCommand : ViewModel
    {
        int id;
        public int Id
        {
            get => id;
            internal set => Set(ref id, value);
        }

        public string Name { internal set; get; }

        private readonly List<AC2PParameter> _Parameters = new();
        public List<AC2PParameter> Parameters => _Parameters;
        public override string ToString() => Name;

    }
    public class AC2PMessage : CanMessage, IUpdatable<AC2PMessage>
    {
        public AC2PMessage() : base()
        {
            Fresh = true;
            Task.Run(() => { Task.Delay(300); Fresh = false; });
            DLC = 8;
            RTR = false;
            IDE = true;
        }
        public AC2PMessage(CanMessage m) : this()
        {
            if (m.DLC != 8 || m.RTR || !m.IDE)
                throw new ArgumentException("CAN message is not compliant with AC2P");
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
        public string PrintParameter(AC2PParameter p)
        {
            StringBuilder retString = new();
            long rawValue = getRaw(Data, p.BitLength, p.StartBit, p.StartByte, p.Signed);
            retString.Append(p.Name + ": ");
            if (p.CustomDecoder != null)
                return p.CustomDecoder(Data);

            if (p.GetMeaning != null)
                return p.GetMeaning((int)rawValue);
            if (p.Meanings != null && p.Meanings.ContainsKey((int)rawValue))
                retString.Append(rawValue.ToString() + " - " + p.Meanings[(int)rawValue]);
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
            if (!AC2P.PGNs.ContainsKey(this.PGN))
                return "PGN not found";
            PGN pgn = AC2P.PGNs[this.PGN];
            string sender, receiver;
            if (AC2P.Devices.ContainsKey(TransmitterType))
                sender = AC2P.Devices[TransmitterType].Name;
            else
                sender = $"(неизвестное устройство №{TransmitterType})";
            if (AC2P.Devices.ContainsKey(ReceiverType))
                receiver = AC2P.Devices[ReceiverType].Name;
            else
                receiver = $"(неизвестное устройство №{ReceiverType})";
            retString.Append($"{sender}({TransmitterAddress})->{receiver}({ReceiverAddress});;");


            retString.Append(pgn.name + ";;");
            if (pgn.multipack)
                retString.Append($"Мультипакет №{Data[0]};");
            if (PGN == 1 && AC2P.commands.ContainsKey(Data[1]))
            {
                AC2PCommand cmd = AC2P.commands[Data[1]];
                retString.Append(cmd.Name + ";");
                if (cmd.Parameters != null)
                    foreach (AC2PParameter p in cmd.Parameters)
                        retString.Append(PrintParameter(p));
            }
            if (pgn.parameters != null)
                foreach (AC2PParameter p in pgn.parameters)
                    if (!pgn.multipack || Data[0] == p.PackNumber)
                        retString.Append(PrintParameter(p));
            return retString.ToString();
        }
        public override string VerboseInfo => GetVerboseInfo().Replace(';', '\n');

        public void Update(AC2PMessage item)
        {
            PGN = item.PGN;
            TransmitterId = item.TransmitterId;
            ReceiverId = item.ReceiverId;
            Data = item.Data;
            Fresh = true;
            Task.Run(() => { Thread.Sleep(300); Fresh = false; });
        }


        public bool IsSimmiliarTo(AC2PMessage m)
        {
            if (PGN != m.PGN)
                return false;
            if (PGN == 1 || PGN == 2)
                if (Data[1] != m.Data[1])
                    return false;
            if (AC2P.PGNs[PGN].multipack && Data[0] != m.Data[0]) //Другой номер мультипакета
                return false;
            return true;
        }
    }

    public class Device
    {
        public int ID;
        public string Name;
        public DeviceType DevType { set; get; }

        public int MaxBlower { get; set; } = 130;
        public double MaxFuelPump { get; set; } = 4;

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
            get
            {
                if (AC2P.BbParameterNames.ContainsKey(id))
                    return AC2P.BbParameterNames[id];
                else
                    return "No data";
            }
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

        public string idString => AC2P.configParameters[Id]?.StringId;
        public string rusName => AC2P.configParameters[Id]?.NameRu;
        public string enName => AC2P.configParameters[Id]?.NameEn;

    }

    public class StatusVariable : ViewModel, IUpdatable<StatusVariable>, IComparable
    {

        public StatusVariable(int var) : base()
        {
            Id = var;
            if (var == 5 ||
                var == 6 ||
                var == 7 ||
                var == 8 ||
                var == 9 ||
                var == 12 ||
                var == 13 ||
                var == 15 ||
                var == 16 ||
                var == 18 ||
                var == 21 ||
                var == 40 ||
                var == 41
                )
                Display = true;

            switch (var)
            {
                case 5: ChartBrush = new SolidColorBrush(Color.FromRgb(255, 255, 0)); break;
                case 15: ChartBrush = new SolidColorBrush(Color.FromRgb(0, 0, 255)); break;
                case 16: ChartBrush = new SolidColorBrush(Color.FromRgb(0, 255, 255)); break;
                case 18: ChartBrush = new SolidColorBrush(Color.FromRgb(0, 255, 0)); break;
                case 21: ChartBrush = new SolidColorBrush(Color.FromRgb(255, 0, 255)); break;
                case 40: ChartBrush = new SolidColorBrush(Color.FromRgb(255, 100, 100)); break;
                default:
                    Random random = new Random((int)DateTime.Now.Ticks);
                    ChartBrush = new SolidColorBrush(Color.FromRgb((byte)random.Next(255), (byte)random.Next(255), (byte)random.Next(255)));
                    break;
            }
        }
        public StatusVariable()
        {
            log = new ChartValues<double>();
        }

        ChartValues<Double> log = new();

        public ChartValues<double> Log => log;
        public int Id { get; set; }

        private long rawVal;

        [AffectsTo(nameof(VerboseInfo), nameof(Value), nameof(FormattedValue))]
        public long RawValue
        {
            get { return rawVal; }
            set { Set(ref rawVal, value); }
        }

        private AC2PParameter assignedParameter;

        public AC2PParameter AssignedParameter
        {
            get { return assignedParameter; }
            set { assignedParameter = value; }
        }

        public string VerboseInfo => AssignedParameter.Decode(RawValue);

        private bool display = false;

        public bool Display { get { return display; } set { Set(ref display, value); } }

        public double Value
        {
            get => rawVal * assignedParameter.a + assignedParameter.b;
        }

        public string FormattedValue
        {
            get => Value.ToString(assignedParameter.OutputFormat);
        }

        public string Name
        {
            get
            {
                if (AC2P.Variables.ContainsKey(Id))
                    return AC2P.Variables[Id].ShortName;
                else
                    return $"Unknown parameter {Id}";
            }
        }

        private Brush chartBrush;


        [AffectsTo(nameof(Color))]
        public System.Windows.Media.Brush ChartBrush
        {
            get => chartBrush;
            set => Set(ref chartBrush, value);
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

        public override string ToString()
        {
            return Name;
        }
    }
    public class ReadedVariable : ViewModel, IUpdatable<ReadedVariable>, IComparable
    {
        int id;
        public int Id { get => id; set => id = value; }

        int _value;
        public int Value
        {
            get => _value;
            set => Set(ref _value, value);

        }

        public void Update(ReadedVariable item)
        {
            Set(ref _value, item.Value);
        }

        public bool IsSimmiliarTo(ReadedVariable item)
        {
            return id == item.id;
        }

        public string Name
        {
            get
            {
                if (AC2P.VariablesNames.ContainsKey(id))
                    return $"{AC2P.VariablesNames[id]}";
                else
                    return "Неизв.переменная # " + id.ToString();
            }
        }
        public string Description => ToString();
        public override string ToString()
        {
            if (AC2P.VariablesNames.ContainsKey(id))
                return $"{AC2P.VariablesNames[id]}: {_value}";
            else
                return "";
        }

        public int CompareTo(object obj)
        {
            return id - (obj as ReadedVariable).id;
        }
    }
    public class BBError : IUpdatable<BBError>, INotifyPropertyChanged, IComparable
    {
        private readonly UpdatableList<ReadedVariable> _variables = new();

        public UpdatableList<ReadedVariable> Variables { get => _variables; }

        public event PropertyChangedEventHandler PropertyChanged;

        public bool IsSimmiliarTo(BBError item)
        {
            return false;
        }



        public int Id { get => 0; }
        public BBError()
        {
            _variables.ListChanged += (s, a) => onChange("Name");
            _variables.AddingNew += (s, a) => onChange("Name");
            _variables.ListChanged += (s, a) => onChange("Description");
            _variables.AddingNew += (s, a) => onChange("Description");
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
                ReadedVariable error = Variables.FirstOrDefault(v => v.Id == 24);

                if (error == null)
                    return "Нет кода ошибки";
                else
                    return $"Код {AC2P.ErrorNames[(int)error.Value]}";
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

    public class AC2PTask : ViewModel
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

        public string StageString
        {
            get
            {
                if (Stage == 0)
                {
                    if (Mode == 1) return "Ожидание комманды";
                    if (Mode == 3) return "Проверка на неостылость";
                    if (Mode == 5) return "Сохранение данных о пуске после завершения работы";
                }

                if (Stage == 1)
                {
                    if (Mode == 0) return "Стартовая диагностика ";
                    if (Mode == 1) return "Страгивание нагнетателя воздуха";
                    if (Mode == 2) return "Ожидание снижения температуры";
                    if (Mode == 3) return "Проверка датчика корпуса на неостылость";
                    if (Mode == 4) return "Ждущий";
                }

                if (Stage == 2)
                {
                    if (Mode == 0) return "Разогрев свечи ";
                    if (Mode == 1) return "Первая попытка розжига";
                    if (Mode == 2) return "Продувка между розжигами ";
                    if (Mode == 3) return "Вторая попытка розжига ";
                    if (Mode == 4) return "Прогрев камеры сгорания ";
                    if (Mode == 5) return "Продувка после срыва пламени на прогреве";
                    if (Mode == 6) return "Продувка перед ждущим ";
                }
                if (Stage == 3)
                {
                    if (Mode == 0) return "Работа на 0 ступени мощности, движемся сверху";
                    if (Mode == 1) return "Работа на 1 ступени мощности, движемся сверху";
                    if (Mode == 2) return "Работа на 2 ступени мощности, движемся сверху";
                    if (Mode == 3) return "Работа на 3 ступени мощности, движемся сверху";
                    if (Mode == 4) return "Работа на 4 ступени мощности, движемся сверху";
                    if (Mode == 5) return "Работа на 5 ступени мощности, движемся сверху";
                    if (Mode == 6) return "Работа на 6 ступени мощности, движемся сверху";
                    if (Mode == 7) return "Работа на 7 ступени мощности, движемся сверху";
                    if (Mode == 8) return "Работа на 8 ступени мощности, движемся сверху";
                    if (Mode == 9) return "Работа на 9 ступени мощности, движемся сверху";
                    if (Mode == 10) return "Работа на 10 ступени мощности, движемся сверху";
                    if (Mode == 11) return "Работа на 1 ступени мощности, движемся снизу";
                    if (Mode == 12) return "Работа на 2 ступени мощности, движемся снизу";
                    if (Mode == 13) return "Работа на 3 ступени мощности, движемся снизу";
                    if (Mode == 14) return "Работа на 4 ступени мощности, движемся снизу";
                    if (Mode == 15) return "Работа на 5 ступени мощности, движемся снизу";
                    if (Mode == 16) return "Работа на 6 ступени мощности, движемся снизу";
                    if (Mode == 17) return "Работа на 7 ступени мощности, движемся снизу";
                    if (Mode == 18) return "Работа на 8 ступени мощности, движемся снизу";
                    if (Mode == 19) return "Работа на 9 ступени мощности, движемся снизу";
                    if (Mode == 20) return "Работа на 10 ступени мощности, движемся снизу";

                    if (Mode == 21) return "Продувка перед вентиляцией";
                    if (Mode == 22) return "Продувка при срыве пламени ";
                    if (Mode == 23) return "Продувка и вентиляция при перегреве";
                    if (Mode == 24) return "Только помпа";
                    if (Mode == 25) return "Малый";
                    if (Mode == 26) return "Средний";
                    if (Mode == 27) return "Сильный";
                    if (Mode == 28) return "Продувка перед ждущим ";
                    if (Mode == 29) return "Ждущий";
                    if (Mode == 30) return "Только помпа";
                    if (Mode == 31) return "Ускоренный нагрев ";
                    if (Mode == 32) return "Розжиг по датчику давления после срыва пламени во время работы ";
                    if (Mode == 34) return "Автокаллибровка перед вентиляцией ";
                    if (Mode == 35) return "Вентиляция";

                    if (Mode == 40) return "Вентиляция на 0 мощности ";
                    if (Mode == 41) return "Вентиляция на 1 мощности";
                    if (Mode == 42) return "Вентиляция на 2 мощности";
                    if (Mode == 43) return "Вентиляция на 3 мощности";
                    if (Mode == 44) return "Вентиляция на 4 мощности";
                    if (Mode == 45) return "Вентиляция на 5 мощности";
                    if (Mode == 46) return "Вентиляция на 6 мощности";
                    if (Mode == 47) return "Вентиляция на 7 мощности";
                    if (Mode == 48) return "Вентиляция на 8 мощности";
                    if (Mode == 49) return "Вентиляция на 9 мощности";

                    if (Mode == 50) return "Режим работы на 0 ступени мощности";
                    if (Mode == 51) return "Режим работы на 1 ступени мощности";
                    if (Mode == 52) return "Режим работы на 2 ступени мощности";
                    if (Mode == 53) return "Режим работы на 3 ступени мощности";
                    if (Mode == 54) return "Режим работы на 4 ступени мощности";
                    if (Mode == 55) return "Режим работы на 5 ступени мощности";
                    if (Mode == 56) return "Режим работы на 6 ступени мощности";
                    if (Mode == 57) return "Режим работы на 7 ступени мощности";
                    if (Mode == 58) return "Режим работы на 8 ступени мощности";
                    if (Mode == 59) return "Режим работы на 9 ступени мощности";

                }
                if (Stage == 4)
                {
                    if (Mode == 0) return "Нормальное завершения работы";
                    if (Mode == 1) return "Продувка при перегреве ";
                    if (Mode == 2) return "Продувка при обрыве датчиков ИП или корпуса";
                    if (Mode == 3) return "Продувка при  неисправностях помпы, ТН, НВ или превышении количества срывов пламени";
                    if (Mode == 4) return "Завершение работы без ТН";
                    if (Mode == 5) return "Завершение работы без свечи и ТН ";
                }
                if (Stage == 5)
                {
                    if (Mode == 0) return "Проверка платы";
                    if (Mode == 1) return "Калибровка коллекторного НВ";
                }
                if (Stage == 6)
                {
                    if (Mode == 0) return "Ручной режим работы";

                }
                return "Unknown mode";
            }
        }
        public string ErrorString => AC2P.ErrorNames.GetValueOrDefault(error, $"Unknown error: {error}");

        public object Clone()
        {
            return MemberwiseClone();
        }
    }
    public class ConnectedDevice : ViewModel
    {

        public ConnectedDevice(DeviceId newId)
        {

            id = newId;
            if (AC2P.Devices.ContainsKey(ID.Type))
                deviceReference = AC2P.Devices[ID.Type];
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
                    return "No firmware data";
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

        private SeriesCollection chartCollection = new();
        public SeriesCollection ChartCollection => chartCollection;

        private readonly UpdatableList<ReadedParameter> _readedParameters = new();
        public UpdatableList<ReadedParameter> readedParameters => _readedParameters;

        private UpdatableList<ReadedBlackBoxValue> _bbValues = new();
        public UpdatableList<ReadedBlackBoxValue> BBValues => _bbValues;

        public BBError currentBBError { set; get; }

        private UpdatableList<BBError> _BBErrors = new();
        public UpdatableList<BBError> BBErrors => _BBErrors;

        public BindingList<MainParameters> Log { set; get; } = new();

        private bool manualMode;

        public bool ManualMode
        {
            get { return manualMode; }
            set { Set(ref manualMode, value); }
        }

        public bool flagEraseDone = false;

        public bool flagSetAdrDone = false;

        public bool flagProgramDone = false;

        public bool flagCheckDone = false;

        public bool flagGetParamDone = false;

        public bool flagGetBBDone = false;

        public bool waitForBB = false;

        public int dataLength = 0;

        public int crc = 0;

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

        private bool[] supportedVariables = new bool[AC2P.Variables.Count];

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
            IsLogWriting = true;
            chartCollection.Clear();
            foreach (var v in Status)
            {
                if (v.Display)
                {
                    LineSeries ls = new()
                    {
                        Stroke = v.ChartBrush,
                        Title = v.Name,
                        Values = v.Log,
                        LineSmoothness = 0
                    };
                    chartCollection.Add(ls);
                }
            }
        }
        public void LogStop()
        {
            IsLogWriting = false;
        }
        public void LogClear()
        {
            foreach (var v in status)
                v.Log.Clear();
        }

        public void LogTick()
        {

            foreach (var s in status)
                s.Log.Add(s.Value);
            Log.Insert(0, (MainParameters)Parameters.Clone());
        }


        public void SaveReport()
        {

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
            if (AC2P.Devices.ContainsKey(Type))
                return $"{Type} - {Address} ({AC2P.Devices[Type]})";
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
        public string NameRu;
        public string NameEn;
        public override string ToString()
        {
            return $"{Id} - {StringId}: {NameRu}";
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
    public class AC2P : ViewModel
    {

        public event EventHandler NewDeviceAquired;

        private SynchronizationContext UIContext;

        static readonly Dictionary<int, string> defMeaningsYesNo = new() { { 0, "Нет" }, { 1, "Да" }, { 2, "Нет данных" }, { 3, "Нет данных" } };
        static readonly Dictionary<int, string> defMeaningsOnOff = new() { { 0, "Выкл" }, { 1, "Вкл" }, { 2, "Нет данных" }, { 3, "Нет данных" } };
        static readonly Dictionary<int, string> defMeaningsAllow = new() { { 0, "Разрешено" }, { 1, "Запрещёно" }, { 2, "Нет данных" }, { 3, "Нет данных" } };
        static readonly Dictionary<int, string> Stages = new() { { 0, "STAGE_Z" }, { 1, "STAGE_P" }, { 2, "STAGE_H" }, { 3, "STAGE_W" }, { 4, "STAGE_F" }, { 5, "STAGE_T" }, { 6, "STAGE_M" } };



        public static Dictionary<int, PGN> PGNs = new();

        public static Dictionary<int, AC2PCommand> commands = new();

        public static Dictionary<int, ConfigParameter> configParameters = new();
        public static Dictionary<int, Variable> Variables = new();
        public static Dictionary<int, string> VariablesNames = new();

        public static Dictionary<int, BbParameter> BbParameters = new();

        public static Dictionary<int, string> ParamtersNames = new();

        public static Dictionary<int, string> BbParameterNames = new();



        private readonly BindingList<ConnectedDevice> _connectedDevices = new();
        public BindingList<ConnectedDevice> ConnectedDevices => _connectedDevices;

        private readonly UpdatableList<AC2PMessage> _messages = new();
        public UpdatableList<AC2PMessage> Messages => _messages;

        public static Dictionary<int, string> ErrorNames = new();

        private readonly CanAdapter canAdapter;

        public bool WaitingForBBErrors { get; set; } = false;

        AC2PTask currentTask = new();

        public AC2PTask CurrentTask
        {
            get => currentTask;
            set => Set(ref currentTask, value);
        }

        private bool CancellationRequested => CurrentTask.CTS.IsCancellationRequested;

        private bool Capture(string n) => CurrentTask.Capture(n);

        private void Done() => CurrentTask.onDone();

        private void Cancel() => CurrentTask.onCancel();

        private void UpdatePercent(int p) => CurrentTask.UpdatePercent(p);

        private void ProcessCanMessage(CanMessage msg)
        {
            AC2PMessage m = new AC2PMessage(msg);
            DeviceId id = m.TransmitterId;

            Debug.WriteLine("<-" + m.ToString());
            if (ConnectedDevices.FirstOrDefault(d => d.ID.Equals(id)) == null)
            {

            }

            ConnectedDevice currentDevice = ConnectedDevices.FirstOrDefault(d => d.ID.Equals(m.TransmitterId));

            if (currentDevice == null)
            {
                currentDevice = new ConnectedDevice(id);
                ConnectedDevices.Add(currentDevice);
                NewDeviceAquired?.Invoke(this, null);
            }


            if (!PGNs.ContainsKey(m.PGN)) return; //Такого PGN нет в библиотеке


            if (m.PGN == 2) //Подтверждение выполненной комманды
            {

                switch (m.Data[1])
                {
                    case 0:
                        currentDevice.Firmware = new byte[] { m.Data[2], m.Data[3], m.Data[4], m.Data[5] };
                        break;
                    case 67:                                               //Вход в ручной режим
                        if (m.Data[2] == 1)
                            currentDevice.ManualMode = true;
                        else
                            currentDevice.ManualMode = false;
                        break;
                }


            }

            foreach (AC2PParameter p in PGNs[m.PGN].parameters)
            {

                if (PGNs[m.PGN].multipack && p.PackNumber != m.Data[0]) continue;
                if (p.Var != 0)
                {
                    StatusVariable sv = new StatusVariable(p.Var);
                    sv.AssignedParameter = p;
                    long rawValue = AC2PMessage.getRaw(m.Data, p.BitLength, p.StartBit, p.StartByte, p.Signed);
                    if (rawValue == Math.Pow(2, p.BitLength) - 1) return; //Неподдерживаемый параметр, ливаем
                    sv.RawValue = rawValue;
                    currentDevice.SupportedVariables[sv.Id] = true;
                    if (currentDevice.Status.TryToAdd(sv)) //Если появляется новая переменная все логи очищаются, чтобы всё шло синхронно
                    {
                        foreach (var s in currentDevice.Status)
                            s.Log.Clear();
                    }


                    if (sv.Id == 1)
                        currentDevice.Parameters.Stage = (int)(rawValue * sv.AssignedParameter.a + sv.AssignedParameter.b);
                    if (sv.Id == 2)
                        currentDevice.Parameters.Mode = (int)(rawValue * sv.AssignedParameter.a + sv.AssignedParameter.b);
                    if (sv.Id == 3)
                        currentDevice.Parameters.WorkTime = (int)(rawValue * sv.AssignedParameter.a + sv.AssignedParameter.b);
                    if (sv.Id == 4)
                        currentDevice.Parameters.StageTime = (int)(rawValue * sv.AssignedParameter.a + sv.AssignedParameter.b);
                    if (sv.Id == 5)
                        currentDevice.Parameters.Voltage = rawValue * sv.AssignedParameter.a + sv.AssignedParameter.b;
                    if (sv.Id == 6)
                        currentDevice.Parameters.FlameSensor = (int)(rawValue * sv.AssignedParameter.a + sv.AssignedParameter.b);
                    if (sv.Id == 7)
                        currentDevice.Parameters.BodyTemp = (int)(rawValue * sv.AssignedParameter.a + sv.AssignedParameter.b);
                    if (sv.Id == 8)
                        currentDevice.Parameters.PanelTemp = (int)(rawValue * sv.AssignedParameter.a + sv.AssignedParameter.b);
                    if (sv.Id == 10)
                        currentDevice.Parameters.InletTemp = (int)(rawValue * sv.AssignedParameter.a + sv.AssignedParameter.b);
                    if (sv.Id == 15)
                        currentDevice.Parameters.RevSet = (int)(rawValue * sv.AssignedParameter.a + sv.AssignedParameter.b);
                    if (sv.Id == 16)
                        currentDevice.Parameters.RevMeasured = (int)(rawValue * sv.AssignedParameter.a + sv.AssignedParameter.b);
                    if (sv.Id == 18)
                        currentDevice.Parameters.FuelPumpMeasured = (rawValue * sv.AssignedParameter.a + sv.AssignedParameter.b);
                    if (sv.Id == 21)
                        currentDevice.Parameters.GlowPlug = (int)(rawValue * sv.AssignedParameter.a + sv.AssignedParameter.b);
                    if (sv.Id == 24)
                        currentDevice.Parameters.Error = (int)(rawValue * sv.AssignedParameter.a + sv.AssignedParameter.b);
                    if (sv.Id == 40)
                        currentDevice.Parameters.LiquidTemp = (int)(rawValue * sv.AssignedParameter.a + sv.AssignedParameter.b);
                    if (sv.Id == 41)
                        currentDevice.Parameters.OverheatTemp = (int)(rawValue * sv.AssignedParameter.a + sv.AssignedParameter.b);





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
                        currentDevice.readedParameters.TryToAdd(new() { Id = parameterId, Value = parameterValue });
                        Debug.WriteLine($"{AC2P.ParamtersNames[parameterId]}={parameterValue}");
                    }
                    else
                        Debug.WriteLine($"Parameter \"{AC2P.ParamtersNames[parameterId]}\" not supported");
                    //Серийник в отдельной переменной
                    if (parameterId == 12)
                        currentDevice.Serial1 = parameterValue;
                    if (parameterId == 13)
                        currentDevice.Serial2 = parameterValue;
                    if (parameterId == 14)
                        currentDevice.Serial3 = parameterValue;

                    currentDevice.flagGetParamDone = true;
                }

            }

            if (m.PGN == 8) //Работа с ЧЯ
            {
                if (m.Data[0] == 4) // Обрабатываем только упешные ответы на запросы
                {
                    if (!WaitingForBBErrors)
                    {
                        int parameterId = m.Data[3] + m.Data[2] * 256;
                        uint parameterValue = ((uint)m.Data[4] * 0x1000000) + ((uint)m.Data[5] * 0x10000) + ((uint)m.Data[6] * 0x100) + (uint)m.Data[7];
                        if (parameterValue != 0xFFFFFFFF)
                            currentDevice.BBValues.TryToAdd(new ReadedBlackBoxValue() { Id = parameterId, Value = parameterValue });

                    }
                    else
                    {
                        if (m.Data[2] == 0xFF && m.Data[3] == 0xFA) //Заголовок отчёта
                        {
                            if (currentDevice.currentBBError.Variables.Count > 0)
                            {
                                BBError e = new BBError();
                                currentDevice.currentBBError = e;
                                currentDevice.BBErrors.Add(e);
                            }
                        }
                        else
                        {
                            if (currentDevice.currentBBError == null) return;
                            ReadedVariable v = new ReadedVariable();
                            v.Id = m.Data[2] * 256 + m.Data[3];
                            v.Value = m.Data[4] * 0x1000000 + m.Data[5] * 0x10000 + m.Data[6] * 0x100 + m.Data[7];
                            currentDevice.currentBBError.Variables.TryToAdd(v);
                        }
                    }
                    currentDevice.flagGetBBDone = true;
                }
            }


            //Переменные без VAR в paramsname.h
            if (m.PGN == 18) //Версия
            {
                currentDevice.Firmware = m.Data[0..4];
                if (m.Data[5] != 0xff && m.Data[6] != 0xff && m.Data[7] != 0xff)
                    try
                    {
                        currentDevice.ProductionDate = new DateOnly(m.Data[7] + 2000, m.Data[6], m.Data[5]);
                    }
                    catch { }
            }

            if (m.PGN == 33)
            {
                switch (m.Data[0])
                {
                    case 1:
                        currentDevice.Serial1 = (uint)(m.Data[1] * 0x1000000 + m.Data[2] * 0x10000 + m.Data[3] * 0x100 + m.Data[4]);
                        break;
                    case 2:
                        currentDevice.Serial2 = (uint)(m.Data[1] * 0x1000000 + m.Data[2] * 0x10000 + m.Data[3] * 0x100 + m.Data[4]);
                        break;
                    case 3:
                        currentDevice.Serial3 = (uint)(m.Data[1] * 0x1000000 + m.Data[2] * 0x10000 + m.Data[3] * 0x100 + m.Data[4]);
                        break;
                    default:
                        break;
                }

            }

            if (m.PGN == 100)
            {
                if (m.Data[0] == 1 && m.Data[1] == 1)
                {
                    Debug.WriteLine("Memory erase confirmed");
                    currentDevice.flagEraseDone = true;
                }
                if (m.Data[0] == 2 && m.Data[1] == 1)
                {
                    Debug.WriteLine("Adress set confirmed");
                    currentDevice.flagSetAdrDone = true;
                }

                if (m.Data[0] == 3 && m.Data[1] == 1)
                {
                    Debug.WriteLine("Flash program confirmed");
                    currentDevice.flagProgramDone = true;
                }
                if (m.Data[0] == 4)
                {
                    currentDevice.crc = m.Data[4] * 0x1000000 + m.Data[5] * 0x10000 + m.Data[6] * 0x100 + m.Data[7];
                    currentDevice.dataLength = m.Data[1] * 0x10000 + m.Data[2] * 0x100 + m.Data[3];
                    currentDevice.flagCheckDone = true;
                }
                if (m.Data[0] == 5)
                {
                    int adr = m.Data[2] * 0x1000000 + m.Data[3] * 0x10000 + m.Data[4] * 0x100 + m.Data[5];
                    Debug.WriteLine($"Adress set to 0X{adr:X}");
                    currentDevice.flagSetAdrDone = true;
                }
            }

            Messages.TryToAdd(m);

        }
        public static void ParseParamsname(string filePath = "paramsname.h")
        {
            Encoding.RegisterProvider(CodePagesEncodingProvider.Instance);
            StreamReader sr = new StreamReader(filePath, Encoding.GetEncoding(1251));
            while (!sr.EndOfStream)
            {
                string tempString = sr.ReadLine();
                List<string> tempList = new List<string>();

                if (tempString.StartsWith("#define PAR"))
                {
                    var p = new ConfigParameter();
                    tempString = tempString.Remove(0, 8);
                    p.NameEn = tempString.Substring(tempString.LastIndexOf('@') + 1);
                    p.NameRu = tempString.Substring(tempString.LastIndexOf("//") + 2, tempString.LastIndexOf('@') - tempString.LastIndexOf("//") - 2);
                    tempString = tempString.Remove(tempString.IndexOf('/'));
                    tempList = tempString.Split(' ').ToList();
                    p.StringId = tempList[0];
                    p.Id = int.Parse(tempList.Last());
                    configParameters.Add(p.Id, p);
                    ParamtersNames.Add(p.Id, p.StringId);
                }

                if (tempString.StartsWith("#define VAR"))
                {

                    Variable v = new Variable();
                    tempString = tempString.Remove(0, 8);

                    if (tempString.Split("//")[0].Split(' ').Last().ToUpper().StartsWith("0X"))
                        v.Id = Convert.ToInt32(tempString.Split("//")[0].Split(' ').Last(), 16);
                    else
                        v.Id = Convert.ToInt32(tempString.Split("//")[0].Split(' ').Last(), 10);
                    v.StringId = tempString.Split("//")[0].Split(' ')[0];
                    tempString = tempString.Split("//")[1];
                    var parts = tempString.Split(';');
                    v.VarType = parts[0].Trim();
                    v.Description = parts[1]?.Trim();
                    if (parts.Length > 2) v.Formula = parts[2]?.Trim();
                    if (parts.Length > 3) v.Format = parts[3]?.Trim();
                    if (parts.Length > 4) v.ShortName = parts[4]?.Trim();
                    Variables.Add(v.Id, v);
                    VariablesNames.Add(v.Id, v.ShortName);
                }

                if (tempString.StartsWith("#define BB"))
                {

                    var b = new BbParameter();
                    tempString = tempString.Remove(0, 8);

                    b.Id = Convert.ToInt32(tempString.Split("//")[0].Split(' ').Last(), 10);
                    b.StringId = tempString.Split("//")[0].Split(' ')[0];
                    b.Description = tempString.Split("//")[1].Trim();
                    BbParameters.Add(b.Id, b);
                    BbParameterNames.Add(b.Id, b.Description);
                }
            }
        }

        public async void ReadBlackBoxData(DeviceId id)
        {
            if (!Capture("Чтение параметров чёрного ящика")) return;
            ConnectedDevice currentDevice = _connectedDevices.FirstOrDefault(d => d.ID == id);
            if (currentDevice == null) return;
            WaitingForBBErrors = false;
            AC2PMessage msg = new()
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
            foreach (var p in BbParameters)
            {

                msg.Data[4] = (byte)(p.Key / 256);
                msg.Data[5] = (byte)(p.Key % 256);
                currentDevice.flagGetBBDone = false;
                SendMessage(msg);
                Debug.WriteLine($"Requesting BB parameter {p.Key}");
                for (int i = 0; i < 100; i++)
                {
                    if (currentDevice.flagGetBBDone) break;
                    await Task.Delay(1);
                    Debug.WriteLineIf(i == 99, $"Error reading parameter {p.Key} ({AC2P.BbParameterNames[p.Key]})");
                }
                if (CancellationRequested)
                {
                    Cancel();
                    return;
                }
                UpdatePercent(100 * counter++ / BbParameters.Count);
            }
            Done();
        }

        public async void ReadErrorsBlackBox(DeviceId id)
        {
            if (!Capture("Чтение ошибок из чёрного ящика")) return;
            WaitingForBBErrors = true;

            ConnectedDevice currentDevice = ConnectedDevices.FirstOrDefault(i => i.ID.Equals(id));

            UIContext.Send(x => currentDevice.BBErrors.Clear(), null);
            UIContext.Send(x => currentDevice.currentBBError = new BBError(), null);
            UIContext.Send(x => currentDevice.BBErrors.Add(currentDevice.currentBBError), null);


            AC2PMessage msg = new AC2PMessage
            {
                PGN = 8,
                TransmitterAddress = 6,
                TransmitterType = 126,
                ReceiverAddress = id.Address,
                ReceiverType = id.Type
            };
            msg.Data[0] = 0x13; //Read Errors
            msg.Data[1] = 0xFF;
            for (int i = 0; i < 512; i++)
            {
                msg.Data[4] = (byte)(i / 256);  //Pair count
                msg.Data[5] = (byte)(i % 256);  //Pair count
                msg.Data[6] = 0x00; //Pair count MSB
                msg.Data[7] = 0x01; //Pair count LSB

                currentDevice.flagGetBBDone = false;
                SendMessage(msg);

                for (int j = 0; j < 100; j++)
                {
                    if (currentDevice.flagGetBBDone) break;
                    await Task.Delay(1);
                    Debug.WriteLineIf(j == 99, $"Error reading BB adress {i}");
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

            AC2PMessage msg = new AC2PMessage();
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

            AC2PMessage msg = new AC2PMessage();
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

            foreach (var p in configParameters)
            {
                AC2PMessage msg = new();
                msg.PGN = 7;
                msg.TransmitterAddress = 6;
                msg.TransmitterType = 126;
                msg.ReceiverAddress = id.Address;
                msg.ReceiverType = id.Type;
                msg.Data[0] = 3; //Read Param
                msg.Data[1] = 0xFF; //Read Param
                msg.Data[2] = (byte)(p.Key / 256);
                msg.Data[3] = (byte)(p.Key % 256);

                currentDevice.flagGetParamDone = false;
                SendMessage(msg);


                for (int j = 0; j < 100; j++)
                {
                    if (currentDevice.flagGetParamDone)
                        break;
                    await Task.Delay(1);
                    Debug.WriteLineIf(j == 99, $"Error reading parameter {p.Key} ({AC2P.ParamtersNames[p.Key]})");
                }
                UpdatePercent(cnt++ * 100 / configParameters.Count);
                if (CancellationRequested)
                {
                    Cancel();
                    return;
                }
            }
            Done();
        }

        public async void SaveParameters(DeviceId id)
        {
            if (!Capture("Сохранение параметров во Flash")) return;
            var dev = ConnectedDevices.FirstOrDefault(d => d.ID.Equals(id));
            if (dev == null) return;
            AC2PMessage msg = new AC2PMessage();
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
            AC2PMessage msg = new AC2PMessage();

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

        public void SendMessage(AC2PMessage m)
        {
            Debug.WriteLine("-> " + (new AC2PMessage(m)).ToString());
            canAdapter.Transmit(m);
        }
        public void SendMessage(DeviceId from, DeviceId to, int pgn, byte[] data)
        {
            AC2PMessage msg = new AC2PMessage();
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
            AC2PMessage message = new AC2PMessage();
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

        public AC2P(CanAdapter adapter)
        {
            UIContext = SynchronizationContext.Current;
            if (adapter == null) throw new ArgumentNullException("Can Adapter reference can't be null");
            canAdapter = adapter;
            adapter.GotNewMessage += Adapter_GotNewMessage;
            SeedStaticData();
            ParseParamsname();

        }

        private void Adapter_GotNewMessage(object sender, EventArgs e)
        {
            UIContext.Send(x => ProcessCanMessage((e as GotMessageEventArgs).receivedMessage), null);
        }

        public static void SeedStaticData()
        {
            #region Device names init
            Devices = new Dictionary<int, Device>() {
            { 0, new (){ID=0,Name="Любой" } } ,
            { 1, new (){ID=1,Name="14ТС-Мини",DevType=DeviceType.Binar } } ,
            { 2, new (){ID=2,Name="Планар 2" ,DevType=DeviceType.Planar}} ,
            { 3, new (){ID=3,Name="Планар 44Д",DevType=DeviceType.Planar }} ,
            { 4, new (){ID=4,Name="30ТСД", DevType=DeviceType.Binar}} ,
            { 5, new (){ID=5,Name="30ТСГ", DevType=DeviceType.Binar }} ,
            { 6, new (){ID=6,Name="Binar-5S B" , DevType=DeviceType.Binar}} ,
            { 7, new (){ID=7,Name="Планар 8Д" ,DevType=DeviceType.Planar}} ,
            { 8, new (){ID=8,Name="OB-8" , DevType=DeviceType.Binar}} ,
            { 9, new (){ID=9,Name="Планар 4Д",DevType=DeviceType.Planar }} ,
            { 10, new (){ID=10,Name="Binar-5S D" , DevType=DeviceType.Binar}} ,
            { 11, new (){ID=11,Name="Планар-9Д, ОВ-8ДК",DevType=DeviceType.Planar }} ,
            { 12, new (){ID=12,Name="Планар-44Б",DevType=DeviceType.Planar }} ,
            { 13, new (){ID=13,Name="Планар-4Б",DevType=DeviceType.Planar }} ,
            { 14, new (){ID=14,Name="Плита" , DevType=DeviceType.Stove}} ,
            { 15, new (){ID=15,Name="Планар-44Г",DevType=DeviceType.Planar }} ,
            { 16, new (){ID=16,Name="ОВ-4" , DevType=DeviceType.Binar}} ,
            { 17, new (){ID=17,Name="14ТСД-10", DevType=DeviceType.Binar }} ,
            { 18, new (){ID=18,Name="Планар 2Б",DevType=DeviceType.Planar }} ,
            { 19, new (){ID=19,Name="Блок управления клапанами." , DevType=DeviceType.ValveControl}} ,
            { 20, new (){ID=20,Name="Планар-6Д" ,DevType=DeviceType.Planar}} ,
            { 21, new (){ID=21,Name="14ТС-10" , DevType=DeviceType.Binar}} ,
            { 22, new (){ID=22,Name="30SP (впрысковый)" , DevType=DeviceType.Binar}} ,
            { 23, new (){ID=23,Name="Бинар 5Б-Компакт" , DevType=DeviceType.Binar,MaxBlower=90,MaxFuelPump=4}} ,
            { 25, new (){ID=25,Name="35SP (впрысковый)", DevType=DeviceType.Binar }} ,
            { 27, new (){ID=27,Name="Бинар 5Д-Компакт", DevType=DeviceType.Binar, MaxBlower=90,MaxFuelPump=4}} ,
            { 29, new (){ID=29,Name="Бинар 6Г-Компакт" , DevType=DeviceType.Binar}} ,
            { 31, new (){ID=31,Name="14ТСГ-Мини", DevType=DeviceType.Binar }} ,
            { 32, new (){ID=32,Name="30SPG (на базе 30SP)", DevType=DeviceType.Binar }} ,
            { 34, new (){ID=34,Name="Binar-10Д" , DevType=DeviceType.Binar, MaxBlower=90}} ,
            { 35, new (){ID=35,Name="Binar-10Б" , DevType=DeviceType.Binar, MaxBlower=90}} ,
            { 123, new (){ID=123,Name="Bootloader", DevType=DeviceType.Bootloader }} ,
            { 126, new (){ID=126,Name="Устройство управления", DevType=DeviceType.HCU }},
            { 255, new (){ID=255,Name="Не задано" }}
        };
            #endregion

            #region PGN names init
            PGNs.Add(0, new() { id = 0, name = "Пустая команда" });
            PGNs.Add(1, new() { id = 1, name = "Комманда управления" });
            PGNs.Add(2, new() { id = 2, name = "Подтверждение на принятую комманду" });
            PGNs.Add(3, new() { id = 3, name = "Запрос параметра или набора данных по определенному номеру (SPN)" });
            PGNs.Add(4, new() { id = 4, name = "Ответ на запрос параметра или набора данных по определенному номеру (SPN)" });
            PGNs.Add(5, new() { id = 5, name = "Запись параметра или набора данных устройства" });
            PGNs.Add(6, new() { id = 6, name = "Запрос параметров по PGN" });
            PGNs.Add(7, new() { id = 7, name = "Запись/чтение параметров работы (конигурация) в/из flash-памяти" });
            PGNs.Add(8, new() { id = 8, name = "Работа с ЧЯ" });
            PGNs.Add(10, new() { id = 10, name = "Стадия, режим, код неисправности, код предупреждения" });
            PGNs.Add(11, new() { id = 11, name = "Напряжение питания, атмосферное давление, ток двигателя" });
            PGNs.Add(12, new() { id = 12, name = "Обороты НВ, частота ТН, свеча, реле" });
            PGNs.Add(13, new() { id = 13, name = "Температуры жидкостных подогревателей" });
            PGNs.Add(14, new() { id = 14, name = "Слежение за пламенем" });
            PGNs.Add(15, new() { id = 15, name = "АЦП 0-3 каналы" });
            PGNs.Add(16, new() { id = 16, name = "АЦП 4-7 каналы" });
            PGNs.Add(17, new() { id = 17, name = "АЦП 8-11 каналы" });
            PGNs.Add(18, new() { id = 18, name = "Версия и дата программного обеспечения" });
            PGNs.Add(19, new() { id = 19, name = "Параметры от центрального блока управления для подогревателя в системе отопления", multipack = true });
            PGNs.Add(20, new() { id = 20, name = "Неисправности" });
            PGNs.Add(21, new() { id = 21, name = "Блок управления системой отопления" });
            PGNs.Add(22, new() { id = 22, name = "Блок управления системой отопления" });
            PGNs.Add(23, new() { id = 23, name = "Блок управления системой отопления" });
            PGNs.Add(24, new() { id = 24, name = "Блок управления системой отопления" });
            PGNs.Add(25, new() { id = 25, name = "Блок управления системой отопления" });
            PGNs.Add(26, new() { id = 26, name = "Блок управления системой отопления" });
            PGNs.Add(27, new() { id = 27, name = "Блок управления системой отопления" });
            PGNs.Add(28, new() { id = 28, name = "Общее время наработки подогревателя" });
            PGNs.Add(29, new() { id = 29, name = "Параметры давления", multipack = true });
            PGNs.Add(30, new() { id = 30, name = "Состояние сигнализации, двигателя автомобиля. Температура датчика воздуха. Напряжение канала двигателя автомобиля" });
            PGNs.Add(31, new() { id = 31, name = "Время работы" });
            PGNs.Add(32, new() { id = 32, name = "Параметры работы жидкостного подогревателя" });
            PGNs.Add(33, new() { id = 33, name = "Серийный номер изделия (мультипакет)", multipack = true });
            PGNs.Add(34, new() { id = 34, name = "Считать данные из flash по адресу" });
            PGNs.Add(35, new() { id = 35, name = "Передача данных на запрос по PGN 34" });
            PGNs.Add(36, new() { id = 36, name = "Передача состояния клапанов, зонда, кода неисправности клапанов" });
            PGNs.Add(37, new() { id = 37, name = "Температуры воздушных отопителей (мультипакет)", multipack = true });
            PGNs.Add(38, new() { id = 38, name = "Температура датчика в пульте" });
            PGNs.Add(39, new() { id = 39, name = "Статусы драйверов ТН, свечи, помпа, реле" });
            PGNs.Add(100, new() { id = 100, name = "Управления памятью (Мультипакет)", multipack = true });
            PGNs.Add(101, new() { id = 101, name = "Заполнение буферного массива для последующей записи во флэш" });
            #endregion

            #region Commands init
            commands.Add(0, new() { Id = 0, Name = "Кто здесь?" });
            commands.Add(1, new AC2PCommand() { Id = 1, Name = "пуск устройства" });
            commands.Add(3, new AC2PCommand() { Id = 3, Name = "остановка устройства" });
            commands.Add(4, new AC2PCommand() { Id = 4, Name = "пуск только помпы" });
            commands.Add(5, new AC2PCommand() { Id = 5, Name = "сброс неисправностей" });
            commands.Add(6, new AC2PCommand() { Id = 6, Name = "задать параметры работы жидкостного подогревателя" });
            commands.Add(7, new AC2PCommand() { Id = 7, Name = "запрос температурных переходов по режимам жидкостного подогревателя" });
            commands.Add(8, new AC2PCommand() { Id = 8, Name = "задать состояние клапанов устройства ”Блок управления клапанами”" });
            commands.Add(9, new AC2PCommand() { Id = 9, Name = "задать параметры работы воздушного отопителя" });
            commands.Add(10, new AC2PCommand() { Id = 10, Name = "запуск в режиме вентиляции (для воздушных отопителей)" });
            commands.Add(20, new AC2PCommand() { Id = 20, Name = "калибровка термопар" });
            commands.Add(21, new AC2PCommand() { Id = 21, Name = "задать параметры частоты ШИМ нагнетателя воздуха" });
            commands.Add(22, new AC2PCommand() { Id = 22, Name = "Reset CPU" });
            commands.Add(45, new AC2PCommand() { Id = 45, Name = "биты реакции на неисправности" });
            commands.Add(65, new AC2PCommand() { Id = 65, Name = "установить значение температуры" });
            commands.Add(66, new AC2PCommand() { Id = 66, Name = "сброс неисправностей" });
            commands.Add(67, new AC2PCommand() { Id = 67, Name = "вход/выход в стадию M (ручное управление) или T (тестирование блока управления)" });
            commands.Add(68, new AC2PCommand() { Id = 68, Name = "задание параметров устройств в стадии M (ручное управление)" });
            commands.Add(69, new AC2PCommand() { Id = 69, Name = "управление устройствами" });
            commands.Add(70, new AC2PCommand() { Id = 69, Name = "Включение/Выключение устройств" });
            #endregion

            #region Command parameters init
            commands[0].Parameters.Add(new() { StartByte = 2, BitLength = 8, GetMeaning = i => ("Устройство: " + Devices[i].Name + ";"), AnswerOnly = true }); ;
            commands[0].Parameters.Add(new() { StartByte = 3, BitLength = 8, Meanings = { { 0, "12 Вольт" }, { 1, "24 Вольта" } }, AnswerOnly = true });
            commands[0].Parameters.Add(new() { StartByte = 4, BitLength = 8, Name = "Верия ПО", AnswerOnly = true });
            commands[0].Parameters.Add(new() { StartByte = 5, BitLength = 8, Name = "Модификация ПО", AnswerOnly = true });

            commands[0].Parameters.Add(new() { StartByte = 2, BitLength = 16, Name = "Время работы", Unit = "мин" });

            commands[0].Parameters.Add(new() { StartByte = 2, BitLength = 16, Name = "Время работы", Unit = "мин" });

            commands[6].Parameters.Add(new() { StartByte = 2, BitLength = 16, Name = "Время работы", Unit = "мин" });
            commands[6].Parameters.Add(new() { StartByte = 4, BitLength = 4, Name = "Режим работы", Meanings = { { 0, "обычный" }, { 1, "экономичный" }, { 2, "догреватель" }, { 3, "отопление" }, { 4, "отопительные системы" } } });
            commands[6].Parameters.Add(new() { StartByte = 4, StartBit = 4, BitLength = 4, Name = "Режим догрева", Meanings = { { 0, "отключен" }, { 1, "автоматический" }, { 2, "ручной" } } });
            commands[6].Parameters.Add(new() { StartByte = 5, BitLength = 16, Name = "Уставка температуры", Unit = "°С" });
            commands[6].Parameters.Add(new() { StartByte = 7, BitLength = 2, Name = "Работа помпы в ждущем режиме", Meanings = defMeaningsOnOff });
            commands[6].Parameters.Add(new() { StartByte = 7, BitLength = 2, StartBit = 2, Name = "Работа помпы при заведённом двигателе", Meanings = defMeaningsOnOff });

            commands[7].Parameters.Add(new() { StartByte = 2, BitLength = 8, Name = "Номер мощности" });

            commands[7].Parameters.Add(new() { StartByte = 3, BitLength = 16, Name = "Температура перехода на большую мощность", AnswerOnly = true });
            commands[7].Parameters.Add(new() { StartByte = 4, BitLength = 16, Name = "Температура перехода на меньшую мощность", AnswerOnly = true });

            commands[8].Parameters.Add(new() { StartByte = 2, BitLength = 2, StartBit = 0, Name = "Состояние клапана 1", Meanings = defMeaningsOnOff });
            commands[8].Parameters.Add(new() { StartByte = 2, BitLength = 2, StartBit = 2, Name = "Состояние клапана 2", Meanings = defMeaningsOnOff });
            commands[8].Parameters.Add(new() { StartByte = 2, BitLength = 2, StartBit = 4, Name = "Состояние клапана 3", Meanings = defMeaningsOnOff });
            commands[8].Parameters.Add(new() { StartByte = 2, BitLength = 2, StartBit = 6, Name = "Состояние клапана 4", Meanings = defMeaningsOnOff });
            commands[8].Parameters.Add(new() { StartByte = 3, BitLength = 2, StartBit = 0, Name = "Состояние клапана 5", Meanings = defMeaningsOnOff });
            commands[8].Parameters.Add(new() { StartByte = 3, BitLength = 2, StartBit = 2, Name = "Состояние клапана 6", Meanings = defMeaningsOnOff });
            commands[8].Parameters.Add(new() { StartByte = 3, BitLength = 2, StartBit = 4, Name = "Состояние клапана 7", Meanings = defMeaningsOnOff });
            commands[8].Parameters.Add(new() { StartByte = 3, BitLength = 2, StartBit = 6, Name = "Состояние клапана 8", Meanings = defMeaningsOnOff });
            commands[8].Parameters.Add(new() { StartByte = 4, BitLength = 1, StartBit = 0, Meanings = { { 0, "Сбросить неисправности" } } });

            commands[9].Parameters.Add(new() { StartByte = 2, BitLength = 16, Name = "Время работы", Unit = "мин" });
            commands[9].Parameters.Add(new() { StartByte = 4, BitLength = 4, Name = "Режим работы", Meanings = { { 0, "не используется" }, { 1, "работа по температуре платы" }, { 2, "работа по температуре пульта" }, { 3, "работа по температуре выносного датчика" }, { 4, "работа по мощности" } } });
            commands[9].Parameters.Add(new() { StartByte = 4, StartBit = 4, BitLength = 2, Name = "Разрешение/запрещение ждущего режима (при работе по датчику температуры)", Meanings = defMeaningsAllow });
            commands[9].Parameters.Add(new() { StartByte = 4, StartBit = 6, BitLength = 2, Name = "Разрешение вращения нагнетателя воздуха на ждущем режиме", Meanings = defMeaningsAllow });
            commands[9].Parameters.Add(new() { StartByte = 5, BitLength = 16, Name = "Уставка температуры помещения", Unit = "°С" });
            commands[9].Parameters.Add(new() { StartByte = 7, BitLength = 4, Name = "Заданное значение мощности" });

            commands[10].Parameters.Add(new() { StartByte = 2, BitLength = 16, Name = "Время работы", Unit = "мин" });

            commands[20].Parameters.Add(new() { StartByte = 2, BitLength = 16, Name = "Калибровочное значение термопары 1", AnswerOnly = true });
            commands[20].Parameters.Add(new() { StartByte = 4, BitLength = 16, Name = "Калибровочное значение термопары 2", AnswerOnly = true });

            commands[21].Parameters.Add(new() { StartByte = 2, BitLength = 8, Name = "Предделитель" });
            commands[21].Parameters.Add(new() { StartByte = 3, BitLength = 8, Name = "Период ШИМ" });
            commands[21].Parameters.Add(new() { StartByte = 5, BitLength = 8, Name = "Требуемая частота", Unit = "Гц" });

            commands[22].Parameters.Add(new() { StartByte = 2, StartBit = 0, BitLength = 8, Name = "Действие после перезагрузки", Meanings = { { 0, "Остаться в загрузчике" }, { 1, "Переход в основную программу без зедержки" }, { 2, "5 секунд в загрузчике" } } });

            commands[45].Parameters.Add(new() { StartByte = 2, StartBit = 0, BitLength = 2, Name = "Игнорирование всех неисправностей", Meanings = defMeaningsYesNo });
            commands[45].Parameters.Add(new() { StartByte = 2, StartBit = 2, BitLength = 2, Name = "Игнорирование неисправностей ТН", Meanings = defMeaningsYesNo });
            commands[45].Parameters.Add(new() { StartByte = 2, StartBit = 4, BitLength = 2, Name = "Игнорирование срывов пламени ", Meanings = defMeaningsYesNo });
            commands[45].Parameters.Add(new() { StartByte = 2, StartBit = 6, BitLength = 2, Name = "Игнорирование неисправностей свечи", Meanings = defMeaningsYesNo });
            commands[45].Parameters.Add(new() { StartByte = 3, StartBit = 0, BitLength = 2, Name = "Игнорирование неисправностей НВ", Meanings = defMeaningsYesNo });
            commands[45].Parameters.Add(new() { StartByte = 3, StartBit = 2, BitLength = 2, Name = "Игнорирование неисправностей датчиков", Meanings = defMeaningsYesNo });
            commands[45].Parameters.Add(new() { StartByte = 3, StartBit = 4, BitLength = 2, Name = "Игнорирование неисправностей помпы", Meanings = defMeaningsYesNo });
            commands[45].Parameters.Add(new() { StartByte = 3, StartBit = 6, BitLength = 2, Name = "Игнорирование перегревов", Meanings = defMeaningsYesNo });

            commands[65].Parameters.Add(new() { StartByte = 2, StartBit = 0, BitLength = 8, Name = "", Meanings = { { 7, "Температура жидкости" }, { 10, "Температура перегрева" }, { 12, "Температура пламени" }, { 13, "Температура корпуса" }, { 27, "Температура воздуха" } } });
            commands[65].Parameters.Add(new() { StartByte = 3, BitLength = 16, Name = "Значение температуры", Unit = "°C" });

            commands[67].Parameters.Add(new() { StartByte = 2, StartBit = 0, BitLength = 2, Name = "", Meanings = { { 0, "Выход из режима М" }, { 1, "Вход в режим М" } } });
            commands[67].Parameters.Add(new() { StartByte = 2, StartBit = 2, BitLength = 2, Name = "", Meanings = { { 0, "Выход из режима Т" }, { 1, "Вход в режим Т" } } });

            commands[68].Parameters.Add(new() { StartByte = 2, StartBit = 0, BitLength = 2, Name = "Состояние помпы", Meanings = defMeaningsOnOff });
            commands[68].Parameters.Add(new() { StartByte = 3, StartBit = 0, BitLength = 8, Name = "Обороты нагнетателя", Unit = "об/с" });
            commands[68].Parameters.Add(new() { StartByte = 4, StartBit = 0, BitLength = 8, Name = "Мощность свечи", Unit = "%" });
            commands[68].Parameters.Add(new() { StartByte = 5, StartBit = 0, BitLength = 16, Name = "Частота ТН", a = 0.01, Unit = "Гц" });

            commands[69].Parameters.Add(new() { StartByte = 2, StartBit = 0, BitLength = 8, Name = "Тип устройства", Meanings = { { 0, "ТН, Гц*10" }, { 1, "Реле(0/1)" }, { 2, "Свеча, %" }, { 3, "Помпа,%" }, { 4, "Шим НВ,%" }, { 23, "Обороты НВ, об/с" } } });
            commands[69].Parameters.Add(new() { StartByte = 3, StartBit = 0, BitLength = 16, Name = "Значение" });

            commands[70].Parameters.Add(new() { StartByte = 2, StartBit = 0, BitLength = 2, Name = "Состояние ТН", Meanings = defMeaningsOnOff });
            commands[70].Parameters.Add(new() { StartByte = 2, StartBit = 2, BitLength = 2, Name = "Состояние реле", Meanings = defMeaningsOnOff });
            commands[70].Parameters.Add(new() { StartByte = 2, StartBit = 4, BitLength = 2, Name = "Состояние свечи", Meanings = defMeaningsOnOff });
            commands[70].Parameters.Add(new() { StartByte = 2, StartBit = 6, BitLength = 2, Name = "Состояние помпы", Meanings = defMeaningsOnOff });
            commands[70].Parameters.Add(new() { StartByte = 3, StartBit = 0, BitLength = 2, Name = "Состояние НВ", Meanings = defMeaningsOnOff });
            #endregion

            #region PGN parameters initialise

            PGNs[3].parameters.Add(new() { Name = "SPN", BitLength = 16, StartByte = 0 });

            PGNs[4].parameters.Add(new() { Name = "SPN", BitLength = 16, StartByte = 0 });
            PGNs[4].parameters.Add(new() { Name = "Значение", BitLength = 32, StartBit = 0, StartByte = 2 });

            PGNs[5].parameters.Add(new() { Name = "SPN", BitLength = 16, StartByte = 0 });
            PGNs[5].parameters.Add(new() { Name = "Значение", BitLength = 32, StartBit = 0, StartByte = 2 });

            PGNs[6].parameters.Add(new() { Name = "PGN", BitLength = 16, StartByte = 0, GetMeaning = x => { if (PGNs.ContainsKey(x)) return PGNs[x].name; else return "Нет такого PGN"; } });

            PGNs[7].parameters.Add(new() { Name = "Команда", BitLength = 8, StartBit = 0, StartByte = 0, Meanings = { { 0, "Стереть конфигурацию" }, { 1, "Запись параметра в ОЗУ" }, { 2, "Запись всех параметров во Flash" }, { 3, "Чтение параметра по номеру" }, { 4, "Успешный ответ на запрос" }, { 5, "Невозможно выполнить" } } });
            PGNs[7].parameters.Add(new() { Name = "Запрошенная команда", BitLength = 8, StartBit = 0, StartByte = 1, Meanings = { { 0, "Стереть конфигурацию" }, { 1, "Запись параметра в ОЗУ" }, { 2, "Запись всех параметров во Flash" }, { 3, "Чтение параметра по номеру" }, { 255, "" } } });
            PGNs[7].parameters.Add(new() { Name = "Параметр", BitLength = 16, StartBit = 0, StartByte = 2, GetMeaning = x => configParameters[x]?.NameRu });
            PGNs[7].parameters.Add(new() { Name = "Value", BitLength = 32, StartBit = 0, StartByte = 4, AnswerOnly = true });

            PGNs[8].parameters.Add(new() { Name = "Команда", BitLength = 4, StartBit = 0, StartByte = 0, Meanings = { { 0, "Стереть ЧЯ" }, { 3, "Чтение ЧЯ" }, { 4, "Ответ" }, { 6, "Чтение параметра (из paramsname.h)" } } });
            PGNs[8].parameters.Add(new() { Name = "Тип:", BitLength = 2, StartBit = 4, StartByte = 0, Meanings = { { 0, "Общие данные" }, { 1, "Неисправности" } } });
            PGNs[8].parameters.Add(new() { Name = "Номер пары", CustomDecoder = d => { if ((d[0] & 0xF) == 3) return "Номер пары:" + (d[4] * 0x100 + d[5]).ToString() + ";"; else return ""; } });
            PGNs[8].parameters.Add(new() { Name = "Номер параметра", CustomDecoder = d => { if ((d[0] & 0xF) == 6) return "Номер параметра:" + (d[4] * 0x100 + d[5]).ToString() + ";"; else return ""; } });
            PGNs[8].parameters.Add(new() { Name = "Число пар", CustomDecoder = d => { if ((d[0] & 0xF) == 6) return "Запрошено пар:" + (d[6] * 0x100 + d[7]).ToString() + ";"; else return ""; } });
            PGNs[8].parameters.Add(new() { Name = "Номер параметра", CustomDecoder = d => { if (d[0] == 4) return "Параметр:" + (d[2] * 256 + d[3]).ToString() + ";"; else return ""; } });
            PGNs[8].parameters.Add(new() { Name = "Значение параметра", CustomDecoder = d => { if (d[0] == 4) return "Значение:" + (d[4] * 0x1000000 + d[5] * 0x10000 + d[6] * 0x100 + d[7]).ToString() + ";"; else return ""; }, AnswerOnly = true });


            PGNs[10].parameters.Add(new() { Name = "Стадия", BitLength = 8, StartByte = 0, Meanings = Stages, Var = 1 });
            PGNs[10].parameters.Add(new() { Name = "Режим", BitLength = 8, StartByte = 1, Var = 2 });
            PGNs[10].parameters.Add(new() { Name = "Код неисправности", BitLength = 8, StartByte = 2, Var = 24, Meanings = ErrorNames });
            PGNs[10].parameters.Add(new() { Name = "Помпа неисправна", BitLength = 2, StartByte = 3, Meanings = defMeaningsYesNo });
            PGNs[10].parameters.Add(new() { Name = "Код предупреждения", BitLength = 8, StartByte = 4 });
            PGNs[10].parameters.Add(new() { Name = "Количество морганий", BitLength = 8, StartByte = 5, Var = 25 });

            PGNs[11].parameters.Add(new() { Name = "Напряжение питания", BitLength = 16, StartByte = 0, a = 0.1, Unit = "В", Var = 5 });
            PGNs[11].parameters.Add(new() { Name = "Атмосферное давление", BitLength = 8, StartByte = 2, Unit = "кПа" });
            PGNs[11].parameters.Add(new() { Name = "Ток двигателя, значения АЦП", BitLength = 16, StartByte = 3 });

            PGNs[12].parameters.Add(new() { Name = "Заданные обороты нагнетателя воздуха", BitLength = 8, StartByte = 0, Unit = "об/с", Var = 15 });
            PGNs[12].parameters.Add(new() { Name = "Измеренные обороты нагнетателя воздуха,", BitLength = 8, StartByte = 1, Unit = "об/с", Var = 16 });
            PGNs[12].parameters.Add(new() { Name = "Заданная частота ТН", BitLength = 16, StartByte = 2, a = 0.01, Unit = "Гц", Var = 17 });
            PGNs[12].parameters.Add(new() { Name = "Реализованная частота ТН", BitLength = 16, StartByte = 4, a = 0.01, Unit = "Гц", Var = 18 });
            PGNs[12].parameters.Add(new() { Name = "Мощность свечи", BitLength = 8, StartByte = 6, Unit = "%", Var = 21 });
            PGNs[12].parameters.Add(new() { Name = "Состояние помпы", BitLength = 2, StartByte = 7, Meanings = defMeaningsOnOff, Var = 46 });
            PGNs[12].parameters.Add(new() { Name = "Состояние реле печки кабины", BitLength = 2, StartByte = 7, StartBit = 2, Meanings = defMeaningsOnOff, Var = 45 });
            PGNs[12].parameters.Add(new() { Name = "Состояние состояние канала сигнализации", BitLength = 2, StartByte = 7, StartBit = 4, Meanings = defMeaningsOnOff, Var = 47 });

            PGNs[13].parameters.Add(new() { Name = "Температура ИП", BitLength = 16, StartByte = 0, Unit = "°C", Var = 6 });
            PGNs[13].parameters.Add(new() { Name = "Температура платы/процессора", BitLength = 8, StartByte = 2, b = -75, Unit = "°C", Var = 59 });
            PGNs[13].parameters.Add(new() { Name = "Температура жидкости", BitLength = 8, StartByte = 3, b = -75, Unit = "°C", Var = 40 });
            PGNs[13].parameters.Add(new() { Name = "Температура перегрева", BitLength = 8, StartByte = 4, b = -75, Unit = "°C", Var = 41 });

            PGNs[14].parameters.Add(new() { Name = "Минимальная температура пламени перед розжигом", BitLength = 16, StartByte = 0, Unit = "°C", Var = 36, Signed = true });
            PGNs[14].parameters.Add(new() { Name = "Граница срыва пламени", BitLength = 16, StartByte = 2, Unit = "°C", Var = 37, Signed = true });
            PGNs[14].parameters.Add(new() { Name = "Граница срыва пламени на прогреве", BitLength = 16, StartByte = 4, Unit = "°C", Signed = true });
            PGNs[14].parameters.Add(new() { Name = "Скорость изменения температуры ИП", BitLength = 16, StartByte = 6, Unit = "°C", Signed = true });


            PGNs[15].parameters.Add(new() { Name = "0 канал АЦП ", BitLength = 16, StartByte = 0, Var = 49 });
            PGNs[15].parameters.Add(new() { Name = "1 канал АЦП ", BitLength = 16, StartByte = 2, Var = 50 });
            PGNs[15].parameters.Add(new() { Name = "2 канал АЦП ", BitLength = 16, StartByte = 4, Var = 51 });
            PGNs[15].parameters.Add(new() { Name = "3 канал АЦП ", BitLength = 16, StartByte = 6, Var = 52 });

            PGNs[16].parameters.Add(new() { Name = "4 канал АЦП ", BitLength = 16, StartByte = 0, Var = 53 });
            PGNs[16].parameters.Add(new() { Name = "5 канал АЦП ", BitLength = 16, StartByte = 2, Var = 54 });
            PGNs[16].parameters.Add(new() { Name = "6 канал АЦП ", BitLength = 16, StartByte = 4, Var = 55 });
            PGNs[16].parameters.Add(new() { Name = "7 канал АЦП ", BitLength = 16, StartByte = 6, Var = 56 });

            PGNs[17].parameters.Add(new() { Name = "8 канал АЦП ", BitLength = 16, StartByte = 0, Var = 57 });
            PGNs[17].parameters.Add(new() { Name = "9 канал АЦП ", BitLength = 16, StartByte = 2, Var = 58 });
            PGNs[17].parameters.Add(new() { Name = "10 канал АЦП ", BitLength = 16, StartByte = 4 });
            PGNs[17].parameters.Add(new() { Name = "11 канал АЦП ", BitLength = 16, StartByte = 6 });

            PGNs[18].parameters.Add(new() { Name = "Вид изделия", BitLength = 8, StartByte = 0, GetMeaning = i => Devices[i]?.Name });
            PGNs[18].parameters.Add(new() { Name = "Напряжение питания", BitLength = 8, StartByte = 1, Meanings = { { 0, "12 Вольт" }, { 1, "24 Вольта" } } });
            PGNs[18].parameters.Add(new() { Name = "Версия ПО", BitLength = 8, StartByte = 2 });
            PGNs[18].parameters.Add(new() { Name = "Модификация ПО", BitLength = 8, StartByte = 3 });
            PGNs[18].parameters.Add(new() { Name = "Дата релиза", BitLength = 24, StartByte = 5, GetMeaning = v => $"{v >> 16}.{(v >> 8) & 0xF}.{v & 0xFF}" });
            //PGNs[18].parameters.Add(new () { Name = "День ", BitLength = 8, StartByte = 5 });    Не красиво выглядит...луше одной строкой
            //PGNs[18].parameters.Add(new () { Name = "Месяц", BitLength = 8, StartByte = 6 });
            //PGNs[18].parameters.Add(new () { Name = "Год", BitLength = 8, StartByte = 7 });

            PGNs[19].parameters.Add(new() { Name = "Подогреватель", BitLength = 2, StartBit = 0, StartByte = 1, PackNumber = 1, Meanings = defMeaningsOnOff });
            PGNs[19].parameters.Add(new() { Name = "Помпы", BitLength = 2, StartBit = 2, StartByte = 1, PackNumber = 1, Meanings = defMeaningsOnOff });
            PGNs[19].parameters.Add(new() { Name = "Вода", BitLength = 2, StartBit = 4, StartByte = 1, PackNumber = 1, Meanings = defMeaningsOnOff });
            PGNs[19].parameters.Add(new() { Name = "Быстрый нагрев воды", BitLength = 2, StartBit = 6, StartByte = 1, PackNumber = 1, Meanings = defMeaningsOnOff });
            PGNs[19].parameters.Add(new() { Name = "Температура бака", BitLength = 8, StartByte = 2, b = -75, Unit = "°С", PackNumber = 1 });
            PGNs[19].parameters.Add(new() { Name = "Атмосферное давление", BitLength = 8, StartByte = 3, Unit = "кПа", PackNumber = 1 });
            PGNs[19].parameters.Add(new() { Name = "Сработал датчик бытовой воды", BitLength = 2, StartByte = 4, PackNumber = 1, Meanings = defMeaningsYesNo });

            PGNs[19].parameters.Add(new() { Name = "Уставка температуры жидкости подогревателя для перехода в ждущий.", BitLength = 8, StartByte = 1, b = -75, PackNumber = 2 });
            PGNs[19].parameters.Add(new() { Name = "Уставка температуры жидкости подогревателя для выхода из ждущего.", BitLength = 8, StartByte = 2, b = -75, PackNumber = 2 });
            PGNs[19].parameters.Add(new() { Name = "Уставка температуры жидкости подогревателя для выхода из ждущего при разборе воды.", BitLength = 8, StartByte = 3, b = -75, PackNumber = 2 });
            PGNs[19].parameters.Add(new() { Name = "Уставка температуры бака для перехода в ждущий.", BitLength = 8, StartByte = 4, b = -75, PackNumber = 2 });
            PGNs[19].parameters.Add(new() { Name = "Уставка температуры бака для выхода из ждущего.", BitLength = 8, StartByte = 5, b = -75, PackNumber = 2 });
            PGNs[19].parameters.Add(new() { Name = "Уставка температуры бака для выхода из ждущего при разборе воды.", BitLength = 8, StartByte = 6, b = -75, PackNumber = 2 });

            PGNs[20].parameters.Add(new() { Name = "Код неисправности", BitLength = 8, StartByte = 0 });
            PGNs[20].parameters.Add(new() { Name = "Количество морганий", BitLength = 8, StartByte = 1 });
            PGNs[20].parameters.Add(new() { Name = "Байт неисправностей 1", BitLength = 8, StartByte = 2 });
            PGNs[20].parameters.Add(new() { Name = "Байт неисправностей 2", BitLength = 8, StartByte = 3 });
            PGNs[20].parameters.Add(new() { Name = "Байт неисправностей 3", BitLength = 8, StartByte = 4 });
            PGNs[20].parameters.Add(new() { Name = "Байт неисправностей 4", BitLength = 8, StartByte = 5 });
            PGNs[20].parameters.Add(new() { Name = "Байт неисправностей 5", BitLength = 8, StartByte = 6 });
            PGNs[20].parameters.Add(new() { Name = "Байт неисправностей 6", BitLength = 8, StartByte = 7 });

            PGNs[21].parameters.Add(new() { Name = "Опорное напряжение процессора", BitLength = 8, StartByte = 0, Unit = "В", a = 0.1 });
            PGNs[21].parameters.Add(new() { Name = "Температура процессора", BitLength = 8, StartByte = 1, Unit = "°C", b = -75 });
            PGNs[21].parameters.Add(new() { Name = "Температура бака", BitLength = 8, StartByte = 2, Unit = "°C", b = -75 });
            PGNs[21].parameters.Add(new() { Name = "Температура теплообменника", BitLength = 8, StartByte = 3, Unit = "°C", b = -75 });
            PGNs[21].parameters.Add(new() { Name = "Температура наружного воздуха", BitLength = 8, StartByte = 4, Unit = "°C", b = -75 });
            PGNs[21].parameters.Add(new() { Name = "Уровень жидкости в баке", BitLength = 8, StartByte = 6 });
            PGNs[21].parameters.Add(new() { Name = "Разбор воды", BitLength = 2, StartByte = 7, Meanings = defMeaningsYesNo });

            PGNs[22].parameters.Add(new() { Name = "Зона 1", BitLength = 2, StartByte = 0, StartBit = 0, Meanings = defMeaningsOnOff });
            PGNs[22].parameters.Add(new() { Name = "Зона 2", BitLength = 2, StartByte = 0, StartBit = 2, Meanings = defMeaningsOnOff });
            PGNs[22].parameters.Add(new() { Name = "Зона 3", BitLength = 2, StartByte = 0, StartBit = 4, Meanings = defMeaningsOnOff });
            PGNs[22].parameters.Add(new() { Name = "Зона 4", BitLength = 2, StartByte = 0, StartBit = 6, Meanings = defMeaningsOnOff });
            PGNs[22].parameters.Add(new() { Name = "Зона 5", BitLength = 2, StartByte = 1, StartBit = 0, Meanings = defMeaningsOnOff });
            PGNs[22].parameters.Add(new() { Name = "Температура зоны 1", BitLength = 8, StartByte = 2, Unit = "°C", b = -75 });
            PGNs[22].parameters.Add(new() { Name = "Температура зоны 2", BitLength = 8, StartByte = 3, Unit = "°C", b = -75 });
            PGNs[22].parameters.Add(new() { Name = "Температура зоны 3", BitLength = 8, StartByte = 4, Unit = "°C", b = -75 });
            PGNs[22].parameters.Add(new() { Name = "Температура зоны 4", BitLength = 8, StartByte = 5, Unit = "°C", b = -75 });
            PGNs[22].parameters.Add(new() { Name = "Температура зоны 5", BitLength = 8, StartByte = 6, Unit = "°C", b = -75 });

            PGNs[23].parameters.Add(new() { Name = "Зад. ШИМ вентилятора зоны 1", BitLength = 8, StartByte = 0, Unit = "%" });
            PGNs[23].parameters.Add(new() { Name = "Зад. ШИМ вентилятора зоны 2", BitLength = 8, StartByte = 1, Unit = "%" });
            PGNs[23].parameters.Add(new() { Name = "Зад. ШИМ вентилятора зоны 3", BitLength = 8, StartByte = 2, Unit = "%" });
            PGNs[23].parameters.Add(new() { Name = "Зад. ШИМ вентилятора зоны 4", BitLength = 8, StartByte = 3, Unit = "%" });
            PGNs[23].parameters.Add(new() { Name = "Зад. ШИМ вентилятора зоны 5", BitLength = 8, StartByte = 4, Unit = "%" });

            PGNs[24].parameters.Add(new() { Name = "Ступень скорости вентилятора зоны 1", BitLength = 4, StartByte = 0, });
            PGNs[24].parameters.Add(new() { Name = "Ступень скорости вентилятора зоны 2", BitLength = 4, StartByte = 0, StartBit = 4 });
            PGNs[24].parameters.Add(new() { Name = "Ступень скорости вентилятора зоны 3", BitLength = 4, StartByte = 1, });
            PGNs[24].parameters.Add(new() { Name = "Ступень скорости вентилятора зоны 4", BitLength = 4, StartByte = 1, StartBit = 4 });
            PGNs[24].parameters.Add(new() { Name = "Ступень скорости вентилятора зоны 5", BitLength = 4, StartByte = 2, });
            PGNs[24].parameters.Add(new() { Name = "Тек. ШИМ вентилятора зоны 1", BitLength = 8, StartByte = 3, Unit = "%" });
            PGNs[24].parameters.Add(new() { Name = "Тек. ШИМ вентилятора зоны 2", BitLength = 8, StartByte = 4, Unit = "%" });
            PGNs[24].parameters.Add(new() { Name = "Тек. ШИМ вентилятора зоны 3", BitLength = 8, StartByte = 5, Unit = "%" });
            PGNs[24].parameters.Add(new() { Name = "Тек. ШИМ вентилятора зоны 4", BitLength = 8, StartByte = 6, Unit = "%" });
            PGNs[24].parameters.Add(new() { Name = "Тек. ШИМ вентилятора зоны 5", BitLength = 8, StartByte = 7, Unit = "%" });

            PGNs[25].parameters.Add(new() { Name = "Дневная уставка зоны 1", BitLength = 8, StartByte = 0, Unit = "°C", b = -75 });
            PGNs[25].parameters.Add(new() { Name = "Дневная уставка зоны 2", BitLength = 8, StartByte = 1, Unit = "°C", b = -75 });
            PGNs[25].parameters.Add(new() { Name = "Дневная уставка зоны 3", BitLength = 8, StartByte = 2, Unit = "°C", b = -75 });
            PGNs[25].parameters.Add(new() { Name = "Дневная уставка зоны 4", BitLength = 8, StartByte = 3, Unit = "°C", b = -75 });
            PGNs[25].parameters.Add(new() { Name = "Дневная уставка зоны 5", BitLength = 8, StartByte = 4, Unit = "°C", b = -75 });

            PGNs[26].parameters.Add(new() { Name = "Ночная уставка зоны 1", BitLength = 8, StartByte = 0, Unit = "°C", b = -75 });
            PGNs[26].parameters.Add(new() { Name = "Ночная уставка зоны 2", BitLength = 8, StartByte = 1, Unit = "°C", b = -75 });
            PGNs[26].parameters.Add(new() { Name = "Ночная уставка зоны 3", BitLength = 8, StartByte = 2, Unit = "°C", b = -75 });
            PGNs[26].parameters.Add(new() { Name = "Ночная уставка зоны 4", BitLength = 8, StartByte = 3, Unit = "°C", b = -75 });
            PGNs[26].parameters.Add(new() { Name = "Ночная уставка зоны 5", BitLength = 8, StartByte = 4, Unit = "°C", b = -75 });

            PGNs[27].parameters.Add(new() { Name = "Ручная уставка ШИМ зоны 1", BitLength = 8, StartByte = 0, Unit = "%" });
            PGNs[27].parameters.Add(new() { Name = "Ручная уставка ШИМ зоны 2", BitLength = 8, StartByte = 1, Unit = "%" });
            PGNs[27].parameters.Add(new() { Name = "Ручная уставка ШИМ зоны 3", BitLength = 8, StartByte = 2, Unit = "%" });
            PGNs[27].parameters.Add(new() { Name = "Ручная уставка ШИМ зоны 4", BitLength = 8, StartByte = 3, Unit = "%" });
            PGNs[27].parameters.Add(new() { Name = "Ручная уставка ШИМ зоны 5", BitLength = 8, StartByte = 4, Unit = "%" });

            PGNs[28].parameters.Add(new() { Name = "Общее время на всех режимах", BitLength = 32, StartByte = 0, Unit = "с" });
            PGNs[28].parameters.Add(new() { Name = "Общее время работы (кроме ожидания команды)", BitLength = 32, StartByte = 4, Unit = "%" });

            PGNs[29].parameters.Add(new() { Name = "Атмосферное давление", BitLength = 8, StartByte = 1, Unit = "кПа", PackNumber = 1 });
            PGNs[29].parameters.Add(new() { Name = "Среднее максимальное значение давления", BitLength = 24, StartByte = 2, Unit = "кПа", a = 0.001, PackNumber = 1 });
            PGNs[29].parameters.Add(new() { Name = "Среднее минимальное значение давления", BitLength = 24, StartByte = 4, Unit = "кПа", a = 0.001, PackNumber = 1 });

            PGNs[29].parameters.Add(new() { Name = "Разница между max и min  значениями", BitLength = 16, StartByte = 1, a = 0.01, Unit = "кПа", PackNumber = 2 });
            PGNs[29].parameters.Add(new() { Name = "Флаг появления пламени по пульсации давления", BitLength = 2, StartByte = 3, Meanings = defMeaningsYesNo, PackNumber = 2 });
            PGNs[29].parameters.Add(new() { Name = "Атмосферное давление", BitLength = 24, StartByte = 4, Unit = "кПа", a = 0.001, PackNumber = 2, Var = 60 });

            PGNs[31].parameters.Add(new() { Name = "Время работы", BitLength = 32, StartByte = 0, Unit = "с", Var = 3 });
            PGNs[31].parameters.Add(new() { Name = "Время работы на режиме", BitLength = 32, StartByte = 4, Unit = "с", Var = 4 });

            PGNs[100].parameters.Add(new() { Name = "Начальный адрес", BitLength = 24, StartByte = 1, PackNumber = 2, GetMeaning = r => $"Начальный адрес: 0X{(r + 0x8000000):X}" });
            PGNs[100].parameters.Add(new() { Name = "Длина данных", BitLength = 32, StartByte = 4, PackNumber = 2 });
            PGNs[100].parameters.Add(new() { Name = "Длина данных", BitLength = 24, StartByte = 1, PackNumber = 4 });
            PGNs[100].parameters.Add(new() { Name = "CRC", BitLength = 32, StartByte = 4, PackNumber = 4, GetMeaning = r => $"CRC: 0X{(r):X}" });
            PGNs[100].parameters.Add(new() { Name = "Адрес фрагмента", BitLength = 32, StartByte = 2, PackNumber = 5, GetMeaning = r => $"Адрес фрагмента: 0X{r:X}" });

            PGNs[101].parameters.Add(new() { Name = "Первое слово", BitLength = 32, StartByte = 0, GetMeaning = r => $"1st: 0X{(r):X}" });
            PGNs[101].parameters.Add(new() { Name = "Второе слово", BitLength = 32, StartByte = 4, GetMeaning = r => $"2nd: 0X{(r):X}" });

            #endregion

            #region Error names init
            ErrorNames = new Dictionary<int, string>() {
                 {0,"No error" },
                 {1  , "Overheat"},
                 {2  , "Overheat"},
                 {3  , "Error of the overheat temp. sensor"},
                 {4  , "Error of the liquid temp. sensor"},
                 {5  , "Open circuit of the flame temp. sensor"},
                 {9  , "Glow plug error"},
                 {10 , "Fan speed does not correspond to the defined"},
                 {12 , "High supply voltage"},
                 {13 , "No ignition"},
                 {14 , "Water pump error"},
                 {15 , "Low supply voltage"},
                 {16 , "Body temp.sensor does not cool down"},
                 {17 , "Short circuit of the fuel pump"},
                 {22 , "Open circuit of the fuel pump"},
                 {27 , "Fan does not rotate"},
                 {28 , "Fan self-rotation"},
                 {29 , "Exceeding the limit of flame blowout"},
                 {36 , "Overheating of the flame indicator"},
                 {40 , "No connection with the heater"},
                 {45 , "Open circuit of the tank temp. sensor"},
                 {46 , "Short circuit of the tank temp. sensor"},
                 {53 , "Open circuit of the flow sensor"},
                 {54 , "Short circuit of the flow sensor"},
                 {55 , "Open circuit of the air temp. sensor"},
                 {56 , "Short circuit of the air temp. sensor"},
                 {57 , "Short circuit of the zone 1 temp. sensor"},
                 {58 , "Open circuit of the zone 1 temp. sensor"},
                 {59 , "Short circuit of the zone 2 temp. sensor"},
                 {60 , "Open circuit of the zone 2 temp. sensor"},
                 {61 , "Short circuit of the zone 3 temp. sensor"},
                 {62 , "Open circuit of the zone 3 temp. sensor"},
                 {63 , "Short circuit of the zone 4 temp. sensor"},
                 {64 , "Open circuit of the zone 4 temp. sensor"},
                 {65 , "Short circuit of the zone 5 temp. sensor"},
                 {66 , "Open circuit of the zone 5 temp. sensor"},
                 {69 , "Short circuit of the pump 1"},
                 {70 , "Open circuit of the pump 1"},
                 {71 , "Short circuit of the pump 2"},
                 {72 , "Open circuit of the pump 2"},
                 {73 , "Short circuit of the pump 3"},
                 {74 , "Open circuit of the pump 3"},
                 {75 , "Short circuit of the pump 4"},
                 {76 , "Open circuit of the pump 4"},
                 {77 , "Short circuit of the pump 5"},
                 {78 , "Open circuit of the pump 5"},
                 {79 , "Short circuit of the fan 1"},
                 {80 , "Open circuit of the fan 1"},
                 {81 , "Short circuit of the fan 2"},
                 {82 , "Open circuit of the fan 2"},
                 {83 , "Short circuit of the fan 3"},
                 {84 , "Open circuit of the fan 3"},
                 {85 , "Short circuit of the fan 4"},
                 {86 , "Open circuit of the fan 4"},
                 {87 , "Short circuit of the fan 5"},
                 {88 , "Open circuit of the fan 5"},
                 {91 , "Liquid level too low"},
                 {92 , "Liquid level too high"},
                 {93 , "Level sensor short circuit"},
                 {94 , "Level sensor open circuit"} };
            #endregion

        }

    }

}




