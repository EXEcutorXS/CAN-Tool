namespace OmniProtocol
{
    public class AcInverterDeviceViewModel : DeviceViewModel
    {
        public AcInverterDeviceViewModel(DeviceId id) : base(id) { }

        public override ACInverterViewModel ACInverterParams { get; } = new();
    }
}
