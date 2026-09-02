using System;
using System.IO.Ports;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Interop;
using CAN_Tool.Libs;
using CAN_Tool.Libs.CanAdapters;
using CommunityToolkit.Mvvm.ComponentModel;
using VSCom.CanApi;
using Peak.Can.Basic;
using Candle;

using static CAN_Tool.Libs.Helper;


//TODO ���������� ��������� ��������, ������ ��� �� ��������� �� ������ � ��������.

namespace CAN_Tool
{
    public class GotCanMessageEventArgs : EventArgs
    {
        public CanMessage receivedMessage;
    }

    public partial class CanMessage : ObservableObject, IComparable, IUpdatable<CanMessage>
    {

        public CanMessage()
        {
            Ide = true;
            Dlc = 8;
            Rtr = false;
        }

        public CanMessage(string str)
        {
            switch (str[0])
            {
                case 't':
                    Ide = false;
                    Rtr = false;
                    break;
                case 'T':
                    Ide = true;
                    Rtr = false;
                    break;
                case 'r':
                    Ide = false;
                    Rtr = true;
                    break;
                case 'R':
                    Ide = true;
                    Rtr = true;
                    break;
                default:
                    throw new FormatException("Can't parse. String must start with 't','T','r' or 'R' ");
            }
            if (Ide)
                Dlc = (byte)int.Parse(str[9].ToString());
            else
                Dlc = (byte)int.Parse(str[4].ToString());
            if (Dlc > 8)
                throw new FormatException($"Can't parse. Message length cant be {Dlc}, max length is 8");

            Id = Convert.ToInt32(Ide ? str.Substring(1, 8) : str.Substring(1, 3), 16);
            Data = new byte[Dlc];

            var shift = !Ide ? 5 : 10;
            for (var i = 0; i < Dlc; i++)
                Data[i] = Convert.ToByte(str.Substring(shift + i * 2, 2), 16);
        }

        public CanMessage(PcanMessage srcMsg)
        {
            Id = (int)srcMsg.ID;
            Ide = srcMsg.MsgType == MessageType.Extended;
            Rtr = srcMsg.MsgType == MessageType.RemoteRequest;
            Data = srcMsg.Data;
            Dlc = srcMsg.DLC;
        }

        public CanMessage(Frame srcMsg)
        {
            Id = (int)(srcMsg.Identifier & 0x1FFFFFFF);
            Ide = srcMsg.Extended;
            Rtr = srcMsg.RTR;
            Data = srcMsg.Data;
            Dlc = srcMsg.Data.Length;
        }

        public PcanMessage toPcanMsg() => new PcanMessage()
        {
            DLC = (byte)this.Dlc,
            Data = this.Data,
            ID = (uint)Id,
            MsgType = Ide ? MessageType.Extended : MessageType.Standard,
        };

        public Frame toCandleMessage()
        {
            Frame frame = new();
            frame.Data = new byte[Dlc];
            Array.Copy(this.Data, frame.Data, Dlc);
            frame.Extended = Ide;
            frame.Identifier = (uint)Id;
            frame.RTR = Rtr;
            return frame;
        }



        [NotifyPropertyChangedFor(nameof(VerboseInfo), nameof(RvcCompatible), nameof(IdeAsString))]
        [ObservableProperty] private bool ide;

        [NotifyPropertyChangedFor(nameof(VerboseInfo), nameof(IdAsText))]
        [ObservableProperty] private int id;

        [NotifyPropertyChangedFor(nameof(VerboseInfo), nameof(RtrAsString), nameof(RvcCompatible))]
        [ObservableProperty] private bool rtr;

        [NotifyPropertyChangedFor(nameof(VerboseInfo), nameof(RvcCompatible), nameof(DataAsText))]
        [ObservableProperty] private int dlc;

        [NotifyPropertyChangedFor(nameof(VerboseInfo), nameof(DataAsText))]
        [ObservableProperty] private byte[] data = new byte[8];

        [ObservableProperty] private string statusString;

