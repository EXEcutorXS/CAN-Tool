using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.IO.Ports;
using System.Threading;
using System.ComponentModel;
using System.Windows.Threading;

namespace Can_Adapter
{
    public class GotMessageEventArgs : EventArgs
    {
        public CanMessage receivedMessage;
    }

    public partial class CanMessage : INotifyPropertyChanged
    {
        private bool ide;

        public bool IDE
        {
            set
            {
                if (ide == value) return;
                ide = value;
                PropChanged("IDE");
            }
            get { return ide; }
        }

        private int id;
        public int ID
        {
            set
            {
                if (id == value) return;
                id = value;
                PropChanged("ID");
                PropChanged("IdAsText");
            }
            get { return id; }
        }
        private bool rtr;

        public bool RTR
        {
            set
            {
                if (rtr == value) return;
                rtr = value;
                PropChanged("RTR");
            }
            get { return rtr; }
        }
        private int dlc;
        public int DLC
        {
            set
            {
                if (dlc == value) return;
                dlc = value;
                PropChanged("DLC");
            }
            get { return dlc; }
        }

        public byte[] data = new byte[8];

        public byte[] Data
        {
            get { return data; }

            set
            {
                if (Enumerable.SequenceEqual(data, value))
                    return;
                data = value;
                PropChanged("Data");
                PropChanged("DataAsText");
            }
        }


        public event PropertyChangedEventHandler PropertyChanged;

        public string DataAsText => GetDataInTextFormat("", " ");

        public string GetDataInTextFormat(string beforeString = "", string afterString = "")
        {
            StringBuilder sb = new StringBuilder("");
            foreach (var item in Data)
                sb.Append($"{beforeString}{item:X02}{afterString}");
            return sb.ToString();
        }

        public string RtrAsString => RTR ? "1" : "0";

        public string IdeAsString => IDE ? "1" : "0";

        public bool RvcCompatible => IDE && DLC == 8 && !RTR;

        public string IdAsText => IDE ? string.Format("{0:X08}", ID) : string.Format("{0:X03}", ID);


        public override string ToString()
        {
            return $"L:{DLC} IDE:{IdeAsString} RTR:{RtrAsString} ID:0x{IdAsText} Data:{GetDataInTextFormat(" ")}";
        }

        public virtual void PropChanged(string propertyName)
        {
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
        }

        public CanMessage()
        {

        }
        public CanMessage(string str)
        {
            switch (str[0])
            {
                case 't':
                    IDE = false;
                    RTR = false;
                    break;
                case 'T':
                    IDE = true;
                    RTR = false;
                    break;
                case 'r':
                    IDE = false;
                    RTR = true;
                    break;
                case 'R':
                    IDE = true;
                    RTR = true;
                    break;
                default:
                    throw new FormatException("Can't parse. String must start with 't','T','r' or 'R' ");
            }
            if (IDE)
                DLC = (byte)int.Parse(str[9].ToString());
            else
                DLC = (byte)int.Parse(str[4].ToString());
            if (DLC > 8)
                throw new FormatException($"Can't parse. Message length cant be {DLC}, max length is 8");

            if (IDE)
                ID = Convert.ToInt32(str.Substring(1, 8), 16);
            else
                ID = Convert.ToInt32(str.Substring(1, 3), 16);
            Data = new byte[DLC];

            int shift;
            if (!IDE)
                shift = 5;
            else
                shift = 10;
            for (int i = 0; i < DLC; i++)
                Data[i] = Convert.ToByte(str.Substring(shift + i * 2, 2), 16);
        }

        public override bool Equals(object obj)
        {
            if (obj == null)
                return false;
            if (ReferenceEquals(obj, this))
                return true;
            if (!(obj is CanMessage))
                return false;
            CanMessage toCompare = (CanMessage)obj;
            if (toCompare.ID != ID || toCompare.DLC != DLC || toCompare.IDE != IDE || toCompare.RTR != RTR)
                return false;
            for (int i = 0; i < toCompare.DLC; i++)
                if (toCompare.Data[i] != Data[i])
                    return false;
            return true;
        }

