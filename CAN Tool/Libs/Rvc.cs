using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using CommunityToolkit.Mvvm.ComponentModel;
using OmniProtocol;
using CAN_Tool.Libs;
using static CAN_Tool.Libs.Helper;
using CAN_Tool;

namespace RVC
{

    public enum paramTyp
    {
        boolean,
        instance,
        percent,
        temperature,
        voltage,
        amperage,
        hertz,
        watts,
        natural,
        seconds,
        minutes,
        hours,
        custom
    };

    public class DGN
    {
        public int Dgn { set; get; }
        public string Name { set; get; }
        public int MaxBroadcastGap { set; get; } = 5000;
        public int MinBroadcastGap { set; get; } = 500;
        
        public string DisplayName => $"{Dgn:X05} {Name}";
        public int idLen; //Describes how many bytes in data are id of packet
        public bool MultiPack = false;
        public DGN(int idLen = 0)
        {
            this.idLen = idLen;
        }

        public string Decode(byte[] data)
        {
            var ret = Name + ": ;";
                foreach (var p in RVC.Parameters)
                    if (p.Dgn==Dgn && (MultiPack==false || data[0]==p.multipackNum))
                        ret += p.ToString(data) + "; ";
            
            return ret;
        }


    }

    public class Parameter
    {
        public int Dgn;
        public string Name;
        public string ShortName;
        public byte Size = 8;
        public paramTyp Type = paramTyp.natural;
        public byte frstByte = 0;
        public byte frstBit = 0;
        public Dictionary<int, string> Meanings;
        public double coefficient = 1;
        public double shift = 0;
        public string Unit = "";
        public bool Id = false;
        public byte multipackNum = 0;

        public Parameter(int dgn)
        {
            Dgn = dgn;
        }

        public Parameter()
        {

        }


        public static long getRaw(byte[] data, int bitLength, int startBit, int startByte)
        {
            long ret;
            switch (bitLength)
            {
                case 1: ret = data[startByte] >> startBit & 0b1; break;
                case 2: ret = data[startByte] >> startBit & 0b11; break;
                case 3: ret = data[startByte] >> startBit & 0b111; break;
                case 4: ret = data[startByte] >> startBit & 0b1111; break;
                case 5: ret = data[startByte] >> startBit & 0b11111; break;
                case 6: ret = data[startByte] >> startBit & 0b111111; break;
                case 7: ret = data[startByte] >> startBit & 0b1111111; break;
                case 8: ret = data[startByte]; break;
                case 16: ret = BitConverter.ToUInt16(new byte[] { data[startByte], data[startByte + 1] }); break;
                case 24: ret = data[startByte + 2] * 65536 + data[startByte + 1] * 256 + data[startByte]; break;
                case 32: ret = BitConverter.ToUInt32(new byte[] { data[startByte], data[startByte + 1], data[startByte + 2], data[startByte + 3] }); break;
                default: throw new Exception("Bad parameter size");
            }
            return ret;
        }

        public string ToString(byte[] data)
        {
            var retString = Name + ": ";

            uint rawData = 0;
            double tempValue = 0;

            rawData = (uint)getRaw(data, Size, frstBit, frstByte);

            if (rawData == Math.Pow(2, Size) - 1)
                return retString += (GetString("t_no_data") + $" ({rawData})");

            if (rawData == Math.Pow(2, Size) - 2)
                return retString += (GetString("t_error") + $" ({rawData})");

            if (Meanings != null && Meanings.ContainsKey((int)rawData))
                return retString += $"{Meanings[(int)rawData]} ({(int)rawData})";

            switch (Type)
            {
                case paramTyp.percent:
                    tempValue = rawData / 2;
                    retString += tempValue.ToString() + "%";
                    break;
                case paramTyp.instance:
                    tempValue = rawData;
                    retString += (rawData == 0) ? "For everyone" : "#" + tempValue.ToString();
                    break;
                case paramTyp.hertz:
                    tempValue = rawData;
                    retString += tempValue.ToString() + " Hz";
                    break;
                case paramTyp.watts:
                    tempValue = rawData;
                    retString += tempValue.ToString() + " W";
                    break;
                case paramTyp.amperage:
                    switch (Size)
                    {
                        case 8: tempValue = rawData; break;
                        case 16: tempValue = -1600 + rawData * 0.05; break;
                        case 32: tempValue = -2000000000.0 + rawData * 0.001; break;
                        default: throw new Exception("Wrong size for current field");
                    }
                    retString += tempValue.ToString() + "A";
                    break;
                case paramTyp.voltage:
                    switch (Size)
                    {
                        case 8: tempValue = rawData; break;
                        case 16: tempValue = rawData * 0.05; break;
                        default: throw new Exception("Wrong size for voltage field");
                    }
                    retString += tempValue.ToString() + "V";
                    break;

                case paramTyp.temperature:
                    switch (Size)
                    {
                        case 8: tempValue = -40 + rawData; break;
                        case 16: tempValue = -273 + rawData * 0.03125; break;
                        default: throw new Exception("Wrong size for Temperature field");
                    }
                    retString += ImperialConverter(tempValue, UnitType_t.Temp).ToString("F2");
                    if (App.Settings.UseImperial)
                        retString += " F°";
                    else
                        retString += " C°";
                    break;
                case paramTyp.custom:
                    tempValue = rawData;
                    retString += $"{tempValue * coefficient + shift} {Unit}";
                    break;
                case paramTyp.boolean:
                    switch (rawData)
                    {
                        case 0: retString += "False"; break;
                        case 1: retString += "True"; break;
                        case 2: retString += "Error"; break;
                        case 3: retString += "Not supported"; break;
                    }
                    break;
                case paramTyp.natural:
                    retString += rawData.ToString();
                    break;
                case paramTyp.seconds:
                    retString += rawData.ToString() + " seconds";
                    break;
                case paramTyp.minutes:
                    retString += rawData.ToString() + " minutes";
                    break;
                case paramTyp.hours:
                    retString += rawData.ToString() + " hours";
                    break;
                default: throw new ArgumentException("Bad parameter type");
            }

            return retString;
        }
    }
    public sealed class RvcMessage : ObservableObject, IComparable, IUpdatable<RvcMessage>
    {
        public DateTime Timestamp { get; } = DateTime.Now;

