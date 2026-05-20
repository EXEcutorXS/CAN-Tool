using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using System.Windows;
using CAN_Tool;
using CAN_Tool.Libs;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using ScottPlot;
using static CAN_Tool.Libs.Helper;


namespace OmniProtocol
{

public partial class Omni : ObservableObject
{
    private readonly object _deviceListLock = new();

    public Omni(CanAdapter canAdapter)
    {
        ArgumentNullException.ThrowIfNull(canAdapter);
        this.canAdapter = canAdapter;
        SeedStaticData();

        try
        {
            var str = File.ReadAllText("presets.json");
            JArray serialised = (JArray)JsonConvert.DeserializeObject(str);
            AllPresets = serialised.ToObject<BindingList<ConfigPreset>>();
        }
        catch (Exception ex)
        {
            Debug.WriteLine($"Can't load preset list, empty list initiated: {ex.Message}");
        }

        allPresets.ListChanged += PresetCollectionChanged;
    }

    private void PresetCollectionChanged(object sender, ListChangedEventArgs e)
    {
        OnPropertyChanged(nameof(AvailableModels));
        OnPropertyChanged(nameof(AvailableVendors));
    }

    public event EventHandler NewDeviceAcquired;

    public WpfPlot plot;

    private static readonly Dictionary<int, string> DefMeaningsYesNo = new() { { 0, "t_no" }, { 1, "t_yes" }, { 2, "t_no_data" }, { 3, "t_no_data" } };
    private static readonly Dictionary<int, string> DefMeaningsOnOff = new() { { 0, "t_off" }, { 1, "t_on" }, { 2, "t_no_data" }, { 3, "t_no_data" } };
    private static readonly Dictionary<int, string> DefMeaningsAllow = new() { { 0, "t_disabled" }, { 1, "t_enabled" }, { 2, "t_no_data" }, { 3, "t_no_data" } };
    private static readonly Dictionary<int, string> Stages = new() { { 0, "STAGE_Z" }, { 1, "STAGE_P" }, { 2, "STAGE_H" }, { 3, "STAGE_W" }, { 4, "STAGE_F" }, { 5, "STAGE_T" }, { 6, "STAGE_M" } };

    public bool UseImperial { set; get; }




    [ObservableProperty] private ObservableCollection<DeviceViewModel> connectedDevices = new();

    [NotifyPropertyChangedFor(nameof(AvailableModels), nameof(AvailableVendors))]
    [ObservableProperty] private DeviceViewModel selectedConnectedDevice;

    public UpdatableList<OmniMessage> Messages { get; } = new();

    private readonly CanAdapter canAdapter;

    [ObservableProperty] private bool readingBbErrorsMode = false;
    [NotifyPropertyChangedFor(nameof(AvailableModels), nameof(AvailableVendors))]
    [ObservableProperty] public static BindingList<ConfigPreset> allPresets = new();
    [NotifyPropertyChangedFor(nameof(AvailableModels))]
    [ObservableProperty] private string selectedVendor;
    [ObservableProperty] private string selectedModel;

    [RelayCommand]
    void ClearMessages() => Messages.Clear();

    [RelayCommand]
    void ClearDevices()
    {
        ConnectedDevices.Clear();
    }

    [RelayCommand]
    void LoadPreset()
    {
        SelectedConnectedDevice.ReadParameters.Clear();
        var Preset = AllPresets.FirstOrDefault(p => p.VendorName == SelectedVendor && p.ModelName == SelectedModel && SelectedConnectedDevice.Id.Type == p.DeviceType);
        if (Preset == null)
        {
            MessageBox.Show($"Can't find preset for {SelectedVendor}-{SelectedModel}");
            return;
        }
        foreach (var parameter in Preset.ParamList)
        {
            SelectedConnectedDevice.ReadParameters.TryToAdd(new ReadedParameter() { Id = parameter.Item1, Value = parameter.Item2 });
        }
    }

    [RelayCommand]
    void SavePreset()
    {
        if (SelectedVendor.Length < 3 || SelectedModel.Length < 3)
        {
            MessageBox.Show("Model and vendor names must contain at least 3 characters");
            return;
        }
        var Preset = AllPresets.FirstOrDefault(p => p.VendorName == SelectedVendor && p.ModelName == SelectedModel && p.DeviceType == SelectedConnectedDevice.Id.Type);
        if (Preset != null)
        {
            MessageBox.Show($"Preset for \"{SelectedVendor} - {SelectedModel}\" already exists, delete it first");
            return;
        }
        ConfigPreset newPreset = new ConfigPreset() { ModelName = SelectedModel, VendorName = SelectedVendor, DeviceType = SelectedConnectedDevice.Id.Type };
        foreach (var parameter in SelectedConnectedDevice.ReadParameters)
        {
            if (parameter.Id < 12 || parameter.Id > 14) //Excluding serial number
                newPreset.ParamList.Add(new Tuple<int, uint>(parameter.Id, parameter.Value));
        }
        AllPresets.Add(newPreset);
        if (File.Exists("presets.json"))
            File.Delete("presets.json");
        string serialized = JsonConvert.SerializeObject(AllPresets);

        StreamWriter sw = new("presets.json", false);
        sw.Write(serialized);
        sw.Flush();
        sw.Dispose();

    }

    [RelayCommand]
    void DeletePreset()
    {
        if (AllPresets.Remove(AllPresets.FirstOrDefault(p => p.ModelName == SelectedModel && p.VendorName == SelectedVendor && p.DeviceType == SelectedConnectedDevice.Id.Type)))
        {
            try
            {
                if (File.Exists("presets.json"))
                    File.Delete("presets.json");
                string serialized = JsonConvert.SerializeObject(AllPresets);

                StreamWriter sw = new("presets.json", false);
                sw.Write(serialized);
                sw.Flush();
                sw.Dispose();
                MessageBox.Show($"Preset for {SelectedVendor}-{SelectedModel} successfully removed");
            }
            catch (Exception ex)
            {
                MessageBox.Show(ex.Message);
            }
        }
        else
        {
            MessageBox.Show($"Preset for {SelectedVendor}-{SelectedModel} not found");
        }

    }

