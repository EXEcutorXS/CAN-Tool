﻿using CAN_Tool.ViewModels.Base;
using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Diagnostics;
using System.IO.Ports;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using CAN_Tool.Libs;
using OmniProtocol;

namespace Can_Adapter
{


    public class GotCanMessageEventArgs : EventArgs
    {
        public CanMessage receivedMessage;
    }



    public class CanMessage : ViewModel, IUpdatable<CanMessage>, IComparable
    {
        private bool ide;

        [AffectsTo(nameof(VerboseInfo), nameof(RvcCompatible), nameof(IdeAsString))]
        public bool IDE
        {
            set => Set(ref ide, value);
            get => ide;
        }

        private int id;

        [AffectsTo(nameof(VerboseInfo), nameof(IdAsText))]
        public int Id
        {
            set => Set(ref id, value);
            get => id;
        }
        private bool rtr;

        [AffectsTo(nameof(VerboseInfo), nameof(RtrAsString), nameof(RvcCompatible))]
        public bool RTR
        {
            set => Set(ref rtr, value);
            get => rtr;
        }

        private int dlc;
        [AffectsTo(nameof(VerboseInfo), nameof(RvcCompatible),nameof(DataAsText))]
        public int DLC
        {
            set => Set(ref dlc, value);
            get => dlc;
        }

        private byte[] data = new byte[8];

        [AffectsTo(nameof(VerboseInfo), nameof(DataAsText),nameof(DataAsULong))]
        public byte[] Data
        {
            get => data;

            set => Set(ref data, value);
        }


        //[AffectsTo(nameof(VerboseInfo), nameof(DataAsText), nameof(Data))]
        public ulong DataAsULong
        {
            get {
                /*
                UInt64 ret = 0;
                ret += ((ulong)(data[0]) << 56);
                ret += ((ulong)(data[1]) << 48);
                ret += ((ulong)(data[2]) << 40);
                ret += ((ulong)(data[3]) << 32);
                ret += ((ulong)(data[4]) << 24);
                ret += ((ulong)(data[5]) << 16);
                ret += ((ulong)(data[6]) << 8);
                ret += (ulong)(data[7]);
                */
                return BitConverter.ToUInt64(data, 0);
                //return ret;
            }
            set
               {
                /*
                var newData = new byte[8];
                for (int i = 0; i < 8; i++)
                {
                    newData[7 - i] = (byte)((value >> (i * 8)) & 0xFF);
                }
                */

                Data = BitConverter.GetBytes(value);
               }
        }


        public string DataAsText => GetDataInTextFormat("", " ");

        public string GetDataInTextFormat(string beforeString = "", string afterString = "")
        {
            StringBuilder sb = new("");
            for(int i = 8-DLC; i< 8;i++)
                sb.Append($"{beforeString}{Data[i]:X02}{afterString}");
            return sb.ToString();
        }

        public string RtrAsString => RTR ? "1" : "0";

        public string IdeAsString => IDE ? "1" : "0";

        public bool RvcCompatible => IDE && DLC == 8 && !RTR;

        public string IdAsText => IDE ? string.Format("{0:X08}", Id) : string.Format("{0:X03}", Id);
        
        private bool fresh;
        public bool Fresh { set => Set(ref fresh, value); get => fresh; }

        public long updatetick;

        public override string ToString()
        {
            return $"L:{DLC} IDE:{IdeAsString} RTR:{RtrAsString} ID:0x{IdAsText} Data:{GetDataInTextFormat(" ")}";
        }

        public string ToShortString()
        {
            return $"{IdeAsString} {RtrAsString} {DLC} {IdAsText} {GetDataInTextFormat(" ")}";
        }


