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


public partial class ReadedParameter : ObservableObject, IUpdatable<ReadedParameter>, IComparable
{
    [ObservableProperty] private int id;

    [ObservableProperty] private uint value;

    public string Name => GetString($"par_{Id}");

    public void Update(ReadedParameter item) => Value = item.Value;

    public bool IsSimiliarTo(ReadedParameter item) => (Id == item.Id);

    public int CompareTo(object obj) => Id - (obj as ReadedParameter).Id;
}

public partial class StatusVariable : ObservableObject, IUpdatable<StatusVariable>, IComparable
{
    public int[] LineWidthes => new int[] { 1, 2, 3, 4, 5 };

    public StatusVariable(int var) : base()
    {
        Id = var;
        Display = App.Settings.ShowFlag[Id];
        chartBrush = new SolidColorBrush(App.Settings.Colors[Id]);
        lineWidth = App.Settings.LineWidthes[Id];
        LineStyle = App.Settings.LineStyles[Id];
        markShape = App.Settings.MarkShapes[Id];

    }

    [ObservableProperty] public int id;

    [NotifyPropertyChangedFor(nameof(VerboseInfo), nameof(Value), nameof(FormattedValue))]
    [ObservableProperty] private long rawValue;
    [ObservableProperty] private bool display = false;
    [ObservableProperty] private OmniPgnParameter assignedParameter;
    [NotifyPropertyChangedFor(nameof(Color))]
    [ObservableProperty] private Brush chartBrush;
    [ObservableProperty] private int lineWidth;
    [ObservableProperty] private LineStyle lineStyle;
    [ObservableProperty] private MarkerShape markShape;

    public string VerboseInfo => AssignedParameter.Decode(RawValue);

    public double Value => ImperialConverter(RawValue * AssignedParameter.a + AssignedParameter.b, AssignedParameter.UnitT);

    public string FormattedValue => Value.ToString(AssignedParameter.OutputFormat);

    public string Name => GetString($"var_{Id}");

    public string ShortName => GetString($"vars_{Id}");

    public System.Drawing.Color Color => System.Drawing.Color.FromArgb(255, (ChartBrush as SolidColorBrush).Color.R, (ChartBrush as SolidColorBrush).Color.G, (ChartBrush as SolidColorBrush).Color.B);

    public bool IsSimiliarTo(StatusVariable item) => Id == item.Id;

    public void Update(StatusVariable item) => RawValue = item.RawValue;

    public int CompareTo(object obj) => Id - (obj as StatusVariable).Id;
}

public partial class BbCommonVariable : ObservableObject, IUpdatable<BbCommonVariable>, IComparable
{
    [ObservableProperty] private int id;

    [ObservableProperty] public int value;

    public string Name => GetString($"var_{Id}");

    public string Description => ToString();

    public int CompareTo(object obj) => Id - ((BbCommonVariable)obj).Id;

    public void Update(BbCommonVariable item) => Value = item.Value;
    public bool IsSimiliarTo(BbCommonVariable item) => Id == item.Id;
    public override string ToString() => $"{Name}: {Value}";
}

public partial class BbError : ObservableObject
{
    public UpdatableList<BbCommonVariable> Variables { get; } = new();

    public override string ToString()
    {
        var retString = new StringBuilder("");
        foreach (var v in Variables)
            retString.Append(v + ";");
        return retString.ToString();
    }

    public string Name
    {
        get
        {
            var error = Variables.FirstOrDefault(v => v.Id == 24); //24 - paramsname.h error code

            return GetString(error == null ? "t_no_error_code" : $"e_{error.Value}");
        }
    }
}

public partial class CommonParameters : ObservableObject, ICloneable
{
    [ObservableProperty] private int revMeasured;
    [ObservableProperty] private int revSet;
    [ObservableProperty] private double fuelPumpMeasured;
    [ObservableProperty] private int glowPlug;
    [ObservableProperty] private double voltage;
    [ObservableProperty] private int stageTime;
    [ObservableProperty] private int modeTime;
    [ObservableProperty] private int setPowerLevel;

    [NotifyPropertyChangedFor(nameof(StageString))]
    [ObservableProperty] private int stage;

    [NotifyPropertyChangedFor(nameof(StageString))]
    [ObservableProperty] private int mode;

    [NotifyPropertyChangedFor(nameof(ErrorString))]
    [ObservableProperty] private int error;
    [ObservableProperty] private int workTime;
    [ObservableProperty] private int flameSensor;
    [ObservableProperty] private int bodyTemp;
    [ObservableProperty] private int liquidTemp;
    [ObservableProperty] private int overheatTemp;
    [ObservableProperty] private int panelTemp;
    [ObservableProperty] private int inletTemp;
    [ObservableProperty] private float pressure;
    [ObservableProperty] private float exPressure;
    [ObservableProperty] private float pcbTemp;
    [ObservableProperty] private float mcuTemp;


    public string StageString => GetString($"m_{Stage}-{Mode}");

    public string ErrorString => Error + " - " + GetString($"e_{Error}");

    public object Clone() => MemberwiseClone();
}



public partial class DeviceId : ObservableObject
{
    public DeviceId(int type, int adr)
    {
        if (type > 127 || adr > 7)
            throw new ArgumentException("Bad device config address must be below 7 and Type - below 127");
        Type = type;
        Address = adr;
    }

    [ObservableProperty] private int type;
    [ObservableProperty] private int address;

    public override string ToString() => Omni.Devices.ContainsKey(Type) ? $"{Type} - {Address} ({Omni.Devices[Type]})" : $"{Type} - {Address}";

    public override int GetHashCode() => Type << 3 + Address;

    public override bool Equals([NotNullWhen(true)] object obj)
    {
        if (obj is not DeviceId)
            return false;
        return GetHashCode() == obj.GetHashCode();
    }
}
