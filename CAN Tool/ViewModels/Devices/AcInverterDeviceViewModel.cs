namespace OmniProtocol
{
    public class AcInverterDeviceViewModel : DeviceViewModel
    {
        public AcInverterDeviceViewModel(DeviceId id) : base(id) { }

        public ACInverterViewModel ACInverterParams { get; } = new();
    }
}
