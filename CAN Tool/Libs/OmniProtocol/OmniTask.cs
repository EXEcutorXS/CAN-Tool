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


public partial class OmniTask : ObservableObject
{
    [NotifyPropertyChangedFor(nameof(TaskStatus))]
    [ObservableProperty]
    private int percentComplete;

    private DateTime capturedTime;

    [NotifyPropertyChangedFor(nameof(TaskStatus))]
    [ObservableProperty] private string name;

    public CancellationTokenSource Cts { get; set; } = new CancellationTokenSource();

    [NotifyPropertyChangedFor(nameof(TaskStatus))]
    [ObservableProperty]
    private bool done;

    [NotifyPropertyChangedFor(nameof(TaskStatus))]
    [ObservableProperty]
    private bool cancelled;

    public event EventHandler TaskDone;
    public event EventHandler TaskCancelled;

    [ObservableProperty] private TimeSpan? lastOperationDuration;

    public void OnDone()
    {
        LastOperationDuration = DateTime.Now - capturedTime;
        Occupied = false;
        PercentComplete = 100;
        Done = true;
        TaskDone?.Invoke(null, null!);
    }

    public void OnCancel()
    {
        Cts.Cancel();
        Occupied = false;
        Cancelled = true;
        TaskCancelled?.Invoke(null, null!);
    }

    public void OnFail(string reason = "")
    {
        FailReason = reason;
        Cts.Cancel();
        Occupied = false;
        Failed = true;
        TaskCancelled?.Invoke(null, null!);
    }


    [NotifyPropertyChangedFor(nameof(TaskStatus))]
    [ObservableProperty] private bool occupied;

    [NotifyPropertyChangedFor(nameof(TaskStatus))]
    [ObservableProperty] private string failReason;

    [NotifyPropertyChangedFor(nameof(TaskStatus))]
    [ObservableProperty] private bool failed;

    public bool Capture(string taskName)
    {
        if (Occupied) return false;
        Occupied = true;
        Name = taskName;
        FailReason = "";
        PercentComplete = 0;
        Done = false;
        Cancelled = false;
        Cts = new CancellationTokenSource();
        capturedTime = DateTime.Now;
        return true;
    }

    public void UpdatePercent(int p)
    {
        PercentComplete = p;
    }

    public string TaskStatus
    {
        get
        {
            if (!Done && !Cancelled && !Failed && Occupied)
                return GetString(Name) + " " + GetString("t_in_progress");
            if (Done)
                return GetString(Name) + " " + GetString("t_done_in") + " " + $"{LastOperationDuration.Value.TotalSeconds:F3} " + GetString("u_s");
            if (Cancelled)
                return GetString(Name) + " " + GetString("t_cancelled");
            if (Failed)
                return GetString(Name) + " " + GetString("t_failed") + ", " + GetString(FailReason);
            return "";
        }
    }

}
