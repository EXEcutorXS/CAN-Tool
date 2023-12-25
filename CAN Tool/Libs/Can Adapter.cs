using System;
using System.IO.Ports;
using System.Text;
using CAN_Tool.Libs;
using CommunityToolkit.Mvvm.ComponentModel;
using Timer = System.Timers.Timer;

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
            Fresh = true;
            updateTick = DateTime.Now.Ticks;
        }

        [NotifyPropertyChangedFor(nameof(VerboseInfo), nameof(RvcCompatible), nameof(IdeAsString))]
        [ObservableProperty] private bool ide;

        [NotifyPropertyChangedFor(nameof(VerboseInfo), nameof(IdAsText))]
        [ObservableProperty] private int id;

        [NotifyPropertyChangedFor(nameof(VerboseInfo), nameof(RtrAsString), nameof(RvcCompatible))]
        [ObservableProperty] private bool rtr;

        [NotifyPropertyChangedFor(nameof(VerboseInfo), nameof(RvcCompatible), nameof(DataAsText))]
        [ObservableProperty] private int dlc;

        [NotifyPropertyChangedFor(nameof(VerboseInfo), nameof(DataAsText), nameof(DataAsULong))]
        [ObservableProperty] private byte[] data = new byte[8];

        public ulong DataAsULong
        {
            get
            {
                var temp = new byte[Data.Length];
                Data.CopyTo(temp, 0);
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
            for (var i = 8 - Dlc; i < 8; i++)
                sb.Append($"{beforeString}{Data[i]:X02}{afterString}");
            return sb.ToString();
        }

        public string RtrAsString => Rtr ? "1" : "0";

        public string IdeAsString => Ide ? "1" : "0";

        public bool RvcCompatible => Ide && Dlc == 8 && !Rtr;

        public string IdAsText => Ide ? $"{Id:X08}" : $"{Id:X03}";

        [ObservableProperty] private bool fresh;

        private long updateTick;

        public override string ToString()
        {
            return $"L:{Dlc} IDE:{IdeAsString} RTR:{RtrAsString} ID:0x{IdAsText} Data:{GetDataInTextFormat(" ")}";
        }

        public string ToShortString()
        {
            return $"{IdeAsString} {RtrAsString} {Dlc} {IdAsText} {GetDataInTextFormat(" ")}";
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
            Fresh = true;
            updateTick = DateTime.Now.Ticks;
        }


        public void FreshCheck()
        {
            if (Fresh && (DateTime.Now.Ticks - updateTick > 3000000))
                Fresh = false;
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

    public enum AdapterStatus { Closed, Ready, Tx }

    public partial class CanAdapter : ObservableObject
    {
        private readonly char[] currentBuf = new char[1024];

        private int ptr;

        public bool PortOpened => serialPort.IsOpen;

        [ObservableProperty] private int failedTransmissions;

        [ObservableProperty] private int successTransmissions;

        [ObservableProperty] private int receivedMessagesCount;


        private int lastSecondReceived;
        private int lastSecondTransmitted;
        private int currentSecondReceived;
        private int currentSecondTransmitted;

        public AdapterStatus Status { private set; get; } = AdapterStatus.Closed;

        private readonly SerialPort serialPort;

        public string PortName
        {
            get => serialPort.PortName;
            set
            {
                if (serialPort.IsOpen)
                    serialPort.Close();
                serialPort.PortName = value;
                OnPropertyChanged();
            }
        }

        public event EventHandler GotNewMessage;

        public void PortOpen()
        {
            serialPort.Open();
            Status = AdapterStatus.Ready;
        }
        public void PortClose()
        {
            serialPort.Close();
            Status = AdapterStatus.Closed;
        }
        public void StartNormal() => serialPort.Write("O\r");
        public void StartListen() => serialPort.Write("L\r");
        public void StartSelfReception() => serialPort.Write("Y\r");
        public void Stop() => serialPort.Write("C\r");
        public void SetBitrate(int bitrate) => serialPort.Write($"S{bitrate}\r");
        public void SetMask(uint mask) => serialPort.Write($"m{mask:X08}\r");
        public void SetAcceptCode(uint code) => serialPort.Write($"M{code:X08}\r");

        public void Transmit(CanMessage msg)
        {

            if (serialPort.IsOpen == false)
                return;

            var startWaitTick = DateTime.Now.Ticks;
            while (Status != AdapterStatus.Ready && DateTime.Now.Ticks - startWaitTick < 30000) { }
            if (Status == AdapterStatus.Tx)
            {
                FailedTransmissions++;
                Status = AdapterStatus.Ready;
            }

            StringBuilder str = new("");
            if (msg.Ide && msg.Rtr) str.Append('R');
            if (!msg.Ide && msg.Rtr) str.Append('r');
            if (msg.Ide && !msg.Rtr) str.Append('T');
            if (!msg.Ide && !msg.Rtr) str.Append('t');
            str.Append(msg.IdAsText);
            str.Append(msg.Dlc);
            str.Append(msg.GetDataInTextFormat());
            str.Append('\r');

            serialPort.Write(str.ToString());
            Status = AdapterStatus.Tx;
            currentSecondTransmitted++;
        }

        private void UartMessageProcess()
        {
            switch (currentBuf[0])
            {
                case 'T':
                case 't':
                case 'r':
                case 'R':
                    try
                    {
                        var m = new CanMessage(new string(currentBuf));
                        GotNewMessage?.Invoke(this, new GotCanMessageEventArgs() { receivedMessage = m });
                        currentSecondReceived++;
                        ReceivedMessagesCount++;
                    }
                    catch
                    {
                        // ignored
                    }

                    break;
                case 'z':
                case 'Z':
                    Status = AdapterStatus.Ready;
                    SuccessTransmissions++;
                    break;
                case '\a':
                    FailedTransmissions++;
                    Status = AdapterStatus.Ready;
                    break;
                default:
                    ptr = 0;
                    break;

            }
        }

        private void DataReceivedHandler(object sender, SerialDataReceivedEventArgs args)
        {
            while (serialPort.IsOpen && serialPort.BytesToRead > 0)
            {
                var newByte = serialPort.ReadByte();
                if (newByte is 13 or 0 or 7 or 'Z' or 'z' or '\a')
                {
                    if (newByte == 13)
                        currentBuf[ptr] = '\0';
                    else
                        currentBuf[ptr] = (char)newByte;
                    UartMessageProcess();
                    ptr = 0;
                }
                else
                    currentBuf[ptr++] = (char)newByte;
            }
        }

        public void InjectMessage(CanMessage m)
        {
            GotNewMessage?.Invoke(this, new GotCanMessageEventArgs() { receivedMessage = m });
        }

        public string StatusString => $"Bus use: Rx/Tx(Total):{lastSecondReceived}/{lastSecondTransmitted},Faults:{FailedTransmissions}";

        public CanAdapter()
        {
            serialPort = new SerialPort();
            serialPort.BaudRate = 250000;
            serialPort.DataReceived += DataReceivedHandler;
            Timer adapterTimer = new(15);
            adapterTimer.Elapsed += TimerTick;
            adapterTimer.Start();
        }

        private int tickCounter;

        private void TimerTick(object sender, EventArgs e)
        {
            tickCounter++;
            if (tickCounter <= 64) return;
            tickCounter = 0;
            lastSecondReceived = currentSecondReceived;
            lastSecondTransmitted = currentSecondTransmitted;
            currentSecondReceived = 0;
            currentSecondTransmitted = 0;
            OnPropertyChanged(nameof(StatusString));
        }

    }
}