    public BindingList<string> AvailableModels
    {
        get
        {
            var initialCollection = AllPresets;
            var distincted = initialCollection.Where(p => p.VendorName == SelectedVendor && p.DeviceType == SelectedConnectedDevice.Id.Type).ToList();
            var ret = new BindingList<string>(distincted.Select(p => p.ModelName).ToList());
            return ret;
        }
    }

    public BindingList<string> AvailableVendors
    {
        get
        {
            var initialCollection = AllPresets;
            var distincted = initialCollection.DistinctBy(p => p.VendorName).Where(p => p.DeviceType == SelectedConnectedDevice?.Id.Type).ToList();
            var ret = new BindingList<string>(distincted.Select(p => p.VendorName).ToList());
            return ret;
        }
    }


    private readonly SynchronizationContext uiContext = SynchronizationContext.Current;

    [ObservableProperty] private OmniTask currentTask = new();

    private bool CancellationRequested => CurrentTask.Cts.IsCancellationRequested;

    private bool Capture(string n) => CurrentTask.Capture(n);

    private void Done() => CurrentTask.OnDone();

    private void Cancel() => CurrentTask.OnCancel();

    private void Fail(string reason = "") => CurrentTask.OnFail(reason);

    private void UpdatePercent(int p) => CurrentTask.UpdatePercent(p);

    public void ProcessCanMessage(CanMessage m)
    {
        ProcessOmniMessage(new OmniMessage(m));

    }

