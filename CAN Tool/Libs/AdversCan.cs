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
using Can_Adapter;
using CAN_Tool.ViewModels.Base;
namespace AdversCan
{
    public class PGN
    {
        public int id;
        public string name = "";
        public bool multipack;
        public List<AC2PParameter> parameters = new List<AC2PParameter>();
    }
    public class AC2PParameter
    {
        public int Id;
        public string ParamsName = "";//from ParamsName.h
        public int StartByte;   //Начальный байт в пакете
        public int StartBit;    //Начальный бит в байте
        public int BitLength;   //Ддина параметра в битах
        public string Name { set; get; }     //Имя параметра
        public string Unit = "";     //Единица измерения
        public double a = 1;         //коэффициент приведение
        public double b = 0;         //смещение
        //value = rawData*a+b
        public string OutputFormat = "";
        public Dictionary<int, string> meanings = new Dictionary<int, string>();
        public Func<int, string> GetMeaning;
        public Func<byte[], string> CustomDecoder;
        public string Tip = "";
        public int PackNumber;
        public int Value { set; get; }

    }
    public class AC2PCommand
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
        private List<AC2PParameter> _Parameters = new List<AC2PParameter>();
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


        public int PGN
        {
            get { return (ID >> 20) & 0x1FF; }
            set
            {
                if (value > 511)
                    throw new ArgumentException("PGN can't be over 511");
                if (ID == value)
                    return;
                ID &= ~(0x1FF << 20);
                ID |= value << 20;
                PropChanged("PGN");
            }
        }
        public int ReceiverType
        {
            get { return (ID >> 13) & 0b1111111; }
            set
            {
                if (value > 127)
                    throw new ArgumentException("ReceiverType can't be over 127");
                if (ReceiverType == value)
                    return;
                ID &= ~(0x7F << 13);
                ID |= value << 13;
                PropChanged("ReceiverType");
            }
        }
        public int ReceiverAddress
        {
            get { return (ID >> 10) & 0b111; }
            set
            {
                if (value > 7)
                    throw new ArgumentException("ReceiverAddress can't be over 7");
                if (ReceiverAddress == value)
                    return;
                ID &= ~(0x3 << 10);
                ID |= value << 10;
                PropChanged("ReceiverAddress");
            }
        }
        public int TransmitterType
        {
            get { return (ID >> 3) & 0x7F; }
            set
            {
                if (value > 127)
                    throw new ArgumentException("TransmitterType can't be over 127");
                if (TransmitterType == value)
                    return;
                ID &= ~(0x7F << 3);
                ID |= value << 3;
                PropChanged("TransmitterType");
            }
        }
        public int TransmitterAddress
        {
            get { return ID & 0b111; }
            set
            {
                if (value > 7)
                    throw new ArgumentException("TransmitterAddress can't be over 7");
                if (TransmitterAddress == value)
                    return;
                ID &= ~(0x3);
                ID |= value;
                PropChanged("TransmitterAddress");
            }
        }

        public DeviceId TransmitterId
        {
            get { return new DeviceId(TransmitterType, TransmitterAddress); }
            set { TransmitterType = value.Type; TransmitterAddress = value.Address; }
        }
        public DeviceId ReceiverId
        {
            get { return new DeviceId(ReceiverType, ReceiverAddress); }
            set { ReceiverType = value.Type; ReceiverAddress = value.Address; }
        }

        public CommandId? Command
        {
            get
            {
                if (PGN != 1 && PGN != 2) return null;
                return new CommandId(Data[0], Data[1]);
            }
            set
            {
                if (value == null) throw new ArgumentNullException("Null is not an option");
                if (PGN != 1 && PGN != 2) throw new Exception("Use Command property only for Command PGNs (1 and 2)");
                Data[0] = value.Value.firstByte;
                Data[1] = value.Value.secondByte;
            }

        }

