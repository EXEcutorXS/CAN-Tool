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
        public string Name = "";     //Имя параметра
        public string Unit = "";     //Единица измерения
        public double a = 1;         //коэффициент приведение
        public double b = 0;         //смещение
        //value = rawData*a+b
        public string OutputFormat = "";
        public Dictionary<int, string> meanings = new Dictionary<int, string>();
        public string Tip = "";
        public int PackNumber;

    }
    class AC2Pcommand
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
        public List<AC2PParameter> parameters = new List<AC2PParameter>();
    }
    public class AC2Pmessage : CanMessage
    {
        public AC2Pmessage() : base()
        {
            DLC = 8;
            RTR = false;
            IDE = true;
        }
        public AC2Pmessage(CanMessage m) : this()
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

        public DeviceId TransmitterId => new DeviceId(TransmitterType, TransmitterAddress);

        public DeviceId ReceiverId => new DeviceId(ReceiverType, ReceiverAddress);

        public bool Similiar(AC2Pmessage m)
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
        public string printParameter(AC2PParameter p)
        {
            StringBuilder retString = new StringBuilder();
            int rawValue;
            retString.Append(p.Name + ": ");
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
        public string VerboseInfo()
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
                AC2Pcommand cmd = AC2P.commands[new CommandId(Data[0], Data[1])];
                retString.Append(cmd.name + ";");
                if (cmd.parameters != null)
                    foreach (AC2PParameter p in cmd.parameters)
                        retString.Append(printParameter(p));
            }
            if (pgn.parameters != null)
                foreach (AC2PParameter p in pgn.parameters)
                    if (!pgn.multipack || Data[0] == p.PackNumber)
                        retString.Append(printParameter(p));
            return retString.ToString();
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

    public class ReadedParameter : INotifyPropertyChanged
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


        public string idString => AC2P.configParameters[Id]?.idString;
        public string rusName => AC2P.configParameters[Id]?.nameRu;
        public string enName => AC2P.configParameters[Id]?.nameEn;

    }
    public class ConnectedDevice
    {
        public DeviceId ID;
        public Dictionary<DateTime, DeviceStatus> Statuses = new Dictionary<DateTime, DeviceStatus>();
        public ObservableCollection<ReadedParameter> readedParameters = new ObservableCollection<ReadedParameter>();
        public override string ToString()
        {
            if (AC2P.Devices.ContainsKey(ID.Type))
                return AC2P.Devices[ID.Type].Name;
            else
                return $"No device <{ID.Type}> in list";
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

        public override bool Equals([NotNullWhen(true)] object obj)
        {
            if (!(obj is CommandId))
                return false;
            CommandId com = (CommandId)obj;
            return firstByte == com.firstByte && secondByte == com.secondByte;
        }


    }
    public struct DeviceId
    {
        public int Type;
        public int Address;
        public DeviceId(int type, int adr)
        {
            if (type > 127 || adr > 6)
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
    }
    public class configParameter
    {
        public int id;
        public string idString;
        public string nameRu;
        public string nameEn;

    }
    static class AC2P
    {
        static readonly Dictionary<int, string> defMeaningsYesNo = new Dictionary<int, string>() { [0] = "Нет", [1] = "Да", [2] = "Нет данных", [3] = "Нет данных" };
        static readonly Dictionary<int, string> defMeaningsOnOff = new Dictionary<int, string>() { [0] = "Выкл", [1] = "Вкл", [2] = "Нет данных", [3] = "Нет данных" };

        public static Dictionary<int, PGN> PGNs = new Dictionary<int, PGN>();

        public static Dictionary<CommandId, AC2Pcommand> commands = new Dictionary<CommandId, AC2Pcommand>();

        public static BindingList<ConnectedDevice> connectedDevices = new BindingList<ConnectedDevice>();

        public static Dictionary<int, configParameter> configParameters = new Dictionary<int, configParameter>();

        public static Dictionary<int, string> paramtersNames = new Dictionary<int, string>();

        public static CanAdapter canAdapter;

        public static event EventHandler progressBarUpdated;

        private static int progress;

        public static int Progress
        {
            get { return progress; }
            set
            {
                progress = value;
                progressBarUpdated?.Invoke(null, EventArgs.Empty);
            }
        }



        public static void ParseCanMessage(CanMessage msg)
        {
            AC2Pmessage m = new AC2Pmessage(msg);
            DeviceId id = m.TransmitterId;

            if (connectedDevices.FirstOrDefault(d => d.ID.Equals(id)) == null)
                connectedDevices.Add(new ConnectedDevice() { ID = id });

            if (m.PGN == 7) //Ответ на запрос параметра
            {
                if (m.Data[0] == 4) // Обрабатываем только упешные ответы на запросы
                {
                    int parameterId = m.Data[3] + m.Data[2] * 256;
                    uint parameterValue = ((uint)m.Data[4] * 0x1000000) + ((uint)m.Data[5] * 0x10000) + ((uint)m.Data[6] * 0x100) + (uint)m.Data[7];
                    if (parameterValue != 0xFFFFFFFF)
                        connectedDevices.First(d => d.ID.Equals(id)).readedParameters.Add(new ReadedParameter() { Id = parameterId, Value = parameterValue });
                }

            }

        }
        public static void ParseParamsname(string filePath = "paramsname.h")
        {
            Encoding.RegisterProvider(CodePagesEncodingProvider.Instance);
            StreamReader sr = new StreamReader(filePath, System.Text.Encoding.GetEncoding(1251));
            while (!sr.EndOfStream)
            {
                string tempString = sr.ReadLine();
                List<string> tempList = new List<string>();
                int ParamNumber;
                string CodeName;
                string englishDescription;
                string russianDescription;


                if (tempString.StartsWith("#define PAR"))
                {
                    tempString = tempString.Remove(0, 8);
                    englishDescription = tempString.Substring(tempString.LastIndexOf('@') + 1);
                    russianDescription = tempString.Substring(tempString.LastIndexOf("//") + 2, tempString.LastIndexOf('@') - tempString.LastIndexOf("//") - 2);
                    tempString = tempString.Remove(tempString.IndexOf('/'));
                    tempList = tempString.Split(' ').ToList();
                    CodeName = tempList[0];
                    ParamNumber = int.Parse(tempList.Last());
                    tempString = "";
                    configParameters.Add(ParamNumber, new configParameter() { id = ParamNumber, idString = CodeName, nameRu = russianDescription, nameEn = englishDescription });
                }
            }
            foreach (var p in configParameters)
                paramtersNames.Add(p.Key, p.Value.idString);
        }

        public static async void ReadAllParameters(DeviceId id)
        {
            int cnt = 0;
            foreach (var p in configParameters)
            {
                AC2Pmessage msg = new AC2Pmessage();
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
                progress = ++cnt * 100 / configParameters.Count;
                progressBarUpdated?.Invoke(null, new EventArgs());

            }
        }

        public static async void SaveParameters(DeviceId id)
        {
            var dev = connectedDevices.FirstOrDefault(d => d.ID.Equals(id));
            if (dev == null) return;
            AC2Pmessage msg = new AC2Pmessage();
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

        public static void EraseParameters(DeviceId id)
        {
            var dev = connectedDevices.FirstOrDefault(d => d.ID.Equals(id));
            if (dev == null) return;
            AC2Pmessage msg = new AC2Pmessage();

            msg.PGN = 7;
            msg.TransmitterAddress = 6;
            msg.TransmitterType = 126;
            msg.ReceiverAddress = id.Address;
            msg.ReceiverType = id.Type;
            msg.Data[0] = 0; //Erase 
            msg.Data[1] = 0xFF;
            canAdapter.Transmit(msg);
        }

        public static Dictionary<int, Device> Devices;

        static AC2P()
        {
            SeedStaticData();
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
            { 126, new Device(){ID=126,Name="Устройство управления" }}
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
            commands.Add(new CommandId(0, 0), new AC2Pcommand() { firstByte = 0, secondByte = 0, name = "Кто здесь?" });
            commands.Add(new CommandId(0, 1), new AC2Pcommand() { firstByte = 0, secondByte = 1, name = "пуск устройства" });
            commands.Add(new CommandId(0, 3), new AC2Pcommand() { firstByte = 0, secondByte = 3, name = "остановка устройства" });
            commands.Add(new CommandId(0, 4), new AC2Pcommand() { firstByte = 0, secondByte = 4, name = "пуск только помпы" });
            commands.Add(new CommandId(0, 5), new AC2Pcommand() { firstByte = 0, secondByte = 5, name = "сброс неисправностей" });
            commands.Add(new CommandId(0, 6), new AC2Pcommand() { firstByte = 0, secondByte = 6, name = "задать параметры работы жидкостного подогревателя" });
            commands.Add(new CommandId(0, 7), new AC2Pcommand() { firstByte = 0, secondByte = 7, name = "запрос температурных переходов по режимам жидкостного подогревателя" });
            commands.Add(new CommandId(0, 8), new AC2Pcommand() { firstByte = 0, secondByte = 8, name = "задать состояние клапанов устройства ”Блок управления клапанами”" });
            commands.Add(new CommandId(0, 9), new AC2Pcommand() { firstByte = 0, secondByte = 9, name = "задать параметры работы воздушного отопителя" });
            commands.Add(new CommandId(0, 10), new AC2Pcommand() { firstByte = 0, secondByte = 10, name = "запуск в режиме вентиляции (для воздушных отопителей)" });
            commands.Add(new CommandId(0, 20), new AC2Pcommand() { firstByte = 0, secondByte = 20, name = "калибровка термопар" });
            commands.Add(new CommandId(0, 21), new AC2Pcommand() { firstByte = 0, secondByte = 21, name = "задать параметры частоты ШИМ нагнетателя воздуха" });
            commands.Add(new CommandId(0, 22), new AC2Pcommand() { firstByte = 0, secondByte = 22, name = "Reset CPU" });
            commands.Add(new CommandId(0, 45), new AC2Pcommand() { firstByte = 0, secondByte = 45, name = "биты реакции на неисправности" });
            commands.Add(new CommandId(0, 65), new AC2Pcommand() { firstByte = 0, secondByte = 65, name = "установить значение температуры" });
            commands.Add(new CommandId(0, 66), new AC2Pcommand() { firstByte = 0, secondByte = 66, name = "сброс неисправностей" });
            commands.Add(new CommandId(0, 67), new AC2Pcommand() { firstByte = 0, secondByte = 67, name = "вход/выход в стадию M (ручное управление) или T (тестирование блока управления)" });
            commands.Add(new CommandId(0, 68), new AC2Pcommand() { firstByte = 0, secondByte = 68, name = "задание параметров устройств в стадии M (ручное управление)" });
            commands.Add(new CommandId(0, 69), new AC2Pcommand() { firstByte = 0, secondByte = 69, name = "управление устройствами" });
            #endregion

            #region PGN parameters initialise
            PGNs[3].parameters.Add(new AC2PParameter() { Name = "SPN", BitLength = 16, StartBit = 0, StartByte = 0 });

            PGNs[7].parameters.Add(new AC2PParameter() { Name = "Команда", BitLength = 8, StartBit = 0, StartByte = 0, meanings = { { 0, "Стереть конфигурацию" }, { 1, "Запись параметра в ОЗУ" }, { 2, "Запись всех параметров во Flash" }, { 3, "Чтение параметра по номеру" }, { 4, "Успешный ответ на запрос" }, { 5, "Невозможно выполнить" } } });
            PGNs[7].parameters.Add(new AC2PParameter() { Name = "Запрошенная команда", BitLength = 8, StartBit = 0, StartByte = 1, meanings = { { 0, "Стереть конфигурацию" }, { 1, "Запись параметра в ОЗУ" }, { 2, "Запись всех параметров во Flash" }, { 3, "Чтение параметра по номеру" }, { 255, "" } } });
            PGNs[7].parameters.Add(new AC2PParameter() { Name = "Параметр", BitLength = 16, StartBit = 0, StartByte = 2, meanings = AC2P.paramtersNames });
            PGNs[7].parameters.Add(new AC2PParameter() { Name = "Value", BitLength = 32, StartBit = 0, StartByte = 4 });

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

            PGNs[29].parameters.Add(new AC2PParameter() { Name = "Атмосферное давление", BitLength = 8, StartByte = 1, Unit = "кПа", PackNumber = 1 });
            PGNs[29].parameters.Add(new AC2PParameter() { Name = "Среднее максимальное значение давления", BitLength = 24, StartByte = 2, Unit = "кПа", a = 0.001, PackNumber = 1 });
            PGNs[29].parameters.Add(new AC2PParameter() { Name = "Среднее минимальное значение давления", BitLength = 24, StartByte = 4, Unit = "кПа", a = 0.001, PackNumber = 1 });

            PGNs[29].parameters.Add(new AC2PParameter() { Name = "Разница между max и min  значениями", BitLength = 16, StartByte = 1, a = 0.01, Unit = "кПа", PackNumber = 2 });
            PGNs[29].parameters.Add(new AC2PParameter() { Name = "Флаг появления пламени по пульсации давления", BitLength = 2, StartByte = 3, meanings = defMeaningsYesNo, PackNumber = 2 });
            PGNs[29].parameters.Add(new AC2PParameter() { Name = "Атмосферное давление", BitLength = 24, StartByte = 4, Unit = "кПа", a = 0.001, PackNumber = 2 });

            PGNs[31].parameters.Add(new AC2PParameter() { Name = "Время работы", BitLength = 32, StartByte = 0, Unit = "с" });
            PGNs[31].parameters.Add(new AC2PParameter() { Name = "Время работы на режиме", BitLength = 32, StartByte = 4, Unit = "с" });
            #endregion
        }

    }


}
