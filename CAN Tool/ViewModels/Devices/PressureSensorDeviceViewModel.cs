using CommunityToolkit.Mvvm.ComponentModel;

namespace OmniProtocol
{
    public partial class PressureSensorDeviceViewModel : DeviceViewModel
    {
        public PressureSensorDeviceViewModel(DeviceId id) : base(id) { }

        public double[] PressureLog { get; } = new double[720000];
        [ObservableProperty] private int pressureLogPointer = 0;
        public bool PressureLogWriting = false;
    }
}
