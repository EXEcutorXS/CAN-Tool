using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Controls;

namespace CAN_Tool.CustomControls
{
    public class SilentSlider : Slider
    {
        public SilentSlider()
        {
            ValueChanged += CustomChange;
        }

        private void CustomChange(object sender, System.Windows.RoutedPropertyChangedEventArgs<double> e)
        {
            if (silentFlag)
            {
                silentFlag = false;
                e.Handled = true;
                return;
            }
        }

        private bool silentFlag = false;

        public void SilentChange(double value)
        {
            silentFlag = true;
            Value = value;
        }
    }
}
