using CAN_Tool.Infrastructure.Commands;
using CAN_Tool.ViewModels.Base;
using OmniProtocol;
using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace CAN_Tool.Libs.Devices
{
    class ExtensionBoardViewModel:ViewModel
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

        private BindingList<int> pwmChannels = new BindingList<int>();
        public BindingList<int> PwmChannels => pwmChannels;

        private BindingList<int> adcChannels = new BindingList<int>();
        public BindingList<int> AdcChannels => adcChannels;

        private BindingList<float> temperatureData = new BindingList<float>();
        public BindingList<float> TemperatureData => temperatureData;

        public LambdaCommand toggleChannel;

        public void onToggleChannelCommandExecute(object parameter)
        {
            int channel = (int)parameter;
            if (channel > 3) return;
            OmniMessage msg = new();
            msg.PGN = 43;
            msg.ReceiverId = parent.ID;
                
                    if (PwmChannels[channel] > 0)
                    {
                        msg.Data[0+2*channel] = 0;
                        msg.Data[1 + 2 * channel] = 0;
                    }
                    else
                    {
                        msg.Data[0 + 2 * channel] = 1000 / 256;
                        msg.Data[1 + 2 * channel] = 1000 % 256;
                    }

        }

        public void onSetChannelPwmCommandExecute(object parameter)
        {
            if (parameter.GetType() != typeof(Tuple<int, int>))
                return;
            int channel = (int)parameter;
            if (channel > 3) return;

        }
    }
}
