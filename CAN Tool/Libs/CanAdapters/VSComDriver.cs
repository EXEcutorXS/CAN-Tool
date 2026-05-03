using System;
using System.Threading.Tasks;
using VSCom.CanApi;

namespace CAN_Tool.Libs.CanAdapters
{
    public class VSComDriver : ICanAdapterDriver
    {
        private readonly VSCAN _canWrapper = new();
        private int _speed;
        private bool _isOpen;
        Task _receiveTask;

        public event EventHandler<GotCanMessageEventArgs> MessageReceived;

        public void OpenNormal(string portName)
        {
            _canWrapper.Open(portName, VSCAN.VSCAN_MODE_NORMAL);
            ConfigureAndStart();
        }

        public void OpenSelfReception(string portName)
        {
            _canWrapper.Open(portName, VSCAN.VSCAN_MODE_SELF_RECEPTION);
            ConfigureAndStart();
        }

        public void OpenListenOnly(string portName)
        {
            _canWrapper.Open(portName, VSCAN.VSCAN_MODE_LISTEN_ONLY);
            ConfigureAndStart();
        }

        private void ConfigureAndStart()
        {
            _isOpen = true;
            _canWrapper.SetSpeed(_speed);
            _canWrapper.SetTimestamp(VSCAN.VSCAN_TIMESTAMP_OFF);
            _canWrapper.SetBlockingRead(VSCAN.VSCAN_IOCTL_ON);
            _receiveTask = Task.Run(ReceiveLoop);
        }

        public void Close() { _isOpen = false; _canWrapper.Close(); }
        

        public void SetBitrate(int bitrate)
        {
            _speed = bitrate;
            if (_isOpen)
                _canWrapper.SetSpeed(bitrate);
        }

        public void Transmit(CanMessage message)
        {
            VSCAN_MSG[] msg = new VSCAN_MSG[1];
            msg[0].Data = message.Data;
            msg[0].Flags |= message.Ide ? VSCAN.VSCAN_FLAGS_EXTENDED : VSCAN.VSCAN_FLAGS_STANDARD;
            if (message.Rtr)
                msg[0].Flags |= VSCAN.VSCAN_FLAGS_REMOTE;
            msg[0].Size = (byte)message.Dlc;
            msg[0].Id = (uint)message.Id;
            uint written = 0;
            try
            {
                _canWrapper.Write(msg, 1, ref written);
                _canWrapper.Flush();
            }
            catch { }
        }

        private void ReceiveLoop()
        {
            VSCAN_MSG[] msgs = new VSCAN_MSG[1];
            uint readbytes = 0;
            while (true)
            {
                _canWrapper.Read(ref msgs, 1, ref readbytes);
                MessageReceived?.Invoke(this, new GotCanMessageEventArgs { receivedMessage = new CanMessage(msgs[0]) });
            }
        }
    }
}
