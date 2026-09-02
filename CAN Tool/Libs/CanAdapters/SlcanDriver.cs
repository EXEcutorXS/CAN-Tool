using System;
using System.IO.Ports;
using System.Text;
using System.Threading;

namespace CAN_Tool.Libs.CanAdapters
{
    // Generic driver for any SLCAN-compatible adapter - see CanAdapter.AdapterType's own
    // comment. Assumes real dedicated hardware, which needs no artificial pacing between
    // transmits (GetDelay() below is 0 here). A device that speaks SLCAN but genuinely
    // can't keep up at that rate - the org's own modem acting as a USB-CAN bridge, notably
    // (see ModemSlcanDriver) - gets its own AdapterType/subclass instead of slowing every
    // SLCAN adapter down to its pace.
    public class SlcanDriver : ICanAdapterDriver
    {
        private readonly SerialPort _port = new();
        private readonly StringBuilder _currentBuf = new();
        protected int _speed;

        // If parsing ever falls behind badly enough that no '\r' shows up for this long,
        // the buffer is garbage (or the device stopped framing) - drop it instead of
        // growing forever.
        private const int MaxBufferChars = 16384;

        public event EventHandler<GotCanMessageEventArgs> MessageReceived;

        public void OpenNormal(string portName) => Open(portName, "O");
        public void OpenSelfReception(string portName) => Open(portName, "Y");
        public void OpenListenOnly(string portName) => Open(portName, "L");

        private void Open(string portName, string modeCommand)
        {
            _port.PortName = portName;
            _port.Open();
            _port.Write("C\r");
            _port.Write($"S{_speed}\r");
            _port.Write($"{modeCommand}\r");
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
            /* This used to be Task.Delay(GetDelay()) without an await - that
               starts a timer task and immediately discards it, so nothing
               ever actually paused here regardless of GetDelay()'s value.
               Fixed to a real blocking wait (safe here: Transmit() is
               synchronous/void, and every call site - including flashing's
               tight per-fragment-byte loops - already runs on a background
               thread via Task.Run, never the UI thread). GetDelay() is 0 in
               this base class (see the class comment above); a slower
               device gets its own subclass overriding it, not a change
               here. */
            var delay = GetDelay();
            if (delay > 0) Thread.Sleep(delay);
        }

        protected virtual int GetDelay() => 0;

        private void DataReceivedHandler(object sender, SerialDataReceivedEventArgs args)
        {
            _currentBuf.Append(_port.ReadExisting());
            ProcessBuffer();
        }

        private void ProcessBuffer()
        {
            // Only process whole lines that have actually arrived (up to the last '\r'),
            // and only pay for what's newly consumed - not for rebuilding/resplitting the
            // whole accumulated buffer on every single receive event. The old version did
            // "_currentBuf += ..." (copies the entire buffer every call) followed by
            // Split('\r') over the whole thing again; under any backlog (parsing falling
            // behind the incoming stream, e.g. on a slow machine) that's O(n^2) and is
            // exactly why this protocol used to bog down on weak computers while it stayed
            // fine on fast ones - a fast machine just never builds up enough backlog to
            // notice.
            int lastCr = -1;
            for (var i = _currentBuf.Length - 1; i >= 0; i--)
            {
                if (_currentBuf[i] == '\r') { lastCr = i; break; }
            }
            if (lastCr < 0)
            {
                if (_currentBuf.Length > MaxBufferChars) _currentBuf.Clear(); // no framing char in a very long run - discard garbage
                return;
            }

            var complete = _currentBuf.ToString(0, lastCr + 1);
            _currentBuf.Remove(0, lastCr + 1);

            foreach (var line in complete.Split('\r'))
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
        }
    }
}
