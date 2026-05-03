using System;
using System.Threading.Tasks;
using System.Windows;
using Candle;

namespace CAN_Tool.Libs.CanAdapters
{
    public class CandleLightDriver : ICanAdapterDriver
    {
        private Device _device;
        private int _speed;

        public event EventHandler<GotCanMessageEventArgs> MessageReceived;

        public void OpenNormal(string portName) => Open();
        public void OpenSelfReception(string portName) => Open();
        public void OpenListenOnly(string portName) => Open();

        private void Open()
        {
            var devices = Device.ListDevices();
            if (devices.Count == 0)
            {
                MessageBox.Show("The CandleLight adapter was not found");
                return;
            }
            _device = devices[0];
            _device.Open();
            _device.Channels[0].Start(BitrateToHz(_speed));
            Task.Run(ReceiveLoop);
        }

        public void Close()
        {
            _device.Channels[0].Stop();
            _device.Close();
        }

        public void SetBitrate(int bitrate)
        {
            _speed = bitrate;
            if (_device is { } d)
            {
                d.Channels[0].Stop();
                d.Channels[0].Start(BitrateToHz(bitrate));
            }
        }

        public void Transmit(CanMessage message) =>
            _device.Channels[0].Send(message.toCandleMessage(), true);

        private void ReceiveLoop()
        {
            while (_device != null)
            {
                var messages = _device.Channels[0].Receive();
                foreach (var msg in messages)
                    MessageReceived?.Invoke(this, new GotCanMessageEventArgs { receivedMessage = new CanMessage(msg) });
            }
        }

        private static int BitrateToHz(int bitrate) => bitrate switch
        {
            0 => 10000,
            1 => 20000,
            2 => 50000,
            3 => 100000,
            4 => 125000,
            5 => 250000,
            6 => 500000,
            7 => 800000,
            _ => 1000000
        };
    }
}