        [NotifyPropertyChangedFor(nameof(StatusString))]
        [ObservableProperty] private string errorCount;




        public string DataAsText => GetDataInTextFormat("", " ");

        public string GetDataInTextFormat(string beforeString = "", string afterString = "")
        {
            StringBuilder sb = new("");
            for (var i = 8 - Dlc; i < 8; i++)
                sb.Append($"{beforeString}{Data[i]:X02}{afterString}");
            return sb.ToString();
        }

        public string RtrAsString => Rtr ? "1" : "0";

        public string IdeAsString => Ide ? "1" : "0";

        public bool RvcCompatible => Ide && Dlc == 8 && !Rtr;

        public string IdAsText => Ide ? $"{Id:X08}" : $"{Id:X03}";

        public override string ToString()
        {
            return $"L:{Dlc} IDE:{IdeAsString} RTR:{RtrAsString} ID:0x{IdAsText} Data:{GetDataInTextFormat(" ")}";
        }

        public string ToShortString()
        {
            return $"{IdeAsString} {RtrAsString} {Dlc} {IdAsText} {GetDataInTextFormat(" ")}";
        }


        public CanMessage(VSCAN_MSG msg)
        {
            Data = msg.Data;
            Id = (int)msg.Id;
            Dlc = msg.Size;
            if ((msg.Flags & VSCAN.VSCAN_FLAGS_STANDARD) != 0)
                Ide = false;
            if ((msg.Flags & VSCAN.VSCAN_FLAGS_EXTENDED) != 0)
                Ide = true;
            if ((msg.Flags & VSCAN.VSCAN_FLAGS_REMOTE) != 0)
                Rtr = true;
            else
                Rtr = false;
        }

        public override bool Equals(object obj)
        {
            if (obj == null)
                return false;
            if (ReferenceEquals(obj, this))
                return true;
            if (obj is not CanMessage toCompare)
                return false;
            if (toCompare.Id != Id || toCompare.Dlc != Dlc || toCompare.Ide != Ide || toCompare.Rtr != Rtr)
                return false;
            for (var i = 0; i < toCompare.Dlc; i++)
                if (toCompare.Data[i] != Data[i])
                    return false;
            return true;
        }

        public override int GetHashCode()
        {
            var ret = Id;
            for (var i = 0; i < Dlc; i++)
            {
                ret ^= Data[i] << (8 * (i % 4));
            }
            return ret;
        }

        public void Update(CanMessage m)
        {
            if (m == null) return;
            Data = m.Data;
            Ide = m.Ide;
            Rtr = m.Rtr;
            Id = m.Id;
            Dlc = m.Dlc;
        }

        public virtual string VerboseInfo => ToString();

        public bool IsSimiliarTo(CanMessage m)
        {
            if (m.Id != Id) return false;
            if (m.Dlc != Dlc) return false;
            if (m.Ide != Ide) return false;
            return m.Rtr == Rtr;
        }

        public int CompareTo(object obj)
        {
            return Id - ((CanMessage)obj).Id;
        }
    }

    public partial class CanAdapter : ObservableObject
    {
        private ICanAdapterDriver _driver;

        public enum AdapterType
        {
            VSCom,
            Slcan, // named after the wire protocol, not the CANable brand - any SLCAN-compatible adapter works
            PCAN,
            CandleLight,
            Ble, // PU-28 bootloader's BLE-CAN bridge mode - see BleDriver
            Modem // the org's own modem's USB-CDC SLCAN bridge - see ModemSlcanDriver for why it's not just Slcan
        }

        public Array AdapterTypes => Enum.GetValues(typeof(AdapterType));

        [ObservableProperty] private AdapterType type = AdapterType.VSCom;
        [ObservableProperty] private bool portOpened = false;
        [ObservableProperty] private string portName = "";
        [ObservableProperty] private int speed = 5;