        public CanMessage()
        {
            IDE = true;
            DLC = 8;
            RTR = false;
            Fresh = true;
            updatetick = DateTime.Now.Ticks;
        }

        
        public CanMessage(string str):base()
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
                Id = Convert.ToInt32(str.Substring(1, 8), 16);
            else
                Id = Convert.ToInt32(str.Substring(1, 3), 16);
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
            if (obj is not CanMessage)
                return false;
            CanMessage toCompare = (CanMessage)obj;
            if (toCompare.Id != Id || toCompare.DLC != DLC || toCompare.IDE != IDE || toCompare.RTR != RTR)
                return false;
            for (int i = 0; i < toCompare.DLC; i++)
                if (toCompare.Data[i] != Data[i])
                    return false;
            return true;
        }

        public override int GetHashCode()
        {
            int ret = Id;
            for (int i = 0; i < DLC; i++)
            {
                ret ^= Data[i] << (8 * (i % 4));
            }
            return ret;
        }
        public void Update(CanMessage m)
        {
            Data = m.Data;
            IDE = m.IDE;
            RTR = m.RTR;
            Id = m.Id;
            DLC = m.DLC;
            Fresh = true;
            updatetick = DateTime.Now.Ticks;
        }

        
        public void FreshCheck()
        {
            if (fresh && (DateTime.Now.Ticks - updatetick > 3000000))
                Fresh = false;
        }

        public virtual string VerboseInfo => ToString();
        public bool IsSimmiliarTo(CanMessage m)
        {
            if (m.Id != Id) return false;
            if (m.DLC != DLC) return false;
            if (m.IDE != IDE) return false;
            if (m.RTR != RTR) return false;
            return true;
        }

