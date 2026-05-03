namespace OmniProtocol
{
    public class HcuDeviceViewModel : DeviceViewModel
    {
        public HcuDeviceViewModel(DeviceId id) : base(id) { }
        public override Timberline20OmniViewModel TimberlineParams { get; } = new();
    }
}