        public event EventHandler GotNewMessage;
        public event EventHandler MessageTransmitted;
        public long ReceivedMessagesCount { get; private set; } = 0;
        public long TransmittedMessagesCount { get; private set; } = 0;
        [ObservableProperty] private string status = "RX: 0  TX: 0";

        private long _lastRxCount = 0;
        private long _lastTxCount = 0;

        partial void OnTypeChanged(AdapterType value) => _driver = CreateDriver(value);

        public CanAdapter()
        {
            _driver = CreateDriver(Type);

            var statsTimer = new System.Windows.Threading.DispatcherTimer();
            statsTimer.Interval = TimeSpan.FromSeconds(1);
            statsTimer.Tick += (_, _) =>
            {
                long rx = ReceivedMessagesCount - _lastRxCount;
                long tx = TransmittedMessagesCount - _lastTxCount;
                _lastRxCount = ReceivedMessagesCount;
                _lastTxCount = TransmittedMessagesCount;
                Status = $"RX: {rx}  TX: {tx}";
            };
            statsTimer.Start();
        }

        private ICanAdapterDriver CreateDriver(AdapterType adapterType)
        {
            if (_driver != null)
                _driver.MessageReceived -= OnDriverMessageReceived;

            ICanAdapterDriver driver = adapterType switch
            {
                AdapterType.VSCom => new VSComDriver(),
                AdapterType.Slcan => new SlcanDriver(),
                AdapterType.PCAN => new PcanDriver(),
                AdapterType.CandleLight => new CandleLightDriver(),
                AdapterType.Ble => new BleDriver(),
                AdapterType.Modem => new ModemSlcanDriver(),
                _ => throw new ArgumentOutOfRangeException(nameof(adapterType))
            };

            driver.MessageReceived += OnDriverMessageReceived;
            return driver;
        }

        // Lets the UI drive BLE device discovery into the same port-list combo box used
        // for serial ports, without hard-coding a dependency on BleDriver specifically.
        public bool DriverSupportsDeviceScan => _driver is IScannableCanAdapterDriver;

        public void StartDeviceScan(Action<string> onDeviceFound)
        {
            if (_driver is IScannableCanAdapterDriver scannable) scannable.StartScan(onDeviceFound);
        }

        public void StopDeviceScan()
        {
            if (_driver is IScannableCanAdapterDriver scannable) scannable.StopScan();
        }

        private void OnDriverMessageReceived(object sender, GotCanMessageEventArgs e)
        {
            ReceivedMessagesCount++;
            GotNewMessage?.Invoke(this, e);
        }

        public void PortOpenNormal(string portName)
        {
            if (PortOpened)
            {
                MessageBox.Show(GetString("t_port_already_opened"));
                return;
            }
            
            _driver.SetBitrate(Speed);
            _driver.OpenNormal(portName);
            PortOpened = true;
        }

        public void PortOpenSelfReception(string portName)
        {
            if (PortOpened)
            {
                MessageBox.Show(GetString("t_port_already_opened"));
                return;
            }

            _driver.SetBitrate(Speed);
            _driver.OpenSelfReception(portName);
            PortOpened = true;
        }

        public void PortOpenListenOnly(string portName = VSCAN.VSCAN_FIRST_FOUND)
        {
            if (PortOpened)
            {
                MessageBox.Show(GetString("t_port_already_opened"));
                return;
            }
            _driver.SetBitrate(Speed);
            _driver.OpenListenOnly(portName);
            PortOpened = true;
        }

        public void PortClose()
        {
            PortOpened = false;
            _driver.Close();
        }

        public void SetBitrate(int bitrate) { Speed = bitrate; _driver.SetBitrate(bitrate); }

        public void Transmit(CanMessage message)
        {
            TransmittedMessagesCount++;
            _driver.Transmit(message);
            MessageTransmitted?.Invoke(this, new GotCanMessageEventArgs { receivedMessage = message });
        }

        public void InjectMessage(CanMessage m) =>
            GotNewMessage?.Invoke(this, new GotCanMessageEventArgs { receivedMessage = m });
    }
}
