﻿using OmniProtocol;
using CAN_Tool.Infrastructure.Commands;
using CAN_Tool.ViewModels.Base;
using Microsoft.Win32;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Threading;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Input;
using System.ComponentModel;
using RVC;
using CAN_Adapter;
using CAN_Tool.Libs;


namespace CAN_Tool.ViewModels
{
    public class NeedToTransmitEventArgs : EventArgs
    {
        public RvcMessage msgToTransmit;
    }

    public class Timberline15Handler : ViewModel
    {

        public event EventHandler NeedToTransmit;

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
                        if (D[4] != 0 || D[5] != 0)
                            HeatExchangerTemperature = (D[4] + D[5] * 256) / 32 - 273;
                    }

                    break;

                case 0x1FE99: //Water heater status 2
                    if (D[0] != 1) return;
                    if (D[2] != 0xFF)
                        WaterEnabled = (D[2] >> 6) == 0;

                    break;

                case 0x1FE97://Pump status
                    if (D[0] != 1) return;
                    if (D[1] != 0xFF)
                        WaterPumpStatus = (D[1] & 0xF) != 0;
                    break;
                case 0x1FFE4://Furnace status
                    if (D[0] != 1) return;
                    if ((D[1] & 3) != 3)
                        ZoneManualFanMode = (D[1] & 3) != 0;
                    if (D[2] != 0xFF)
                        ZoneFanMeasuredSpeed = D[2] / 2;
                    break;
                case 0x1FFE2://Thermostat status 1
                    if (D[0] != 1) return;
                    if ((D[1] & 0xF) != 0xF) return; ZoneEnabled = (D[1] & 0x2) == 2;
                    if (D[3] != 0xFF || D[4] != 0xFF) CurrentSetpoint = (D[3] + D[4] * 256) / 32 - 273;
                    break;

                case 0x1FEF7://Thermostat schedule 1
                    if (D[0] != 1) return;
                    if (D[1] == 0)
                    {
                        if (D[2] < 24) NightStartHour = D[2];
                        if (D[3] < 60) NightStartMinute = D[3];
                        if (D[4] != 0xFF || D[5] != 0xFF) SetpointNight = (D[4] + D[5] * 256) / 32 - 273;
                    }
                    if (D[1] == 1)
                    {
                        if (D[2] < 24) DayStartHour = D[2];
                        if (D[3] < 60) DayStartMinute = D[3];
                        if (D[4] != 0xFF || D[5] != 0xFF) SetpointDay = (D[4] + D[5] * 256) / 32 - 273;
                    }
                    break;

                case 0x1FF9C://Ambient temperature
                    if (D[0] != 1) return;
                    if ((D[1] != 0xFF) || (D[2] != 0xFF))
                        ZoneTemperature = (D[1] + D[2] * 256) / 32 - 273;
                    break;

                case 0x1EF65: //Proprietary dgn
                    switch (D[0])
                    {
                        case 0x84:
                            if ((D[1] & 3) != 3) SolenoidStatus = (D[1] & 3) != 0;
                            if (D[2] != 0xFF || D[3] != 0xFF) TankTemperature = (D[2] + D[3] * 256) / 32 - 273;
                            if (D[4] != 0xFF || D[5] != 0xFF) HeaterTemperature = (D[4] + D[5] * 256) / 32 - 273;
                            break;
                        case 0x85: //Estimated time
                            if (D[1] != 0xFF || D[2] != 0xFF || D[3] != 0xFF) systemEstimatedTime = D[1] + D[2] * 256 + D[3] * 65536;
                            if (D[4] != 0xFF || D[5] != 0xFF) WaterEstimatedTime = D[4] + D[5] * 256;
                            if (D[6] != 0xFF || D[7] != 0xFF) pumpEstimatedTime = D[6] + D[7] * 256;
                            break;
                        case 0x86: // Heater info
                            if (D[1] != 0xFF || D[2] != 0xFF || D[3] != 0xFF) HeaterTotalMinutes = D[1] + D[2] * 256 + D[3] * 65536;
                            HeaterVersion[0] = D[4];
                            HeaterVersion[1] = D[5];
                            HeaterVersion[2] = D[6];
                            HeaterVersion[3] = D[7];
                            break;
                        case 0x87: //Panel Info
                            if (D[1] != 0xFF || D[2] != 0xFF || D[3] != 0xFF) PanelMinutesSinceStart = D[1] + D[2] * 256 + D[3] * 65536;
                            PanelVersion[0] = D[4];
                            PanelVersion[1] = D[5];
                            PanelVersion[2] = D[6];
                            PanelVersion[3] = D[7];
                            break;
                        case 0x88: //HCU info
                            HcuVersion[0] = D[4];
                            HcuVersion[1] = D[5];
                            HcuVersion[2] = D[6];
                            HcuVersion[3] = D[7];
                            break;
                        case 0x8A: //Timers config status
                            HcuVersion[0] = D[4];
                            HcuVersion[1] = D[5];
                            HcuVersion[2] = D[6];
                            HcuVersion[3] = D[7];
                            break;
                    }

