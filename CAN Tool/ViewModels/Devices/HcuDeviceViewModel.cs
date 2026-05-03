namespace OmniProtocol
{
    public class HcuDeviceViewModel : DeviceViewModel
    {
        public HcuDeviceViewModel(DeviceId id) : base(id) { }
        public Timberline20OmniViewModel TimberlineParams { get; } = new();
    }
}