        private byte priority;
        public byte Priority
        {
            get => priority;
            set { if (SetProperty(ref priority, value)) OnPropertyChanged(nameof(VerboseInfo)); }
        }

        private int dgn;
        public int Dgn
        {
            get => dgn;
            set { if (SetProperty(ref dgn, value)) OnPropertyChanged(nameof(VerboseInfo)); }
        }

        private byte sourceAdress;
        public byte SourceAdress
        {
            get => sourceAdress;
            set { if (SetProperty(ref sourceAdress, value)) OnPropertyChanged(nameof(VerboseInfo)); }
        }

        private byte[] data;

        public byte Instance => Data[0];

        public byte[] Data
        {
            get => data;
            set
            {
                if (SetProperty(ref data, value))
                {
                    OnPropertyChanged(nameof(VerboseInfo));
                    OnPropertyChanged(nameof(DataAsText));
                    OnPropertyChanged(nameof(DataAsULong));
                    OnPropertyChanged(nameof(Instance));
                }
            }
        }

        public ulong DataAsULong
        {
            get
            {
                var temp = new byte[data.Length];
                data.CopyTo(temp, 0);
                Array.Reverse(temp);
                return BitConverter.ToUInt64(temp);
            }
            set
            {
                var temp = BitConverter.GetBytes(value);
                Array.Reverse(temp);
                Data = temp;
            }
        }


        public string DataAsText => GetDataInTextFormat("", " ");

        public string GetDataInTextFormat(string beforeString = "", string afterString = "")
        {
            StringBuilder sb = new("");
            for (var i = 0; i < 8; i++)
                sb.Append($"{beforeString}{Data[i]:X02}{afterString}");
            return sb.ToString();
        }

        public RvcMessage()
        {
            Priority = 6;
            Dgn = 0x1FFFF;
            SourceAdress = 101;
            data = new byte[] { 255, 255, 255, 255, 255, 255, 255, 255 };
        }

        public RvcMessage(CanMessage msg) : base()
        {
            if (msg == null)
                throw new ArgumentNullException("Source CAN Message can't be null");
            if (msg.Dlc != 8)
                throw new ArgumentException("DLC of Source Message must be 8");
            if (msg.Ide == false)
                throw new ArgumentException("RV-C supports only extended CAN frame format (IDE=1)");
            if (msg.Rtr == true)
                throw new ArgumentException("RV-C do not supports data requests (RTR must be 0)");
            Priority = (byte)((msg.Id >> 26) & 7);
            Dgn = msg.Id >> 8 & 0x1FFFF;
            SourceAdress = (byte)(msg.Id & 0xFF);
            Data = msg.Data;
        }

        public CanMessage ToCanMessage()
        {
            var msg = new CanMessage();
            msg.Id = Priority << 26 | Dgn << 8 | SourceAdress;
            msg.Dlc = 8;
            msg.Ide = true;
            msg.Rtr = false;
            msg.Data = Data;
            return msg;
        }

        public override bool Equals(object obj)
        {
            if (obj == null)
                return false;
            if (!(obj is RvcMessage))
                return false;
            var comp = obj as RvcMessage;
            return ToCanMessage().Equals(comp.ToCanMessage());
        }

        public override string ToString()
        {
            var ret = $"{Timestamp:HH:mm:ss.fff} | {Priority} | {Dgn:X05} {SourceAdress:D3} ||";
            foreach (var item in Data)
                ret += $" {item:X02} ";
            return ret;
        }

        public string VerboseInfo => PrintParameters().Replace(';', '\n');

        public bool IsProprietaryDgn => (Dgn & 0x1FF00) == 0x1EF00 || (Dgn & 0x1FF00) == 0xEF00;

        public string PrintParameters()
        {
            var lookupDgn = IsProprietaryDgn ? 0xEF00 : Dgn;
            if (!RVC.DGNs.ContainsKey(lookupDgn))
                return $"{Dgn:X5} is not supported yet";
            return RVC.DGNs[lookupDgn].Decode(Data);
        }

        public override int GetHashCode()
        {
            return ToCanMessage().GetHashCode();
        }

        public int CompareTo(object other)
        {
            var o = other as RvcMessage;
            if (Dgn != o.Dgn)
                return Dgn - o.Dgn;
            else
                return (Data[0] - o.Data[0]);
        }

        public void Update(RvcMessage item)
        {
            Data = item.Data;
            SourceAdress = item.SourceAdress;
            Priority = item.Priority;
        }

        public bool IsSimiliarTo(RvcMessage item)
        {
            if (IsProprietaryDgn && item.IsProprietaryDgn)
            {
                // Proprietary packets from different SAs (different DGNs) are distinct
                if (Dgn != item.Dgn) return false;
                // Within the same destination SA, distinguish by sub-command (Data[0])
                return Data[0] == item.Data[0];
            }
            if (Dgn != item.Dgn) return false;
            if (!RVC.DGNs.ContainsKey(Dgn))
                return true;
            for (var i = 0; i < RVC.DGNs[Dgn].idLen; i++)
                if (Data[i] != item.Data[i])
                    return false;
            return true;
        }
    }
}