    public void ProcessOmniMessage(OmniMessage m)
    {

        var id = m.TransmitterId;

        DeviceViewModel senderDevice;
        bool isNew = false;
        lock (_deviceListLock)
        {
            senderDevice = ConnectedDevices.FirstOrDefault(d => d.Id.Equals(m.TransmitterId));
            if (senderDevice == null)
            {
                senderDevice = DeviceViewModel.Create(id);
                ConnectedDevices.Add(senderDevice);
                isNew = true;
            }
        }
        if (isNew)
        {
            NewDeviceAcquired?.Invoke(this, null);
            if (senderDevice.Id.Type != 123)        //Requesting basic data, but not for bootloaders
                Task.Run(() => RequestSerial(id));
        }

        if (Pgns.ContainsKey(m.Pgn))
            foreach (var p in Pgns[m.Pgn].parameters)
            {

                if (Pgns[m.Pgn].multiPack && p.PackNumber != m.Data[0]) continue;
                if (p.Var == 0) continue;
                var sv = new StatusVariable(p.Var);
                sv.AssignedParameter = p;
                var rawValue = OmniMessage.GetRawValue(m.Data, p.BitLength, p.StartBit, p.StartByte, p.Signed);
                if (Math.Abs(rawValue - (Math.Pow(2, p.BitLength) - 1)) < 0.3) continue; //Unsupported parameter
                sv.RawValue = rawValue;
                senderDevice.SupportedVariables[sv.Id] = true;
                senderDevice.Status.TryToAdd(sv);

                switch (sv.Id)
                {
                    case 1:
                        senderDevice.Parameters.Stage = (int)(rawValue * sv.AssignedParameter.a + sv.AssignedParameter.b);
                        break;
                    case 2:
                        senderDevice.Parameters.Mode = (int)(rawValue * sv.AssignedParameter.a + sv.AssignedParameter.b);
                        break;
                    case 3:
                        senderDevice.Parameters.WorkTime = (int)(rawValue * sv.AssignedParameter.a + sv.AssignedParameter.b);
                        break;
                    case 4:
                        senderDevice.Parameters.StageTime = (int)(rawValue * sv.AssignedParameter.a + sv.AssignedParameter.b);
                        break;
                    case 5:
                        senderDevice.Parameters.Voltage = rawValue * sv.AssignedParameter.a + sv.AssignedParameter.b;
                        break;
                    case 6:
                        senderDevice.Parameters.FlameSensor = (int)ImperialConverter(rawValue * sv.AssignedParameter.a + sv.AssignedParameter.b, sv.AssignedParameter.UnitT);
                        break;
                    case 7:
                        senderDevice.Parameters.BodyTemp = (int)ImperialConverter(rawValue * sv.AssignedParameter.a + sv.AssignedParameter.b, sv.AssignedParameter.UnitT);
                        break;
                    case 8:
                        senderDevice.Parameters.PanelTemp = (int)ImperialConverter(rawValue * sv.AssignedParameter.a + sv.AssignedParameter.b, sv.AssignedParameter.UnitT);
                        break;
                    case 10:
                        senderDevice.Parameters.InletTemp = (int)ImperialConverter(rawValue * sv.AssignedParameter.a + sv.AssignedParameter.b, sv.AssignedParameter.UnitT);
                        break;
                    case 15:
                        senderDevice.Parameters.RevSet = (int)(rawValue * sv.AssignedParameter.a + sv.AssignedParameter.b);
                        if (senderDevice is HeaterDeviceViewModel hd15 && !hd15.OverrideState.BlowerOverriden)
                            hd15.OverrideState.BlowerOverridenRevs = senderDevice.Parameters.RevSet;
                        break;
                    case 16:
                        senderDevice.Parameters.RevMeasured = (int)(rawValue * sv.AssignedParameter.a + sv.AssignedParameter.b);
                        break;
                    case 18:
                        senderDevice.Parameters.FuelPumpMeasured = (rawValue * sv.AssignedParameter.a + sv.AssignedParameter.b);
                        break;
                    case 21:
                        senderDevice.Parameters.GlowPlug = (int)(rawValue * sv.AssignedParameter.a + sv.AssignedParameter.b);
                        break;
                    case 24:
                        senderDevice.Parameters.Error = (int)(rawValue * sv.AssignedParameter.a + sv.AssignedParameter.b);
                        break;
                    case 40:
                        senderDevice.Parameters.LiquidTemp = (int)ImperialConverter(rawValue * sv.AssignedParameter.a + sv.AssignedParameter.b, sv.AssignedParameter.UnitT);
                        break;
                    case 41:
                        senderDevice.Parameters.OverheatTemp = (int)ImperialConverter(rawValue * sv.AssignedParameter.a + sv.AssignedParameter.b, sv.AssignedParameter.UnitT);
                        break;
                    case 59:
                        senderDevice.Parameters.McuTemp = (int)(rawValue * sv.AssignedParameter.a + sv.AssignedParameter.b);
                        break;
                    case 60:
                        senderDevice.Parameters.Pressure = (float)ImperialConverter(rawValue * sv.AssignedParameter.a + sv.AssignedParameter.b, sv.AssignedParameter.UnitT);
                        if (senderDevice is PressureSensorDeviceViewModel ps60 && ps60.PressureLogWriting)
                            ps60.PressureLog[ps60.PressureLogPointer++] = senderDevice.Parameters.Pressure;
                        break;
                    case 131:
                        senderDevice.Parameters.ExPressure = (float)ImperialConverter(rawValue * sv.AssignedParameter.a + sv.AssignedParameter.b, sv.AssignedParameter.UnitT);
                        break;
                    case 132:
                        senderDevice.Parameters.SetPowerLevel = (int)ImperialConverter(rawValue * sv.AssignedParameter.a + sv.AssignedParameter.b, sv.AssignedParameter.UnitT);
                        break;
                    case 134:
                        senderDevice.ACInverterParams.CompressorRevsSet = (int)(rawValue * sv.AssignedParameter.a + sv.AssignedParameter.b);
                        break;
                    case 135:
                        senderDevice.ACInverterParams.CompressorRevsMeasured = (int)(rawValue * sv.AssignedParameter.a + sv.AssignedParameter.b);
                        break;
                    case 136:
                        senderDevice.ACInverterParams.CondensorPwmSet = (int)(rawValue * sv.AssignedParameter.a + sv.AssignedParameter.b);
                        break;
                    case 138:
                        senderDevice.ACInverterParams.CompressorCurrent = (int)(rawValue * sv.AssignedParameter.a + sv.AssignedParameter.b);
                        break;
                    case 139:
                        senderDevice.ACInverterParams.CondensorCurrent = (int)(rawValue * sv.AssignedParameter.a + sv.AssignedParameter.b);
                        break;
                    case 145:
                        senderDevice.Parameters.PcbTemp = (int)(rawValue * sv.AssignedParameter.a + sv.AssignedParameter.b);
                        break;
                }
            }

        switch (m.Pgn)
        {
            case 2://Command ack
                switch (m.Data[1])
                {
                    case 0:
                        senderDevice.Firmware[0] = m.Data[2];
                        senderDevice.Firmware[1] = m.Data[3];
                        senderDevice.Firmware[2] = m.Data[4];
                        senderDevice.Firmware[3] = m.Data[5];
                        break;
                    case 67:
                        senderDevice.ManualMode = m.Data[2] == 1;
                        break;
                }

                break;
            case 7: //Answer for param request
                {
                    if (m.Data[0] == 4) // Processing only successful answers
                    {
                        var parameterId = m.Data[3] + m.Data[2] * 256;
                        var parameterValue = ((uint)m.Data[4] * 0x1000000) + ((uint)m.Data[5] * 0x10000) + ((uint)m.Data[6] * 0x100) + m.Data[7];
                        if (parameterValue != 0xFFFFFFFF)
                        {
                            senderDevice.ReadParameters.TryToAdd(new ReadedParameter { Id = parameterId, Value = parameterValue });
                            Debug.WriteLine($"{GetString($"par_{parameterId}")}={parameterValue}");
                        }
                        else
                        if (GotResource($"par_{parameterId}"))
                            Debug.WriteLine($"{GetString($"par_{parameterId}")} not supported");

                        switch (parameterId)//Serial num in separate var
                        {
                            case 12: senderDevice.Serial[0] = (int)parameterValue; break;
                            case 13: senderDevice.Serial[1] = (int)parameterValue; break;
                            case 14: senderDevice.Serial[2] = (int)parameterValue; break;
                        }

                        senderDevice.flagGetParamDone = true;
                    }

                    if (m.Data[0] == 5) // Parameter not supported
                        senderDevice.flagGetParamDone = true;

                    break;
                }
            case 8: //Black box 
                {
                    if (m.Data[0] == 4) // Processing only successful answers
                    {
                        if (!ReadingBbErrorsMode)
                        {
                            var parameterId = m.Data[3] + m.Data[2] * 256;
                            var parameterValue = ((uint)m.Data[4] * 0x1000000) + ((uint)m.Data[5] * 0x10000) + ((uint)m.Data[6] * 0x100) + m.Data[7];
                            if (parameterValue != 0xFFFFFFFF)
                                senderDevice.BbValues.TryToAdd(new ReadedBlackBoxValue() { Id = parameterId, Value = parameterValue });

                        }
                        else
                        {
                            if (m.Data[2] == 0xFF && m.Data[3] == 0xFA) //Report header
                                senderDevice.BbErrors.AddNew();
                            else
                            {
                                BbCommonVariable v = new()
                                {
                                    Id = m.Data[2] * 256 + m.Data[3],
                                    Value = m.Data[4] * 0x1000000 + m.Data[5] * 0x10000 + m.Data[6] * 0x100 + m.Data[7]
                                };
                                if (v.Id != 65535 && senderDevice.BbErrors.Count > 0)
                                    senderDevice.BbErrors.Last().Variables.TryToAdd(v);
                            }
                        }
                        senderDevice.flagGetBbDone = true;
                    }

                    break;
                }
            case 18: //Version
                {
                    if (m.Data[0] != 123)
                    {
                        senderDevice.Firmware[0] = m.Data[0];
                        senderDevice.Firmware[1] = m.Data[1];
                        senderDevice.Firmware[2] = m.Data[2];
                        senderDevice.Firmware[3] = m.Data[3];
                    }
                    else
                    {
                        senderDevice.BootFirmware[0] = m.Data[0];
                        senderDevice.BootFirmware[1] = m.Data[1];
                        senderDevice.BootFirmware[2] = m.Data[2];
                        senderDevice.BootFirmware[3] = m.Data[3];
                    }
                    if (m.Data[5] != 0xff && m.Data[6] != 0xff && m.Data[7] != 0xff)
                        try
                        {
                            senderDevice.ProductionDate = new DateOnly(m.Data[7] + 2000, m.Data[6], m.Data[5]);
                        }
                        catch {/*ignored*/}

                    break;
                }

            case 19:
                {
                    if (m.Data[0] == 4)
                    {
                        if (m.Data[1] < 4) senderDevice.TimberlineParams.Zones[0].Connected = (zoneType_t)m.Data[1];
                        if (m.Data[2] < 4) senderDevice.TimberlineParams.Zones[1].Connected = (zoneType_t)m.Data[2];
                        if (m.Data[3] < 4) senderDevice.TimberlineParams.Zones[2].Connected = (zoneType_t)m.Data[3];
                        if (m.Data[4] < 4) senderDevice.TimberlineParams.Zones[3].Connected = (zoneType_t)m.Data[4];
                        if (m.Data[5] < 4) senderDevice.TimberlineParams.Zones[4].Connected = (zoneType_t)m.Data[5];
                    }

                    break;
                }
            case 21:
                {
                    if (m.Data[2] != 255) senderDevice.TimberlineParams.TankTemperature = m.Data[2] - 75;
                    if (m.Data[4] != 255) senderDevice.TimberlineParams.OutsideTemperature = m.Data[4] - 75;
                    if (m.Data[6] != 255) senderDevice.TimberlineParams.LiquidLevel = m.Data[6];
                    if ((m.Data[7] & 3) != 3) senderDevice.TimberlineParams.DomesticWaterFlow = (m.Data[7] & 3) != 0;
                    break;
                }
            case 22:
                {
                    if ((m.Data[0] & 3) != 3)
                    {
                        if ((m.Data[0] & 3) == 0) senderDevice.TimberlineParams.Zones[0].State = zoneState_t.Off;
                        if ((m.Data[0] & 3) == 1) senderDevice.TimberlineParams.Zones[0].State = zoneState_t.Heat;
                        if ((m.Data[0] & 3) == 2) senderDevice.TimberlineParams.Zones[0].State = zoneState_t.Fan;
                    }

                    if (((m.Data[0] >> 2) & 3) != 3)
                    {
                        if (((m.Data[0] >> 2) & 3) == 0) senderDevice.TimberlineParams.Zones[1].State = zoneState_t.Off;
                        if (((m.Data[0] >> 2) & 3) == 1) senderDevice.TimberlineParams.Zones[1].State = zoneState_t.Heat;
                        if (((m.Data[0] >> 2) & 3) == 2) senderDevice.TimberlineParams.Zones[1].State = zoneState_t.Fan;
                    }

                    if (((m.Data[0] >> 4) & 3) != 3)
                    {
                        if (((m.Data[0] >> 4) & 3) == 0) senderDevice.TimberlineParams.Zones[2].State = zoneState_t.Off;
                        if (((m.Data[0] >> 4) & 3) == 1) senderDevice.TimberlineParams.Zones[2].State = zoneState_t.Heat;
                        if (((m.Data[0] >> 4) & 3) == 2) senderDevice.TimberlineParams.Zones[2].State = zoneState_t.Fan;
                    }
                    if (((m.Data[0] >> 6) & 3) != 3)
                    {
                        if (((m.Data[0] >> 6) & 3) == 0) senderDevice.TimberlineParams.Zones[3].State = zoneState_t.Off;
                        if (((m.Data[0] >> 6) & 3) == 1) senderDevice.TimberlineParams.Zones[3].State = zoneState_t.Heat;
                        if (((m.Data[0] >> 6) & 3) == 2) senderDevice.TimberlineParams.Zones[3].State = zoneState_t.Fan;
                    }
                    if ((m.Data[1] & 3) != 3)
                    {
                        if ((m.Data[1] & 3) == 0) senderDevice.TimberlineParams.Zones[4].State = zoneState_t.Off;
                        if ((m.Data[1] & 3) == 1) senderDevice.TimberlineParams.Zones[4].State = zoneState_t.Heat;
                        if ((m.Data[1] & 3) == 2) senderDevice.TimberlineParams.Zones[4].State = zoneState_t.Fan;
                    }

                    if (m.Data[2] != 255) senderDevice.TimberlineParams.Zones[0].CurrentTemperature = m.Data[2] - 75;
                    if (m.Data[3] != 255) senderDevice.TimberlineParams.Zones[1].CurrentTemperature = m.Data[3] - 75;
                    if (m.Data[4] != 255) senderDevice.TimberlineParams.Zones[2].CurrentTemperature = m.Data[4] - 75;
                    if (m.Data[5] != 255) senderDevice.TimberlineParams.Zones[3].CurrentTemperature = m.Data[5] - 75;
                    if (m.Data[6] != 255) senderDevice.TimberlineParams.Zones[4].CurrentTemperature = m.Data[6] - 75;

                    if ((m.Data[7] & 3) != 3) senderDevice.TimberlineParams.HeaterEnabled = (m.Data[7] & 3) != 0;
                    if (((m.Data[7] >> 2) & 3) != 3) senderDevice.TimberlineParams.ElementEnabled = ((m.Data[7] >> 2) & 3) != 0;
                    break;
                }
            case 23:
                {
                    if (m.Data[0] != 255) senderDevice.TimberlineParams.Zones[0].SetPwmPercent = m.Data[0];
                    if (m.Data[1] != 255) senderDevice.TimberlineParams.Zones[1].SetPwmPercent = m.Data[1];
                    if (m.Data[2] != 255) senderDevice.TimberlineParams.Zones[2].SetPwmPercent = m.Data[2];
                    if (m.Data[3] != 255) senderDevice.TimberlineParams.Zones[3].SetPwmPercent = m.Data[3];
                    if (m.Data[4] != 255) senderDevice.TimberlineParams.Zones[4].SetPwmPercent = m.Data[4];
                    break;
                }
            case 24:
                {
                    if ((m.Data[0] & 15) != 15) senderDevice.TimberlineParams.Zones[0].FanStage = m.Data[0] & 15;
                    if (((m.Data[0] >> 4) & 15) != 15) senderDevice.TimberlineParams.Zones[1].FanStage = (m.Data[0] >> 4) & 15;
                    if ((m.Data[1] & 15) != 15) senderDevice.TimberlineParams.Zones[2].FanStage = m.Data[1] & 15;
                    if (((m.Data[1] >> 4) & 15) != 15) senderDevice.TimberlineParams.Zones[3].FanStage = (m.Data[0] >> 4) & 15;
                    if ((m.Data[2] & 15) != 15) senderDevice.TimberlineParams.Zones[4].FanStage = m.Data[2] & 15;
                    if (m.Data[3] != 255) senderDevice.TimberlineParams.Zones[0].CurrentPwm = m.Data[3];
                    if (m.Data[4] != 255) senderDevice.TimberlineParams.Zones[1].CurrentPwm = m.Data[4];
                    if (m.Data[5] != 255) senderDevice.TimberlineParams.Zones[2].CurrentPwm = m.Data[5];
                    if (m.Data[6] != 255) senderDevice.TimberlineParams.Zones[3].CurrentPwm = m.Data[6];
                    if (m.Data[7] != 255) senderDevice.TimberlineParams.Zones[4].CurrentPwm = m.Data[7];
                    break;
                }
            case 25:
                {
                    if (m.Data[0] != 255) senderDevice.TimberlineParams.Zones[0].TempSetPointDay = m.Data[0] - 75;
                    if (m.Data[1] != 255) senderDevice.TimberlineParams.Zones[1].TempSetPointDay = m.Data[1] - 75;
                    if (m.Data[2] != 255) senderDevice.TimberlineParams.Zones[2].TempSetPointDay = m.Data[2] - 75;
                    if (m.Data[3] != 255) senderDevice.TimberlineParams.Zones[3].TempSetPointDay = m.Data[3] - 75;
                    if (m.Data[4] != 255) senderDevice.TimberlineParams.Zones[4].TempSetPointDay = m.Data[4] - 75;
                    break;
                }
            case 26:
                {
                    if (m.Data[0] != 255) senderDevice.TimberlineParams.Zones[0].TempSetPointNight = m.Data[0] - 75;
                    if (m.Data[1] != 255) senderDevice.TimberlineParams.Zones[1].TempSetPointNight = m.Data[1] - 75;
                    if (m.Data[2] != 255) senderDevice.TimberlineParams.Zones[2].TempSetPointNight = m.Data[2] - 75;
                    if (m.Data[3] != 255) senderDevice.TimberlineParams.Zones[3].TempSetPointNight = m.Data[3] - 75;
                    if (m.Data[4] != 255) senderDevice.TimberlineParams.Zones[4].TempSetPointNight = m.Data[4] - 75;
                    break;
                }
            case 27:
                {
                    if (m.Data[0] != 255) senderDevice.TimberlineParams.Zones[0].ManualPercent = m.Data[0];
                    if (m.Data[1] != 255) senderDevice.TimberlineParams.Zones[1].ManualPercent = m.Data[1];
                    if (m.Data[2] != 255) senderDevice.TimberlineParams.Zones[2].ManualPercent = m.Data[2];
                    if (m.Data[3] != 255) senderDevice.TimberlineParams.Zones[3].ManualPercent = m.Data[3];
                    if (m.Data[4] != 255) senderDevice.TimberlineParams.Zones[4].ManualPercent = m.Data[4];
                    if ((m.Data[5] & 3) != 3) senderDevice.TimberlineParams.Zones[0].ManualMode = (m.Data[5] & 3) != 0;
                    if (((m.Data[5] >> 2) & 3) != 3) senderDevice.TimberlineParams.Zones[1].ManualMode = ((m.Data[5] >> 2) & 3) != 0;
                    if (((m.Data[5] >> 4) & 3) != 3) senderDevice.TimberlineParams.Zones[2].ManualMode = ((m.Data[5] >> 4) & 3) != 0;
                    if (((m.Data[5] >> 6) & 3) != 3) senderDevice.TimberlineParams.Zones[3].ManualMode = ((m.Data[5] >> 6) & 3) != 0;
                    if ((m.Data[6] & 3) != 3) senderDevice.TimberlineParams.Zones[4].ManualMode = (m.Data[6] & 3) != 0;
                    break;
                }
            case 33:
                switch (m.Data[0])
                {
                    case 1:
                        senderDevice.Serial[0] = (int)(m.Data[1] * 0x1000000 + m.Data[2] * 0x10000 + m.Data[3] * 0x100 + m.Data[4]);
                        break;
                    case 2:
                        senderDevice.Serial[1] = (int)(m.Data[1] * 0x1000000 + m.Data[2] * 0x10000 + m.Data[3] * 0x100 + m.Data[4]);
                        break;
                    case 3:
                        senderDevice.Serial[2] = (int)(m.Data[1] * 0x1000000 + m.Data[2] * 0x10000 + m.Data[3] * 0x100 + m.Data[4]);
                        break;
                    default:
                        break;
                }

                break;
            case 47:
                if ((m.Data[0] & 3) < 2) senderDevice.OverrideState.FuelPumpOverriden = (m.Data[0] & 3) > 0;
                if (((m.Data[0] >> 2) & 3) < 2) senderDevice.OverrideState.RelayOverriden = ((m.Data[0] >> 2) & 3) > 0;
                if (((m.Data[0] >> 4) & 3) < 2) senderDevice.OverrideState.GlowPlugOverriden = ((m.Data[0] >> 4) & 3) > 0;
                if (((m.Data[0] >> 6) & 3) < 2) senderDevice.OverrideState.PumpOverriden = ((m.Data[0] >> 6) & 3) > 0;
                if ((m.Data[1] & 3) < 2) senderDevice.OverrideState.BlowerOverriden = (m.Data[1] & 3) > 0;

                if ((m.Data[2] & 3) < 2 && senderDevice.OverrideState.PumpOverriden) senderDevice.OverrideState.PumpOverridenState = (m.Data[1] & 3) > 0;
                if (((m.Data[2] >> 2) & 3) < 2 && senderDevice.OverrideState.RelayOverriden) senderDevice.OverrideState.RelayOverridenState = ((m.Data[2] >> 2) & 3) > 0;
                if (m.Data[3] != 255 && senderDevice.OverrideState.BlowerOverriden) senderDevice.OverrideState.BlowerOverridenRevs = m.Data[3];
                if (m.Data[4] != 255 && senderDevice.OverrideState.GlowPlugOverriden) senderDevice.OverrideState.GlowPlugOverridenPower = m.Data[4];
                if ((m.Data[5] != 255 || m.Data[6] != 255) && senderDevice.OverrideState.FuelPumpOverriden) senderDevice.OverrideState.FuelPumpOverridenFrequencyX100 = m.Data[5] * 256 + m.Data[6];
                break;


            case 49:
                if ((m.Data[0] & 3) < 3)
                    senderDevice.GenericLoadTripple.LoadMode1 = (LoadMode_t)(m.Data[0] & 3);
                if (((m.Data[0] >> 2) & 3) < 3)
                    senderDevice.GenericLoadTripple.LoadMode2 = (LoadMode_t)((m.Data[0] >> 2) & 3);
                if (((m.Data[0] >> 4) & 3) < 3)
                    senderDevice.GenericLoadTripple.LoadMode3 = (LoadMode_t)((m.Data[0] >> 4) & 3);

                if (m.Data[1] <= 100)
                    senderDevice.GenericLoadTripple.PwmLevel1 = m.Data[1];
                if (m.Data[2] <= 100)
                    senderDevice.GenericLoadTripple.PwmLevel2 = m.Data[2];
                if (m.Data[3] <= 100)
                    senderDevice.GenericLoadTripple.PwmLevel3 = m.Data[3];
                break;

            case 50:
                switch (m.Data[0])
                {
                    case 0:
                        if (m.Data[1] != 255)
                            senderDevice.ACInverterParams.CompressorRevsSet = m.Data[1];
                        if (m.Data[2] != 255)
                            senderDevice.ACInverterParams.CompressorRevsMeasured = m.Data[2];
                        break;

                }
                break;

            case 100:
                if (m.Data[0] == 1 && m.Data[1] == 1)
                {
                    senderDevice.flagEraseDone = true;
                }
                if (m.Data[0] == 2 && m.Data[1] == 1)
                {
                    senderDevice.flagSetAdrDone = true;
                }
                if (m.Data[0] == 3 && m.Data[1] == 1)
                {
                    senderDevice.flagProgramDone = true;
                }
                break;
            case 105:
                {
                    if (m.Data[0] == 1)
                    {
                        senderDevice.fragmentAddress = (uint)(m.Data[1] * 0x1000000 + m.Data[2] * 0x10000 + m.Data[3] * 0x100 + m.Data[4]);
                        Debug.WriteLine($"Adress set to 0X{senderDevice.fragmentAddress:X}");
                        senderDevice.flagSetAdrDone = true;
                    }

                    if (m.Data[0] == 3)
                    {
                        senderDevice.receivedFragmentLength = m.Data[1] * 0x10000 + m.Data[2] * 0x100 + m.Data[3];
                        senderDevice.receivedFragmentCrc = m.Data[4] * 0x1000000U + m.Data[5] * 0x10000U + m.Data[6] * 0x100U + m.Data[7];
                        Debug.WriteLine($"Data fragment len:{senderDevice.receivedFragmentLength},CRC:{senderDevice.receivedFragmentCrc:X}");
                        senderDevice.flagDataGetDone = true;
                    }

                    if (m.Data[0] == 5)
                    {
                        if (m.Data[1] == 0)
                        {
                            Debug.WriteLine("Flash fragment successed");
                            senderDevice.flagProgramDone = true;
                        }
                        else
                            Debug.WriteLine("Flash fragment failed");
                    }

                    if (m.Data[0] == 7)
                    {
                        if (m.Data[1] == 0)
                        {
                            Debug.WriteLine("Memory erase confirmed");
                            senderDevice.flagEraseDone = true;
                        }
                        else
                            Debug.WriteLine("Memory erase fail");
                    }

                    break;
                }
        }

        Messages.TryToAdd(m);

    }

