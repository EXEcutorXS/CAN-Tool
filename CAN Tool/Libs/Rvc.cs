using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Runtime.Serialization;
using System.IO;
using Can_Adapter;
using System.Formats.Asn1;
using System.Text.Json;
using System.Windows.Controls;
using CAN_Tool.ViewModels.Base;
using CAN_Tool.ViewModels;
using System.ComponentModel;
using OmniProtocol;
using CAN_Tool.Libs;
using System.Threading;
using System.Windows.Data;
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
        custom
    };

    public class DGN:ViewModel
    {
        public int Dgn { set; get; }
        public string Name { set; get; }
        public int MaxBroadcastGap { set; get; } = 5000;
        public int MinBroadcastGap { set; get; } = 500;
        public List<Parameter> Parameters { set; get; }
        public bool HasInstance { set; get; } = false;
        public string DisplayName => $"{Dgn:X05} {Name}";
        public int idLength; //Describes how many bytes in data are id of packet
        public DGN(int idLen = 0)
        {
            HasInstance = true;
            Parameters = new List<Parameter>();

            if (idLen>0)
            {
                Parameters.Add(new Parameter { Name = "Instance", ShortName = "#", Type = paramTyp.instance, Size = 8, fstByte = 0, Id = true });
                idLength = idLen;
            }
        }

        public string Decode(byte[] data)
        {
            string ret = Name + ": ;";
            foreach (Parameter p in Parameters)
            {
                ret += p.ToString(data) + "; ";
            }
            return ret;
        }


    }

    public class Parameter
    {
        public string Name;
        public string ShortName;
        public byte Size = 8;
        public paramTyp Type = paramTyp.natural;
        public byte fstByte = 0;
        public byte frstFit = 0;
        public Dictionary<uint, string> Meanings;
        public double coefficient = 1;
        public double shift = 0;
        public string Unit = "";
        public bool Id = false;

        public Parameter(string name)
        {
            Name = name;
        }

        public Parameter()
        {
         
        }

        public uint RawData(byte[] data)
        {
            switch (Size)
            {
                case 2: return (uint)(data[fstByte] >> frstFit) & 0x3;
                case 4: return (uint)(data[fstByte] >> frstFit) & 0xF;
                case 6: return (uint)(data[fstByte] >> frstFit) & 0x3F;
                case 8: return (data[fstByte]);
                case 16: return (uint)(data[fstByte] + data[fstByte + 1] * 256);
                case 32: return (uint)(data[fstByte] + data[fstByte + 1] * 0x100 + data[fstByte + 2] * 0x10000 + data[fstByte + 3] * 0x1000000);
                default: throw new ArgumentException("Bad parameter size");
            }
        }

        public string ToString(byte[] data)
        {
            string retString = Name + ": ";

            uint rawData = 0;
            double tempValue = 0;

            switch (Size)
            {
                case 2: rawData = (uint)((data[fstByte] >> frstFit) & 0x3); break;
                case 4: rawData = (uint)((data[fstByte] >> frstFit) & 0xF); break;
                case 6: rawData = (uint)((data[fstByte] >> frstFit) & 0x3F); break;
                case 8: rawData = (uint)(data[fstByte]); break;
                case 16: rawData = (uint)(data[fstByte] + data[fstByte + 1] * 256); break;
                case 32: rawData = (uint)(data[fstByte] + data[fstByte + 1] * 0x100 + data[fstByte + 2] * 0x10000 + data[fstByte + 3] * 0x1000000); break;
                default: throw new ArgumentException("Bad parameter size");
            }

            switch (Size)
            {
                case 2:
                    if (rawData == 2) return retString += "Error";
                    if (rawData == 3) return retString += "No Data";
                    break;
                case 4:
                    if (rawData == 14) return retString += "Error";
                    if (rawData == 15) return retString += "No Data";
                    break;
                case 6:
                    if (rawData == 62) return retString += "Error";
                    if (rawData == 63) return retString += "No Data";
                    break;
                case 8:
                    if (rawData == byte.MaxValue - 1) return retString += "Error";
                    if (rawData == byte.MaxValue) return retString += "No Data";
                    break;
                case 16:
                    if (rawData == UInt16.MaxValue - 1) return retString += "Error";
                    if (rawData == UInt16.MaxValue) return retString += "No Data";
                    break;
                case 32:
                    if (rawData == UInt32.MaxValue - 1) return retString += "Error";
                    if (rawData == UInt32.MaxValue) return retString += "No Data";
                    break;
            }

            if (Meanings != null)
            { 
            if (Meanings.ContainsKey(rawData)) return retString += Meanings[rawData];
            else return retString+$"No meaning for {rawData}";
            }

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
                    retString += ImperialConverter(tempValue, UnitType.Temp).ToString("F2");
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

                default: throw new ArgumentException("Bad parameter type");
            }

            return retString;
        }
    }
    public sealed class RvcMessage : CanMessage, IComparable, IUpdatable<RvcMessage>
    {
        private byte priority;

        [AffectsTo(nameof(VerboseInfo))]
        public byte Priority { set => Set(ref priority, value); get => priority; }

        private int dgn;
        [AffectsTo(nameof(VerboseInfo))]
        public int Dgn { set => Set(ref dgn, value); get => dgn; }

        private byte sourceAdress;
        [AffectsTo(nameof(VerboseInfo))]
        public byte SourceAdress { set => Set(ref sourceAdress, value); get => sourceAdress; }
        private byte[] data = new byte[8];
        [AffectsTo(nameof(VerboseInfo), nameof(Instance), nameof(DataAsText))]
        
        public byte Instance => Data[0];

        public IEnumerable<Parameter> Parameters => (RVC.DGNs.ContainsKey(Dgn)) ? RVC.DGNs[Dgn].Parameters : null;

        public RvcMessage() : base()
        {
            Priority = 6;
            Dgn = 0x1FFFF;
            SourceAdress = 100;

            Fresh = true;
            
        }

        public RvcMessage(CanMessage msg):base()
        {
            if (msg == null)
                throw new ArgumentNullException("Source CAN Message can't be null");
            if (msg.DLC != 8)
                throw new ArgumentException("DLC of Source Message must be 8");
            if (msg.IDE == false)
                throw new ArgumentException("RV-C supports only extended CAN frame format (IDE=1)");
            if (msg.RTR == true)
                throw new ArgumentException("RV-C do not supports data requests (RTR must be 0)");
            Priority = (byte)((msg.Id >> 26) & 7);
            Dgn = msg.Id >> 8 & 0x1FFFF;
            SourceAdress = (byte)(msg.Id & 0xFF);
            Data = msg.Data;

            Fresh = true;
        }

        public CanMessage GetCanMessage()
        {
            var msg = new CanMessage();
            msg.Id = Priority << 26 | Dgn << 8 | SourceAdress;
            msg.DLC = 8;
            msg.IDE = true;
            msg.RTR = false;
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
            return GetCanMessage().Equals(comp.GetCanMessage());
        }

        public override string ToString()
        {

            string ret = $"{Priority} | {Dgn:X05} {SourceAdress:D3} ||";
            foreach (var item in Data)
                ret += $" {item:X02} ";
            return ret;
        }

        public new string VerboseInfo => PrintParameters().Replace(';', '\n');

        public string PrintParameters()
        {
            if (!RVC.DGNs.ContainsKey(Dgn))
                return $"{Dgn:X} is not supported yet";
            return RVC.DGNs[Dgn].Decode(Data);
        }

        public override int GetHashCode()
        {
            return GetCanMessage().GetHashCode();
        }

        public new int CompareTo(object other)
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
            Fresh = true;
            updatetick = DateTime.Now.Ticks;
        }

        public bool IsSimmiliarTo(RvcMessage item)
        {
            if (Dgn != item.Dgn) return false;
            if (!RVC.DGNs.ContainsKey(Dgn)) return true;
            for(int i = 0; i < RVC.DGNs[Dgn].idLength;i++)
                if (Data[i]!=item.Data[i]) return false;
            return true;

        }
    }
}
