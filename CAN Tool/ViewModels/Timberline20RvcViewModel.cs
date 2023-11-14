using CAN_Tool.ViewModels.Base;
using RVC;
using ScottPlot.Drawing.Colormaps;
using ScottPlot.MarkerShapes;
using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Diagnostics.Eventing.Reader;
using System.Linq;
using System.Security.Policy;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Interop;
using System.Windows.Threading;

namespace CAN_Tool.ViewModels
{
    public enum zoneState_t { Off, Heat, Fan }

    public enum heaterIcon { Idle, Blowing, Ignition, Lit }

    public class ZoneHandler : ViewModel
    {
        public ZoneHandler()
        {

        }

        public ZoneHandler(int zoneNumber)
        {
            this.zoneNumber = zoneNumber;
        }

        private readonly int zoneNumber;

        public int ZoneNumber => zoneNumber;
        private int tempSetpointDay = 22;
        public int TempSetpointDay { set => Set(ref tempSetpointDay, value); get => tempSetpointDay; }

        private int tempSetpointNight = 20;
        public int TempSetpointNight { set => Set(ref tempSetpointNight, value); get => tempSetpointNight; }

        private int tempSetpointCurrent = 20;
        public int TempSetpointCurrent { set => Set(ref tempSetpointCurrent, value); get => tempSetpointCurrent; }

        private int currentTemperature = 0;
        public int CurrentTemperature { set => Set(ref currentTemperature, value); get => currentTemperature; }

        private bool connected = false;
        public bool Connected { set => Set(ref connected, value); get => connected; }

        private zoneState_t state = zoneState_t.Off;
        public zoneState_t State { set => Set(ref state, value); get => state; }

        private bool manualMode = false;
        public bool ManualMode { set => Set(ref manualMode, value); get => manualMode; }

        private int manualPercent = 40;
        public int ManualPercent { set => Set(ref manualPercent, value); get => manualPercent; }

        private int currentPwm = 50;
        public int CurrentPwm { set => Set(ref currentPwm, value); get => currentPwm; }

        private bool broadcastTemperature;
        public bool BroadcastTemperature { set => Set(ref broadcastTemperature, value); get => broadcastTemperature; }

        private bool selected;
        public bool Selected { set => Set(ref selected, value); get => selected; }

        private int rvcTemperature = 30;
        public int RvcTemperature { set => Set(ref rvcTemperature, value); get => rvcTemperature; }

    }


    public class Timberline20Handler : ViewModel
    {
        private DispatcherTimer timer;

        public event EventHandler NeedToTransmit;

        public Timberline20Handler()
        {
            hcuVersion = new byte[4];
            heaterVersion = new byte[4];
            panelVersion = new byte[4];

            timer = new();
            timer.Interval = new TimeSpan(0, 0, 1);
            timer.Start();
            timer.Tick += TimerCallback;

            Zones.Add(new(0));
            Zones.Add(new(1));
            Zones.Add(new(2));
            Zones.Add(new(3));
            Zones.Add(new(4));

            SelectedZone = Zones[0];

            AuxTemp.Add(new());
            AuxTemp.Add(new());
            AuxTemp.Add(new());
            AuxTemp.Add(new());

            auxPumpOverride.AddNew();
            auxPumpOverride.AddNew();
            auxPumpOverride.AddNew();

            AuxPumpEstimatedTime.AddNew();
            AuxPumpEstimatedTime.AddNew();
            AuxPumpEstimatedTime.AddNew();

            AuxPumpStatus.AddNew();
            AuxPumpStatus.AddNew();
            AuxPumpStatus.AddNew();
        }