        public int CompareTo(object obj)
        {
            return id - (obj as CanMessage).id;
        }
    }


    public class CanAdapter : ViewModel
    {

        private readonly char[] currentBuf = new char[1024];

        private int ptr = 0;

        public UInt32 Version = 0;

        public int Bitrate = 250;
        public bool PortOpened => serialPort.IsOpen;

        private bool txDone = true;

        private bool TxFail = true;
        public bool TxDone { private set => Set(ref txDone, value); get => txDone; }

        private int errorCounter = 0;
        public int ErrorCounter { private set => Set(ref errorCounter, value); get => errorCounter; }

        private int failedMessagesCounter = 0;
        public int FailedMessagesCounter { private set => Set(ref failedMessagesCounter, value); get => failedMessagesCounter; }

        private int lastSecondReceived;
        private int lastSecondTransmitted;
        private int currentSecodeReceived;
        private int currentSecodeTransmitted;
        private System.Timers.Timer oneSecondTimer;

        //public int LastSecondReceived { get => lastSecondReceived; set => Set(ref lastSecondReceived, value); }



        private readonly SynchronizationContext UIContext;

        private readonly SerialPort serialPort;

        private readonly UpdatableList<CanMessage> _messages = new();

        public UpdatableList<CanMessage> Messages { get => _messages; }

        public List<CanMessage> MessagesQueue { set; get; }

        public string PortName
        {
            get { return serialPort.PortName; }
            set
            {
                if (serialPort.IsOpen)
                    serialPort.Close();
                serialPort.PortName = value;
                OnPropertyChanged("PortName");
            }
        }


        public event EventHandler GotNewMessage;

        public event EventHandler TransmissionSuccess;

        public event EventHandler ErrorReported;

        public void PortOpen() => serialPort.Open();
        public void PortClose() => serialPort.Close();
        public void StartNormal() => serialPort.Write("O\r");
        public void StartListen() => serialPort.Write("L\r");
        public void StartSelfReception() => serialPort.Write("Y\r");
        public void Stop() => serialPort.Write("C\r");
        public void SetBitrate(int bitrate) => serialPort.Write($"S{bitrate}\r");

        public void TransmitFast(CanMessage msg)
        {

            TxDone = false;
            TxFail = false;

            if (serialPort.IsOpen == false)
                return;
            StringBuilder str = new("");
            if (msg.IDE && msg.RTR)
                str.Append('R');
            if (!msg.IDE && msg.RTR)
                str.Append('r');
            if (msg.IDE && !msg.RTR)
                str.Append('T');
            if (!msg.IDE && !msg.RTR)
                str.Append('t');
            str.Append(msg.IdAsText);
            str.Append(msg.DLC);
            str.Append(msg.GetDataInTextFormat());
            str.Append('\r');

            serialPort.Write(str.ToString());
            currentSecodeTransmitted++;
        }

        public void Transmit(CanMessage msg)
        {

            TxDone = false;
            TxFail = false;

            if (serialPort.IsOpen == false)
                return;
            StringBuilder str = new("");
            if (msg.IDE && msg.RTR)
                str.Append('R');
            if (!msg.IDE && msg.RTR)
                str.Append('r');
            if (msg.IDE && !msg.RTR)
                str.Append('T');
            if (!msg.IDE && !msg.RTR)
                str.Append('t');
            str.Append(msg.IdAsText);
            str.Append(msg.DLC);
            str.Append(msg.GetDataInTextFormat());
            str.Append('\r');

            serialPort.Write(str.ToString());
            currentSecodeTransmitted++;
            for (int i = 0; i < 20; i++)
            {
                Task.Delay(1);
                if (txDone || TxFail)
                    return;
            }
            
        }

        public bool TransmitWithCheck(CanMessage msg)
        {

            TxDone = false;
            TxFail = false;

            if (serialPort.IsOpen == false)
                return false;
            StringBuilder str = new("");
            if (msg.IDE && msg.RTR)
                str.Append('R');
            if (!msg.IDE && msg.RTR)
                str.Append('r');
            if (msg.IDE && !msg.RTR)
                str.Append('T');
            if (!msg.IDE && !msg.RTR)
                str.Append('t');
            str.Append(msg.IdAsText);
            str.Append(msg.DLC);
            str.Append(msg.GetDataInTextFormat());
            str.Append('\r');

            serialPort.Write(str.ToString());
            currentSecodeTransmitted++;
            for (int i = 0; i < 1000; i++)
            {
                Thread.Sleep(1);
                if (txDone) return true;
                if (TxFail) return false;
            }
            return false;
        }

        public bool TransmitForSure(CanMessage msg, int tries)
        {
            while (TransmitWithCheck(msg) == false && tries-- > 0)
                currentSecodeTransmitted++;
            if (tries == 0)
            {
                return false;
            }
            else
                return true;

        }

        private void UartMessageProcess()
        {
            switch (currentBuf[0])
            {
                case 'T':
                case 't':
                case 'r':
                case 'R':
                    var m = new CanMessage(new string(currentBuf));
                    GotNewMessage?.Invoke(this, new GotCanMessageEventArgs() { receivedMessage = m });
                    UIContext.Send((x) => Messages.TryToAdd(m), null);
                    currentSecodeReceived++;
                    break;
                case 'z':
                case 'Z':
                    TxDone = true;
                    TransmissionSuccess?.Invoke(this, new());
                    break;
                case '\a':
                    TxFail = true;
                    ErrorCounter++;
                    Debug.WriteLine("<<ERROR>>", "CAN");
                    ErrorReported?.Invoke(this, new());
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
                if (newByte == 13 || newByte == 0 || newByte == 7 || newByte == 'Z' || newByte == 'z')
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

        public string Status => $"Bus use: Rx/Tx(Total):{lastSecondReceived}/{lastSecondTransmitted}({lastSecondTransmitted+lastSecondReceived}) ,Faults:{failedMessagesCounter}";

        public CanAdapter()
        {
            serialPort = new SerialPort();
            serialPort.BaudRate = 256000;
            serialPort.DataReceived += new SerialDataReceivedEventHandler(DataReceivedHandler);
            UIContext = SynchronizationContext.Current;
            oneSecondTimer = new(1000);
            oneSecondTimer.Elapsed += EverySecond;
            oneSecondTimer.Start();
        }

        private void EverySecond(object sender, EventArgs e)
        {
            lastSecondReceived = currentSecodeReceived;
            lastSecondTransmitted = currentSecodeTransmitted;
            currentSecodeReceived= 0;
            currentSecodeTransmitted= 0;
            OnPropertyChanged(nameof(Status));
        }
        
    }
}
