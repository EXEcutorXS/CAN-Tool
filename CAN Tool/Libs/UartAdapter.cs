using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using OmniProtocol;
using System.IO.Ports;
using Microsoft.VisualBasic;
using System.Windows.Media.Animation;
using Can_Adapter;

namespace CAN_Tool.Libs
{

    internal class UartAdapter
    {
        public SerialPort SelectedPort { set; get; }

        private bool escapeSequenceStarted = false;

        public DeviceId ConnectedDevice { set; get; }

        public void Open()
        {
            try
            {
                SelectedPort.Open();
                SelectedPort.DataReceived += GetBytes;
            }
            catch { }
        }

        public void Close()
        {
            try { SelectedPort.Close(); } catch { }
        }

        private List<byte> rxBuffer = new();

        private bool CheckCrc()
        {
            return true; //CRC is fine;
        }

        private void RequestId()
        {

        }

        private void HandleMessage()
        {
            if (ConnectedDevice == null) //Device is not connected
            {
                rxBuffer.Clear();
                RequestId();
                return;
            }

            OmniMessage msg = new OmniMessage();
            msg.TransmitterId = ConnectedDevice;
            msg.ReceiverId = new DeviceId(126, 6);
            msg.PGN = rxBuffer[0] * 256 + rxBuffer[1];

            if (msg.PGN > 511) { rxBuffer.Clear(); return; } //PGN can't be more than 511
            
            rxBuffer.Skip(2).ToArray().CopyTo(msg.Data, 8);
            GotNewMessage?.Invoke(this, new GotMessageEventArgs() { receivedMessage = msg });
        }

        public event EventHandler GotNewMessage;

        private void GetBytes(object sender, EventArgs a)
        {
            while (SelectedPort.BytesToRead > 0)
            {
                byte c = (byte)SelectedPort.ReadChar();
                switch (c)
                {
                    case 0xCC:
                        if (!escapeSequenceStarted)
                            escapeSequenceStarted = true;
                        else
                        {
                            if (c == 0xAA)
                                rxBuffer.Clear();
                            else if (c == 0xBB)
                                HandleMessage();
                            else if (c == 0xCC)
                                rxBuffer.Add(c);
                            else
                                rxBuffer.Clear(); //Error situation

                            escapeSequenceStarted = false;
                        }
                        break;
                    default:
                        rxBuffer.Append(c);
                        break;
                }

                rxBuffer.Add((byte)SelectedPort.ReadByte());
            }
        }

    }
}
