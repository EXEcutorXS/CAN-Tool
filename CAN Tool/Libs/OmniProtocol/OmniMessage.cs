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
using System.Windows;
using CommunityToolkit.Mvvm.Input;
using System.IO;
using Newtonsoft;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using System.Collections.Specialized;
using ScottPlot.WPF;
using System.Collections.ObjectModel;
using System.Windows.Markup;


namespace OmniProtocol
{

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
            if (!Omni.Pgns.ContainsKey(this.Pgn))
                return "Pgn not found";

            var pgn = Omni.Pgns[Pgn];
            var sender = Omni.Devices.ContainsKey(TransmitterId.Type) ? Omni.Devices[TransmitterId.Type].Name : $"({GetString("t_unknown_device")} в„–{TransmitterId.Type})";
            var receiver = Omni.Devices.ContainsKey(ReceiverId.Type) ? Omni.Devices[ReceiverId.Type].Name : $"({GetString("t_unknown_device")} в„–{ReceiverId.Type})";
            retString.Append($"{sender}({TransmitterId.Address})->{receiver}({ReceiverId.Address});;");


            retString.Append(GetString(pgn.name) + ";;");
            if (pgn.multiPack)
                retString.Append($"{GetString("t_multipack")} в„–{Data[0]};");
            if (Pgn == 1 && Omni.Commands.ContainsKey(Data[1] + Data[0] * 256))
            {
                var cmd = Omni.Commands[Data[1] + Data[0] * 256];
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
            if (Omni.Pgns.ContainsKey(Pgn) && Omni.Pgns[Pgn].multiPack && Data[0] != m.Data[0]) //Р”СЂСѓРіРѕР№ РЅРѕРјРµСЂ РјСѓР»СЊС‚РёРїР°РєРµС‚Р°
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

}