        public void ProcessMesage(RvcMessage msg)
        {
            byte[] D = msg.Data;
            switch (msg.Dgn)
            {
                case 0x1FFF7://Water heater status
                    if (D[0] != 1) return;
                    if (D[1] != 255)
                    {
                        HeaterEnabled = (D[1] & 1) != 0;
                        ElementEnabled = (D[1] & 2) != 0;
                    }

                    break;


                case 0x1FE97://Pump status
                    pumpStatus_t newStatus = pumpStatus_t.off;
                    bool overriden = false;
                    if (D[0] > 8) return;
                    if ((D[1] & 0xF) != 0xF)
                        switch (D[1] & 0xf)
                        {
                            case 0: newStatus = pumpStatus_t.off; break;
                            case 1: newStatus = pumpStatus_t.on; break;
                            case 5: newStatus = pumpStatus_t.overriden; overriden = true; break;
                            default: throw new ArgumentException("Wrong pump status");
                        }
                    switch (D[0])
                    {
                        case 1: Pump1Override = overriden; WaterPump1Status = newStatus; break;
                        case 2: Pump2Override = overriden; WaterPump2Status = newStatus; break;
                        case 5: HeaterPumpOverride = overriden; HeaterPumpStatus = newStatus; break;
                        case 6: WaterPumpAux1Override = overriden; auxPumpStatus[0] = newStatus; break;
                        case 7: WaterPumpAux2Override = overriden; auxPumpStatus[1] = newStatus; break;
                        case 8: WaterPumpAux3Override = overriden; auxPumpStatus[2] = newStatus; break;
                    }
                    break;

                case 0x1FFE4://Furnace status
                    if (D[0] > 5) return;
                    if ((D[1] & 3) != 3)
                        Zones[D[0] - 1].ManualMode = (D[1] & 3) != 0;
                    if (D[2] != 0xFF)
                        Zones[D[0] - 1].CurrentPwm = D[2] / 2;
                    break;
                case 0x1FFE2://Thermostat status 1
                    if (D[0] > 5) return;
                    if ((D[1] & 0xF) != 0xF)
                    {
                        if ((D[1] & 0xF) == 0) Zones[D[0] - 1].State = zoneState_t.Off;
                        if ((D[1] & 0xF) == 2 || (D[1] & 0xF) == 3) Zones[D[0] - 1].State = zoneState_t.Heat;
                        if ((D[1] & 0xF) == 4) Zones[D[0] - 1].State = zoneState_t.Fan;
                    }

                    if (D[3] != 0xFF || D[4] != 0xFF) Zones[D[0] - 1].TempSetpointCurrent = (D[3] + D[4] * 256) / 32 - 273;
                    break;

                case 0x1FEF7://Thermostat schedule 1
                    if (D[0] > 5) return;
                    if (D[1] == 0)
                    {
                        if (D[2] < 24 && D[3] < 60)
                            NightStart = new TimeOnly(D[2], D[3]);
                        if (D[4] != 0xFF || D[5] != 0xFF) Zones[D[0] - 1].TempSetpointNight = (D[4] + D[5] * 256) / 32 - 273;
                    }
                    if (D[1] == 1)
                    {
                        if (D[2] < 24 && D[3] < 60)
                            DayStart = new TimeOnly(D[2], D[3]);
                        if (D[4] != 0xFF || D[5] != 0xFF) Zones[D[0] - 1].TempSetpointDay = (D[4] + D[5] * 256) / 32 - 273;
                    }
                    break;

                case 0x1FF9C://Ambient temperature
                    if (D[0] > 6) return;
                    if (D[1] != 0xFF || D[2] != 0xFF)
                        if (D[0] < 6)
                            Zones[D[0] - 1].CurrentTemperature = (D[1] + D[2] * 256) / 32 - 273;
                        else if (D[0] == 6)
                            OutsideTemperature = (D[1] + D[2] * 256) / 32 - 273;
                        else if (D[0] > 6 && D[0] < 11)
                        {
                            AuxTemp[D[0] - 7] = (float)((D[1] + D[2] * 256) / 32.0 - 273);
                        }
                    break;

                case 0x1EF65: //Proprietary dgn
                    switch (D[0])
                    {
                        case 0xA0:
                            if (D[1] != 0xFF) TankTemperature = D[1] - 40;
                            if (D[2] != 0xFF) HeaterTemperature = D[2] - 40;
                            if (D[3] != 0xFF) Zones[0].ManualPercent = (byte)(D[3] / 2);
                            if (D[4] != 0xFF) Zones[1].ManualPercent = (byte)(D[4] / 2);
                            if (D[5] != 0xFF) Zones[2].ManualPercent = (byte)(D[5] / 2);
                            if (D[6] != 0xFF) Zones[3].ManualPercent = (byte)(D[6] / 2);
                            if (D[7] != 0xFF) Zones[4].ManualPercent = (byte)(D[7] / 2);
                            break;
                        case 0xA1: //Estimated time
                            if (D[1] != 0xFF || D[2] != 0xFF || D[3] != 0xFF) SystemEstimatedTime = D[1] + D[2] * 256 + D[3] * 65536;
                            break;
                        case 0xA2: //Pump timers #1
                            if (D[1] != 0xFF || D[2] != 0xFF) Pump1EstimatedTime = D[1] + D[2] * 256;
                            if (D[3] != 0xFF || D[4] != 0xFF) Pump2EstimatedTime = D[3] + D[4] * 256;
                            if (D[5] != 0xFF || D[6] != 0xFF) HeaterPumpEstimatedTime = D[5] + D[6] * 256;
                            break;
                        case 0xA3: //Pump timers #2
                            if (D[1] != 0xFF || D[2] != 0xFF) auxPumpEstimatedTime[0] = D[1] + D[2] * 256;
                            if (D[3] != 0xFF || D[4] != 0xFF) auxPumpEstimatedTime[1] = D[3] + D[4] * 256;
                            if (D[5] != 0xFF || D[6] != 0xFF) auxPumpEstimatedTime[2] = D[5] + D[6] * 256;
                            break;
                        case 0xA4: // Heater info
                            if (D[1] != 0xFF || D[2] != 0xFF || D[3] != 0xFF) HeaterTotalMinutes = D[1] + D[2] * 256 + D[3] * 65536;
                            HeaterVersion = new byte[] { D[4], D[5], D[6], D[7] };
                            break;
                        case 0xA5: //Panel Info
                            PanelVersion = new byte[] { D[4], D[5], D[6], D[7] };
                            break;
                        case 0xA6: //HCU info
                            HcuVersion = new byte[] { D[4], D[5], D[6], D[7] };
                            break;
                        case 0xA8: //Timers config status
                            if (D[1] != 0xFF || D[2] != 0xFF)
                            {
                                if (D[1] < 1) SystemDuration = 1;
                                else if (D[1] > 100) SystemDuration = 100; //Unlimited
                                else SystemDuration = D[1];
                            }
                            if (D[2] != 0xFF)
                            {
                                if (D[2] < 2) PumpDuration = 2;
                                else if (D[2] > 60) SystemDuration = 60; //Unlimited
                                else SystemDuration = D[2];
                            }
                            break;
                        case 0xA9:
                            if ((D[1] & 3) != 3) DomesticWater = (D[1] & 3) != 0;
                            if ((D[2] != 255)) HeaterIconCode = (heaterIcon)D[1];
                            break;
                    }
                    break;
            }
        }

