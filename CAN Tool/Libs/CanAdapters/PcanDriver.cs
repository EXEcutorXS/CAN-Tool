using System;
using Peak.Can.Basic;

namespace CAN_Tool.Libs.CanAdapters
{
    public class PcanDriver : ICanAdapterDriver
    {
        private readonly Worker _worker = new(PcanChannel.Usb01, Bitrate.Pcan250);

        public event EventHandler<GotCanMessageEventArgs> MessageReceived;

        public PcanDriver()
        {
            _worker.MessageAvailable += OnMessageAvailable;
        }

        public void OpenNormal(string portName)
        {
            _worker.ListenOnly = false;
            _worker.Start();
        }

        public void OpenSelfReception(string portName) => OpenNormal(portName);

        public void OpenListenOnly(string portName)
        {
            _worker.ListenOnly = true;
            _worker.Start();
        }

        public void Close() => _worker.Stop();

        public void SetBitrate(int bitrate)
        {
            _worker.BitrateCan = bitrate switch
            {
                0 => Bitrate.Pcan10,
                1 => Bitrate.Pcan20,
                2 => Bitrate.Pcan50,
                3 => Bitrate.Pcan100,
                4 => Bitrate.Pcan125,
                5 => Bitrate.Pcan250,
                6 => Bitrate.Pcan500,
                7 => Bitrate.Pcan800,
                _ => Bitrate.Pcan1000
            };
        }

        public void Transmit(CanMessage message) => _worker.Transmit(message.toPcanMsg());

        private void OnMessageAvailable(object sender, MessageAvailableEventArgs e)
        {
            while (_worker.Dequeue(out PcanMessage msg, out _))
            {
                try
                {
                    MessageReceived?.Invoke(this, new GotCanMessageEventArgs { receivedMessage = new CanMessage(msg) });
                }
                catch { }
            }
        }
    }
}
