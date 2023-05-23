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

    public class UartAdapter
    {
        public SerialPort SelectedPort { set; get; } = new();

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

        private List<byte> rxBuffer = new(); // 2 bytes PGN + 8 bytes Data + 2 bytes CRC

        private List<byte> txBuffer = new(); // 2 bytes PGN + 8 bytes Data + 2 bytes CRC

        private void AddByteToTxBuffer(byte b)
        {
            if (b != 0xCC)
                txBuffer.Add(b);
            else
            {
                txBuffer.Add(0xCC); //escape sequence
                txBuffer.Add(0xCC);
            }
        }
        public void Transmit(OmniMessage m)
        {
            List<byte> txBuffer = new();
            txBuffer.Add(0xCC);
            txBuffer.Add(0xAA);
            AddByteToTxBuffer((byte)(m.PGN >> 8));
            AddByteToTxBuffer((byte)(m.PGN & 0xFF));
            foreach (byte b in m.Data)
                AddByteToTxBuffer(b);
            UInt16 crc = calcCrc(txBuffer.ToArray(),10);
            AddByteToTxBuffer((byte)(crc >> 8));
            AddByteToTxBuffer((byte)(crc & 0xFF));
            txBuffer.Add(0xCC);
            txBuffer.Add(0xBB);
            SelectedPort.Write(txBuffer.ToArray(),0,txBuffer.Count);
        }
        private bool CheckCrc()
        {
            return BitConverter.ToUInt16(rxBuffer.ToArray(), 10) == calcCrc(rxBuffer.ToArray(),10);
        }

        private UInt16 calcCrc(byte[] data,int len)
        {
            byte inByte = 0;
            UInt16 clCRC = 0xffff;
            for (UInt16 i = 0; i < len; i++)
            {
                inByte = rxBuffer[i];
                byte bt = 0;
                for (byte j = 1; j <= 8; j++)
                {
                    bt = (byte)(clCRC & 1);
                    clCRC = (ushort)(clCRC >> 1);
                    if ((inByte & 1) != bt) clCRC = (ushort)(clCRC ^ 0xA001);
                    inByte = (byte)(inByte >> 1);
                }
            }
            return clCRC;
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
            if (BitConverter.ToUInt16(rxBuffer.Take(2).ToArray(), 0) > 511) { rxBuffer.Clear(); return; } //PGN can't be more than 511
            if (!CheckCrc()) { rxBuffer.Clear(); return; } //wrong CRC - clearing buffer and leaving

            OmniMessage msg = new OmniMessage();
            msg.TransmitterId = ConnectedDevice;
            msg.ReceiverId = new DeviceId(126, 6);
            msg.PGN = rxBuffer[0] * 256 + rxBuffer[1];

            rxBuffer.Skip(2).ToArray().CopyTo(msg.Data, 8);
            GotNewMessage?.Invoke(this, new GotOmniMessageEventArgs() { receivedMessage = msg });
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
