namespace OmniProtocol
{
    public class AcPanelDeviceViewModel : DeviceViewModel
    {
        public AcPanelDeviceViewModel(DeviceId id) : base(id) { }

        public ACPanelViewModel ACPanelParams { get; } = new();
    }
}
