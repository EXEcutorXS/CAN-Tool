using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using System.Windows;
using CAN_Tool;
using CAN_Tool.Libs;
using CAN_Tool.ViewModels;
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

        // Периодический опрос "кто в загрузчике" (PGN6/18 на ReceiverId.Type=123) - чтобы
        // только что перешедшее в загрузчик устройство появилось в ConnectedDevices, не
        // дожидаясь, пока оно само о себе объявит. Раньше жил в конструкторе
        // FirmwarePageViewModel (создавался один раз на всё приложение) - перенесено сюда,
        // т.к. отдельной вкладки-синглтона для загрузчика больше нет.
        var whoIsHereTimer = new System.Timers.Timer(1000);
        whoIsHereTimer.Elapsed += SendWhoIsHere;
        whoIsHereTimer.Start();
    }

    private void SendWhoIsHere(object sender, System.Timers.ElapsedEventArgs e)
    {
        OmniMessage msg = new();
        msg.Pgn = 6;
        msg.ReceiverId.Type = 123;
        msg.Data[0] = 0;
        msg.Data[1] = 18;
        canAdapter.Transmit(msg.ToCanMessage());
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

    // Строки, собираемые из протокола PGN61/62 (см. DecodeStringTransferAnnounce/
    // DecodeStringTransferData ниже) - одна запись на пару (отправитель, StringId), живёт,
    // пока не очищена вручную (см. ClearStringTransfers), обновляется по мере прихода новых
    // пакетов PGN62. Ключ - не публичный API, только для поиска существующей записи.
    public ObservableCollection<StringTransferEntry> StringTransfers { get; } = new();
    private readonly Dictionary<(int type, int address, int stringId), StringTransferEntry> stringTransferIndex = new();

    [RelayCommand]
    void ClearStringTransfers()
    {
        StringTransfers.Clear();
        stringTransferIndex.Clear();
    }

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
                    // Данные зон Timberline - шлёт только MBC-2, реальный пульт этот PGN не
                    // отправляет никогда. Ловит в т.ч. старые MBC-2, которые ещё репортуют
                    // версию 126.x.x.x (тип "зашит" в неё жёстко и до разделения на 125/126
                    // совпадал с пультом) - на шине сейчас может быть сразу и настоящий пульт, и
                    // такой MBC-2 под одним и тем же Id.Type. senderDevice переприсваивается,
                    // т.к. подтверждение личности заменяет весь объект в ConnectedDevices
                    // (PanelDeviceViewModel -> HcuDeviceViewModel, см. ConfirmMbc2Identity ниже) -
                    // остальная обработка этого сообщения должна идти уже в новый объект.
                    senderDevice = ConfirmMbc2Identity(senderDevice);

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

            case 60:
                {
                    var mp = senderDevice.ModemParams;
                    switch (m.Data[0])
                    {
                        case 0: // регистрация/роуминг/интернет/mqtt + CSQ + тип сети, каждые 5 сек
                            if ((m.Data[1] & 3) < 2) mp.Registered = (m.Data[1] & 1) != 0;
                            if (((m.Data[1] >> 2) & 3) < 2) mp.Roaming = ((m.Data[1] >> 2) & 1) != 0;
                            if (((m.Data[1] >> 4) & 3) < 2) mp.InternetConnected = ((m.Data[1] >> 4) & 1) != 0;
                            if (((m.Data[1] >> 6) & 3) < 2) mp.MqttConnected = ((m.Data[1] >> 6) & 1) != 0;
                            mp.Csq = m.Data[2] == 0xFF ? -1 : m.Data[2];
                            mp.NetworkAcT = m.Data[3] == 0xFF ? -1 : m.Data[3];
                            break;
                        case 1: // флаги настроек, раз в 30-60 сек
                            if ((m.Data[1] & 3) < 2) mp.OnlySmsMode = (m.Data[1] & 1) != 0;
                            if (((m.Data[1] >> 2) & 3) < 2) mp.FaultReport = ((m.Data[1] >> 2) & 1) != 0;
                            if (((m.Data[1] >> 4) & 3) < 2) mp.CmdAck = ((m.Data[1] >> 4) & 1) != 0;
                            if (((m.Data[1] >> 6) & 3) < 2) mp.TempUnitF = ((m.Data[1] >> 6) & 1) != 0;
                            // Force2gOnly (D[2]) / AllowRoaming (D[3]) - плоские байты вне
                            // 2-битной схемы D[1] (там больше нет свободных пар бит), см.
                            // DataActualizator::sendSettings()/ModemSettings::SendByteSetting:
                            // 0xFF значит "без изменений".
                            if (m.Data[2] != 0xFF) mp.Force2gOnly = m.Data[2] != 0;
                            if (m.Data[3] != 0xFF) mp.AllowRoaming = m.Data[3] != 0;
                            break;
                        case 4: // статус авторегистрации (см. Modem::AutoRegisterStatus), по изменению
                            mp.AutoRegStatus = m.Data[1];
                            break;
                        case 2: // код оператора, раз в 30-60 сек
                            mp.OperatorCode = m.Data[1] == 0xFF
                                ? ""
                                : new string(new[] { (char)m.Data[1], (char)m.Data[2], (char)m.Data[3], (char)m.Data[4], (char)m.Data[5] });
                            break;
                        case 3: // LAC + Cell ID, раз в 30-60 сек
                            if (m.Data[1] == 0xFF && m.Data[2] == 0xFF)
                            {
                                mp.Lac = -1;
                                mp.CellId = -1;
                            }
                            else
                            {
                                mp.Lac = (m.Data[1] << 8) | m.Data[2];
                                mp.CellId = ((long)m.Data[3] << 24) | ((long)m.Data[4] << 16) | ((long)m.Data[5] << 8) | m.Data[6];
                            }
                            break;
                    }
                    break;
                }

            case 61: // Передача строк - управление (D[0]=1 анонс, D[0]=2 запрос - см. StringTransfer.h)
                DecodeStringTransferAnnounce(m);
                break;

            case 62: // Передача строк - пакет данных (5 байт на кадр)
                DecodeStringTransferData(senderDevice, m);
                break;

            case 100:
                {
                    var fw = senderDevice as BootloaderDeviceViewModel;
                    if (fw == null) break;
                    if (m.Data[0] == 1 && m.Data[1] == 1)
                        fw.flagEraseDone = true;
                    if (m.Data[0] == 2 && m.Data[1] == 1)
                        fw.flagSetAdrDone = true;
                    if (m.Data[0] == 3 && m.Data[1] == 1)
                        fw.flagProgramDone = true;
                    break;
                }
            case 105:
                DecodeFragmentProtocolResponse(senderDevice as BootloaderDeviceViewModel, m);
                break;
            case 110: //3-е поколение протокола прошивки (PGN110/111) - формат ответов идентичен 105
                DecodeFragmentProtocolResponse(senderDevice as BootloaderDeviceViewModel, m);
                // Субпакеты 14-17 (версии сохранённого ПО) шлёт ПУ28 и будучи в загрузчике
                // (BootloaderDeviceViewModel), и в обычном рабочем режиме как пульт
                // (PanelDeviceViewModel) - оба наследуются от общего Pu28DeviceViewModel.
                DecodePu28ImageVersions(senderDevice as Pu28DeviceViewModel, m);
                break;
            case 107: //External flash (memory dump) - обслуживается и загрузчиком, и пультом
                // в обычном режиме (оба - Pu28DeviceViewModel, см. Pu28DeviceViewModel.cs)
                {
                    var fw = senderDevice as Pu28DeviceViewModel;
                    if (fw == null) break;
                    if (m.Data[0] == 1)
                    {
                        fw.extFragmentAddress = (uint)(m.Data[1] * 0x1000000 + m.Data[2] * 0x10000 + m.Data[3] * 0x100 + m.Data[4]);
                        fw.flagExtSetAdrDone = true;
                    }

                    if (m.Data[0] == 3)
                    {
                        fw.extReceivedFragmentLength = m.Data[1] * 0x10000 + m.Data[2] * 0x100 + m.Data[3];
                        fw.extReceivedFragmentCrc = m.Data[4] * 0x1000000U + m.Data[5] * 0x10000U + m.Data[6] * 0x100U + m.Data[7];
                        fw.flagExtDataGetDone = true;
                    }

                    if (m.Data[0] == 5 && m.Data[1] == 0)
                        fw.flagExtProgramDone = true;

                    if (m.Data[0] == 7 && m.Data[1] == 0)
                        fw.flagExtEraseDone = true;

                    if (m.Data[0] == 15 && m.Data[1] == 0)
                        fw.flagExtEraseDone = true;

                    if (m.Data[0] == 17)
                    {
                        fw.extBulkReadLen = (uint)(m.Data[1] * 0x10000 + m.Data[2] * 0x100 + m.Data[3]);
                        fw.extBulkReadCrc = m.Data[4] * 0x1000000U + m.Data[5] * 0x10000U + m.Data[6] * 0x100U + m.Data[7];
                        fw.flagExtBulkReadDone = true;
                    }

                    break;
                }
            case 109: //Streamed raw data from external flash bulk read (PGN107 case16)
                {
                    var fw = senderDevice as Pu28DeviceViewModel;
                    fw?.AppendExtReadData(m.Data);
                    break;
                }
        }

        Messages.TryToAdd(m);

    }

    // Сейчас на шине может одновременно быть настоящий пульт (PanelDeviceViewModel, Id.Type==126)
    // и старый MBC-2, который тоже репортует версию 126.x.x.x (тип "зашит" в неё жёстко и до
    // разделения на 125/126 совпадал с пультом) - на разных адресах, оба неотличимы по одной
    // только версии. Единственный надёжный признак - PGN24 (данные зон Timberline), которые
    // шлёт исключительно MBC-2 (см. case 24 выше). У пульта и MBC-2 разные ViewModel-классы
    // (PanelDeviceViewModel/HcuDeviceViewModel) и, соответственно, разные View в OmniModeView.xaml
    // (Pu28MemoryControl/HcuOmniControl) - простой подменой отображаемого имени/картинки тут не
    // обойтись, нужно физически заменить объект в ConnectedDevices на HcuDeviceViewModel с тем
    // же Id, перенеся уже известные версии, чтобы не мигать "0.0.0.0" сразу после подмены.
    // Не-PanelDeviceViewModel (уже HcuDeviceViewModel, или что угодно ещё) пропускается без
    // изменений - дёшево звать на каждый PGN24.
    private const int Mbc2DeviceType = 125; // см. DeviceViewModel.VulnerableMbcDeviceType

    private DeviceViewModel ConfirmMbc2Identity(DeviceViewModel senderDevice)
    {
        if (senderDevice is not PanelDeviceViewModel panel) return senderDevice;

        var index = ConnectedDevices.IndexOf(panel);
        if (index < 0) return senderDevice; // уже убрано с шины (например, отключилось)

        var hcu = new HcuDeviceViewModel(new DeviceId(panel.Id.Type, panel.Id.Address))
        {
            Firmware = new BindingList<int>(panel.Firmware.ToList()),
            BootFirmware = new BindingList<int>(panel.BootFirmware.ToList()),
            Serial = new BindingList<int>(panel.Serial.ToList()),
            ProductionDate = panel.ProductionDate,
        };
        if (Devices.TryGetValue(Mbc2DeviceType, out var mbc2Template))
            hcu.DeviceReference = mbc2Template;

        ConnectedDevices[index] = hcu;
        if (ReferenceEquals(SelectedConnectedDevice, panel))
            SelectedConnectedDevice = hcu;

        return hcu;
    }

    // Общий разбор ответов протокола фрагментов прошивки - формат байт (тег/длина/CRC/статус)
    // одинаков у PGN105 и PGN110 (3-е поколение), меняется только сам PGN и то, каким
    // алгоритмом отправитель посчитал CRC (это уже решается на стороне BootloaderDeviceViewModel).
    private void DecodeFragmentProtocolResponse(BootloaderDeviceViewModel fw, OmniMessage m)
    {
        if (fw == null) return;
        if (m.Data[0] == 1)
        {
            fw.fragmentAddress = (uint)(m.Data[1] * 0x1000000 + m.Data[2] * 0x10000 + m.Data[3] * 0x100 + m.Data[4]);
            Debug.WriteLine($"Adress set to 0X{fw.fragmentAddress:X}");
            fw.flagSetAdrDone = true;
        }

        if (m.Data[0] == 3)
        {
            fw.receivedFragmentLength = m.Data[1] * 0x10000 + m.Data[2] * 0x100 + m.Data[3];
            fw.receivedFragmentCrc = m.Data[4] * 0x1000000U + m.Data[5] * 0x10000U + m.Data[6] * 0x100U + m.Data[7];
            Debug.WriteLine($"Data fragment len:{fw.receivedFragmentLength},CRC:{fw.receivedFragmentCrc:X}");
            fw.flagDataGetDone = true;
        }

        if (m.Data[0] == 5)
        {
            if (m.Data[1] == 0)
            {
                Debug.WriteLine("Flash fragment successed");
                fw.flagProgramDone = true;
            }
            else
                Debug.WriteLine("Flash fragment failed");
        }

        if (m.Data[0] == 7)
        {
            if (m.Data[1] == 0)
            {
                Debug.WriteLine("Memory erase confirmed");
                fw.flagEraseDone = true;
            }
            else
                Debug.WriteLine("Memory erase fail");
        }

        if (m.Data[0] == 9)
        {
            fw.readResultOk = m.Data[1] == 0;
            // D[2]=data>>24, D[3]=data>>16, D[4]=data>>8, D[5]=data&0xFF
            // где data = *(uint32_t*)adr - ARM little-endian, byte[addr] это LSB.
            // Собираем в том же порядке, чтобы получить исходное значение uint32.
            fw.readResultData = m.Data[2] * 0x1000000U + m.Data[3] * 0x10000U
                              + m.Data[4] * 0x100U + m.Data[5];
            Debug.WriteLine($"Read response: ok={fw.readResultOk}, data=0x{fw.readResultData:X08}");
            fw.flagReadDone = true;
        }

        if (m.Data[0] == 11)
        {
            fw.verifyResultOk = m.Data[1] == 0;
            fw.verifyResultCrc = m.Data[2] * 0x1000000U + m.Data[3] * 0x10000U
                               + m.Data[4] * 0x100U + m.Data[5];
            Debug.WriteLine($"Verify response: ok={fw.verifyResultOk}, CRC=0x{fw.verifyResultCrc:X08}");
            fw.flagVerifyDone = true;
        }
    }

    // Субпакеты 14-17 PGN110: периодическая трансляция версий ПО (собственный образ + 3
    // OTA-слота внешней flash-памяти) - шлёт и загрузчик ПУ28, и его основная программа
    // (пульт), см. Pu28DeviceViewModel. D[1..4] = версия в том же порядке, что у
    // Firmware/BootFirmware; D[5..7] - резерв.
    private void DecodePu28ImageVersions(Pu28DeviceViewModel target, OmniMessage m)
    {
        if (target == null) return;
        if (m.Data[0] is < 14 or > 17) return;

        // BindingList<int> не реализует INotifyCollectionChanged - привязка
        // "{Binding OwnImageVersion, Converter=...}" в Pu28MemoryControl.xaml обновляется
        // только когда меняется САМО свойство (генерируемый [ObservableProperty] сеттер шлёт
        // PropertyChanged), а не когда мутируют элементы уже существующего списка через
        // индексатор - поэтому присваиваем новый BindingList целиком, а не правим старый на
        // месте (иначе UI не перерисовывался до смены вкладки, которая пересоздаёт биндинг).
        var version = new BindingList<int> { m.Data[1], m.Data[2], m.Data[3], m.Data[4] };

        switch (m.Data[0])
        {
            case 14: target.OwnImageVersion = version; break;
            case 15: target.Slot0ImageVersion = version; break;
            case 16: target.Slot1ImageVersion = version; break;
            case 17: target.Slot2ImageVersion = version; break;
        }
    }

    // Находит существующую запись сборки строки по (отправитель, StringId) или создаёт новую -
    // отправитель для обоих субпакетов PGN61/62 это TransmitterId сообщения (тот, кто реально
    // передаёт байты строки: либо сразу пушит её - см. StringTransfer.h::sendString, либо
    // отвечает на чужой запрос), а не инициатор запроса.
    private StringTransferEntry GetOrCreateStringEntry(OmniMessage m, int stringId)
    {
        var key = (m.TransmitterId.Type, m.TransmitterId.Address, stringId);
        if (stringTransferIndex.TryGetValue(key, out var entry)) return entry;

        entry = new StringTransferEntry(
            new DeviceId(m.TransmitterId.Type, m.TransmitterId.Address),
            new DeviceId(m.ReceiverId.Type, m.ReceiverId.Address),
            stringId);
        stringTransferIndex[key] = entry;
        StringTransfers.Add(entry);
        return entry;
    }

    // PGN61 D[0]=1 "анонс" (см. StringTransfer.h): D[2-3]=StringId (LE), D[4-5]=длина в байтах
    // (LE), D[6]=кодировка. Заново обнуляет буфер сборки - анонс всегда предшествует свежей
    // передаче. D[0]=2 "запрос" не несёт данных строки - пропускаем, показывать нечего.
    private void DecodeStringTransferAnnounce(OmniMessage m)
    {
        if (m.Data[0] != 1) return;

        var stringId = m.Data[2] | (m.Data[3] << 8);
        var length = m.Data[4] | (m.Data[5] << 8);

        var entry = GetOrCreateStringEntry(m, stringId);
        entry.Encoding = (StringEncoding_t)m.Data[6];
        entry.DeclaredLength = length;
        entry.Buffer = new byte[length];
        entry.Text = "";
    }

    // PGN62 "данные" (см. StringTransfer.h): D[0-1]=StringId (LE), D[2]=номер пакета,
    // D[3-7]=5 байт данных (абсолютное смещение = номер_пакета*5+n). Отправитель (см.
    // StringTransfer.cpp::handler(), uint8_t d[5]={0xFF,...}) забивает байты последнего пакета
    // сверх реальной длины строки значением 0xFF - их нельзя копировать в буфер, иначе Text
    // получит от 0 до 4 лишних символов "?" в зависимости от остатка длины по модулю 5 (именно
    // так этот баг и проявлялся). Если анонс не был виден (подключились посреди передачи) -
    // длина неизвестна, буфер расширяется по факту пришедших пакетов как раньше.
    private void DecodeStringTransferData(DeviceViewModel senderDevice, OmniMessage m)
    {
        var stringId = m.Data[0] | (m.Data[1] << 8);
        var offset = m.Data[2] * 5;

        var entry = GetOrCreateStringEntry(m, stringId);
        if (entry.DeclaredLength >= 0)
        {
            var copyLen = Math.Clamp(entry.DeclaredLength - offset, 0, 5);
            if (copyLen > 0) Array.Copy(m.Data, 3, entry.Buffer, offset, copyLen);
        }
        else
        {
            if (entry.Buffer.Length < offset + 5)
                Array.Resize(ref entry.Buffer, offset + 5);
            Array.Copy(m.Data, 3, entry.Buffer, offset, 5);
        }

        entry.Text = DecodeStringBytes(entry.Buffer, entry.Encoding, entry.DeclaredLength);

        // Id 1-19 - общая таблица параметров модем<->пульт (см. StringId enum в
        // StringTransfer.h), осмысленна только когда её реально шлёт модем (Id.Type==121) -
        // раскладываем по конкретным полям ModemViewModel, чтобы страница модема показывала их
        // напрямую, а не только в общем окне "Строки".
        if (senderDevice.Id.Type == 121)
            ApplyModemString(senderDevice.ModemParams, stringId, entry.Text);
    }

    // См. StringId enum в C:\source\...\User\Can\StringTransfer.h - держать номера синхронно.
    // Только то, что реально показывают экраны ПУ28 (ModemInfo.cpp/ModemInternetInfo.cpp) - PIN
    // и телефоны (id 2-7) настраиваются по SMS, на этих экранах не отображаются, поэтому здесь
    // не разложены (видны как есть в общем окне "Строки").
    private static void ApplyModemString(ModemViewModel mp, int stringId, string text)
    {
        switch (stringId)
        {
            case 1: mp.Imei = text; break;
            case 8: mp.InternetCheckUrl = text; break;
            case 9: mp.MqttBroker = text; break;
            case 10: mp.MqttLogin = text; break;
            case 11: mp.MqttPassword = text; break;
            case 12: mp.LastSmsText = text; break;
            case 13: mp.LastSmsNum = text; break;
            case 16: mp.OperatorName = text; break;
            case 19: mp.ConnectionLink = text; break;
        }
    }

    // length>=0 (анонс был виден) - ровно столько байт реальные, остальное в data - паддинг
    // 0xFF последнего пакета (см. DecodeStringTransferData), декодировать не нужно. length<0
    // (анонс пропущен, точный размер неизвестен) - подстраховкой ищем нуль-терминатор, на
    // случай если он всё же есть в хвосте; для UTF-16 терминатор - пара нулевых байт по чётному
    // смещению (ASCII-символы в UTF-16LE сами содержат нулевой старший байт).
    private static string DecodeStringBytes(byte[] data, StringEncoding_t encoding, int length)
    {
        if (length < 0)
        {
            if (encoding == StringEncoding_t.Utf16)
            {
                length = data.Length;
                for (var i = 0; i + 1 < data.Length; i += 2)
                    if (data[i] == 0 && data[i + 1] == 0) { length = i; break; }
            }
            else
            {
                var zeroAt = Array.IndexOf(data, (byte)0);
                length = zeroAt >= 0 ? zeroAt : data.Length;
            }
        }
        length = Math.Min(length, data.Length);
        if (length <= 0) return "";

        try
        {
            return encoding switch
            {
                StringEncoding_t.Utf8 => Encoding.UTF8.GetString(data, 0, length),
                StringEncoding_t.Utf16 => Encoding.Unicode.GetString(data, 0, length),
                StringEncoding_t.Win1251 => Encoding.GetEncoding(1251).GetString(data, 0, length),
                _ => Encoding.ASCII.GetString(data, 0, length),
            };
        }
        catch
        {
            return BitConverter.ToString(data, 0, length);
        }
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

    // Параметр "Device's CAN address" (см. Resources/local.csv par_18) при записи меняет
    // адрес устройства на шине немедленно - если отправить его не последним, все следующие
    // за ним Write Param в этом же проходе уходят на уже устаревший id.Address, и устройство
    // их просто не видит (оно теперь слушает новый адрес). Поэтому в SaveParameters он всегда
    // отправляется в самом конце, после всех остальных настроек.
    private const int DeviceAddressParameterId = 18;

    public async void SaveParameters(DeviceId id)
    {
        if (!Capture("t_saving_params_to_flash")) return;
        var dev = ConnectedDevices.FirstOrDefault(d => d.Id.Equals(id));
        if (dev == null) return;
        var msg = new OmniMessage();
        var tempCollection = new List<ReadedParameter>();
        ReadedParameter addressParameter = null;
        foreach (var p in dev.ReadParameters)
        {
            if (p.Id == DeviceAddressParameterId) addressParameter = p;
            else tempCollection.Add(p);
        }
        if (addressParameter != null) tempCollection.Add(addressParameter);
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