    public void ProcessUartMessage(byte[] buf)
    {

    }

    public void ProcessMessage(UInt16 pgn, byte[] data)
    {

    }

    public async void ReadBlackBoxData(DeviceId id)
    {
        if (!Capture("t_reading_bb_parameters")) return;
        var currentDevice = ConnectedDevices.FirstOrDefault(d => d.Id == id);
        if (currentDevice == null) return;
        ReadingBbErrorsMode = false;
        OmniMessage msg = new()
        {
            Pgn = 8,
            TransmitterId = new(126, 6),
            ReceiverId = new(id.Type, id.Address),
            Data =
            {
                [0] = 6, //Read Single Param
                [1] = 0xFF //Read Param
            }
        };

        var counter = 0;
        var parameterCount = 64;
        for (var p = 0; p < parameterCount; p++)
        {
            msg.Data[4] = (byte)(p / 256);
            msg.Data[5] = (byte)(p % 256);
            currentDevice.flagGetBbDone = false;
            for (var t = 0; t < 7; t++)
            {
                SendMessage(msg);
                var success = false;
                for (var i = 0; i < 50; i++)
                {
                    if (currentDevice.flagGetBbDone)
                    {
                        success = true;
                        break;
                    }
                    await Task.Delay(1);
                    Debug.WriteLineIf(i == 49, $"Error reading parameter {p} ({GetString($"bb_{p}")}), attempt:{t}");
                }
                if (success) break;
                if (t == 6)
                    Fail("Can't read black box parameter");
            }

            if (CancellationRequested)
            {
                Cancel();
                return;
            }
            UpdatePercent(100 * counter++ / parameterCount);
        }
        Done();
    }

