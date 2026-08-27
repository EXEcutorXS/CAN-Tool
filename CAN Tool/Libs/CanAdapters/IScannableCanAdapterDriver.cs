using System;

namespace CAN_Tool.Libs.CanAdapters
{
    // Optional capability for adapters that need live device discovery instead of a
    // fixed OS-level port list (e.g. BLE). When a driver implements this, the UI's
    // "refresh ports" action drives a scan into the same PortList/PortName combo box
    // used for serial ports, rather than calling SerialPort.GetPortNames().
    public interface IScannableCanAdapterDriver
    {
        void StartScan(Action<string> deviceFound);
        void StopScan();
    }
}