        public string printParameter(AC2PParameter p)
        {
            StringBuilder retString = new StringBuilder();
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
            if (p.meanings != null && p.meanings.ContainsKey(rawValue))
                retString.Append(rawValue.ToString() + " - " + p.meanings[rawValue]);
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
            StringBuilder retString = new StringBuilder();
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
            retString.Append($"{sender}({TransmitterAddress})->{receiver}({ReceiverAddress});");


            retString.Append(pgn.name + ';');
            if (pgn.multipack)
                retString.Append($"Мультипакет №{Data[0]};");
            if (PGN == 1 && AC2P.commands.ContainsKey(new CommandId(Data[0], Data[1])))
            {
                AC2PCommand cmd = AC2P.commands[new CommandId(Data[0], Data[1])];
                retString.Append(cmd.name + ";");
                if (cmd.Parameters != null)
                    foreach (AC2PParameter p in cmd.Parameters)
                        retString.Append(printParameter(p));
            }
            if (pgn.parameters != null)
                foreach (AC2PParameter p in pgn.parameters)
                    if (!pgn.multipack || Data[0] == p.PackNumber)
                        retString.Append(printParameter(p));
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
    public class DeviceStatus
    {
        public Dictionary<string, double> Variables;
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

    public class ReadedVariable : ViewModel, IUpdatable<ReadedVariable>
    {
        int id;
        public int Id { get => id; set => id = value; }

        int _value;
        public int Value
        {
            get => _value;
            set
            {
                if (value == _value) return;
                Set(ref _value, value);
            }
        }
        public event PropertyChangedEventHandler PropertyChanged;

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
                    return $"{AC2P.VariablesNames[id]}: {_value}";
                else
                    return "Параметр# " + id.ToString();
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
        private UpdatableList<ReadedVariable> _variables = new UpdatableList<ReadedVariable>();

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
            StringBuilder retString = new StringBuilder("");
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
                    return $"Код {AC2P.ErrorNames[error.Value]}";
            }
        }

        void onChange(string propName)
        {
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propName));
        }
    }
    public class ConnectedDevice
    {
        public DeviceId ID;

        BindingList<Variable> _variables = new BindingList<Variable>();

        public BindingList<Variable> Variables => _variables;

        private UpdatableList<ReadedParameter> _readedParameters = new UpdatableList<ReadedParameter>();
        public UpdatableList<ReadedParameter> readedParameters => _readedParameters;

        private UpdatableList<ReadedBlackBoxValue> _bbValues = new UpdatableList<ReadedBlackBoxValue>();
        public UpdatableList<ReadedBlackBoxValue> BBValues => _bbValues;


        public BBError currentError { set; get; }
        private UpdatableList<BBError> _BBErrors = new UpdatableList<BBError>();
        public UpdatableList<BBError> BBErrors => _BBErrors;


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
            if (!(obj is CommandId))
                return false;
            return ((CommandId)obj).GetHashCode() == GetHashCode();
        }
        public override string ToString()
        {
            return $"{firstByte:D2},{secondByte:D2}]";
        }

    }
    public struct DeviceId
    {
        public int Type;
        public int Address;
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
    public class AC2P
    {
        private SynchronizationContext UIContext;

        static readonly Dictionary<int, string> defMeaningsYesNo = new Dictionary<int, string>() { { 0, "Нет" }, { 1, "Да" }, { 2, "Нет данных" }, { 3, "Нет данных" } };
        static readonly Dictionary<int, string> defMeaningsOnOff = new Dictionary<int, string>() { { 0, "Выкл" }, { 1, "Вкл" }, { 2, "Нет данных" }, { 3, "Нет данных" } };
        static readonly Dictionary<int, string> defMeaningsAllow = new Dictionary<int, string>() { { 0, "Разрешено" }, { 1, "Запрещёно" }, { 2, "Нет данных" }, { 3, "Нет данных" } };
        static readonly Dictionary<int, string> Stages = new Dictionary<int, string>() { { 0, "STAGE_Z" }, { 1, "STAGE_P" }, { 2, "STAGE_H" }, { 3, "STAGE_W" }, { 4, "STAGE_F" }, { 5, "STAGE_T" }, { 6, "STAGE_M" } };

        public static Dictionary<int, PGN> PGNs = new Dictionary<int, PGN>();

        public static Dictionary<CommandId, AC2PCommand> commands = new Dictionary<CommandId, AC2PCommand>();

        public static Dictionary<int, configParameter> configParameters = new Dictionary<int, configParameter>();
        public static Dictionary<int, Variable> Variables = new Dictionary<int, Variable>();
        public static Dictionary<int, BbParameter> BbParameters = new Dictionary<int, BbParameter>();

        public static Dictionary<int, string> ParamtersNames = new Dictionary<int, string>();
        public static Dictionary<int, string> VariablesNames = new Dictionary<int, string>();
        public static Dictionary<int, string> BbParameterNames = new Dictionary<int, string>();

        private BindingList<ConnectedDevice> _connectedDevices = new BindingList<ConnectedDevice>();
        public BindingList<ConnectedDevice> ConnectedDevices => _connectedDevices;

        private UpdatableList<AC2PMessage> _messages = new UpdatableList<AC2PMessage>();
        public UpdatableList<AC2PMessage> Messages => _messages;

        public static Dictionary<int, string> ErrorNames = new Dictionary<int, string>();

        private CanAdapter canAdapter;

        public bool WaitingForBBErrors { get; set; } = false;


        public void ParseCanMessage(CanMessage msg)
        {
            AC2PMessage m = new AC2PMessage(msg);
            DeviceId id = m.TransmitterId;

            if (ConnectedDevices.FirstOrDefault(d => d.ID.Equals(id)) == null)
                UIContext.Send(x => ConnectedDevices.Add(new ConnectedDevice() { ID = id }), null);

            ConnectedDevice currentDevice = ConnectedDevices.First(d => d.ID.Equals(m.TransmitterId));

            if (m.PGN == 7) //Ответ на запрос параметра
            {
                if (m.Data[0] == 4) // Обрабатываем только упешные ответы на запросы
                {
                    int parameterId = m.Data[3] + m.Data[2] * 256;
                    uint parameterValue = ((uint)m.Data[4] * 0x1000000) + ((uint)m.Data[5] * 0x10000) + ((uint)m.Data[6] * 0x100) + (uint)m.Data[7];
                    if (parameterValue != 0xFFFFFFFF)
                        UIContext.Send(x => currentDevice.readedParameters.TryToAdd(new ReadedParameter() { Id = parameterId, Value = parameterValue }), null);
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
                            UIContext.Send(x => currentDevice.BBValues.TryToAdd(new ReadedBlackBoxValue() { Id = parameterId, Value = parameterValue }), null);
                    }
                    else
                    {
                        if (m.Data[2] == 0xFF && m.Data[3] == 0xFA) //Заголовок отчёта
                        {
                            if (currentDevice.currentError.Variables.Count > 0)
                            {
                                BBError e = new BBError();
                                currentDevice.currentError = e;
                                UIContext.Send(x => currentDevice.BBErrors.Add(e), null);
                            }
                        }
                        else
                        {
                            if (currentDevice.currentError == null || m.Data[2] == 0xFF && m.Data[3] == 0xFF) return;
                            ReadedVariable v = new ReadedVariable();
                            v.Id = m.Data[2] * 256 + m.Data[3];
                            v.Value = m.Data[4] * 0x1000000 + m.Data[5] * 0x10000 + m.Data[6] * 0x100 + m.Data[7];
                            UIContext.Send(x => currentDevice.currentError.Variables.TryToAdd(v), null);
                        }
                    }
                }

            }

            UIContext.Send(x => Messages.TryToAdd(m), null);

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

        public async void ReadCommonBlackBox(DeviceId id, CancellationToken ct)
        {
            WaitingForBBErrors = false;
            AC2PMessage msg = new AC2PMessage();
            msg.PGN = 8;
            msg.TransmitterAddress = 6;
            msg.TransmitterType = 126;
            msg.ReceiverAddress = id.Address;
            msg.ReceiverType = id.Type;
            msg.Data[0] = 6; //Read Single Param
            msg.Data[1] = 0xFF; //Read Param
            foreach (var p in BbParameters)
            {
                
                msg.Data[4] = (byte)(p.Key / 256);
                msg.Data[5] = (byte)(p.Key % 256);
                canAdapter.Transmit(msg);
                await Task.Run(() => Thread.Sleep(100));
                if (ct.IsCancellationRequested) break;
            }


        }

        public async void ReadErrorsBlackBox(DeviceId id, CancellationToken ct)
        {
            WaitingForBBErrors = true;
            ConnectedDevice currentDevice = ConnectedDevices.FirstOrDefault(i => i.ID.Equals(id));
            UIContext.Send(x =>
            {
                currentDevice.BBErrors.Clear();
                currentDevice.currentError = new BBError();
                currentDevice.BBErrors.Add(currentDevice.currentError);
            }, null);

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
                if (ct.IsCancellationRequested) break;
            }
        }
        public async void ReadAllParameters(DeviceId id)
        {
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
            }
        }

        public async void SaveParameters(DeviceId id)
        {
            var dev = ConnectedDevices.FirstOrDefault(d => d.ID.Equals(id));
            if (dev == null) return;
            AC2PMessage msg = new AC2PMessage();
            List<ReadedParameter> tempCollection = new List<ReadedParameter>();
            foreach (var p in dev.readedParameters)
                tempCollection.Add(p);

            foreach (var p in tempCollection)
            {
                msg.PGN = 7;
                msg.TransmitterAddress = 6;
                msg.TransmitterType = 126;
                msg.ReceiverAddress = id.Address;
                msg.ReceiverType = id.Type;
                msg.Data[0] = 1; //Write Param
                msg.Data[1] = 0xFF; //Read Param
                msg.Data[2] = (byte)(p.Id / 256);
                msg.Data[3] = (byte)(p.Id % 256);
                msg.Data[4] = (byte)((p.Value >> 24) & 0xFF);
                msg.Data[5] = (byte)((p.Value >> 16) & 0xFF);
                msg.Data[6] = (byte)((p.Value >> 8) & 0xFF);
                msg.Data[7] = (byte)((p.Value) & 0xFF);
                canAdapter.Transmit(msg);
                await Task.Run(() => Thread.Sleep(100));
            }
            msg.Data[0] = 0;
            canAdapter.Transmit(msg);
            await Task.Run(() => Thread.Sleep(1000));
            msg.Data[0] = 2;
            canAdapter.Transmit(msg);
        }

        public void EraseParameters(DeviceId id)
        {
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
        }

        private void Adapter_GotNewMessage(object sender, EventArgs e)
        {
            ParseCanMessage((e as GotMessageEventArgs).receivedMessage);
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

            commands[new CommandId(0, 0)].Parameters.Add(new AC2PParameter() { StartByte = 2, BitLength = 8, GetMeaning = i => ("Устройство: " + Devices[i].Name + ";") });
            commands[new CommandId(0, 0)].Parameters.Add(new AC2PParameter() { StartByte = 3, BitLength = 8, meanings = { { 0, "12 Вольт" }, { 1, "24 Вольта" } } });
            commands[new CommandId(0, 0)].Parameters.Add(new AC2PParameter() { StartByte = 4, BitLength = 8, Name = "Верия ПО" });
            commands[new CommandId(0, 0)].Parameters.Add(new AC2PParameter() { StartByte = 5, BitLength = 8, Name = "Модификация ПО" });

            commands[new CommandId(0, 1)].Parameters.Add(new AC2PParameter() { StartByte = 2, BitLength = 16, Name = "Время работы", Unit = "c" });

            commands[new CommandId(0, 4)].Parameters.Add(new AC2PParameter() { StartByte = 2, BitLength = 16, Name = "Время работы", Unit = "c" });

            commands[new CommandId(0, 6)].Parameters.Add(new AC2PParameter() { StartByte = 2, BitLength = 16, Name = "Время работы", Unit = "c" });
            commands[new CommandId(0, 6)].Parameters.Add(new AC2PParameter() { StartByte = 4, BitLength = 4, Name = "Режим работы", meanings = { { 0, "обычный" }, { 1, "экономичный" }, { 2, "догреватель" }, { 3, "отопление" }, { 4, "отопительные системы" } } });
            commands[new CommandId(0, 6)].Parameters.Add(new AC2PParameter() { StartByte = 4, StartBit = 4, BitLength = 4, Name = "Режим догрева", meanings = { { 0, "отключен" }, { 1, "автоматический" }, { 2, "ручной" } } });
            commands[new CommandId(0, 6)].Parameters.Add(new AC2PParameter() { StartByte = 5, BitLength = 16, Name = "Уставка температуры", Unit = "°С" });
            commands[new CommandId(0, 6)].Parameters.Add(new AC2PParameter() { StartByte = 7, BitLength = 2, Name = "Работы помпы в ждущем режиме", meanings = defMeaningsOnOff });
            commands[new CommandId(0, 6)].Parameters.Add(new AC2PParameter() { StartByte = 7, BitLength = 2, StartBit = 2, Name = "Работы помпы при заведённом двигателе", meanings = defMeaningsOnOff });

            commands[new CommandId(0, 7)].Parameters.Add(new AC2PParameter() { StartByte = 2, BitLength = 8, Name = "Номер мощности" });

            commands[new CommandId(0, 7)].Parameters.Add(new AC2PParameter() { StartByte = 2, BitLength = 8, Name = "Номер мощности" });
            commands[new CommandId(0, 7)].Parameters.Add(new AC2PParameter() { StartByte = 3, BitLength = 16, Name = "Температура перехода на большую мощность" });
            commands[new CommandId(0, 7)].Parameters.Add(new AC2PParameter() { StartByte = 4, BitLength = 16, Name = "Температура перехода на меньшую мощность" });

            commands[new CommandId(0, 8)].Parameters.Add(new AC2PParameter() { StartByte = 2, BitLength = 2, StartBit = 0, Name = "Состояние клапана 1", meanings = defMeaningsOnOff });
            commands[new CommandId(0, 8)].Parameters.Add(new AC2PParameter() { StartByte = 2, BitLength = 2, StartBit = 2, Name = "Состояние клапана 2", meanings = defMeaningsOnOff });
            commands[new CommandId(0, 8)].Parameters.Add(new AC2PParameter() { StartByte = 2, BitLength = 2, StartBit = 4, Name = "Состояние клапана 3", meanings = defMeaningsOnOff });
            commands[new CommandId(0, 8)].Parameters.Add(new AC2PParameter() { StartByte = 2, BitLength = 2, StartBit = 6, Name = "Состояние клапана 4", meanings = defMeaningsOnOff });
            commands[new CommandId(0, 8)].Parameters.Add(new AC2PParameter() { StartByte = 3, BitLength = 2, StartBit = 0, Name = "Состояние клапана 5", meanings = defMeaningsOnOff });
            commands[new CommandId(0, 8)].Parameters.Add(new AC2PParameter() { StartByte = 3, BitLength = 2, StartBit = 2, Name = "Состояние клапана 6", meanings = defMeaningsOnOff });
            commands[new CommandId(0, 8)].Parameters.Add(new AC2PParameter() { StartByte = 3, BitLength = 2, StartBit = 4, Name = "Состояние клапана 7", meanings = defMeaningsOnOff });
            commands[new CommandId(0, 8)].Parameters.Add(new AC2PParameter() { StartByte = 3, BitLength = 2, StartBit = 6, Name = "Состояние клапана 8", meanings = defMeaningsOnOff });
            commands[new CommandId(0, 8)].Parameters.Add(new AC2PParameter() { StartByte = 4, BitLength = 1, StartBit = 0, meanings = { { 0, "Сбросить неисправности" } } });

            commands[new CommandId(0, 9)].Parameters.Add(new AC2PParameter() { StartByte = 2, BitLength = 16, Name = "Время работы", Unit = "c" });
            commands[new CommandId(0, 9)].Parameters.Add(new AC2PParameter() { StartByte = 4, BitLength = 4, Name = "Режим работы", meanings = { { 0, "не используется" }, { 1, "работа по температуре платы" }, { 2, "работа по температуре пульта" }, { 3, "работа по температуре выносного датчика" }, { 4, "работа по мощности" } } });
            commands[new CommandId(0, 9)].Parameters.Add(new AC2PParameter() { StartByte = 4, StartBit = 4, BitLength = 2, Name = "Разрешение/запрещение ждущего режима (при работе по датчику температуры)", meanings = defMeaningsAllow });
            commands[new CommandId(0, 9)].Parameters.Add(new AC2PParameter() { StartByte = 4, StartBit = 6, BitLength = 2, Name = "Разрешение вращения нагнетателя воздуха на ждущем режиме", meanings = defMeaningsAllow });
            commands[new CommandId(0, 9)].Parameters.Add(new AC2PParameter() { StartByte = 5, BitLength = 16, Name = "Уставка температуры помещения", Unit = "°С" });
            commands[new CommandId(0, 9)].Parameters.Add(new AC2PParameter() { StartByte = 7, BitLength = 4, Name = "Заданное значение мощности" });

            commands[new CommandId(0, 10)].Parameters.Add(new AC2PParameter() { StartByte = 2, BitLength = 16, Name = "Время работы", Unit = "c" });

            commands[new CommandId(0, 20)].Parameters.Add(new AC2PParameter() { StartByte = 2, BitLength = 16, Name = "Калибровочное значение термопары 1" });
            commands[new CommandId(0, 20)].Parameters.Add(new AC2PParameter() { StartByte = 4, BitLength = 16, Name = "Калибровочное значение термопары 2" });

            commands[new CommandId(0, 21)].Parameters.Add(new AC2PParameter() { StartByte = 2, BitLength = 8, Name = "Предделитель" });
            commands[new CommandId(0, 21)].Parameters.Add(new AC2PParameter() { StartByte = 3, BitLength = 8, Name = "Период ШИМ" });
            commands[new CommandId(0, 21)].Parameters.Add(new AC2PParameter() { StartByte = 5, BitLength = 8, Name = "Требуемая частота", Unit = "Гц" });

            commands[new CommandId(0, 22)].Parameters.Add(new AC2PParameter() { StartByte = 2, StartBit = 0, BitLength = 8, Name = "Действие после перезагрузки", meanings = { { 0, "Остаться в загрузчике" }, { 1, "Переход в основную программу без зедержки" } } });

            commands[new CommandId(0, 45)].Parameters.Add(new AC2PParameter() { StartByte = 2, StartBit = 0, BitLength = 2, Name = "Игнорирование всех неисправностей", meanings = defMeaningsYesNo });
            commands[new CommandId(0, 45)].Parameters.Add(new AC2PParameter() { StartByte = 2, StartBit = 2, BitLength = 2, Name = "Игнорирование неисправностей ТН", meanings = defMeaningsYesNo });
            commands[new CommandId(0, 45)].Parameters.Add(new AC2PParameter() { StartByte = 2, StartBit = 4, BitLength = 2, Name = "Игнорирование срывов пламени ", meanings = defMeaningsYesNo });
            commands[new CommandId(0, 45)].Parameters.Add(new AC2PParameter() { StartByte = 2, StartBit = 6, BitLength = 2, Name = "Игнорирование неисправностей свечи", meanings = defMeaningsYesNo });
            commands[new CommandId(0, 45)].Parameters.Add(new AC2PParameter() { StartByte = 3, StartBit = 0, BitLength = 2, Name = "Игнорирование неисправностей НВ", meanings = defMeaningsYesNo });
            commands[new CommandId(0, 45)].Parameters.Add(new AC2PParameter() { StartByte = 3, StartBit = 2, BitLength = 2, Name = "Игнорирование неисправностей датчиков", meanings = defMeaningsYesNo });
            commands[new CommandId(0, 45)].Parameters.Add(new AC2PParameter() { StartByte = 3, StartBit = 4, BitLength = 2, Name = "Игнорирование неисправностей помпы", meanings = defMeaningsYesNo });
            commands[new CommandId(0, 45)].Parameters.Add(new AC2PParameter() { StartByte = 3, StartBit = 6, BitLength = 2, Name = "Игнорирование перегревов", meanings = defMeaningsYesNo });

            commands[new CommandId(0, 65)].Parameters.Add(new AC2PParameter() { StartByte = 2, StartBit = 0, BitLength = 8, Name = "", meanings = { { 7, "Температура жидкости" }, { 10, "Температура перегрева" }, { 12, "Температура пламени" }, { 13, "Температура корпуса" }, { 27, "Температура воздуха" } } });
            commands[new CommandId(0, 65)].Parameters.Add(new AC2PParameter() { StartByte = 3, BitLength = 16, Name = "Значение температуры", Unit = "°C" });

            commands[new CommandId(0, 66)].Parameters.Add(new AC2PParameter() { StartByte = 2, StartBit = 0, BitLength = 2, Name = "", meanings = { { 0, "Выход из режима М" }, { 1, "Вход в режим М" } } });
            commands[new CommandId(0, 66)].Parameters.Add(new AC2PParameter() { StartByte = 2, StartBit = 2, BitLength = 2, Name = "", meanings = { { 0, "Выход из режима Т" }, { 1, "Вход в режим Т" } } });

            commands[new CommandId(0, 68)].Parameters.Add(new AC2PParameter() { StartByte = 2, StartBit = 0, BitLength = 2, Name = "Состояние помпы", meanings = defMeaningsOnOff });
            commands[new CommandId(0, 68)].Parameters.Add(new AC2PParameter() { StartByte = 3, StartBit = 0, BitLength = 8, Name = "Обороты нагнетателя", Unit = "об/с" });
            commands[new CommandId(0, 68)].Parameters.Add(new AC2PParameter() { StartByte = 4, StartBit = 0, BitLength = 8, Name = "Мощность свечи", Unit = "%" });
            commands[new CommandId(0, 68)].Parameters.Add(new AC2PParameter() { StartByte = 5, StartBit = 0, BitLength = 8, Name = "Частота ТН", a = 0.01, Unit = "Гц" });

            commands[new CommandId(0, 68)].Parameters.Add(new AC2PParameter() { StartByte = 2, StartBit = 0, BitLength = 8, Name = "Тип устройства", meanings = { { 0, "ТН, Гц*10" }, { 1, "Реле(0/1)" }, { 2, "Свеча, %" }, { 3, "Помпа,%" }, { 4, "Шим НВ,%" }, { 23, "Обороты НВ, об/с" } } });
            commands[new CommandId(0, 68)].Parameters.Add(new AC2PParameter() { StartByte = 3, StartBit = 0, BitLength = 16, Name = "Значение" });

            commands[new CommandId(0, 70)].Parameters.Add(new AC2PParameter() { StartByte = 2, StartBit = 0, BitLength = 2, Name = "Состояние ТН", meanings = defMeaningsOnOff });
            commands[new CommandId(0, 70)].Parameters.Add(new AC2PParameter() { StartByte = 2, StartBit = 2, BitLength = 2, Name = "Состояние реле", meanings = defMeaningsOnOff });
            commands[new CommandId(0, 70)].Parameters.Add(new AC2PParameter() { StartByte = 2, StartBit = 4, BitLength = 2, Name = "Состояние свечи", meanings = defMeaningsOnOff });
            commands[new CommandId(0, 70)].Parameters.Add(new AC2PParameter() { StartByte = 2, StartBit = 6, BitLength = 2, Name = "Состояние помпы", meanings = defMeaningsOnOff });
            commands[new CommandId(0, 70)].Parameters.Add(new AC2PParameter() { StartByte = 3, StartBit = 0, BitLength = 2, Name = "Состояние НВ", meanings = defMeaningsOnOff });


            #region PGN parameters initialise

            PGNs[3].parameters.Add(new AC2PParameter() { Name = "SPN", BitLength = 16, StartByte = 0 });

            PGNs[4].parameters.Add(new AC2PParameter() { Name = "SPN", BitLength = 16, StartByte = 0 });
            PGNs[4].parameters.Add(new AC2PParameter() { Name = "Значение", BitLength = 32, StartBit = 0, StartByte = 2 });

            PGNs[5].parameters.Add(new AC2PParameter() { Name = "SPN", BitLength = 16, StartByte = 0 });
            PGNs[5].parameters.Add(new AC2PParameter() { Name = "Значение", BitLength = 32, StartBit = 0, StartByte = 2 });

            PGNs[6].parameters.Add(new AC2PParameter() { Name = "PGN", BitLength = 16, StartByte = 0, GetMeaning = x => PGNs[x]?.name });

            PGNs[7].parameters.Add(new AC2PParameter() { Name = "Команда", BitLength = 8, StartBit = 0, StartByte = 0, meanings = { { 0, "Стереть конфигурацию" }, { 1, "Запись параметра в ОЗУ" }, { 2, "Запись всех параметров во Flash" }, { 3, "Чтение параметра по номеру" }, { 4, "Успешный ответ на запрос" }, { 5, "Невозможно выполнить" } } });
            PGNs[7].parameters.Add(new AC2PParameter() { Name = "Запрошенная команда", BitLength = 8, StartBit = 0, StartByte = 1, meanings = { { 0, "Стереть конфигурацию" }, { 1, "Запись параметра в ОЗУ" }, { 2, "Запись всех параметров во Flash" }, { 3, "Чтение параметра по номеру" }, { 255, "" } } });
            PGNs[7].parameters.Add(new AC2PParameter() { Name = "Параметр", BitLength = 16, StartBit = 0, StartByte = 2, GetMeaning = x => configParameters[x]?.NameRu });
            PGNs[7].parameters.Add(new AC2PParameter() { Name = "Value", BitLength = 32, StartBit = 0, StartByte = 4 });

            PGNs[8].parameters.Add(new AC2PParameter() { Name = "Команда", BitLength = 4, StartBit = 0, StartByte = 0, meanings = { { 0, "Стереть ЧЯ" }, { 3, "Чтение ЧЯ" }, { 4, "Ответ" }, { 6, "Чтение параметра (из paramsname.h)" } } });
            PGNs[8].parameters.Add(new AC2PParameter() { Name = "Тип:", BitLength = 2, StartBit = 4, StartByte = 0, meanings = { { 0, "Общие данные" }, { 1, "Неисправности" } } });
            PGNs[8].parameters.Add(new AC2PParameter() { Name = "Номер пары", CustomDecoder = d => { if ((d[0] & 0xF) == 3) return "Номер пары:" + (d[4] * 0x100 + d[5]).ToString() + ";"; else return ""; } });
            PGNs[8].parameters.Add(new AC2PParameter() { Name = "Номер параметра", CustomDecoder = d => { if ((d[0] & 0xF) == 6) return "Номер параметра:" + (d[4] * 0x100 + d[5]).ToString() + ";"; else return ""; } });
            PGNs[8].parameters.Add(new AC2PParameter() { Name = "Число пар", CustomDecoder = d => { if ((d[0] & 0xF) == 6) return "Запрошено пар:" + (d[6] * 0x100 + d[7]).ToString() + ";"; else return ""; } });
            PGNs[8].parameters.Add(new AC2PParameter() { Name = "Номер параметра", CustomDecoder = d => { if (d[0] == 4) return "Параметр:" + (d[2] * 256 + d[3]).ToString() + ";"; else return ""; } });
            PGNs[8].parameters.Add(new AC2PParameter() { Name = "Номер параметра", CustomDecoder = d => { if (d[0] == 4) return "Значение:" + (d[4] * 0x1000000 + d[5] * 0x10000 + d[6] * 0x100 + d[7]).ToString() + ";"; else return ""; } });


            PGNs[10].parameters.Add(new AC2PParameter() { Name = "Стадия", BitLength = 8, StartByte = 0, meanings = Stages });
            PGNs[10].parameters.Add(new AC2PParameter() { Name = "Режим", BitLength = 8, StartByte = 1 });
            PGNs[10].parameters.Add(new AC2PParameter() { Name = "Код неисправности", BitLength = 8, StartByte = 2 });
            PGNs[10].parameters.Add(new AC2PParameter() { Name = "Помпа неисправна", BitLength = 2, StartByte = 3, meanings = defMeaningsYesNo });
            PGNs[10].parameters.Add(new AC2PParameter() { Name = "Код предупреждения", BitLength = 8, StartByte = 4 });
            PGNs[10].parameters.Add(new AC2PParameter() { Name = "Количество морганий", BitLength = 8, StartByte = 5 });

            PGNs[11].parameters.Add(new AC2PParameter() { Name = "Напряжение питания", BitLength = 16, StartByte = 0, a = 0.1, Unit = "В" });
            PGNs[11].parameters.Add(new AC2PParameter() { Name = "Атмосферное давление", BitLength = 8, StartByte = 2, Unit = "кПа" });
            PGNs[11].parameters.Add(new AC2PParameter() { Name = "Ток двигателя, значения АЦП", BitLength = 16, StartByte = 3 });

            PGNs[12].parameters.Add(new AC2PParameter() { Name = "Заданные обороты нагнетателя воздуха", BitLength = 8, StartByte = 0, Unit = "об/с" });
            PGNs[12].parameters.Add(new AC2PParameter() { Name = "Измеренные обороты нагнетателя воздуха,", BitLength = 8, StartByte = 1, Unit = "об/с" });
            PGNs[12].parameters.Add(new AC2PParameter() { Name = "Заданная частота ТН", BitLength = 16, StartByte = 2, a = 0.01, Unit = "Гц" });
            PGNs[12].parameters.Add(new AC2PParameter() { Name = "Реализованная частота ТН", BitLength = 16, StartByte = 4, a = 0.01, Unit = "Гц" });
            PGNs[12].parameters.Add(new AC2PParameter() { Name = "Мощность свечи", BitLength = 8, StartByte = 6, Unit = "%" });
            PGNs[12].parameters.Add(new AC2PParameter() { Name = "Состояние помпы", BitLength = 2, StartByte = 7, meanings = defMeaningsOnOff });
            PGNs[12].parameters.Add(new AC2PParameter() { Name = "Состояние реле печки кабины", BitLength = 2, StartByte = 7, StartBit = 2, meanings = defMeaningsOnOff });
            PGNs[12].parameters.Add(new AC2PParameter() { Name = "Состояние состояние канала сигнализации", BitLength = 2, StartByte = 7, StartBit = 4, meanings = defMeaningsOnOff });

            PGNs[13].parameters.Add(new AC2PParameter() { Name = "Температура ИП", BitLength = 16, StartByte = 0, Unit = "°C" });
            PGNs[13].parameters.Add(new AC2PParameter() { Name = "Температура платы/процессора", BitLength = 8, StartByte = 2, b = -75, Unit = "°C" });
            PGNs[13].parameters.Add(new AC2PParameter() { Name = "Температура жидкости", BitLength = 8, StartByte = 3, b = -75, Unit = "°C" });
            PGNs[13].parameters.Add(new AC2PParameter() { Name = "Температура температура перегрева", BitLength = 8, StartByte = 4, b = -75, Unit = "°C" });

            PGNs[14].parameters.Add(new AC2PParameter() { Name = "Минимальная температура пламени перед розжигом", BitLength = 16, StartByte = 0, Unit = "°C" });
            PGNs[14].parameters.Add(new AC2PParameter() { Name = "Граница срыва пламени", BitLength = 16, StartByte = 2, Unit = "°C" });
            PGNs[14].parameters.Add(new AC2PParameter() { Name = "граница срыва пламени на прогреве", BitLength = 16, StartByte = 4, Unit = "°C" });
            PGNs[14].parameters.Add(new AC2PParameter() { Name = "Скорость изменения температуры ИП", BitLength = 16, StartByte = 6, Unit = "°C" });


            PGNs[15].parameters.Add(new AC2PParameter() { Name = "0 канал АЦП ", BitLength = 16, StartByte = 0 });
            PGNs[15].parameters.Add(new AC2PParameter() { Name = "1 канал АЦП ", BitLength = 16, StartByte = 2 });
            PGNs[15].parameters.Add(new AC2PParameter() { Name = "2 канал АЦП ", BitLength = 16, StartByte = 4 });
            PGNs[15].parameters.Add(new AC2PParameter() { Name = "3 канал АЦП ", BitLength = 16, StartByte = 6 });

            PGNs[16].parameters.Add(new AC2PParameter() { Name = "4 канал АЦП ", BitLength = 16, StartByte = 0 });
            PGNs[16].parameters.Add(new AC2PParameter() { Name = "5 канал АЦП ", BitLength = 16, StartByte = 2 });
            PGNs[16].parameters.Add(new AC2PParameter() { Name = "6 канал АЦП ", BitLength = 16, StartByte = 4 });
            PGNs[16].parameters.Add(new AC2PParameter() { Name = "7 канал АЦП ", BitLength = 16, StartByte = 6 });

            PGNs[17].parameters.Add(new AC2PParameter() { Name = "8 канал АЦП ", BitLength = 16, StartByte = 0 });
            PGNs[17].parameters.Add(new AC2PParameter() { Name = "9 канал АЦП ", BitLength = 16, StartByte = 2 });
            PGNs[17].parameters.Add(new AC2PParameter() { Name = "10 канал АЦП ", BitLength = 16, StartByte = 4 });
            PGNs[17].parameters.Add(new AC2PParameter() { Name = "11 канал АЦП ", BitLength = 16, StartByte = 6 });

            PGNs[18].parameters.Add(new AC2PParameter() { Name = "Вид изделия", BitLength = 8, StartByte = 0, GetMeaning = i => Devices[i]?.Name });
            PGNs[18].parameters.Add(new AC2PParameter() { Name = "Напряжение питания", BitLength = 8, StartByte = 1, meanings = { { 0, "12 Вольт" }, { 1, "24 Вольта" } } });
            PGNs[18].parameters.Add(new AC2PParameter() { Name = "Версия ПО", BitLength = 8, StartByte = 2 });
            PGNs[18].parameters.Add(new AC2PParameter() { Name = "Модификация ПО", BitLength = 8, StartByte = 3 });
            PGNs[18].parameters.Add(new AC2PParameter() { Name = "Дата релиза", BitLength = 24, StartByte = 5, GetMeaning = v => $"{v >> 16}.{(v >> 8) & 0xF}.{v & 0xFF}" });
            //PGNs[18].parameters.Add(new AC2PParameter() { Name = "День ", BitLength = 8, StartByte = 5 });    Не красиво выглядит...луше одной строкой
            //PGNs[18].parameters.Add(new AC2PParameter() { Name = "Месяц", BitLength = 8, StartByte = 6 });
            //PGNs[18].parameters.Add(new AC2PParameter() { Name = "Год", BitLength = 8, StartByte = 7 });

            PGNs[19].parameters.Add(new AC2PParameter() { Name = "Подогреватель", BitLength = 2, StartBit = 0, StartByte = 1, PackNumber = 1, meanings = defMeaningsOnOff });
            PGNs[19].parameters.Add(new AC2PParameter() { Name = "Помпы", BitLength = 2, StartBit = 2, StartByte = 1, PackNumber = 1, meanings = defMeaningsOnOff });
            PGNs[19].parameters.Add(new AC2PParameter() { Name = "Вода", BitLength = 2, StartBit = 4, StartByte = 1, PackNumber = 1, meanings = defMeaningsOnOff });
            PGNs[19].parameters.Add(new AC2PParameter() { Name = "Быстрый нагрев воды", BitLength = 2, StartBit = 6, StartByte = 1, PackNumber = 1, meanings = defMeaningsOnOff });
            PGNs[19].parameters.Add(new AC2PParameter() { Name = "Температура бака", BitLength = 8, StartByte = 2, b = -75, Unit = "°С", PackNumber = 1 });
            PGNs[19].parameters.Add(new AC2PParameter() { Name = "Атмосферное давление", BitLength = 8, StartByte = 3, Unit = "кПа", PackNumber = 1 });
            PGNs[19].parameters.Add(new AC2PParameter() { Name = "Сработал датчик бытовой воды", BitLength = 2, StartByte = 4, PackNumber = 1, meanings = defMeaningsYesNo });

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
            PGNs[29].parameters.Add(new AC2PParameter() { Name = "Флаг появления пламени по пульсации давления", BitLength = 2, StartByte = 3, meanings = defMeaningsYesNo, PackNumber = 2 });
            PGNs[29].parameters.Add(new AC2PParameter() { Name = "Атмосферное давление", BitLength = 24, StartByte = 4, Unit = "кПа", a = 0.001, PackNumber = 2 });

            PGNs[31].parameters.Add(new AC2PParameter() { Name = "Время работы", BitLength = 32, StartByte = 0, Unit = "с" });
            PGNs[31].parameters.Add(new AC2PParameter() { Name = "Время работы на режиме", BitLength = 32, StartByte = 4, Unit = "с" });
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