        public override int GetHashCode()
        {
            int ret = ID;
            for (int i = 0; i < DLC; i++)
            {
                ret ^= Data[i] << (8 * (i % 4));
            }
            return ret;
        }
        public void Update(CanMessage msg)
        {
            Data = msg.Data;
            IDE = msg.IDE;
            RTR = msg.RTR;
            ID = msg.ID;
        }

        public bool SimilliarTo(CanMessage m)
        {
            if (m.ID != ID) return false;
            if (m.DLC != DLC) return false;
            if (m.IDE != IDE) return false;
            if (m.RTR != RTR) return false;
            return true;
        }
    }
    public class CanAdapter
    {

        char[] currentBuf = new char[1024];
        int ptr = 0;

        public UInt32 Version = 0;

        public int Bitrate = 250;
        public bool PortOpened => serialPort.IsOpen;



        SerialPort serialPort;

        private BindingList<CanMessage> _messages = new BindingList<CanMessage>();

        public BindingList<CanMessage> Messages { get => _messages; }

        public string PortName
        {
            get { return serialPort.PortName; }
            set
            {
                if (serialPort.IsOpen)
                    serialPort.Close();
                serialPort.PortName = value;
            }
        }


        public event EventHandler GotNewMessage;

        public event EventHandler TransmissionSuccess;

        public event EventHandler ErrorReported;

        public void PortOpen() => serialPort.Open();
        public void PortClose() => serialPort.Close();

        public void StartNormal()
        {
            serialPort.Write("O\r");
        }

        public void StartListen()
        {
            serialPort.Write("L\r");
        }

        public void startSelfReception()
        {
            serialPort.Write("Y\r");
        }

        public void Stop()
        {
            serialPort.Write("C\r");
        }

        public void SetBitrate(int bitrate)
        {
            if (PortOpened)
                serialPort.Write($"S{bitrate}\r");
        }

        public void Transmit(CanMessage msg)
        {
            if (serialPort.IsOpen == false)
                return;
            StringBuilder str = new StringBuilder("");
            if (msg.IDE && msg.RTR)
                str.Append('R');
            if (!msg.IDE && msg.RTR)
                str.Append('r');
            if (msg.IDE && !msg.RTR)
                str.Append('T');
            if (!msg.IDE && !msg.RTR)
                str.Append('t');
            str.Append(msg.IdAsText);
            str.Append(msg.DLC.ToString());
            str.Append(msg.GetDataInTextFormat());
            str.Append('\r');
            serialPort.Write(str.ToString());
        }

        private void uartMessageProcess()
        {
            switch (currentBuf[0])
            {
                case 'T':
                case 't':
                case 'r':
                case 'R':
                    var m = new CanMessage(new string(currentBuf));
                    GotNewMessage?.Invoke(this, new GotMessageEventArgs() { receivedMessage = m });
                    var foundMessage = Messages.FirstOrDefault(o => o.SimilliarTo(m));
                    if (foundMessage != null)
                        foundMessage.Update(m);
                    else
                        App.Current.Dispatcher.Invoke(()=> Messages.Add(m));
                    break;
                case 'Z':
                    TransmissionSuccess?.Invoke(this, new EventArgs());
                    break;
                case '\a':
                    ErrorReported?.Invoke(this, new EventArgs());
                    break;
                default:
                    break;

            }
        }


        private void DataReceivedHandler(object sender, SerialDataReceivedEventArgs args)
        {
            while (serialPort.IsOpen && serialPort.BytesToRead > 0)
            {
                int newByte = serialPort.ReadByte();
                if (newByte == 13 || newByte == 0)
                {
                    currentBuf[ptr] = '\0';
                    uartMessageProcess();
                    ptr = 0;
                }
                else
                    currentBuf[ptr++] = (char)newByte;
            }
        }

        public CanAdapter()
        {
            serialPort = new SerialPort();
            serialPort.DataReceived += new SerialDataReceivedEventHandler(DataReceivedHandler);
        }
    }
}
