using System;


namespace OmniProtocol
{

    public enum DeviceType { Binar, Planar, Hcu, ValveControl, BootLoader, CookingPanel, ExtensionBoard, PressureSensor, GenericLoadSingle, GenericLoadTripple, AcInverter }


    public class GotOmniMessageEventArgs : EventArgs
    {
        public OmniMessage receivedMessage;
    }

}
