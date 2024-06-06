using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;

namespace CAN_Tool.Controls
{
    class MyCheckBox:CheckBox
    {
        public MyCheckBox()
        {
            Checked += emptyAction;
            Unchecked += emptyAction;
        }

        public void emptyAction(object Sender, RoutedEventArgs e)
        {
            e.Handled = true;
        }
    }
}
