using System;
using System.IO.Ports;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Interop;
using CAN_Tool.Libs;
using CommunityToolkit.Mvvm.ComponentModel;
using VSCom.CanApi;

using static CAN_Tool.Libs.Helper;

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

    public partial class CanAdapter : ObservableObject
    {
        VSCAN canWrapper = new();

        SerialPort port = new(); //Used for Canable

        private string currentBuf = "";

        private Task MessageReceivingTask;
        public enum AdapterType
        {
            VSCom,
            Canable
        }

        public Array AdapterTypes => Enum.GetValues(typeof(AdapterType));
        [ObservableProperty] AdapterType type = AdapterType.VSCom;
        [ObservableProperty] bool portOpened = false;
        [ObservableProperty] string portName = "";
        [ObservableProperty] int speed = 5;

        public event EventHandler GotNewMessage;


        private void messageReceiver()
        {
            VSCAN_MSG[] msgs = new VSCAN_MSG[1];
            uint readbytes = 0;
            while (PortOpened)
            {
                canWrapper.Read(ref msgs, 1, ref readbytes);
                GotNewMessage.Invoke(this, new GotCanMessageEventArgs() { receivedMessage = new CanMessage(msgs[0]) });
            }
        }

        public void PortOpenNormal(string portName = VSCAN.VSCAN_FIRST_FOUND)
        {
            if (!PortOpened)
            {
                PortOpened = true;
                if (Type == AdapterType.VSCom)
                {
                    canWrapper.Open(portName, VSCAN.VSCAN_MODE_NORMAL);
                    canWrapper.SetSpeed(Speed);
                    canWrapper.SetTimestamp(VSCAN.VSCAN_TIMESTAMP_OFF);
                    canWrapper.SetBlockingRead(VSCAN.VSCAN_IOCTL_ON);

                    MessageReceivingTask = Task.Run(messageReceiver);
                }
                if (Type == AdapterType.Canable)
                {
                    port.PortName = portName;
                    port.Open();
                    port.Write("O\r");
                    port.Write($"S{Speed}\r");
                    port.DataReceived += DataReceivedHandler;
                }
            }
            else
            {
                MessageBox.Show(GetString("t_port_already_opened"));
            }
        }

        public void PortOpenSelfReception(string portName = VSCAN.VSCAN_FIRST_FOUND)
        {
            if (!PortOpened)
            {
                PortOpened = true;
                if (Type == AdapterType.VSCom)
                {
                    canWrapper.Open(portName, VSCAN.VSCAN_MODE_SELF_RECEPTION);
                    canWrapper.SetSpeed(Speed);
                    canWrapper.SetTimestamp(VSCAN.VSCAN_TIMESTAMP_OFF);
                    canWrapper.SetBlockingRead(VSCAN.VSCAN_IOCTL_ON);

                    MessageReceivingTask = Task.Run(messageReceiver);
                }
                if (Type == AdapterType.Canable)
                {
                    port.PortName = portName;
                    port.Open();
                    port.Write("Y\r");
                    port.Write($"S{Speed}\r");
                    port.DataReceived += DataReceivedHandler;
                }
            }
            else
            {
                MessageBox.Show(GetString("t_port_already_opened"));
            }
        }

        public void PortOpenListenOnly(string portName = VSCAN.VSCAN_FIRST_FOUND)
        {
            if (!PortOpened)
            {
                PortOpened = true;
                if (Type == AdapterType.VSCom)
                {
                    canWrapper.Open(portName, VSCAN.VSCAN_MODE_LISTEN_ONLY);
                    canWrapper.SetSpeed(Speed);
                    canWrapper.SetTimestamp(VSCAN.VSCAN_TIMESTAMP_OFF);
                    canWrapper.SetBlockingRead(VSCAN.VSCAN_IOCTL_ON);

                    MessageReceivingTask = Task.Run(messageReceiver);
                }
                if (Type == AdapterType.Canable)
                {
                    port.PortName = portName;
                    port.Open();
                    port.Write("L\r");
                    port.Write($"S{Speed}\r");
                    port.DataReceived += DataReceivedHandler;
                }
            }
            else
            {
                MessageBox.Show(GetString("t_port_already_opened"));
            }
        }



        public void PortClose()
        {

            PortOpened = false;
            if (Type == AdapterType.VSCom)
                canWrapper.Close();
            if (Type == AdapterType.Canable)
            {
                port.DataReceived -= DataReceivedHandler;
                Thread CloseDown = new Thread(new ThreadStart(CloseSerialOnExit)); //close port in new thread to avoid hang
                CloseDown.Start(); //close port in new thread to avoid hang

            }
        }

        private void CloseSerialOnExit()
        {
            try
            {
                port.Close(); //close the serial port
            }
            catch (Exception ex)
            {
                MessageBox.Show(ex.Message); //catch any serial port closing error messages
            }
        }
        public void SetBitrate(int bitrate)
        {
            if (Type == AdapterType.VSCom)
                canWrapper.SetSpeed(bitrate);
            if (Type == AdapterType.Canable)
            {
                if (PortOpened)
                {
                    port.Write($"S{bitrate}\r");
                }
            }
        }


        public async Task Transmit(CanMessage message)
        {
            if (Type == AdapterType.VSCom)
            {
                VSCAN_MSG[] msg = new VSCAN_MSG[1];
                msg[0].Data = message.Data;
                if (message.Ide)
                    msg[0].Flags |= VSCAN.VSCAN_FLAGS_EXTENDED;
                else
                    msg[0].Flags |= VSCAN.VSCAN_FLAGS_STANDARD;
                if (message.Rtr)
                    msg[0].Flags |= VSCAN.VSCAN_FLAGS_REMOTE;

                msg[0].Size = (byte)message.Dlc;
                msg[0].Id = (uint)message.Id;
                uint written = 0;
                try
                {
                    canWrapper.Write(msg, 1, ref written);
                    canWrapper.Flush();
                }
                catch (Exception ex)
                {

                }
            }
            if (Type == AdapterType.Canable)
            {
                if (port.IsOpen == false)
                    return;

                await WaitForTxBufferEmpty();
                StringBuilder str = new("");
                if (message.Ide && message.Rtr) str.Append('R');
                if (!message.Ide && message.Rtr) str.Append('r');
                if (message.Ide && !message.Rtr) str.Append('T');
                if (!message.Ide && !message.Rtr) str.Append('t');
                str.Append(message.IdAsText);
                str.Append(message.Dlc);
                str.Append(message.GetDataInTextFormat());
                str.Append("\r");

                port.Write(str.ToString());
                await WaitForTxBufferEmpty();
            }
        }

        private async Task WaitForTxBufferEmpty(CancellationToken cancellationToken = default,
                                       int timeoutMs = 1000)
        {
            int elapsed = 0;
            int checkInterval = 10; // проверяем каждые 10 мс

            while (port.BytesToWrite > 0 && elapsed < timeoutMs)
            {
                await Task.Delay(checkInterval, cancellationToken);
                elapsed += checkInterval;
            }

            if (port.BytesToWrite > 0)
            {
                throw new TimeoutException("Таймаут ожидания отправки данных");
            }

            // Дополнительная небольшая задержка для надежности
            await Task.Delay(10, cancellationToken);
        }

        //Ret value - More messages available in buffer
        private void UartMessageProcess()
        {
            string[] splitted = currentBuf.Split('\r');
            foreach (var line in splitted)
            {
                if (line.Length == 0) continue;
                switch (line[0])
                {
                    case 'T':
                    case 't':
                    case 'r':
                    case 'R':
                        try
                        {
                            var m = new CanMessage(new string(currentBuf));
                            GotNewMessage?.Invoke(this, new GotCanMessageEventArgs() { receivedMessage = m });
                        }
                        catch
                        {
                            // ignored
                        }

                        break;
                    default:
                        continue;
                }
            }
            currentBuf = splitted[^1]; //If last messge is not completed it will be saved to buffer, otherwise it will be zero length string
        }

        private void DataReceivedHandler(object sender, SerialDataReceivedEventArgs args)
        {
            currentBuf += (port.ReadExisting());
            UartMessageProcess();
        }
        public void InjectMessage(CanMessage m)
        {
            GotNewMessage?.Invoke(this, new GotCanMessageEventArgs() { receivedMessage = m });
        }

        public CanAdapter()
        {

        }
    }
}