        public void ToggleHeater()
        {
            RvcMessage msg = new() { Dgn = 0x1FFF6 };
            msg.Data[0] = 1;
            msg.Data[1] = (byte)(0b11110000 + (!HeaterEnabled ? 1 : 0) + ((ElementEnabled ? 1 : 0) << 1));

            NeedToTransmit.Invoke(this, new NeedToTransmitEventArgs() { msgToTransmit = msg });

        }

        public void ToggleElement()
        {
            RvcMessage msg = new() { Dgn = 0x1FFF6 };
            msg.Data[0] = 1;
            msg.Data[1] = (byte)(0b11110000 + (HeaterEnabled ? 1 : 0) + ((!ElementEnabled ? 1 : 0) << 1));

            NeedToTransmit?.Invoke(this, new NeedToTransmitEventArgs() { msgToTransmit = msg });
        }



        public void ToggleHeaterPumpOverride()
        {

            RvcMessage msg = new() { Dgn = 0x1FE96 };
            msg.Data[0] = 5;
            if (HeaterPumpOverride)
                msg.Data[1] = 0b11110000;
            else
                msg.Data[1] = 0b11110101;

            NeedToTransmit?.Invoke(this, new NeedToTransmitEventArgs() { msgToTransmit = msg });
        }

        public void TogglePump1Override()
        {

            RvcMessage msg = new() { Dgn = 0x1FE96 };
            msg.Data[0] = 1;
            if (Pump1Override)
                msg.Data[1] = 0b11110000;
            else
                msg.Data[1] = 0b11110101;

            NeedToTransmit?.Invoke(this, new NeedToTransmitEventArgs() { msgToTransmit = msg });
        }