    public async void CheckPump(DeviceViewModel cd)
    {
        if (!Capture(GetString("b_1000_ticks"))) return;
        if (cd == null) return;

        OmniMessage msg = new()
        {
            TransmitterId = new(126, 6),
            ReceiverId = new(cd.Id.Type, cd.Id.Address),
            Pgn = 1,
            Data = new byte[8]

        };
        msg.Data[0] = 0;
        msg.Data[1] = 68;
        msg.Data[2] = 0;
        msg.Data[3] = 0;
        msg.Data[4] = 0;
        msg.Data[5] = 400 / 256;
        msg.Data[6] = 400 % 256;
        var startTime = DateTime.Now;
        SendMessage(msg);
        while (true)
        {
            UpdatePercent((int)((DateTime.Now - startTime).TotalSeconds / 2.5));
            await Task.Delay(100);
            if (CancellationRequested || (DateTime.Now - startTime).TotalSeconds > 250)
                break;
        }
        msg.Data[5] = 0;
        msg.Data[6] = 0;
        SendMessage(msg);
        if (CancellationRequested)
            Cancel();
        else
            Done();
    }

    public async void ReadErrorsBlackBox(DeviceId id)
    {
        if (!Capture("t_reading_b_errors")) return;
        ReadingBbErrorsMode = true;

        var currentDevice = ConnectedDevices.FirstOrDefault(i => i.Id.Equals(id));

        uiContext.Send(x =>
        {
            if (currentDevice == null) return;
            currentDevice.BbErrors.Clear();
        }, null);

        var msg = new OmniMessage
        {
            Pgn = 8,
            ReceiverId = new(id.Type, id.Address),
            Data =
            {
                [0] = 0x13, //Read Errors
                [1] = 0xFF
            }
        };
        if (currentDevice != null)
            for (var i = 0; i < currentDevice.DeviceReference.BBErrorsLen; i++)
            {
                msg.Data[4] = (byte)(i / 256); //Pair count
                msg.Data[5] = (byte)(i % 256); //Pair count
                msg.Data[6] = 0x00; //Pair count MSB
                msg.Data[7] = 0x01; //Pair count LSB

                currentDevice.flagGetBbDone = false;

                for (var t = 0; t < 7; t++)
                {
                    SendMessage(msg);
                    var success = false;
                    for (var j = 0; j < 50; j++)
                    {
                        if (currentDevice.flagGetBbDone)
                        {
                            success = true;
                            break;
                        }

                        await Task.Delay(1);
                        Debug.WriteLineIf(j == 49, $"Error reading BB address {i},attempt:{t + 1}");
                    }

                    if (success) break;
                    if (t == 6)
                        Fail("Can't read black box error");
                }

                if (CancellationRequested)
                {
                    Cancel();
                    return;
                }

                UpdatePercent(100 * i / 512);
            }

        Done();
    }

