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
using System.Drawing;

namespace AdversCan
{
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
        public int StartByte;   //Начальный байт в пакете
        public int StartBit;    //Начальный бит в байте
        public int BitLength;   //Длина параметра в битах
        public bool Signed = false; //Число со знаком
        public string Name { set; get; }     //Имя параметра
        private string unit = "";     //Единица измерения
        public double a = 1;         //коэффициент приведения
        public double b = 0;         //смещение
        //value = rawData*a+b
        public string Unit { get => unit; set => unit = value; }
        public string OutputFormat
        {
            get
            {
                if (a == 1)
                    return "";
                else if (a >= 0.09)
                    return "F1";
                else
                    return "F2";
            }
        }
        private Dictionary<int, string> meanings = new(); //Словарь с расшифровками значений параметров
        public Dictionary<int, string> Meanings { get => meanings; set => meanings = value; }
        public Func<int, string> GetMeaning; //Принимает на вход сырое значение, возвращает строку с расшифровкой значения параметра
        public Func<byte[], string> CustomDecoder; //Если для декодирования нужен весь пакет данных
        public string Tip = ""; //Подсказка
        public int PackNumber; //Номер пакета в мультипакете
        public int Var; //Соответствующая переменная из paramsName.h
        public double DefaultValue; //Для конструктора комманд
        public double MinValue;// Для правильного масштаба графиков
        public double MaxValue;// Для правильного масштаба графиков
        public bool AnswerOnly = false;

