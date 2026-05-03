namespace OmniProtocol
{
    public class GenericLoadTrippleDeviceViewModel : DeviceViewModel
    {
        public GenericLoadTrippleDeviceViewModel(DeviceId id) : base(id) { }

        public GenericLoadTrippleViewModel GenericLoadTripple { get; } = new();
    }
}
