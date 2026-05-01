using System;
using System.IO.Ports;
using System.Text;
using System.Threading.Tasks;

namespace CAN_Tool.Libs.CanAdapters
{
    public class CanableDriver : ICanAdapterDriver
    {
        private readonly SerialPort _port = new();
        private string _currentBuf = "";
        private int _speed;

        public event EventHandler<GotCanMessageEventArgs> MessageReceived;

        public void OpenNormal(string portName) => Open(portName, "O");
        public void OpenSelfReception(string portName) => Open(portName, "Y");
        public void OpenListenOnly(string portName) => Open(portName, "L");

        private void Open(string portName, string modeCommand)
        {
            _port.PortName = portName;
            _port.Open();
            _port.Write($"{modeCommand}\r");
            _port.Write($"S{_speed}\r");
            _port.DataReceived += DataReceivedHandler;
        }

        public void Close()
        {
            _port.DataReceived -= DataReceivedHandler;
            new System.Threading.Thread(() =>
            {
                try { _port.Close(); }
                catch { }
            }).Start();
        }

        public void SetBitrate(int bitrate)
        {
            _speed = bitrate;
            if (_port.IsOpen)
                _port.Write($"S{bitrate}\r");
        }

        public void Transmit(CanMessage message)
        {
            if (!_port.IsOpen) return;

            var str = new StringBuilder();
            str.Append(message.Ide switch
            {
                true when message.Rtr => 'R',
                false when message.Rtr => 'r',
                true => 'T',
                false => 't'
            });
            str.Append(message.IdAsText);
            str.Append(message.Dlc);
            str.Append(message.GetDataInTextFormat());
            str.Append('\r');
            _port.Write(str.ToString());
            Task.Delay(GetDelay());
        }

        private int GetDelay() => _speed switch
        {
            0 => 20,
            1 => 8,
            2 => 4,
            3 => 2,
            _ => 1
        };

        private void DataReceivedHandler(object sender, SerialDataReceivedEventArgs args)
        {
            _currentBuf += _port.ReadExisting();
            ProcessBuffer();
        }

        private void ProcessBuffer()
        {
            var splitted = _currentBuf.Split('\r');
            foreach (var line in splitted)
            {
                if (line.Length == 0) continue;
                if (line[0] is 'T' or 't' or 'r' or 'R')
                {
                    try
                    {
                        var m = new CanMessage(line);
                        MessageReceived?.Invoke(this, new GotCanMessageEventArgs { receivedMessage = m });
                    }
                    catch { }
                }
            }
            _currentBuf = splitted[^1];
        }
    }
}
