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

    public partial class OmniZoneHandler : ObservableObject
    {

        [ObservableProperty] private int tempSetPointDay = 22;

        [ObservableProperty] private int tempSetPointNight = 20;

        [ObservableProperty] private int currentTemperature;

        [NotifyPropertyChangedFor(nameof(ManualMode))]
        [ObservableProperty] private zoneType_t connected = zoneType_t.Disconnected;

        [ObservableProperty]
        private zoneState_t state = zoneState_t.Off;

        [ObservableProperty] private bool manualMode;

        [ObservableProperty] private int manualPercent = 40;

        [ObservableProperty] private int setPwmPercent = 50;

        [ObservableProperty] private int fanStage = 2;

        [ObservableProperty] private int currentPwm = 50;
    }

    public partial class GenericLoadTrippleViewModel : ObservableObject
    {


        public GenericLoadTrippleViewModel()
        {

        }

        [ObservableProperty] public LoadMode_t loadMode1;
        [ObservableProperty] public LoadMode_t loadMode2;
        [ObservableProperty] public LoadMode_t loadMode3;
        [ObservableProperty] public int pwmLevel1;
        [ObservableProperty] public int pwmLevel2;
        [ObservableProperty] public int pwmLevel3;
    }

    public partial class ACInverterViewModel : ObservableObject
    {
        [ObservableProperty] public int compressorRevsSet;
        [ObservableProperty] public int compressorRevsMeasured;
        [ObservableProperty] public float compressorCurrent;
        [ObservableProperty] public float condensorCurrent;
        [ObservableProperty] public float condensorPwmSet;
    }

    public partial class Timberline20OmniViewModel : ObservableObject
    {
        //private void ZoneChanged(OmniZoneHandler newSelectedZone) => SelectedZone = newSelectedZone;

        public Timberline20OmniViewModel()
        {
            for (var i = 0; i < 5; i++)
                Zones.Add(new OmniZoneHandler());

            SelectedZone = zones[0];
        }

        [ObservableProperty] private int tankTemperature;

        [ObservableProperty] private int outsideTemperature;

        [ObservableProperty] private int liquidLevel;

        [ObservableProperty] private bool heaterEnabled;

        [ObservableProperty] private bool elementEnabled;

        [ObservableProperty] private bool domesticWaterFlow;

        [ObservableProperty] private DateTime time;

        [ObservableProperty] private OmniZoneHandler selectedZone;

        [ObservableProperty] private BindingList<OmniZoneHandler> zones = new();
    }

    public partial class ACPanelViewModel : ObservableObject
    {
        [ObservableProperty] private float setTemperature;
        [ObservableProperty] private float currentTemperature;
        [ObservableProperty] private bool isOn;
        [ObservableProperty] private int mode;
        [ObservableProperty] private int fanSpeed;
    }

}
