namespace OmniProtocol
{
    public class GenericLoadTrippleDeviceViewModel : DeviceViewModel
    {
        public GenericLoadTrippleDeviceViewModel(DeviceId id) : base(id) { }

        public override GenericLoadTrippleViewModel GenericLoadTripple { get; } = new();
    }
}
