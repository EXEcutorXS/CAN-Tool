using System;
using System.Collections.Generic;
using System.IO.Ports;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Interop;
using CAN_Tool.CustomControls;
using CAN_Tool.Libs;
using CommunityToolkit.Mvvm.ComponentModel;
using ScottPlot.Drawing.Colormaps;
using ScottPlot.Renderable;
using VSCom.CanApi;
using Windows.Devices.Input;
using Windows.Networking;
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

        private Task MessageReceivingTask;

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
            canWrapper.Close();
        }

        public void PortOpenNormal(string portName = VSCAN.VSCAN_FIRST_FOUND)
        {
            if (!PortOpened)
            {
                PortOpened = true;
                canWrapper.Open(portName, VSCAN.VSCAN_MODE_NORMAL);
                canWrapper.SetSpeed(Speed);
                canWrapper.SetTimestamp(VSCAN.VSCAN_TIMESTAMP_OFF);
                canWrapper.SetBlockingRead(VSCAN.VSCAN_IOCTL_ON);

                MessageReceivingTask = Task.Run(messageReceiver);
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
                canWrapper.Open(portName, VSCAN.VSCAN_MODE_SELF_RECEPTION);
                canWrapper.SetSpeed(Speed);
                canWrapper.SetTimestamp(VSCAN.VSCAN_TIMESTAMP_OFF);
                canWrapper.SetBlockingRead(VSCAN.VSCAN_IOCTL_ON);

                MessageReceivingTask = Task.Run(messageReceiver);
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
                canWrapper.Open(portName, VSCAN.VSCAN_MODE_LISTEN_ONLY);
                canWrapper.SetSpeed(Speed);
                canWrapper.SetTimestamp(VSCAN.VSCAN_TIMESTAMP_OFF);
                canWrapper.SetBlockingRead(VSCAN.VSCAN_IOCTL_ON);

                MessageReceivingTask = Task.Run(messageReceiver);
            }
            else
            {
                MessageBox.Show(GetString("t_port_already_opened"));
            }
        }



        public void PortClose() => PortOpened = false;

        public void SetBitrate(int bitrate) => canWrapper.SetSpeed(bitrate);


        public void Transmit(CanMessage message)
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
                MessageBox.Show(ex.Message);
            }
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