        public void TogglePump2Override()
        {

            RvcMessage msg = new() { Dgn = 0x1FE96 };
            msg.Data[0] = 2;
            if (Pump2Override)
                msg.Data[1] = 0b11110000;
            else
                msg.Data[1] = 0b11110101;

            NeedToTransmit?.Invoke(this, new NeedToTransmitEventArgs() { msgToTransmit = msg });
        }
        public void ToggleAuxPump1Override()
        {

            RvcMessage msg = new() { Dgn = 0x1FE96 };
            msg.Data[0] = 6;
            if (WaterPumpAux1Override)
                msg.Data[1] = 0b11110000;
            else
                msg.Data[1] = 0b11110101;

            NeedToTransmit?.Invoke(this, new NeedToTransmitEventArgs() { msgToTransmit = msg });
        }

        public void ToggleAuxPump2Override()
        {

            RvcMessage msg = new() { Dgn = 0x1FE96 };
            msg.Data[0] = 7;
            if (WaterPumpAux2Override)
                msg.Data[1] = 0b11110000;
            else
                msg.Data[1] = 0b11110101;

            NeedToTransmit?.Invoke(this, new NeedToTransmitEventArgs() { msgToTransmit = msg });
        }

        public void ToggleAuxPump3Override()
        {

            RvcMessage msg = new() { Dgn = 0x1FE96 };
            msg.Data[0] = 8;
            if (WaterPumpAux3Override)
                msg.Data[1] = 0b11110000;
            else
                msg.Data[1] = 0b11110101;

            NeedToTransmit?.Invoke(this, new NeedToTransmitEventArgs() { msgToTransmit = msg });
        }

        public void ToggleZoneState(int zone)
        {
            RvcMessage msg = new() { Dgn = 0x1FEF9 };
            msg.Data[0] = (byte)(1 + zone);
            switch (Zones[zone].State)
            {
                case zoneState_t.Off: msg.Data[1] = 0b11110010; break;
                case zoneState_t.Heat: msg.Data[1] = 0b11110100; break;
                case zoneState_t.Fan: msg.Data[1] = 0b11110000; break;
            }

            NeedToTransmit?.Invoke(this, new NeedToTransmitEventArgs() { msgToTransmit = msg });
        }

        public void ToggleFanManualMode(int zone)
        {
            RvcMessage msg = new() { Dgn = 0x1FFE3 };
            msg.Data[0] = (byte)(1 + zone);
            msg.Data[1] = (byte)(0b11111100 + (!Zones[zone].ManualMode ? 1 : 0));

            NeedToTransmit?.Invoke(this, new NeedToTransmitEventArgs() { msgToTransmit = msg });
        }


        public void SetFanManualSpeed(byte zone, byte speed)
        {
            RvcMessage msg = new() { Dgn = 0x1FFE3 };
            msg.Data[0] = (byte)(1 + zone);
            msg.Data[2] = (byte)(speed * 2);

            NeedToTransmit?.Invoke(this, new NeedToTransmitEventArgs() { msgToTransmit = msg });
        }