    public void EraseCommonBlackBox(DeviceId id)
    {
        if (!Capture("t_bb_common_erasing")) return;

        var msg = new OmniMessage
        {
            Pgn = 8,
            ReceiverId = new(id.Type, id.Address),
            Data =
            {
                [0] = 0x0, //Erase Common
                [1] = 0xFF,
                [4] = 0xFF,
                [5] = 0xFF,
                [6] = 0xFF,
                [7] = 0xFF
            }
        };
        SendMessage(msg);

        Done();
    }

    public void EraseErrorsBlackBox(DeviceId id)
    {
        if (!Capture("t_bb_errors_erasing")) return;

        var msg = new OmniMessage();
        msg.Pgn = 8;
        msg.TransmitterId.Address = 6;
        msg.TransmitterId.Type = 126;
        msg.ReceiverId.Address = id.Address;
        msg.ReceiverId.Type = id.Type;
        msg.Data[0] = 0x10; //Erase Errors
        msg.Data[1] = 0xFF;
        msg.Data[4] = 0xFF;
        msg.Data[5] = 0xFF;
        msg.Data[6] = 0xFF;
        msg.Data[7] = 0xFF;
        SendMessage(msg);

        Done();
    }
    public async void ReadAllParameters(DeviceId id)
    {
        if (!Capture("t_reading_flash_parameters")) return;
        var cnt = 0;

        var currentDevice = ConnectedDevices.FirstOrDefault(i => i.Id.Equals(id));

        for (var parId = 0; parId < 700; parId++) //Currently we have 600 parameters, 100 just in case
        {
            //if (!GotResource($"par_{parId}")) // Requesting even if we know nothing about it
            //    continue;
            OmniMessage msg = new();
            msg.Pgn = 7;
            msg.TransmitterId.Address = 6;
            msg.TransmitterId.Type = 126;
            msg.ReceiverId.Address = id.Address;
            msg.ReceiverId.Type = id.Type;
            msg.Data[0] = 3; //Read Param
            msg.Data[1] = 0xFF; //Read Param
            msg.Data[2] = (byte)(parId / 256);
            msg.Data[3] = (byte)(parId % 256);

            currentDevice.flagGetParamDone = false;


            for (var t = 0; t < 7; t++)
            {
                SendMessage(msg);
                var success = false;

                for (var j = 0; j < 50; j++)
                {
                    if (currentDevice.flagGetParamDone)
                    {
                        success = true;
                        break;
                    }
                    await Task.Delay(1);
                    Debug.WriteLineIf(j == 49, $"Error reading parameter {parId}, attempt {t + 1})");
                }
                if (success) break;
                if (t == 6)
                    Fail("Can't read parameter");
            }
            UpdatePercent(cnt++ * 100 / 601);
            if (CancellationRequested)
            {
                Cancel();
                return;
            }
        }
        Done();
    }