                    break;

            }
        }

        public void ToggleHeater()
        {
            RvcMessage msg = new() { Dgn = 0x1FFF6 };
            msg.Data[0] = 1;
            msg.Data[1] = (byte)(0b11111100 + (!HeaterEnabled ? 1 : 0) + ((ElementEnabled ? 1 : 0) << 1));

            NeedToTransmit.Invoke(this, new NeedToTransmitEventArgs() { msgToTransmit = msg });

        }

        public void ToggleElement()
        {
            RvcMessage msg = new() { Dgn = 0x1FFF6 };
            msg.Data[0] = 1;
            msg.Data[1] = (byte)(0b11111100 + (HeaterEnabled ? 1 : 0) + ((!ElementEnabled ? 1 : 0) << 1));

            NeedToTransmit.Invoke(this, new NeedToTransmitEventArgs() { msgToTransmit = msg });
        }

        public void ToggleWater()
        {
            RvcMessage msg = new() { Dgn = 0x1FFF6 };
            msg.Data[0] = 1;
            msg.Data[1] = (byte)(0b11111100 + (!HeaterEnabled ? 1 : 0) + ((ElementEnabled ? 1 : 0) << 1));

            NeedToTransmit.Invoke(this, new NeedToTransmitEventArgs() { msgToTransmit = msg });
        }

        public void TogglePump()
        {

        }

        public void ToggleZone()
        {
            RvcMessage msg = new() { Dgn = 0x1FEF9 };
            msg.Data[0] = 1;
            msg.Data[1] = (byte)(0xF0 + (!ZoneEnabled ? 2 : 0));

            NeedToTransmit.Invoke(this, new NeedToTransmitEventArgs() { msgToTransmit = msg });
        }

        public void ToggleFanManualMode()
        {
            RvcMessage msg = new() { Dgn = 0x1FFE3};
            msg.Data[0] = 1;
            msg.Data[1] = (byte)(0b111111 + (!ZoneManualFanMode ? 1 : 0));

            NeedToTransmit.Invoke(this, new NeedToTransmitEventArgs() { msgToTransmit = msg });
        }

        public void SetFanManualSpeed(byte speed)
        {
            RvcMessage msg = new() { Dgn = 0x1FFE3 };
            msg.Data[0] = 1;
            msg.Data[2] = (byte)(ZoneManualFanSpeed*2);

            NeedToTransmit.Invoke(this, new NeedToTransmitEventArgs() { msgToTransmit = msg });
        }

        public void SetDaySetpoint(int temp)
        {
            RvcMessage msg = new() { Dgn = 0x1FEF5 };
            msg.Data[0] = 1;
            msg.Data[1] = 1;
            UInt16 tmp = (UInt16)((temp + 273) * 32);
            msg.Data[4] = (byte)(tmp & 0xFF);
            msg.Data[5] = (byte)((tmp>>8) & 0xFF);

            NeedToTransmit.Invoke(this, new NeedToTransmitEventArgs() { msgToTransmit = msg });
            
        }

        public void SetNightSetpoint(int temp)
        {
            RvcMessage msg = new() { Dgn = 0x1FEF5 };
            msg.Data[0] = 1;
            msg.Data[1] = 0;
            UInt16 tmp = (UInt16)((temp + 273) * 32);
            msg.Data[4] = (byte)(tmp & 0xFF);
            msg.Data[5] = (byte)((tmp >> 8) & 0xFF);

            NeedToTransmit.Invoke(this, new NeedToTransmitEventArgs() { msgToTransmit = msg });
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

            NeedToTransmit.Invoke(this, new NeedToTransmitEventArgs() { msgToTransmit = msg });
        }

        public void SetSystemLimitationTime(int minutes)
        {

        }

        public void SetWaterLimitationTime(int minutes)
        {

        }

        public void OverrideTempSensor(int temperature)
        {
            RvcMessage msg = new();
            msg.Dgn = 0x1FF9C;
            msg.Priority = 6;
            msg.Data[0] = 1;
            UInt16 tmp = (UInt16)((temperature + 273) * 32);
            msg.Data[1] = (byte)tmp;
            msg.Data[2] = (byte)(tmp>>8);

            NeedToTransmit.Invoke(this, new NeedToTransmitEventArgs() { msgToTransmit = msg });
        }


        public Timberline15Handler()
        {
            hcuVersion = new byte[4];
            heaterVersion = new byte[4];
            panelVersion = new byte[4];
        }

        private int tankTemperature;
        public int TankTemperature { set => Set(ref tankTemperature, value); get => tankTemperature; }

        private int heatExchangerTemperature;
        public int HeatExchangerTemperature { set => Set(ref heatExchangerTemperature, value); get => heatExchangerTemperature; }

        private int heaterTemperature;
        public int HeaterTemperature { set => Set(ref heaterTemperature, value); get => heaterTemperature; }

        private bool waterPumpStatus;
        public bool WaterPumpStatus { set => Set(ref waterPumpStatus, value); get => waterPumpStatus; }

        private bool solenoidStatus;
        public bool SolenoidStatus { set => Set(ref solenoidStatus, value); get => solenoidStatus; }

        private bool heaterEnabled;
        public bool HeaterEnabled { set => Set(ref heaterEnabled, value); get => heaterEnabled; }

        private bool elementEnabled;
        public bool ElementEnabled { set => Set(ref elementEnabled, value); get => elementEnabled; }

        private bool waterEnabled;
        public bool WaterEnabled { set => Set(ref waterEnabled, value); get => waterEnabled; }

        private bool zoneEnabled;
        public bool ZoneEnabled { set => Set(ref zoneEnabled, value); get => zoneEnabled; }


        private int setpointDay;
        public int SetpointDay { set => Set(ref setpointDay, value); get => setpointDay; }

        private int setpointNight;
        public int SetpointNight { set => Set(ref setpointNight, value); get => setpointNight; }

        private int currentSetpoint;
        public int CurrentSetpoint { set => Set(ref currentSetpoint, value); get => currentSetpoint; }

        private int zoneManualFanSpeed;
        public int ZoneManualFanSpeed { set => Set(ref zoneManualFanSpeed, value); get => zoneManualFanSpeed; }

        private bool zoneManualFanMode;
        public bool ZoneManualFanMode { set => Set(ref zoneManualFanMode, value); get => zoneManualFanMode; }

        private int zoneFanMeasuredSpeed;
        public int ZoneFanMeasuredSpeed { set => Set(ref zoneFanMeasuredSpeed, value); get => zoneFanMeasuredSpeed; }

        private int systemLimitationTime;
        public int SystemLimitationTime { set => Set(ref systemLimitationTime, value); get => systemLimitationTime; }

        private int waterLimitationTime;
        public int WaterLimitationTime { set => Set(ref waterLimitationTime, value); get => waterLimitationTime; }

        private int systemEstimatedTime;
        public int SystemEstimatedTime { set => Set(ref systemEstimatedTime, value); get => systemEstimatedTime; }

        private int pumpEstimatedTime;
        public int PumpEstimatedTime { set => Set(ref pumpEstimatedTime, value); get => pumpEstimatedTime; }

        private int waterEstimatedTime;
        public int WaterEstimatedTime { set => Set(ref waterEstimatedTime, value); get => waterEstimatedTime; }

        private int panelTimeSinceStart;
        public int PanelMinutesSinceStart { set => Set(ref panelTimeSinceStart, value); get => panelTimeSinceStart; }

        private bool panelSensorOn;
        public bool PanelSensorOn { set => Set(ref panelSensorOn, value); get => panelSensorOn; }

        private int zoneTemperature;
        public int ZoneTemperature { set => Set(ref zoneTemperature, value); get => zoneTemperature; }

        private int dayStartHour;
        public int DayStartHour { set => Set(ref dayStartHour, value); get => dayStartHour; }

        private int dayStartMinute;
        public int DayStartMinute { set => Set(ref dayStartMinute, value); get => dayStartMinute; }

        private int nightStartHour;
        public int NightStartHour { set => Set(ref nightStartHour, value); get => nightStartHour; }

        private int nightStartMinute;
        public int NightStartMinute { set => Set(ref nightStartMinute, value); get => nightStartMinute; }

        private int heaterTotalMinutes;
        public int HeaterTotalMinutes { set => Set(ref heaterTotalMinutes, value); get => heaterTotalMinutes; }

        private int waterDuration;
        public int WaterDuration { set => Set(ref waterDuration, value); get => waterDuration; }

        private int systemDuration;
        public int SystemDuration { set => Set(ref systemDuration, value); get => systemDuration; }


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

    }

    internal class RvcPageViewModel : ViewModel
    {

        public Timberline15Handler Timberline15 { get; }
        private MainWindowViewModel vm;
        public MainWindowViewModel VM => vm;

        public UpdatableList<RvcMessage> MessageList { get; } = new();

        public Dictionary<int, DGN> DgnList => RVC.RVC.DGNs;

        public bool SpamState { set; get; }

        public System.Timers.Timer RefreshTimer;


        public int SpamInterval { set; get; } = 100;

        public bool RandomDgn { set; get; } = false;

        Task spamTask;

        private void SendMessage(object sender, EventArgs e)
        {
            VM.CanAdapter.Transmit((e as NeedToTransmitEventArgs).msgToTransmit.ToCanMessage());
        }
        public RvcPageViewModel(MainWindowViewModel vm)
        {
            this.vm = vm;

            SaveRvcLogCommand = new LambdaCommand(OnSaveRvcLogCommandExecuted, x => true);
            SendRvcMessageCommand = new LambdaCommand(OnSendRvcMessageCommandExecuted, x => true);

            RefreshTimer = new(250);
            RefreshTimer.Elapsed += RefreshTimerTick;
            RefreshTimer.Start();

            spamTask = Task.Run(SpamFunction);

            Timberline15 = new();
            Timberline15.NeedToTransmit += SendMessage;

        }

        private void SpamFunction()
        {
            while (true)
            {
                Thread.Sleep(SpamInterval);

                if (SpamEnabled)
                {
                    if (RandomDgn)
                    {
                        CanMessage msg = new RvcMessage() { Dgn = new Random((int)DateTime.Now.Ticks).Next(0, 0x1FFFF) }.ToCanMessage();
                        vm.CanAdapter.Transmit(msg);
                    }
                    else
                        vm.CanAdapter.Transmit(ConstructedMessage.ToCanMessage());
                }

            }
        }

        private bool spamEnabled;
        public bool SpamEnabled
        {
            get => spamEnabled;
            set => Set(ref spamEnabled, value);
        }

        private RvcMessage selectedMessage;
        public RvcMessage SelectedMessage
        {
            get => selectedMessage;
            set => Set(ref selectedMessage, value);
        }

        public void ProcessMessage(CanMessage m)
        {
            if (m.RvcCompatible)
            {
                var rvcm = new RvcMessage(m);
                MessageList.TryToAdd(rvcm);
                Timberline15.ProcessMesage(rvcm);
            }
        }

        public ICommand SaveRvcLogCommand { set; get; }

        private void OnSaveRvcLogCommandExecuted(object parameter)
        {
            string path = Environment.GetFolderPath(Environment.SpecialFolder.Desktop) + "\\RVC_Log.txt";
            string log = "";
            foreach (var m in MessageList)
                log += m.ToString() + '|' + m.PrintParameters() + '\n';
            File.WriteAllText(path, log);
        }

        private void RefreshTimerTick(object sender, EventArgs e)
        {
            foreach (var m in MessageList)
                m.FreshCheck();
            if (spamTask != null && spamTask.Status != TaskStatus.Running && spamTask.Status != TaskStatus.WaitingToRun && spamTask.Status != TaskStatus.Created)
                spamTask = Task.Run(SpamFunction);
        }

        public ICommand SendRvcMessageCommand { set; get; }

        private void OnSendRvcMessageCommandExecuted(object parameter)
        {
            CanMessage msg = ConstructedMessage.ToCanMessage();
            vm.CanAdapter.Transmit(msg);
        }

        public RvcMessage ConstructedMessage { set; get; } = new() { Dgn = 0x1FFFF, SourceAdress = 80, Priority = 6, Data = new byte[] { 0xff, 0xff, 0xff, 0xff, 0xff, 0xff, 0xff, 0xff } };

    }
}
