using CAN_Tool.Libs;
using CAN_Tool.ViewModels;
using CAN_Tool;
using CommunityToolkit.Mvvm.ComponentModel;
using OmniProtocol;
using ScottPlot;
using System.Collections.Generic;
using System;
using System.ComponentModel;
using System.Diagnostics;
using System.Diagnostics.CodeAnalysis;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Threading;
using System.Windows.Media;
using static CAN_Tool.Libs.Helper;
using static Omni;
using System.Windows;
using CommunityToolkit.Mvvm.Input;
using System.IO;
using Newtonsoft;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using System.Collections.Specialized;
using ScottPlot.WPF;
using System.Collections.ObjectModel;
using System.Windows.Markup;


namespace OmniProtocol
{

    public enum UnitType { None, Temp, Volt, Current, Pressure, Flow, Rpm, Rps, Percent, Second, Minute, Hour, Day, Month, Year, Frequency }

    public enum DeviceType { Binar, Planar, Hcu, ValveControl, BootLoader, CookingPanel, ExtensionBoard, PressureSensor, GenericLoadSingle, GenericLoadTripple, AcInverter }

    public enum LoadMode_t { Off = 0, Toggle = 1, Pwm = 2 };

    public class GotOmniMessageEventArgs : EventArgs
    {
        public OmniMessage receivedMessage;
    }

}