    public async void RequestSerial(DeviceId id)
    {
        for (byte i = 1; i < 4; i++)
        {
            RequestPgn(id, 33, i);
            await Task.Delay(100);
        }
        RequestPgn(id, 18);
    }

    public async void SaveParameters(DeviceId id)
    {
        if (!Capture("t_saving_params_to_flash")) return;
        var dev = ConnectedDevices.FirstOrDefault(d => d.Id.Equals(id));
        if (dev == null) return;
        var msg = new OmniMessage();
        var tempCollection = new List<ReadedParameter>();
        foreach (var p in dev.ReadParameters)
            tempCollection.Add(p);
        var cnt = 0;
        foreach (var p in tempCollection)
        {
            msg.Pgn = 7;
            msg.TransmitterId.Address = 6;
            msg.TransmitterId.Type = 126;
            msg.ReceiverId.Address = id.Address;
            msg.ReceiverId.Type = id.Type;
            msg.Data[0] = 1; //Write Param to raw
            msg.Data[1] = 0xFF;
            msg.Data[2] = (byte)(p.Id / 256);
            msg.Data[3] = (byte)(p.Id % 256);
            msg.Data[4] = (byte)((p.Value >> 24) & 0xFF);
            msg.Data[5] = (byte)((p.Value >> 16) & 0xFF);
            msg.Data[6] = (byte)((p.Value >> 8) & 0xFF);
            msg.Data[7] = (byte)((p.Value) & 0xFF);
            SendMessage(msg);
            await Task.Run(() => Thread.Sleep(100));
            UpdatePercent(cnt++ * 100 / tempCollection.Count);
            if (CancellationRequested)
            {
                Cancel();
                return;
            }
        }
        await Task.Run(() => Thread.Sleep(100));
        msg.Data[0] = 2;
        SendMessage(msg);
        Done();
    }

