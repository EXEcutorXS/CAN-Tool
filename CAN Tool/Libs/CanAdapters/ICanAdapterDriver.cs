using System;

namespace CAN_Tool.Libs.CanAdapters
{
    public interface ICanAdapterDriver
    {
        event EventHandler<GotCanMessageEventArgs> MessageReceived;

        void OpenNormal(string portName);
        void OpenSelfReception(string portName);
        void OpenListenOnly(string portName);
        void Close();
        void SetBitrate(int bitrate);
        void Transmit(CanMessage message);
    }
}