        public void SetDaySetpoint(int zone, int temp)
        {
            RvcMessage msg = new() { Dgn = 0x1FEF5 };
            msg.Data[0] = (byte)(1 + zone);
            msg.Data[1] = 1;
            ushort tmp = (ushort)((temp + 273) * 32);
            msg.Data[4] = (byte)(tmp & 0xFF);
            msg.Data[5] = (byte)(tmp >> 8 & 0xFF);

            NeedToTransmit?.Invoke(this, new NeedToTransmitEventArgs() { msgToTransmit = msg });

        }

        public void SetNightSetpoint(int zone, int temp)
        {
            RvcMessage msg = new() { Dgn = 0x1FEF5 };
            msg.Data[0] = (byte)(1 + zone);
            msg.Data[1] = 0;
            ushort tmp = (ushort)((temp + 273) * 32);
            msg.Data[4] = (byte)(tmp & 0xFF);
            msg.Data[5] = (byte)(tmp >> 8 & 0xFF);

            NeedToTransmit?.Invoke(this, new NeedToTransmitEventArgs() { msgToTransmit = msg });
        }

        public void SetTime(DateTime dateTime)
        {
            RvcMessage msg = new();
            msg.Dgn = 0x1FFFF;
            msg.Priority = 6;
            msg.Data[0] = (byte)(dateTime.Year - 2000);
            msg.Data[1] = (byte)dateTime.Month;
            msg.Data[2] = (byte)dateTime.Day;
            msg.Data[3] = (byte)dateTime.DayOfWeek;
            msg.Data[4] = (byte)dateTime.Hour;
            msg.Data[5] = (byte)dateTime.Minute;
            msg.Data[6] = (byte)dateTime.Second;

            NeedToTransmit?.Invoke(this, new NeedToTransmitEventArgs() { msgToTransmit = msg });
        }

        public void ClearErrors()
        {
            RvcMessage msg = new();
            msg.Dgn = 0x1EF65;
            msg.Priority = 6;
            msg.Data[0] = 0x81;

            NeedToTransmit?.Invoke(this, new NeedToTransmitEventArgs() { msgToTransmit = msg });
        }

        public void SetSystemDuration(int hours)
        {
            if (hours < 1) hours = 1;
            if (hours > 100) hours = 100;

            RvcMessage msg = new();
            msg.Dgn = 0x1EF65;
            msg.Priority = 6;
            msg.Data[0] = 0xA7;
            msg.Data[1] = (byte)hours;

            NeedToTransmit?.Invoke(this, new NeedToTransmitEventArgs() { msgToTransmit = msg });
        }

        public void SetPumpOverrideDuration(int minutes)
        {
            if (minutes < 2) minutes = 2;
            if (minutes > 60) minutes = 60;

            RvcMessage msg = new();
            msg.Dgn = 0x1EF65;
            msg.Priority = 6;
            msg.Data[0] = 0xA7;
            msg.Data[2] = (byte)minutes;

            NeedToTransmit?.Invoke(this, new NeedToTransmitEventArgs() { msgToTransmit = msg });
        }


        public void SetDayStart(int hours, int minutes)
        {
            if (minutes > 59) minutes = 59;
            if (hours > 23) minutes = 23;

            RvcMessage msg = new();
            msg.Dgn = 0x1FEF5;
            msg.Priority = 6;
            msg.Data[0] = 1;
            msg.Data[1] = 1;
            msg.Data[2] = (byte)hours;
            msg.Data[3] = (byte)minutes;

            NeedToTransmit?.Invoke(this, new NeedToTransmitEventArgs() { msgToTransmit = msg });
        }

        public void SetNightStart(int hours, int minutes)
        {
            if (minutes > 59) minutes = 59;
            if (hours > 23) minutes = 23;

            RvcMessage msg = new();
            msg.Dgn = 0x1FEF5;
            msg.Priority = 6;
            msg.Data[0] = 1;
            msg.Data[1] = 0;
            msg.Data[2] = (byte)hours;
            msg.Data[3] = (byte)minutes;

            NeedToTransmit?.Invoke(this, new NeedToTransmitEventArgs() { msgToTransmit = msg });
        }

