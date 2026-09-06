using System;
using System.ComponentModel;
using CommunityToolkit.Mvvm.ComponentModel;
using static CAN_Tool.Libs.Helper;


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

    // Поля и их разбор в Omni.cs case 60/61/62 повторяют ровно то, что показывают/пишут
    // страницы модема на самом ПУ28 (см. C:\source\PU28-Timberline\User\Activity\ModemInfo.cpp,
    // ModemInternetInfo.cpp, ModemSettings.cpp и C:\source\...\Main\ModemData.h) - какие поля
    // там только выводятся на экран (Registered/Roaming/Csq/OperatorName/Imei/LastSms*/
    // InternetConnected/MqttConnected/NetworkAcT/InternetCheckUrl/MqttBroker/MqttLogin/
    // MqttPassword/ConnectionLink/AutoRegStatus/TempUnitF), а какие пульт реально меняет
    // (OnlySmsMode/Force2gOnly/AllowRoaming/FaultReport/CmdAck - через ModemSettings.cpp, плюс
    // отдельная команда "auto-register" через PGN1/30) - см. ModemDeviceViewModel.cs.
    public partial class ModemViewModel : ObservableObject
    {
        [ObservableProperty] public bool registered;
        [ObservableProperty] public bool roaming;
        [ObservableProperty] public int csq = -1;          // -1 = нет данных
        [ObservableProperty] public bool onlySmsMode;
        [ObservableProperty] public bool faultReport;
        [ObservableProperty] public bool cmdAck;
        [ObservableProperty] public bool tempUnitF;         // только отображается - пульт этим не управляет
        [ObservableProperty] public string operatorCode = "";
        [ObservableProperty] public int lac = -1;           // -1 = нет данных
        [ObservableProperty] public long cellId = -1;        // -1 = нет данных

        // ── PGN60 sub0/sub4 (см. ModemInternetInfo.cpp) ──────────────────
        [ObservableProperty] public bool internetConnected;
        [ObservableProperty] public bool mqttConnected;

        // Сырое значение <AcT> из AT+COPS? (3GPP 27.007) - реальная используемая технология
        // связи, НЕ то же самое, что настройка Force2gOnly. -1 = нет данных/не зарегистрирован.
        // NetworkTech ниже бакетирует его в "2G"/"3G"/"4G" тем же способом, что и
        // ModemInternetInfo::DrawMode() на самом ПУ28.
        [NotifyPropertyChangedFor(nameof(NetworkTech))]
        [ObservableProperty] public int networkAcT = -1;

        public string NetworkTech => NetworkAcT switch
        {
            0 or 1 or 3 or 8 => "2G",
            2 or 4 or 5 or 6 => "3G",
            7 or 9 => "4G",
            _ => "--",
        };

        // 0=idle, 1=busy, 2=done, 3=error - см. AutoRegisterCommand в ModemDeviceViewModel.cs.
        [NotifyPropertyChangedFor(nameof(AutoRegStatusText))]
        [ObservableProperty] public int autoRegStatus;

        public string AutoRegStatusText => AutoRegStatus switch
        {
            1 => GetString("t_autoreg_busy"),
            2 => GetString("t_autoreg_done"),
            3 => GetString("t_autoreg_error"),
            _ => GetString("t_autoreg_idle"),
        };

        // ── Настройки, которые пульт реально позволяет менять (ModemSettings.cpp) ──
        [ObservableProperty] public bool force2gOnly;
        [ObservableProperty] public bool allowRoaming;

        // ── Строки PGN61/62 (см. Omni.cs DecodeStringTransferData -> ApplyModemString) ──
        [ObservableProperty] public string imei = "";
        [ObservableProperty] public string operatorName = "";
        [ObservableProperty] public string lastSmsText = "";
        [ObservableProperty] public string lastSmsNum = "";
        [ObservableProperty] public string internetCheckUrl = "";
        [ObservableProperty] public string mqttBroker = "";
        [ObservableProperty] public string mqttLogin = "";
        [ObservableProperty] public string mqttPassword = "";
        [ObservableProperty] public string connectionLink = "";
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
