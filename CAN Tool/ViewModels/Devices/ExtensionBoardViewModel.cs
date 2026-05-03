using CommunityToolkit.Mvvm.ComponentModel;
using OmniProtocol;
using System.ComponentModel;

namespace CAN_Tool.ViewModels.Devices
{
    class ExtensionBoardViewModel : ObservableObject
    {
        DeviceViewModel parent;

        public ExtensionBoardViewModel(DeviceViewModel parent)
        {
            this.parent = parent;

            pwmChannels.AddNew();
            pwmChannels.AddNew();
            pwmChannels.AddNew();
            adcChannels.AddNew();
            adcChannels.AddNew();
            adcChannels.AddNew();
            adcChannels.AddNew();
            temperatureData.AddNew();
            temperatureData.AddNew();
            temperatureData.AddNew();
            temperatureData.AddNew();
        }

        private BindingList<int> pwmChannels = new();
        public BindingList<int> PwmChannels => pwmChannels;

        private BindingList<int> adcChannels = new();
        public BindingList<int> AdcChannels => adcChannels;

        private BindingList<float> temperatureData = new();
        public BindingList<float> TemperatureData => temperatureData;
    }
}