        public void OverrideTempSensor(int zone, int temperature)
        {
            RvcMessage msg = new();
            msg.Dgn = 0x1FF9C;
            msg.Priority = 6;
            msg.Data[0] = (byte)(1 + zone);
            ushort tmp = (ushort)((temperature + 273) * 32);
            msg.Data[1] = (byte)tmp;
            msg.Data[2] = (byte)(tmp >> 8);

            NeedToTransmit?.Invoke(this, new NeedToTransmitEventArgs() { msgToTransmit = msg });
        }


        void TimerCallback(object sender, EventArgs e)
        {
            if (Zones[zoneToOverride].BroadcastTemperature)
                OverrideTempSensor(zoneToOverride, Zones[zoneToOverride].RvcTemperature);
            zoneToOverride++;
            if (zoneToOverride > 4) zoneToOverride = 0;
        }




        private int tankTemperature;
        public int TankTemperature { set => Set(ref tankTemperature, value); get => tankTemperature; }

        private int heatExchangerTemperature;
        public int HeatExchangerTemperature { set => Set(ref heatExchangerTemperature, value); get => heatExchangerTemperature; }

        private int heaterTemperature;
        public int HeaterTemperature { set => Set(ref heaterTemperature, value); get => heaterTemperature; }

        private int outsideTemperature;
        public int OutsideTemperature { set => Set(ref outsideTemperature, value); get => outsideTemperature; }

        private int liquidLevel;
        public int LiquidLEvel { set => Set(ref liquidLevel, value); get => liquidLevel; }

        private pumpStatus_t heaterPumpStatus;
        public pumpStatus_t HeaterPumpStatus { set => Set(ref heaterPumpStatus, value); get => heaterPumpStatus; }

        private pumpStatus_t waterPump1Status;
        public pumpStatus_t WaterPump1Status { set => Set(ref waterPump1Status, value); get => waterPump1Status; }

        private pumpStatus_t waterPump2Status;
        public pumpStatus_t WaterPump2Status { set => Set(ref waterPump2Status, value); get => waterPump2Status; }

        private BindingList<pumpStatus_t> auxPumpStatus = new();
        public BindingList<pumpStatus_t> AuxPumpStatus => auxPumpStatus;

        private bool heaterPumpOverride;
        public bool HeaterPumpOverride { set => Set(ref heaterPumpOverride, value); get => heaterPumpOverride; }

        private bool pump1Override;
        public bool Pump1Override { set => Set(ref pump1Override, value); get => pump1Override; }

        private bool pump2Override;
        public bool Pump2Override { set => Set(ref pump2Override, value); get => pump2Override; }

        private bool waterPumpAux1Override;
        public bool WaterPumpAux1Override { set => Set(ref waterPumpAux1Override, value); get => waterPumpAux1Override; }

        private bool waterPumpAux2Override;
        public bool WaterPumpAux2Override { set => Set(ref waterPumpAux2Override, value); get => waterPumpAux2Override; }

        private bool waterPumpAux3Override;
        public bool WaterPumpAux3Override { set => Set(ref waterPumpAux3Override, value); get => waterPumpAux3Override; }

        private bool heaterEnabled;
        public bool HeaterEnabled { set => Set(ref heaterEnabled, value); get => heaterEnabled; }

        private bool elementEnabled;
        public bool ElementEnabled { set => Set(ref elementEnabled, value); get => elementEnabled; }

        private bool underfloorHeatingEnabled;
        public bool UnderfloorHeatingEnabled { set => Set(ref underfloorHeatingEnabled, value); get => underfloorHeatingEnabled; }

        private bool enginePreheatEnabled;
        public bool EnginePreheatEnabled { set => Set(ref enginePreheatEnabled, value); get => enginePreheatEnabled; }

        private bool domesticWater;
        public bool DomesticWater { set => Set(ref domesticWater, value); get => domesticWater; }

