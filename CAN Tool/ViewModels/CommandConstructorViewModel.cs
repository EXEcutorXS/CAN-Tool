using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using CAN_Tool.ViewModels.Base;
using System.ComponentModel;
using System.Windows.Input;
using System.Windows;
using CAN_Tool.Infrastructure.Commands;
using System.IO.Ports;
using Can_Adapter;
using AdversCan;
using System.Windows.Threading;
using System.Threading;
using System.Windows.Data;
using System.Globalization;

namespace CAN_Tool.ViewModels
{

    public class CommandConstructorViewModel : ViewModel
    {

        private AC2P AC2Pinstatnce;
        CommandConstructorViewModel(AC2P instance)
        {
            AC2Pinstatnce = instance;
        }

    }
}