        public string Decode(int rawValue)
        {
            StringBuilder retString = new();
            retString.Append(Name + ": ");
            if (CustomDecoder != null)
                return "Custom decoders not supported";

            if (GetMeaning != null)
                return GetMeaning(rawValue);
            if (Meanings != null && Meanings.ContainsKey(rawValue))
                retString.Append(rawValue.ToString() + " - " + Meanings[rawValue]);
            else
            {
                if (rawValue == Math.Pow(2, BitLength) - 1)
                    retString.Append($"Нет данных({rawValue})");
                else
                {
                    double rawDouble = (double)rawValue;
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
        public CommandId Id
        {
            get { return new CommandId(firstByte, secondByte); }
            set
            {
                firstByte = value.firstByte;
                secondByte = value.secondByte;
            }
        }

        public byte firstByte;
        public byte secondByte;
        public string name = "";
        public string Name => name;
        private readonly List<AC2PParameter> _Parameters = new();
        public List<AC2PParameter> Parameters => _Parameters;
        public override string ToString()
        {
            return name;
        }
    }
    public class AC2PMessage : CanMessage, IUpdatable<AC2PMessage>
    {
        public AC2PMessage() : base()
        {
            DLC = 8;
            RTR = false;
            IDE = true;
        }
        public AC2PMessage(CanMessage m) : this()
        {
            if (m.DLC != 8 || m.RTR || !m.IDE)
                throw new ArgumentException("CAN message is not compliant with AC2P");
            Data = m.Data;
            ID = m.ID;
            return;
        }

        [AffectsTo("VerboseInfo")]
        public int PGN
        {
            get { return (ID >> 20) & 0x1FF; }
            set
            {
                if (value > 511)
                    throw new ArgumentException("PGN can't be over 511");
                if (ID == value)
                    return;
                int temp = ID;
                temp &= ~(0x1FF << 20);
                temp |= value << 20;
                ID = temp;

            }
        }
        [AffectsTo("VerboseInfo")]
        public int ReceiverType
        {
            get { return (ID >> 13) & 0b1111111; }
            set
            {
                if (value > 127)
                    throw new ArgumentException("ReceiverType can't be over 127");
                if (ReceiverType == value)
                    return;
                int temp = ID;
                temp &= ~(0x7F << 13);
                temp |= value << 13;
                ID = temp;
            }
        }
        [AffectsTo("VerboseInfo")]
        public int ReceiverAddress
        {
            get { return (ID >> 10) & 0b111; }
            set
            {
                if (value > 7)
                    throw new ArgumentException("ReceiverAddress can't be over 7");
                if (ReceiverAddress == value)
                    return;
                int temp = ID;
                temp &= ~(0x3 << 10);
                temp |= value << 10;
                ID = temp;
            }
        }
        [AffectsTo("VerboseInfo")]
        public int TransmitterType
        {
            get { return (ID >> 3) & 0x7F; }
            set
            {
                if (value > 127)
                    throw new ArgumentException("TransmitterType can't be over 127");
                if (TransmitterType == value)
                    return;
                int temp = ID;
                temp &= ~(0x7F << 3);
                temp |= value << 3;
                ID = temp;
            }
        }
        [AffectsTo("VerboseInfo")]
        public int TransmitterAddress
        {
            get { return ID & 0b111; }
            set
            {
                if (value > 7)
                    throw new ArgumentException("TransmitterAddress can't be over 7");
                if (TransmitterAddress == value)
                    return;
                int temp = ID;
                temp &= ~(0x3);
                temp |= value;
                ID = temp;
            }
        }

        [AffectsTo("VerboseInfo")]
        public DeviceId TransmitterId
        {
            get { return new DeviceId(TransmitterType, TransmitterAddress); }
            set { TransmitterType = value.Type; TransmitterAddress = value.Address; }
        }

        [AffectsTo("VerboseInfo")]
        public DeviceId ReceiverId
        {
            get { return new DeviceId(ReceiverType, ReceiverAddress); }
            set { ReceiverType = value.Type; ReceiverAddress = value.Address; }
        }

        [AffectsTo("VerboseInfo")]
        public CommandId? Command
        {
            get
            {
                if (PGN != 1 && PGN != 2) return null;
                return new CommandId(Data[0], Data[1]);
            }
            set
            {
                if (value == null) throw new ArgumentNullException(nameof(CommandId));
                if (PGN != 1 && PGN != 2) throw new Exception("Use Command property only for Command PGNs (1 and 2)");
                Data[0] = value.Value.firstByte;
                Data[1] = value.Value.secondByte;
            }

        }

        public string PrintParameter(AC2PParameter p)
        {
            StringBuilder retString = new();
            int rawValue;
            retString.Append(p.Name + ": ");
            if (p.CustomDecoder != null)
                return p.CustomDecoder(Data);
            switch (p.BitLength)
            {
                case 1: rawValue = Data[p.StartByte] >> p.StartBit & 0b1; break;
                case 2: rawValue = Data[p.StartByte] >> p.StartBit & 0b11; break;
                case 3: rawValue = Data[p.StartByte] >> p.StartBit & 0b111; break;
                case 4: rawValue = Data[p.StartByte] >> p.StartBit & 0b1111; break;
                case 8: rawValue = Data[p.StartByte]; break;
                case 16: rawValue = Data[p.StartByte] * 256 + Data[p.StartByte + 1]; break;
                case 24: rawValue = Data[p.StartByte] * 65536 + Data[p.StartByte + 1] * 256 + Data[p.StartByte + 2]; break;
                case 32: rawValue = Data[p.StartByte] * 16777216 + Data[p.StartByte + 1] * 65536 + Data[p.StartByte + 2] * 256 + Data[p.StartByte + 3]; break;
                default: throw new Exception("Bad parameter size");
            }
            if (p.GetMeaning != null)
                return p.GetMeaning(rawValue);
            if (p.Meanings != null && p.Meanings.ContainsKey(rawValue))
                retString.Append(rawValue.ToString() + " - " + p.Meanings[rawValue]);
            else
            {
                if (rawValue == Math.Pow(2, p.BitLength) - 1)
                    retString.Append($"Нет данных({rawValue})");
                else
                {
                    double rawDouble = (double)rawValue;
                    double value = rawDouble * p.a + p.b;
                    retString.Append(value.ToString(p.OutputFormat) + p.Unit + '(' + rawValue.ToString() + ')');
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
            if (PGN == 1 && AC2P.commands.ContainsKey(new CommandId(Data[0], Data[1])))
            {
                AC2PCommand cmd = AC2P.commands[new CommandId(Data[0], Data[1])];
                retString.Append(cmd.name + ";");
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
        public string VerboseInfo => GetVerboseInfo().Replace(';', '\n');

        public void Update(AC2PMessage item)
        {
            PGN = item.PGN;
            TransmitterId = item.TransmitterId;
            ReceiverId = item.ReceiverId;
            Data = item.Data;
        }


        public bool IsSimmiliarTo(AC2PMessage m)
        {
            if (PGN != m.PGN)
                return false;
            if (PGN == 1 || PGN == 2)
                if (!Command.Equals(m.Command))
                    return false;
            if (PGN == 3 || PGN == 4)
                if (Data[0] != m.Data[0] || Data[1] != m.Data[1]) //Другой SPN
                    return false;
            if (PGN == 7)
                if (Data[0] != m.Data[0] || Data[2] != m.Data[2] || Data[3] != m.Data[3]) //Другой Параметр Конфигурации или команда
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
        public List<CommandId> SupportedCommands;
        public string photo;


        public override string ToString()
        {
            return Name;
        }

    }
    public class ReadedBlackBoxValue : INotifyPropertyChanged, IUpdatable<ReadedBlackBoxValue>
    {
        private int id;

        public int Id
        {
            get { return id; }
            set
            {
                if (id == value)
                    return;
                onChange("Id");
                id = value;
            }
        }
        private uint val;

        public uint Value
        {
            get { return val; }
            set
            {
                if (val == value)
                    return;
                val = value;
                onChange("Value");
            }
        }

        public event PropertyChangedEventHandler PropertyChanged;

        void onChange(string propName)
        {
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propName));
        }

        public void Update(ReadedBlackBoxValue item)
        {
            Value = item.Value;
        }

        public bool IsSimmiliarTo(ReadedBlackBoxValue item)
        {
            return (id == item.id);
        }


        public string Description => AC2P.BbParameterNames[id];

    }
    public class ReadedParameter : INotifyPropertyChanged, IUpdatable<ReadedParameter>
    {

        private int id;

        public int Id
        {
            get { return id; }
            set
            {
                if (id == value)
                    return;
                onChange("Id");
                id = value;
            }
        }

        private uint val;

        public uint Value
        {
            get { return val; }
            set
            {
                if (val == value)
                    return;
                val = value;
                onChange("Value");
            }
        }

        public event PropertyChangedEventHandler PropertyChanged;

        void onChange(string propName)
        {
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propName));
        }

        public void Update(ReadedParameter item)
        {
            Value = item.Value;
        }

        public bool IsSimmiliarTo(ReadedParameter item)
        {
            return (id == item.id);
        }

        public string idString => AC2P.configParameters[Id]?.StringId;
        public string rusName => AC2P.configParameters[Id]?.NameRu;
        public string enName => AC2P.configParameters[Id]?.NameEn;

    }

    public class StatusVariable : ViewModel, IUpdatable<StatusVariable>
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
                case 5: ChartBrush = new SolidColorBrush(System.Windows.Media.Color.FromRgb(255, 255, 0)); break;
                case 15: ChartBrush = new SolidColorBrush(System.Windows.Media.Color.FromRgb(0, 0, 255)); break;
                case 16: ChartBrush = new SolidColorBrush(System.Windows.Media.Color.FromRgb(0, 255, 255)); break;
                case 18: ChartBrush = new SolidColorBrush(System.Windows.Media.Color.FromRgb(0, 255, 0)); break;
                case 21: ChartBrush = new SolidColorBrush(System.Windows.Media.Color.FromRgb(255, 0, 255)); break;
                case 40: ChartBrush = new SolidColorBrush(System.Windows.Media.Color.FromRgb(255, 100, 100)); break;
                default:
                    Random random = new Random((int)DateTime.Now.Ticks);
                    ChartBrush = new SolidColorBrush(System.Windows.Media.Color.FromRgb((byte)random.Next(255), (byte)random.Next(255), (byte)random.Next(255)));
                    break;
            }
        }
        public StatusVariable()
        {

        }
        public int Id { get; set; }

        private int rawVal;

        [AffectsTo("VerboseInfo", "Value")]
        public int RawValue
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

        private System.Windows.Media.Brush chartBrush;

        public System.Drawing.Color Color => System.Drawing.Color.FromArgb(255, (chartBrush as SolidColorBrush).Color.R, (chartBrush as SolidColorBrush).Color.G, (chartBrush as SolidColorBrush).Color.B);


        [AffectsTo("Color")]
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

    }
    public class ReadedVariable : ViewModel, IUpdatable<ReadedVariable>
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
    }
    public class BBError : IUpdatable<BBError>, INotifyPropertyChanged
    {
        private readonly UpdatableList<ReadedVariable> _variables = new();

        public UpdatableList<ReadedVariable> Variables { get => _variables; }

        public event PropertyChangedEventHandler PropertyChanged;

        public bool IsSimmiliarTo(BBError item)
        {
            return false;
        }

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
    }

    public class AC2PTask : ViewModel
    {
        int percentComplete;
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

        private bool occupied;

        public bool Occupied
        {
            get { return occupied; }
            set { Set(ref occupied, value); }
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
            return true;
        }
        public void UpdatePercent(int p)
        {
            PercentComplete = p;
        }

    }

    public class ConnectedDevice : ViewModel
    {
        public ConnectedDevice()
        {
            LogInit();
        }

        private DeviceId id;

        public DeviceId ID
        {
            get { return id; }
            set { Set(ref id, value); }
        }

        UpdatableList<StatusVariable> status = new();
        public UpdatableList<StatusVariable> Status => status;

        private readonly UpdatableList<ReadedParameter> _readedParameters = new();
        public UpdatableList<ReadedParameter> readedParameters => _readedParameters;

        private UpdatableList<ReadedBlackBoxValue> _bbValues = new();
        public UpdatableList<ReadedBlackBoxValue> BBValues => _bbValues;

        public BBError currentBBError { set; get; }

        private UpdatableList<BBError> _BBErrors = new();
        public UpdatableList<BBError> BBErrors => _BBErrors;

        private bool manualMode;

        public bool ManualMode
        {
            get { return manualMode; }
            set { Set(ref manualMode, value); }
        }

        private int revMeasured;
        public int RevMeasured
        {
            get { return revMeasured; }
            set { Set(ref revMeasured, value); }
        }

        public string Name => ToString();
        public string ImagePath => $"/Images/{id.Type}.jpg";
        public override string ToString()
        {
            if (AC2P.Devices.ContainsKey(ID.Type))
                return AC2P.Devices[ID.Type].Name;
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
                LogDataOverrun.Invoke(this, null);
            }
        }

        public void LogInit(int length = 86400)
        {
            LogCurrentPos = 0;
            LogData = new List<double[]>();
            foreach (var var in AC2P.Variables)
            {
                LogData.Add(new double[length]);
            }
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

        public event EventHandler LogDataOverrun;

    }
    public struct CommandId
    {
        public byte firstByte;
        public byte secondByte;

        public CommandId(byte fb, byte sb)
        {
            firstByte = fb;
            secondByte = sb;
            return;
        }

        public override int GetHashCode()
        {
            return firstByte * 256 + secondByte;
        }

        public override bool Equals([NotNullWhen(true)] object obj)
        {
            if (obj is not CommandId)
                return false;
            return ((CommandId)obj).GetHashCode() == GetHashCode();
        }
        public override string ToString()
        {
            return $"{firstByte:D2},{secondByte:D2}]";
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
    public class configParameter
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

        public event EventHandler NewDeviveAquired;

        private SynchronizationContext UIContext;

        static readonly Dictionary<int, string> defMeaningsYesNo = new() { { 0, "Нет" }, { 1, "Да" }, { 2, "Нет данных" }, { 3, "Нет данных" } };
        static readonly Dictionary<int, string> defMeaningsOnOff = new() { { 0, "Выкл" }, { 1, "Вкл" }, { 2, "Нет данных" }, { 3, "Нет данных" } };
        static readonly Dictionary<int, string> defMeaningsAllow = new() { { 0, "Разрешено" }, { 1, "Запрещёно" }, { 2, "Нет данных" }, { 3, "Нет данных" } };
        static readonly Dictionary<int, string> Stages = new() { { 0, "STAGE_Z" }, { 1, "STAGE_P" }, { 2, "STAGE_H" }, { 3, "STAGE_W" }, { 4, "STAGE_F" }, { 5, "STAGE_T" }, { 6, "STAGE_M" } };



        public static Dictionary<int, PGN> PGNs = new();

        public static Dictionary<CommandId, AC2PCommand> commands = new();

        public static Dictionary<int, configParameter> configParameters = new();
        public static Dictionary<int, Variable> Variables = new();
        public static Dictionary<int, BbParameter> BbParameters = new();

        public static Dictionary<int, string> ParamtersNames = new();
        public static Dictionary<int, string> VariablesNames = new();
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
            get { return currentTask; }
            set { Set(ref currentTask, value); }
        }


        private bool CancellationRequested => CurrentTask.CTS.IsCancellationRequested;

        private bool Capture(string n) => CurrentTask.Capture(n);

        private void Done() => CurrentTask.onDone();

        private void Cancel() => CurrentTask.onCancel();

        private void UpdatePercent(int p) => CurrentTask.UpdatePercent(p);

        private void ParseCanMessage(CanMessage msg)
        {
            AC2PMessage m = new AC2PMessage(msg);
            DeviceId id = m.TransmitterId;

            if (ConnectedDevices.FirstOrDefault(d => d.ID.Equals(id)) == null)
            {
                ConnectedDevices.Add(new ConnectedDevice() { ID = id });
                NewDeviveAquired?.Invoke(this, null);
            }

            ConnectedDevice currentDevice = ConnectedDevices.First(d => d.ID.Equals(m.TransmitterId));


            if (!PGNs.ContainsKey(m.PGN)) return; //Такого PGN нет в библиотеке


            if (m.PGN == 2) //Подтверждение выполненной комманды
            {

                switch (m.Data[1])
                {
                    case 67:
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
                    int rawValue;
                    switch (p.BitLength)
                    {
                        case 1: rawValue = m.Data[p.StartByte] >> p.StartBit & 0b1; break;
                        case 2: rawValue = m.Data[p.StartByte] >> p.StartBit & 0b11; break;
                        case 3: rawValue = m.Data[p.StartByte] >> p.StartBit & 0b111; break;
                        case 4: rawValue = m.Data[p.StartByte] >> p.StartBit & 0b1111; break;
                        case 8: rawValue = m.Data[p.StartByte]; break;
                        case 16: rawValue = m.Data[p.StartByte] * 256 + m.Data[p.StartByte + 1]; break;
                        case 24: rawValue = m.Data[p.StartByte] * 65536 + m.Data[p.StartByte + 1] * 256 + m.Data[p.StartByte + 2]; break;
                        case 32: rawValue = m.Data[p.StartByte] * 16777216 + m.Data[p.StartByte + 1] * 65536 + m.Data[p.StartByte + 2] * 256 + m.Data[p.StartByte + 3]; break;
                        default: throw new Exception("Bad parameter size");
                    }
                    if (rawValue == Math.Pow(2, p.BitLength) - 1) return; //Неподдерживаемый параметр, ливаем
                    sv.RawValue = rawValue;
                    currentDevice.SupportedVariables[sv.Id] = true;
                    currentDevice.Status.TryToAdd(sv);
                    
                    if (sv.Id == 16)                                 // Измеренные обороты нагнетателя
                        currentDevice.RevMeasured = rawValue;
                }
            }
            if (m.PGN == 7) //Ответ на запрос параметра
            {
                if (m.Data[0] == 4) // Обрабатываем только упешные ответы на запросы
                {
                    int parameterId = m.Data[3] + m.Data[2] * 256;
                    uint parameterValue = ((uint)m.Data[4] * 0x1000000) + ((uint)m.Data[5] * 0x10000) + ((uint)m.Data[6] * 0x100) + (uint)m.Data[7];
                    if (parameterValue != 0xFFFFFFFF)
                        currentDevice.readedParameters.TryToAdd(new ReadedParameter() { Id = parameterId, Value = parameterValue }); //TODO remove all send commands
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
                    var p = new configParameter();
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
            WaitingForBBErrors = false;
            AC2PMessage msg = new();
            msg.PGN = 8;
            msg.TransmitterAddress = 6;
            msg.TransmitterType = 126;
            msg.ReceiverAddress = id.Address;
            msg.ReceiverType = id.Type;
            msg.Data[0] = 6; //Read Single Param
            msg.Data[1] = 0xFF; //Read Param

            int counter = 0;
            foreach (var p in BbParameters)
            {

                msg.Data[4] = (byte)(p.Key / 256);
                msg.Data[5] = (byte)(p.Key % 256);
                canAdapter.Transmit(msg);
                await Task.Run(() => Thread.Sleep(100));
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

            currentDevice.BBErrors.Clear();
            currentDevice.currentBBError = new BBError();
            currentDevice.BBErrors.Add(currentDevice.currentBBError);


            AC2PMessage msg = new AC2PMessage();
            msg.PGN = 8;
            msg.TransmitterAddress = 6;
            msg.TransmitterType = 126;
            msg.ReceiverAddress = id.Address;
            msg.ReceiverType = id.Type;
            msg.Data[0] = 0x13; //Read Errors
            msg.Data[1] = 0xFF;
            for (int i = 0; i < 1024; i++)
            {
                msg.Data[4] = (byte)(i / 256);  //Pair count
                msg.Data[5] = (byte)(i % 256);  //Pair count
                msg.Data[6] = 0x00; //Pair count MSB
                msg.Data[7] = 0x01; //Pair count LSB

                canAdapter.Transmit(msg);
                await Task.Run(() => Thread.Sleep(40));
                if (CancellationRequested)
                {
                    Cancel();
                    return;
                }
                UpdatePercent(100 * i / 1024);
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
            canAdapter.Transmit(msg);

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
            canAdapter.Transmit(msg);

            Done();
        }
        public async void ReadAllParameters(DeviceId id)
        {
            if (!Capture("Чтение параметров из Flash")) return;
            int cnt = 0;
            foreach (var p in configParameters)
            {
                AC2PMessage msg = new AC2PMessage();
                msg.PGN = 7;
                msg.TransmitterAddress = 6;
                msg.TransmitterType = 126;
                msg.ReceiverAddress = id.Address;
                msg.ReceiverType = id.Type;
                msg.Data[0] = 3; //Read Param
                msg.Data[1] = 0xFF; //Read Param
                msg.Data[2] = (byte)(p.Key / 256);
                msg.Data[3] = (byte)(p.Key % 256);
                canAdapter.Transmit(msg);
                await Task.Run(() => Thread.Sleep(100));
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
                canAdapter.Transmit(msg);
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
            canAdapter.Transmit(msg);
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
            canAdapter.Transmit(msg);
            Done();
        }

        public void SendMessage(AC2PMessage m)
        {
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
            canAdapter.Transmit(msg);
        }
        public void SendCommand(CommandId com, DeviceId dev, byte[] data = null)
        {
            AC2PMessage message = new AC2PMessage();
            message.PGN = 1;
            message.TransmitterAddress = 6;
            message.TransmitterType = 126;
            message.ReceiverAddress = dev.Address;
            message.ReceiverType = dev.Type;
            message.Command = com;
            for (int i = 0; i < 6; i++)
            {
                if (data != null)
                    message.Data[i + 2] = data[i];
                else
                    message.Data[i + 2] = 0xFF;
            }
            canAdapter.Transmit(message);
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
            UIContext.Send(x => ParseCanMessage((e as GotMessageEventArgs).receivedMessage), null);
        }

        public static void SeedStaticData()
        {

            Devices = new Dictionary<int, Device>() {
            { 0, new Device(){ID=0,Name="Любой" } } ,
            { 1, new Device(){ID=1,Name="14ТС-Мини" } } ,
            { 2, new Device(){ID=2,Name="Планар 2" }} ,
            { 3, new Device(){ID=3,Name="Планар 44Д" }} ,
            { 4, new Device(){ID=4,Name="30ТСД" }} ,
            { 5, new Device(){ID=5,Name="30ТСГ" }} ,
            { 6, new Device(){ID=6,Name="Binar-5S B" }} ,
            { 7, new Device(){ID=7,Name="Планар 8Д" }} ,
            { 8, new Device(){ID=8,Name="OB-8" }} ,
            { 9, new Device(){ID=9,Name="Планар 4Д" }} ,
            { 10, new Device(){ID=10,Name="Binar-5S D" }} ,
            { 11, new Device(){ID=11,Name="Планар-9Д, ОВ-8ДК" }} ,
            { 12, new Device(){ID=12,Name="Планар-44Б" }} ,
            { 13, new Device(){ID=13,Name="Планар-4Б" }} ,
            { 14, new Device(){ID=14,Name="Плита" }} ,
            { 15, new Device(){ID=15,Name="Планар-44Г" }} ,
            { 16, new Device(){ID=16,Name="ОВ-4" }} ,
            { 17, new Device(){ID=17,Name="14ТСД-10" }} ,
            { 18, new Device(){ID=18,Name="Планар 2Б" }} ,
            { 19, new Device(){ID=19,Name="Блок управления клапанами." }} ,
            { 20, new Device(){ID=20,Name="Планар-6Д" }} ,
            { 21, new Device(){ID=21,Name="14ТС-10" }} ,
            { 22, new Device(){ID=22,Name="30SP (впрысковый)" }} ,
            { 23, new Device(){ID=23,Name="Бинар 5Б-Компакт" }} ,
            { 25, new Device(){ID=25,Name="35SP (впрысковый)" }} ,
            { 27, new Device(){ID=27,Name="Бинар 5Д-Компакт" }} ,
            { 29, new Device(){ID=29,Name="Бинар 6Г-Компакт" }} ,
            { 31, new Device(){ID=31,Name="14ТСГ-Мини" }} ,
            { 32, new Device(){ID=32,Name="30SPG (на базе 30SP)" }} ,
            { 34, new Device(){ID=34,Name="Binar-10Д" }} ,
            { 35, new Device(){ID=35,Name="Binar-10Б" }} ,
            { 123, new Device(){ID=123,Name="Bootloader" }} ,
            { 126, new Device(){ID=126,Name="Устройство управления" }},
            { 255, new Device(){ID=255,Name="Не задано" }}
        };

            #region PGN initialise
            PGNs.Add(0, new PGN() { id = 0, name = "Пустая команда" });
            PGNs.Add(1, new PGN() { id = 1, name = "Комманда управления" });
            PGNs.Add(2, new PGN() { id = 2, name = "Подтверждение на принятую комманду" });
            PGNs.Add(3, new PGN() { id = 3, name = "Запрос параметра или набора данных по определенному номеру (SPN)" });
            PGNs.Add(4, new PGN() { id = 4, name = "Ответ на запрос параметра или набора данных по определенному номеру (SPN)" });
            PGNs.Add(5, new PGN() { id = 5, name = "Запись параметра или набора данных устройства" });
            PGNs.Add(6, new PGN() { id = 6, name = "Запрос параметров по PGN" });
            PGNs.Add(7, new PGN() { id = 7, name = "Запись/чтение параметров работы (конигурация) в/из flash-памяти" });
            PGNs.Add(8, new PGN() { id = 8, name = "Работа с ЧЯ" });
            PGNs.Add(10, new PGN() { id = 10, name = "Стадия, режим, код неисправности, код предупреждения" });
            PGNs.Add(11, new PGN() { id = 11, name = "Напряжение питания, атмосферное давление, ток двигателя" });
            PGNs.Add(12, new PGN() { id = 12, name = "Обороты НВ, частота ТН, свеча, реле" });
            PGNs.Add(13, new PGN() { id = 13, name = "Температуры жидкостных подогревателей" });
            PGNs.Add(14, new PGN() { id = 14, name = "Слежение за пламенем" });
            PGNs.Add(15, new PGN() { id = 15, name = "АЦП 0-3 каналы" });
            PGNs.Add(16, new PGN() { id = 16, name = "АЦП 4-7 каналы" });
            PGNs.Add(17, new PGN() { id = 17, name = "АЦП 8-11 каналы" });
            PGNs.Add(18, new PGN() { id = 18, name = "Версия и дата программного обеспечения" });
            PGNs.Add(19, new PGN() { id = 19, name = "Параметры от центрального блока управления для подогревателя в системе отопления", multipack = true });
            PGNs.Add(20, new PGN() { id = 20, name = "Неисправности" });
            PGNs.Add(21, new PGN() { id = 21, name = "Блок управления системой отопления" });
            PGNs.Add(22, new PGN() { id = 22, name = "Блок управления системой отопления" });
            PGNs.Add(23, new PGN() { id = 23, name = "Блок управления системой отопления" });
            PGNs.Add(24, new PGN() { id = 24, name = "Блок управления системой отопления" });
            PGNs.Add(25, new PGN() { id = 25, name = "Блок управления системой отопления" });
            PGNs.Add(26, new PGN() { id = 26, name = "Блок управления системой отопления" });
            PGNs.Add(27, new PGN() { id = 27, name = "Блок управления системой отопления" });
            PGNs.Add(28, new PGN() { id = 28, name = "Общее время наработки подогревателя" });
            PGNs.Add(29, new PGN() { id = 29, name = "Параметры давления", multipack = true });
            PGNs.Add(30, new PGN() { id = 30, name = "Состояние сигнализации, двигателя автомобиля. Температура датчика воздуха. Напряжение канала двигателя автомобиля" });
            PGNs.Add(31, new PGN() { id = 31, name = "Время работы" });
            PGNs.Add(32, new PGN() { id = 32, name = "Параметры работы жидкостного подогревателя" });
            PGNs.Add(33, new PGN() { id = 33, name = "Серийный номер изделия (мультипакет)", multipack = true });
            PGNs.Add(34, new PGN() { id = 34, name = "Считать данные из flash по адресу" });
            PGNs.Add(35, new PGN() { id = 35, name = "Передача данных на запрос по PGN 34" });
            PGNs.Add(36, new PGN() { id = 36, name = "Передача состояния клапанов, зонда, кода неисправности клапанов" });
            PGNs.Add(37, new PGN() { id = 37, name = "Температуры воздушных отопителей (мультипакет)", multipack = true });
            PGNs.Add(38, new PGN() { id = 38, name = "Температура датчика в пульте" });
            PGNs.Add(39, new PGN() { id = 39, name = "Статусы драйверов ТН, свечи, помпа, реле" });
            PGNs.Add(100, new PGN() { id = 100, name = "Управления памятью (Мультипакет)", multipack = true });
            PGNs.Add(101, new PGN() { id = 101, name = "Заполнение буферного массива для последующей записи во флэш" });
            #endregion

            #region Commands initialise
            commands.Add(new CommandId(0, 0), new AC2PCommand() { firstByte = 0, secondByte = 0, name = "Кто здесь?" });
            commands.Add(new CommandId(0, 1), new AC2PCommand() { firstByte = 0, secondByte = 1, name = "пуск устройства" });
            commands.Add(new CommandId(0, 3), new AC2PCommand() { firstByte = 0, secondByte = 3, name = "остановка устройства" });
            commands.Add(new CommandId(0, 4), new AC2PCommand() { firstByte = 0, secondByte = 4, name = "пуск только помпы" });
            commands.Add(new CommandId(0, 5), new AC2PCommand() { firstByte = 0, secondByte = 5, name = "сброс неисправностей" });
            commands.Add(new CommandId(0, 6), new AC2PCommand() { firstByte = 0, secondByte = 6, name = "задать параметры работы жидкостного подогревателя" });
            commands.Add(new CommandId(0, 7), new AC2PCommand() { firstByte = 0, secondByte = 7, name = "запрос температурных переходов по режимам жидкостного подогревателя" });
            commands.Add(new CommandId(0, 8), new AC2PCommand() { firstByte = 0, secondByte = 8, name = "задать состояние клапанов устройства ”Блок управления клапанами”" });
            commands.Add(new CommandId(0, 9), new AC2PCommand() { firstByte = 0, secondByte = 9, name = "задать параметры работы воздушного отопителя" });
            commands.Add(new CommandId(0, 10), new AC2PCommand() { firstByte = 0, secondByte = 10, name = "запуск в режиме вентиляции (для воздушных отопителей)" });
            commands.Add(new CommandId(0, 20), new AC2PCommand() { firstByte = 0, secondByte = 20, name = "калибровка термопар" });
            commands.Add(new CommandId(0, 21), new AC2PCommand() { firstByte = 0, secondByte = 21, name = "задать параметры частоты ШИМ нагнетателя воздуха" });
            commands.Add(new CommandId(0, 22), new AC2PCommand() { firstByte = 0, secondByte = 22, name = "Reset CPU" });
            commands.Add(new CommandId(0, 45), new AC2PCommand() { firstByte = 0, secondByte = 45, name = "биты реакции на неисправности" });
            commands.Add(new CommandId(0, 65), new AC2PCommand() { firstByte = 0, secondByte = 65, name = "установить значение температуры" });
            commands.Add(new CommandId(0, 66), new AC2PCommand() { firstByte = 0, secondByte = 66, name = "сброс неисправностей" });
            commands.Add(new CommandId(0, 67), new AC2PCommand() { firstByte = 0, secondByte = 67, name = "вход/выход в стадию M (ручное управление) или T (тестирование блока управления)" });
            commands.Add(new CommandId(0, 68), new AC2PCommand() { firstByte = 0, secondByte = 68, name = "задание параметров устройств в стадии M (ручное управление)" });
            commands.Add(new CommandId(0, 69), new AC2PCommand() { firstByte = 0, secondByte = 69, name = "управление устройствами" });
            commands.Add(new CommandId(0, 70), new AC2PCommand() { firstByte = 0, secondByte = 69, name = "Включение/Выключение устройств" });
            #endregion

            commands[new CommandId(0, 0)].Parameters.Add(new AC2PParameter() { StartByte = 2, BitLength = 8, GetMeaning = i => ("Устройство: " + Devices[i].Name + ";"), AnswerOnly = true }); ;
            commands[new CommandId(0, 0)].Parameters.Add(new AC2PParameter() { StartByte = 3, BitLength = 8, Meanings = { { 0, "12 Вольт" }, { 1, "24 Вольта" } }, AnswerOnly = true });
            commands[new CommandId(0, 0)].Parameters.Add(new AC2PParameter() { StartByte = 4, BitLength = 8, Name = "Верия ПО", AnswerOnly = true });
            commands[new CommandId(0, 0)].Parameters.Add(new AC2PParameter() { StartByte = 5, BitLength = 8, Name = "Модификация ПО", AnswerOnly = true });

            commands[new CommandId(0, 1)].Parameters.Add(new AC2PParameter() { StartByte = 2, BitLength = 16, Name = "Время работы", Unit = "c" });

            commands[new CommandId(0, 4)].Parameters.Add(new AC2PParameter() { StartByte = 2, BitLength = 16, Name = "Время работы", Unit = "c" });

            commands[new CommandId(0, 6)].Parameters.Add(new AC2PParameter() { StartByte = 2, BitLength = 16, Name = "Время работы", Unit = "c" });
            commands[new CommandId(0, 6)].Parameters.Add(new AC2PParameter() { StartByte = 4, BitLength = 4, Name = "Режим работы", Meanings = { { 0, "обычный" }, { 1, "экономичный" }, { 2, "догреватель" }, { 3, "отопление" }, { 4, "отопительные системы" } } });
            commands[new CommandId(0, 6)].Parameters.Add(new AC2PParameter() { StartByte = 4, StartBit = 4, BitLength = 4, Name = "Режим догрева", Meanings = { { 0, "отключен" }, { 1, "автоматический" }, { 2, "ручной" } } });
            commands[new CommandId(0, 6)].Parameters.Add(new AC2PParameter() { StartByte = 5, BitLength = 16, Name = "Уставка температуры", Unit = "°С" });
            commands[new CommandId(0, 6)].Parameters.Add(new AC2PParameter() { StartByte = 7, BitLength = 2, Name = "Работа помпы в ждущем режиме", Meanings = defMeaningsOnOff });
            commands[new CommandId(0, 6)].Parameters.Add(new AC2PParameter() { StartByte = 7, BitLength = 2, StartBit = 2, Name = "Работа помпы при заведённом двигателе", Meanings = defMeaningsOnOff });

            commands[new CommandId(0, 7)].Parameters.Add(new AC2PParameter() { StartByte = 2, BitLength = 8, Name = "Номер мощности" });

            commands[new CommandId(0, 7)].Parameters.Add(new AC2PParameter() { StartByte = 3, BitLength = 16, Name = "Температура перехода на большую мощность", AnswerOnly = true });
            commands[new CommandId(0, 7)].Parameters.Add(new AC2PParameter() { StartByte = 4, BitLength = 16, Name = "Температура перехода на меньшую мощность", AnswerOnly = true });

            commands[new CommandId(0, 8)].Parameters.Add(new AC2PParameter() { StartByte = 2, BitLength = 2, StartBit = 0, Name = "Состояние клапана 1", Meanings = defMeaningsOnOff });
            commands[new CommandId(0, 8)].Parameters.Add(new AC2PParameter() { StartByte = 2, BitLength = 2, StartBit = 2, Name = "Состояние клапана 2", Meanings = defMeaningsOnOff });
            commands[new CommandId(0, 8)].Parameters.Add(new AC2PParameter() { StartByte = 2, BitLength = 2, StartBit = 4, Name = "Состояние клапана 3", Meanings = defMeaningsOnOff });
            commands[new CommandId(0, 8)].Parameters.Add(new AC2PParameter() { StartByte = 2, BitLength = 2, StartBit = 6, Name = "Состояние клапана 4", Meanings = defMeaningsOnOff });
            commands[new CommandId(0, 8)].Parameters.Add(new AC2PParameter() { StartByte = 3, BitLength = 2, StartBit = 0, Name = "Состояние клапана 5", Meanings = defMeaningsOnOff });
            commands[new CommandId(0, 8)].Parameters.Add(new AC2PParameter() { StartByte = 3, BitLength = 2, StartBit = 2, Name = "Состояние клапана 6", Meanings = defMeaningsOnOff });
            commands[new CommandId(0, 8)].Parameters.Add(new AC2PParameter() { StartByte = 3, BitLength = 2, StartBit = 4, Name = "Состояние клапана 7", Meanings = defMeaningsOnOff });
            commands[new CommandId(0, 8)].Parameters.Add(new AC2PParameter() { StartByte = 3, BitLength = 2, StartBit = 6, Name = "Состояние клапана 8", Meanings = defMeaningsOnOff });
            commands[new CommandId(0, 8)].Parameters.Add(new AC2PParameter() { StartByte = 4, BitLength = 1, StartBit = 0, Meanings = { { 0, "Сбросить неисправности" } } });

            commands[new CommandId(0, 9)].Parameters.Add(new AC2PParameter() { StartByte = 2, BitLength = 16, Name = "Время работы", Unit = "c" });
            commands[new CommandId(0, 9)].Parameters.Add(new AC2PParameter() { StartByte = 4, BitLength = 4, Name = "Режим работы", Meanings = { { 0, "не используется" }, { 1, "работа по температуре платы" }, { 2, "работа по температуре пульта" }, { 3, "работа по температуре выносного датчика" }, { 4, "работа по мощности" } } });
            commands[new CommandId(0, 9)].Parameters.Add(new AC2PParameter() { StartByte = 4, StartBit = 4, BitLength = 2, Name = "Разрешение/запрещение ждущего режима (при работе по датчику температуры)", Meanings = defMeaningsAllow });
            commands[new CommandId(0, 9)].Parameters.Add(new AC2PParameter() { StartByte = 4, StartBit = 6, BitLength = 2, Name = "Разрешение вращения нагнетателя воздуха на ждущем режиме", Meanings = defMeaningsAllow });
            commands[new CommandId(0, 9)].Parameters.Add(new AC2PParameter() { StartByte = 5, BitLength = 16, Name = "Уставка температуры помещения", Unit = "°С" });
            commands[new CommandId(0, 9)].Parameters.Add(new AC2PParameter() { StartByte = 7, BitLength = 4, Name = "Заданное значение мощности" });

            commands[new CommandId(0, 10)].Parameters.Add(new AC2PParameter() { StartByte = 2, BitLength = 16, Name = "Время работы", Unit = "c" });

            commands[new CommandId(0, 20)].Parameters.Add(new AC2PParameter() { StartByte = 2, BitLength = 16, Name = "Калибровочное значение термопары 1", AnswerOnly = true });
            commands[new CommandId(0, 20)].Parameters.Add(new AC2PParameter() { StartByte = 4, BitLength = 16, Name = "Калибровочное значение термопары 2", AnswerOnly = true });

            commands[new CommandId(0, 21)].Parameters.Add(new AC2PParameter() { StartByte = 2, BitLength = 8, Name = "Предделитель" });
            commands[new CommandId(0, 21)].Parameters.Add(new AC2PParameter() { StartByte = 3, BitLength = 8, Name = "Период ШИМ" });
            commands[new CommandId(0, 21)].Parameters.Add(new AC2PParameter() { StartByte = 5, BitLength = 8, Name = "Требуемая частота", Unit = "Гц" });

            commands[new CommandId(0, 22)].Parameters.Add(new AC2PParameter() { StartByte = 2, StartBit = 0, BitLength = 8, Name = "Действие после перезагрузки", Meanings = { { 0, "Остаться в загрузчике" }, { 1, "Переход в основную программу без зедержки" } } });

            commands[new CommandId(0, 45)].Parameters.Add(new AC2PParameter() { StartByte = 2, StartBit = 0, BitLength = 2, Name = "Игнорирование всех неисправностей", Meanings = defMeaningsYesNo });
            commands[new CommandId(0, 45)].Parameters.Add(new AC2PParameter() { StartByte = 2, StartBit = 2, BitLength = 2, Name = "Игнорирование неисправностей ТН", Meanings = defMeaningsYesNo });
            commands[new CommandId(0, 45)].Parameters.Add(new AC2PParameter() { StartByte = 2, StartBit = 4, BitLength = 2, Name = "Игнорирование срывов пламени ", Meanings = defMeaningsYesNo });
            commands[new CommandId(0, 45)].Parameters.Add(new AC2PParameter() { StartByte = 2, StartBit = 6, BitLength = 2, Name = "Игнорирование неисправностей свечи", Meanings = defMeaningsYesNo });
            commands[new CommandId(0, 45)].Parameters.Add(new AC2PParameter() { StartByte = 3, StartBit = 0, BitLength = 2, Name = "Игнорирование неисправностей НВ", Meanings = defMeaningsYesNo });
            commands[new CommandId(0, 45)].Parameters.Add(new AC2PParameter() { StartByte = 3, StartBit = 2, BitLength = 2, Name = "Игнорирование неисправностей датчиков", Meanings = defMeaningsYesNo });
            commands[new CommandId(0, 45)].Parameters.Add(new AC2PParameter() { StartByte = 3, StartBit = 4, BitLength = 2, Name = "Игнорирование неисправностей помпы", Meanings = defMeaningsYesNo });
            commands[new CommandId(0, 45)].Parameters.Add(new AC2PParameter() { StartByte = 3, StartBit = 6, BitLength = 2, Name = "Игнорирование перегревов", Meanings = defMeaningsYesNo });

            commands[new CommandId(0, 65)].Parameters.Add(new AC2PParameter() { StartByte = 2, StartBit = 0, BitLength = 8, Name = "", Meanings = { { 7, "Температура жидкости" }, { 10, "Температура перегрева" }, { 12, "Температура пламени" }, { 13, "Температура корпуса" }, { 27, "Температура воздуха" } } });
            commands[new CommandId(0, 65)].Parameters.Add(new AC2PParameter() { StartByte = 3, BitLength = 16, Name = "Значение температуры", Unit = "°C" });

            commands[new CommandId(0, 67)].Parameters.Add(new AC2PParameter() { StartByte = 2, StartBit = 0, BitLength = 2, Name = "", Meanings = { { 0, "Выход из режима М" }, { 1, "Вход в режим М" } } });
            commands[new CommandId(0, 67)].Parameters.Add(new AC2PParameter() { StartByte = 2, StartBit = 2, BitLength = 2, Name = "", Meanings = { { 0, "Выход из режима Т" }, { 1, "Вход в режим Т" } } });

            commands[new CommandId(0, 68)].Parameters.Add(new AC2PParameter() { StartByte = 2, StartBit = 0, BitLength = 2, Name = "Состояние помпы", Meanings = defMeaningsOnOff });
            commands[new CommandId(0, 68)].Parameters.Add(new AC2PParameter() { StartByte = 3, StartBit = 0, BitLength = 8, Name = "Обороты нагнетателя", Unit = "об/с" });
            commands[new CommandId(0, 68)].Parameters.Add(new AC2PParameter() { StartByte = 4, StartBit = 0, BitLength = 8, Name = "Мощность свечи", Unit = "%" });
            commands[new CommandId(0, 68)].Parameters.Add(new AC2PParameter() { StartByte = 5, StartBit = 0, BitLength = 16, Name = "Частота ТН", a = 0.01, Unit = "Гц" });

            commands[new CommandId(0, 68)].Parameters.Add(new AC2PParameter() { StartByte = 2, StartBit = 0, BitLength = 8, Name = "Тип устройства", Meanings = { { 0, "ТН, Гц*10" }, { 1, "Реле(0/1)" }, { 2, "Свеча, %" }, { 3, "Помпа,%" }, { 4, "Шим НВ,%" }, { 23, "Обороты НВ, об/с" } } });
            commands[new CommandId(0, 68)].Parameters.Add(new AC2PParameter() { StartByte = 3, StartBit = 0, BitLength = 16, Name = "Значение" });

            commands[new CommandId(0, 70)].Parameters.Add(new AC2PParameter() { StartByte = 2, StartBit = 0, BitLength = 2, Name = "Состояние ТН", Meanings = defMeaningsOnOff });
            commands[new CommandId(0, 70)].Parameters.Add(new AC2PParameter() { StartByte = 2, StartBit = 2, BitLength = 2, Name = "Состояние реле", Meanings = defMeaningsOnOff });
            commands[new CommandId(0, 70)].Parameters.Add(new AC2PParameter() { StartByte = 2, StartBit = 4, BitLength = 2, Name = "Состояние свечи", Meanings = defMeaningsOnOff });
            commands[new CommandId(0, 70)].Parameters.Add(new AC2PParameter() { StartByte = 2, StartBit = 6, BitLength = 2, Name = "Состояние помпы", Meanings = defMeaningsOnOff });
            commands[new CommandId(0, 70)].Parameters.Add(new AC2PParameter() { StartByte = 3, StartBit = 0, BitLength = 2, Name = "Состояние НВ", Meanings = defMeaningsOnOff });


            #region PGN parameters initialise

            PGNs[3].parameters.Add(new AC2PParameter() { Name = "SPN", BitLength = 16, StartByte = 0 });

            PGNs[4].parameters.Add(new AC2PParameter() { Name = "SPN", BitLength = 16, StartByte = 0 });
            PGNs[4].parameters.Add(new AC2PParameter() { Name = "Значение", BitLength = 32, StartBit = 0, StartByte = 2 });

            PGNs[5].parameters.Add(new AC2PParameter() { Name = "SPN", BitLength = 16, StartByte = 0 });
            PGNs[5].parameters.Add(new AC2PParameter() { Name = "Значение", BitLength = 32, StartBit = 0, StartByte = 2 });

            PGNs[6].parameters.Add(new AC2PParameter() { Name = "PGN", BitLength = 16, StartByte = 0, GetMeaning = x => { if (PGNs.ContainsKey(x)) return PGNs[x].name; else return "Нет такого PGN"; } });

            PGNs[7].parameters.Add(new AC2PParameter() { Name = "Команда", BitLength = 8, StartBit = 0, StartByte = 0, Meanings = { { 0, "Стереть конфигурацию" }, { 1, "Запись параметра в ОЗУ" }, { 2, "Запись всех параметров во Flash" }, { 3, "Чтение параметра по номеру" }, { 4, "Успешный ответ на запрос" }, { 5, "Невозможно выполнить" } } });
            PGNs[7].parameters.Add(new AC2PParameter() { Name = "Запрошенная команда", BitLength = 8, StartBit = 0, StartByte = 1, Meanings = { { 0, "Стереть конфигурацию" }, { 1, "Запись параметра в ОЗУ" }, { 2, "Запись всех параметров во Flash" }, { 3, "Чтение параметра по номеру" }, { 255, "" } } });
            PGNs[7].parameters.Add(new AC2PParameter() { Name = "Параметр", BitLength = 16, StartBit = 0, StartByte = 2, GetMeaning = x => configParameters[x]?.NameRu });
            PGNs[7].parameters.Add(new AC2PParameter() { Name = "Value", BitLength = 32, StartBit = 0, StartByte = 4, AnswerOnly = true });

            PGNs[8].parameters.Add(new AC2PParameter() { Name = "Команда", BitLength = 4, StartBit = 0, StartByte = 0, Meanings = { { 0, "Стереть ЧЯ" }, { 3, "Чтение ЧЯ" }, { 4, "Ответ" }, { 6, "Чтение параметра (из paramsname.h)" } } });
            PGNs[8].parameters.Add(new AC2PParameter() { Name = "Тип:", BitLength = 2, StartBit = 4, StartByte = 0, Meanings = { { 0, "Общие данные" }, { 1, "Неисправности" } } });
            PGNs[8].parameters.Add(new AC2PParameter() { Name = "Номер пары", CustomDecoder = d => { if ((d[0] & 0xF) == 3) return "Номер пары:" + (d[4] * 0x100 + d[5]).ToString() + ";"; else return ""; } });
            PGNs[8].parameters.Add(new AC2PParameter() { Name = "Номер параметра", CustomDecoder = d => { if ((d[0] & 0xF) == 6) return "Номер параметра:" + (d[4] * 0x100 + d[5]).ToString() + ";"; else return ""; } });
            PGNs[8].parameters.Add(new AC2PParameter() { Name = "Число пар", CustomDecoder = d => { if ((d[0] & 0xF) == 6) return "Запрошено пар:" + (d[6] * 0x100 + d[7]).ToString() + ";"; else return ""; } });
            PGNs[8].parameters.Add(new AC2PParameter() { Name = "Номер параметра", CustomDecoder = d => { if (d[0] == 4) return "Параметр:" + (d[2] * 256 + d[3]).ToString() + ";"; else return ""; } });
            PGNs[8].parameters.Add(new AC2PParameter() { Name = "Значение параметра", CustomDecoder = d => { if (d[0] == 4) return "Значение:" + (d[4] * 0x1000000 + d[5] * 0x10000 + d[6] * 0x100 + d[7]).ToString() + ";"; else return ""; }, AnswerOnly = true });


            PGNs[10].parameters.Add(new AC2PParameter() { Name = "Стадия", BitLength = 8, StartByte = 0, Meanings = Stages, Var = 1 });
            PGNs[10].parameters.Add(new AC2PParameter() { Name = "Режим", BitLength = 8, StartByte = 1, Var = 2 });
            PGNs[10].parameters.Add(new AC2PParameter() { Name = "Код неисправности", BitLength = 8, StartByte = 2, Var = 24, Meanings = ErrorNames });
            PGNs[10].parameters.Add(new AC2PParameter() { Name = "Помпа неисправна", BitLength = 2, StartByte = 3, Meanings = defMeaningsYesNo });
            PGNs[10].parameters.Add(new AC2PParameter() { Name = "Код предупреждения", BitLength = 8, StartByte = 4 });
            PGNs[10].parameters.Add(new AC2PParameter() { Name = "Количество морганий", BitLength = 8, StartByte = 5, Var = 25 });

            PGNs[11].parameters.Add(new AC2PParameter() { Name = "Напряжение питания", BitLength = 16, StartByte = 0, a = 0.1, Unit = "В", Var = 5 });
            PGNs[11].parameters.Add(new AC2PParameter() { Name = "Атмосферное давление", BitLength = 8, StartByte = 2, Unit = "кПа" });
            PGNs[11].parameters.Add(new AC2PParameter() { Name = "Ток двигателя, значения АЦП", BitLength = 16, StartByte = 3 });

            PGNs[12].parameters.Add(new AC2PParameter() { Name = "Заданные обороты нагнетателя воздуха", BitLength = 8, StartByte = 0, Unit = "об/с", Var = 15 });
            PGNs[12].parameters.Add(new AC2PParameter() { Name = "Измеренные обороты нагнетателя воздуха,", BitLength = 8, StartByte = 1, Unit = "об/с", Var = 16 });
            PGNs[12].parameters.Add(new AC2PParameter() { Name = "Заданная частота ТН", BitLength = 16, StartByte = 2, a = 0.01, Unit = "Гц", Var = 17 });
            PGNs[12].parameters.Add(new AC2PParameter() { Name = "Реализованная частота ТН", BitLength = 16, StartByte = 4, a = 0.01, Unit = "Гц", Var = 18 });
            PGNs[12].parameters.Add(new AC2PParameter() { Name = "Мощность свечи", BitLength = 8, StartByte = 6, Unit = "%", Var = 21 });
            PGNs[12].parameters.Add(new AC2PParameter() { Name = "Состояние помпы", BitLength = 2, StartByte = 7, Meanings = defMeaningsOnOff, Var = 46 });
            PGNs[12].parameters.Add(new AC2PParameter() { Name = "Состояние реле печки кабины", BitLength = 2, StartByte = 7, StartBit = 2, Meanings = defMeaningsOnOff, Var = 45 });
            PGNs[12].parameters.Add(new AC2PParameter() { Name = "Состояние состояние канала сигнализации", BitLength = 2, StartByte = 7, StartBit = 4, Meanings = defMeaningsOnOff, Var = 47 });

            PGNs[13].parameters.Add(new AC2PParameter() { Name = "Температура ИП", BitLength = 16, StartByte = 0, Unit = "°C", Var = 6 });
            PGNs[13].parameters.Add(new AC2PParameter() { Name = "Температура платы/процессора", BitLength = 8, StartByte = 2, b = -75, Unit = "°C", Var = 59 });
            PGNs[13].parameters.Add(new AC2PParameter() { Name = "Температура жидкости", BitLength = 8, StartByte = 3, b = -75, Unit = "°C", Var = 40 });
            PGNs[13].parameters.Add(new AC2PParameter() { Name = "Температура перегрева", BitLength = 8, StartByte = 4, b = -75, Unit = "°C", Var = 41 });

            PGNs[14].parameters.Add(new AC2PParameter() { Name = "Минимальная температура пламени перед розжигом", BitLength = 16, StartByte = 0, Unit = "°C", Var = 36 });
            PGNs[14].parameters.Add(new AC2PParameter() { Name = "Граница срыва пламени", BitLength = 16, StartByte = 2, Unit = "°C", Var = 37 });
            PGNs[14].parameters.Add(new AC2PParameter() { Name = "Граница срыва пламени на прогреве", BitLength = 16, StartByte = 4, Unit = "°C" });
            PGNs[14].parameters.Add(new AC2PParameter() { Name = "Скорость изменения температуры ИП", BitLength = 16, StartByte = 6, Unit = "°C" });


            PGNs[15].parameters.Add(new AC2PParameter() { Name = "0 канал АЦП ", BitLength = 16, StartByte = 0, Var = 49 });
            PGNs[15].parameters.Add(new AC2PParameter() { Name = "1 канал АЦП ", BitLength = 16, StartByte = 2, Var = 50 });
            PGNs[15].parameters.Add(new AC2PParameter() { Name = "2 канал АЦП ", BitLength = 16, StartByte = 4, Var = 51 });
            PGNs[15].parameters.Add(new AC2PParameter() { Name = "3 канал АЦП ", BitLength = 16, StartByte = 6, Var = 52 });

            PGNs[16].parameters.Add(new AC2PParameter() { Name = "4 канал АЦП ", BitLength = 16, StartByte = 0, Var = 53 });
            PGNs[16].parameters.Add(new AC2PParameter() { Name = "5 канал АЦП ", BitLength = 16, StartByte = 2, Var = 54 });
            PGNs[16].parameters.Add(new AC2PParameter() { Name = "6 канал АЦП ", BitLength = 16, StartByte = 4, Var = 55 });
            PGNs[16].parameters.Add(new AC2PParameter() { Name = "7 канал АЦП ", BitLength = 16, StartByte = 6, Var = 56 });

            PGNs[17].parameters.Add(new AC2PParameter() { Name = "8 канал АЦП ", BitLength = 16, StartByte = 0, Var = 57 });
            PGNs[17].parameters.Add(new AC2PParameter() { Name = "9 канал АЦП ", BitLength = 16, StartByte = 2, Var = 58 });
            PGNs[17].parameters.Add(new AC2PParameter() { Name = "10 канал АЦП ", BitLength = 16, StartByte = 4 });
            PGNs[17].parameters.Add(new AC2PParameter() { Name = "11 канал АЦП ", BitLength = 16, StartByte = 6 });

            PGNs[18].parameters.Add(new AC2PParameter() { Name = "Вид изделия", BitLength = 8, StartByte = 0, GetMeaning = i => Devices[i]?.Name });
            PGNs[18].parameters.Add(new AC2PParameter() { Name = "Напряжение питания", BitLength = 8, StartByte = 1, Meanings = { { 0, "12 Вольт" }, { 1, "24 Вольта" } } });
            PGNs[18].parameters.Add(new AC2PParameter() { Name = "Версия ПО", BitLength = 8, StartByte = 2 });
            PGNs[18].parameters.Add(new AC2PParameter() { Name = "Модификация ПО", BitLength = 8, StartByte = 3 });
            PGNs[18].parameters.Add(new AC2PParameter() { Name = "Дата релиза", BitLength = 24, StartByte = 5, GetMeaning = v => $"{v >> 16}.{(v >> 8) & 0xF}.{v & 0xFF}" });
            //PGNs[18].parameters.Add(new AC2PParameter() { Name = "День ", BitLength = 8, StartByte = 5 });    Не красиво выглядит...луше одной строкой
            //PGNs[18].parameters.Add(new AC2PParameter() { Name = "Месяц", BitLength = 8, StartByte = 6 });
            //PGNs[18].parameters.Add(new AC2PParameter() { Name = "Год", BitLength = 8, StartByte = 7 });

            PGNs[19].parameters.Add(new AC2PParameter() { Name = "Подогреватель", BitLength = 2, StartBit = 0, StartByte = 1, PackNumber = 1, Meanings = defMeaningsOnOff });
            PGNs[19].parameters.Add(new AC2PParameter() { Name = "Помпы", BitLength = 2, StartBit = 2, StartByte = 1, PackNumber = 1, Meanings = defMeaningsOnOff });
            PGNs[19].parameters.Add(new AC2PParameter() { Name = "Вода", BitLength = 2, StartBit = 4, StartByte = 1, PackNumber = 1, Meanings = defMeaningsOnOff });
            PGNs[19].parameters.Add(new AC2PParameter() { Name = "Быстрый нагрев воды", BitLength = 2, StartBit = 6, StartByte = 1, PackNumber = 1, Meanings = defMeaningsOnOff });
            PGNs[19].parameters.Add(new AC2PParameter() { Name = "Температура бака", BitLength = 8, StartByte = 2, b = -75, Unit = "°С", PackNumber = 1 });
            PGNs[19].parameters.Add(new AC2PParameter() { Name = "Атмосферное давление", BitLength = 8, StartByte = 3, Unit = "кПа", PackNumber = 1 });
            PGNs[19].parameters.Add(new AC2PParameter() { Name = "Сработал датчик бытовой воды", BitLength = 2, StartByte = 4, PackNumber = 1, Meanings = defMeaningsYesNo });

            PGNs[19].parameters.Add(new AC2PParameter() { Name = "Уставка температуры жидкости подогревателя для перехода в ждущий.", BitLength = 8, StartByte = 1, b = -75, PackNumber = 2 });
            PGNs[19].parameters.Add(new AC2PParameter() { Name = "Уставка температуры жидкости подогревателя для выхода из ждущего.", BitLength = 8, StartByte = 2, b = -75, PackNumber = 2 });
            PGNs[19].parameters.Add(new AC2PParameter() { Name = "Уставка температуры жидкости подогревателя для выхода из ждущего при разборе воды.", BitLength = 8, StartByte = 3, b = -75, PackNumber = 2 });
            PGNs[19].parameters.Add(new AC2PParameter() { Name = "Уставка температуры бака для перехода в ждущий.", BitLength = 8, StartByte = 4, b = -75, PackNumber = 2 });
            PGNs[19].parameters.Add(new AC2PParameter() { Name = "Уставка температуры бака для выхода из ждущего.", BitLength = 8, StartByte = 5, b = -75, PackNumber = 2 });
            PGNs[19].parameters.Add(new AC2PParameter() { Name = "Уставка температуры бака для выхода из ждущего при разборе воды.", BitLength = 8, StartByte = 6, b = -75, PackNumber = 2 });

            PGNs[20].parameters.Add(new AC2PParameter() { Name = "Код неисправности", BitLength = 8, StartByte = 0 });
            PGNs[20].parameters.Add(new AC2PParameter() { Name = "Количество морганий", BitLength = 8, StartByte = 1 });
            PGNs[20].parameters.Add(new AC2PParameter() { Name = "Байт неисправностей 1", BitLength = 8, StartByte = 2 });
            PGNs[20].parameters.Add(new AC2PParameter() { Name = "Байт неисправностей 2", BitLength = 8, StartByte = 3 });
            PGNs[20].parameters.Add(new AC2PParameter() { Name = "Байт неисправностей 3", BitLength = 8, StartByte = 4 });
            PGNs[20].parameters.Add(new AC2PParameter() { Name = "Байт неисправностей 4", BitLength = 8, StartByte = 5 });
            PGNs[20].parameters.Add(new AC2PParameter() { Name = "Байт неисправностей 5", BitLength = 8, StartByte = 6 });
            PGNs[20].parameters.Add(new AC2PParameter() { Name = "Байт неисправностей 6", BitLength = 8, StartByte = 7 });

            PGNs[29].parameters.Add(new AC2PParameter() { Name = "Атмосферное давление", BitLength = 8, StartByte = 1, Unit = "кПа", PackNumber = 1 });
            PGNs[29].parameters.Add(new AC2PParameter() { Name = "Среднее максимальное значение давления", BitLength = 24, StartByte = 2, Unit = "кПа", a = 0.001, PackNumber = 1 });
            PGNs[29].parameters.Add(new AC2PParameter() { Name = "Среднее минимальное значение давления", BitLength = 24, StartByte = 4, Unit = "кПа", a = 0.001, PackNumber = 1 });

            PGNs[29].parameters.Add(new AC2PParameter() { Name = "Разница между max и min  значениями", BitLength = 16, StartByte = 1, a = 0.01, Unit = "кПа", PackNumber = 2 });
            PGNs[29].parameters.Add(new AC2PParameter() { Name = "Флаг появления пламени по пульсации давления", BitLength = 2, StartByte = 3, Meanings = defMeaningsYesNo, PackNumber = 2 });
            PGNs[29].parameters.Add(new AC2PParameter() { Name = "Атмосферное давление", BitLength = 24, StartByte = 4, Unit = "кПа", a = 0.001, PackNumber = 2, Var = 60 });

            PGNs[31].parameters.Add(new AC2PParameter() { Name = "Время работы", BitLength = 32, StartByte = 0, Unit = "с", Var = 3 });
            PGNs[31].parameters.Add(new AC2PParameter() { Name = "Время работы на режиме", BitLength = 32, StartByte = 4, Unit = "с", Var = 4 });
            #endregion

            ErrorNames = new Dictionary<int, string>() {
              {1  , "overheat"},
{2  , "overheat"},
{3  , "error of the overheat temp. sensor"},
{4  , "error of the liquid temp. sensor"},
{5  , "open circuit of the flame temp. sensor"},
{9  , "glow plug error"},
{10 , "fan speed does not correspond to the defined"},
{12 , "high supply voltage"},
{13 , "no ignition"},
{14 , "water pump error"},
{15 , "low supply voltage"},
{16 , "body temp.sensor does not cool down"},
{17 , "short circuit of the fuel pump"},
{22 , "open circuit of the fuel pump"},
{27 , "fan does not rotate"},
{28 , "fan self-rotation"},
{29 , "exceeding the limit of flame blowout"},
{36 , "overheating of the flame indicator"},
{40 , "no connection with the heater"},
{45 , "open circuit of the tank temp. sensor"},
{46 , "short circuit of the tank temp. sensor"},
{53 , "open circuit of the flow sensor"},
{54 , "short circuit of the flow sensor"},
{55 , "open circuit of the air temp. sensor"},
{56 , "short circuit of the air temp. sensor"},
{57 , "short circuit of the zone 1 temp. sensor"},
{58 , "open circuit of the zone 1 temp. sensor"},
{59 , "short circuit of the zone 2 temp. sensor"},
{60 , "open circuit of the zone 2 temp. sensor"},
{61 , "short circuit of the zone 3 temp. sensor"},
{62 , "open circuit of the zone 3 temp. sensor"},
{63 , "short circuit of the zone 4 temp. sensor"},
{64 , "open circuit of the zone 4 temp. sensor"},
{65 , "short circuit of the zone 5 temp. sensor"},
{66 , "open circuit of the zone 5 temp. sensor"},
{69 , "short circuit of the pump 1"},
{70 , "open circuit of the pump 1"},
{71 , "short circuit of the pump 2"},
{72 , "open circuit of the pump 2"},
{73 , "short circuit of the pump 3"},
{74 , "open circuit of the pump 3"},
{75 , "short circuit of the pump 4"},
{76 , "open circuit of the pump 4"},
{77 , "short circuit of the pump 5"},
{78 , "open circuit of the pump 5"},
{79 , "short circuit of the fan 1"},
{80 , "open circuit of the fan 1"},
{81 , "short circuit of the fan 2"},
{82 , "open circuit of the fan 2"},
{83 , "short circuit of the fan 3"},
{84 , "open circuit of the fan 3"},
{85 , "short circuit of the fan 4"},
{86 , "open circuit of the fan 4"},
{87 , "short circuit of the fan 5"},
{88 , "open circuit of the fan 5"},
{91 , "liquid level too low"},
{92 , "liquid level too high"},
{93 , "level sensor short circuit"},
{94 , "level sensor open circuit"} };

        }

    }

}