        private BindingList<ZoneHandler> zones = new();
        public BindingList<ZoneHandler> Zones => zones;

        private ZoneHandler selectedZone = null;
        [AffectsTo(nameof(SelectedZoneNumber))]
        public ZoneHandler SelectedZone { set => Set(ref selectedZone, value); get => selectedZone; }

        private int selectedZoneNumber;
        public int SelectedZoneNumber => Zones.IndexOf(SelectedZone);


        BindingList<float> auxTemp = new();
        public BindingList<float> AuxTemp => auxTemp;

        BindingList<bool> auxPumpOverride = new();
        public BindingList<bool> AuxPumpOverride => auxPumpOverride;

        BindingList<int> auxPumpEstimatedTime = new();
        public BindingList<int> AuxPumpEstimatedTime => auxPumpEstimatedTime;

        private int systemEstimatedTime;
        public int SystemEstimatedTime { set => Set(ref systemEstimatedTime, value); get => systemEstimatedTime; }

        private int heaterPumpEstimatedTime;
        public int HeaterPumpEstimatedTime { set => Set(ref heaterPumpEstimatedTime, value); get => heaterPumpEstimatedTime; }

        private int pump1EstimatedTime;
        public int Pump1EstimatedTime { set => Set(ref pump1EstimatedTime, value); get => pump1EstimatedTime; }

        private int pump2EstimatedTime;
        public int Pump2EstimatedTime { set => Set(ref pump2EstimatedTime, value); get => pump2EstimatedTime; }

        private int heaterTotalMinutes;
        public int HeaterTotalMinutes { set => Set(ref heaterTotalMinutes, value); get => heaterTotalMinutes; }

        private int waterDuration;
        public int WaterDuration { set => Set(ref waterDuration, value); get => waterDuration; }

        private int systemDuration;
        [AffectsTo(nameof(SystemDurationString))]
        public int SystemDuration { set => Set(ref systemDuration, value); get => systemDuration; }

        public string SystemDurationString
        {
            get
            {
                if (SystemDuration < 24)
                    return $"{systemDuration} H";
                else if (SystemDuration < 100)
                    return $"{systemDuration / 24} D";
                else return "Unlimited";
            }
        }

        private int pumpDuration;
        public int PumpDuration { set => Set(ref pumpDuration, value); get => pumpDuration; }


        private TimeOnly? dayStart;
        public TimeOnly? DayStart { set => Set(ref dayStart, value); get => dayStart; }

        private TimeOnly? nightStart;
        public TimeOnly? NightStart { set => Set(ref nightStart, value); get => nightStart; }


        private byte[] heaterVersion;
        [AffectsTo(nameof(HeaterVersionString))]
        public byte[] HeaterVersion { set => Set(ref heaterVersion, value); get => heaterVersion; }

        public string HeaterVersionString { get => $"{heaterVersion[0]:D03}.{heaterVersion[1]:D03}.{heaterVersion[2]:D03}.{heaterVersion[3]:D03}"; }

        private byte[] hcuVersion;
        [AffectsTo(nameof(HcuVersionString))]
        public byte[] HcuVersion { set => Set(ref hcuVersion, value); get => hcuVersion; }

        public string HcuVersionString { get => $"{hcuVersion[0]:D03}.{hcuVersion[1]:D03}.{hcuVersion[2]:D03}.{hcuVersion[3]:D03}"; }

        private byte[] panelVersion;
        [AffectsTo(nameof(PanelVersionString))]
        public byte[] PanelVersion { set => Set(ref panelVersion, value); get => panelVersion; }

        public string PanelVersionString { get => $"{panelVersion[0]:D03}.{panelVersion[1]:D03}.{panelVersion[2]:D03}.{panelVersion[3]:D03}"; }

        private int zoneToOverride = 0;

        heaterIcon heaterIconCode = 0;
        public heaterIcon HeaterIconCode { set => Set(ref heaterIconCode, value); get => heaterIconCode; }


    }

}