    public void ResetParameters(DeviceId id)
    {
        if (!Capture("t_config_erase")) return;
        var dev = ConnectedDevices.FirstOrDefault(d => d.Id.Equals(id));
        if (dev == null) return;
        var msg = new OmniMessage();

        msg.Pgn = 7;
        msg.ReceiverId.Address = id.Address;
        msg.ReceiverId.Type = id.Type;
        msg.Data[0] = 0; //Erase 
        msg.Data[1] = 0xFF;
        SendMessage(msg);
        Done();
    }

    public void SendMessage(OmniMessage m)
    {
        if ((bool)canAdapter?.PortOpened)
            canAdapter.Transmit(m.ToCanMessage());
        // uartAdapter removed - CAN only
    }

    public void SendMessage(DeviceId from, DeviceId to, int pgn, byte[] data)
    {
        var msg = new OmniMessage();
        msg.Pgn = pgn;
        msg.TransmitterId.Address = from.Address;
        msg.TransmitterId.Type = from.Type;
        msg.ReceiverId.Address = to.Address;
        msg.ReceiverId.Type = to.Type;
        data.CopyTo(msg.Data, 0);
        SendMessage(msg);
    }
    public void SendCommand(int com, DeviceId dev, byte[] data = null)
    {
        var message = new OmniMessage();
        message.Pgn = 1;
        message.TransmitterId.Address = 6;
        message.TransmitterId.Type = 126;
        message.ReceiverId.Address = dev.Address;
        message.ReceiverId.Type = dev.Type;
        message.Data[1] = (byte)com;
        for (var i = 0; i < 6; i++)
        {
            if (data != null)
                message.Data[i + 2] = data[i];
            else
                message.Data[i + 2] = 0xFF;
        }
        SendMessage(message);
    }

    public void RequestPgn(DeviceId device, int pgn, byte? multiPack = null)
    {
        var message = new OmniMessage();
        message.Pgn = 6;
        message.TransmitterId.Address = 6;
        message.TransmitterId.Type = 126;
        message.ReceiverId.Address = device.Address;
        message.ReceiverId.Type = device.Type;
        message.Data[0] = (byte)(pgn >> 8);
        message.Data[1] = (byte)(pgn & 0xFF);
        if (multiPack != null)
            message.Data[2] = (byte)multiPack;
        else
            message.Data[2] = 255;
        SendMessage(message);
    }



}
}