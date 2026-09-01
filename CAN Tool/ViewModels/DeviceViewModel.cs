using CAN_Tool;
using CAN_Tool.Libs;
using CAN_Tool.ViewModels;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.IO;
using System.Linq;
using System.Windows;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using static CAN_Tool.Libs.Helper;

namespace OmniProtocol
{
    public partial class DeviceViewModel : ObservableObject
    {
        public DeviceViewModel(DeviceId newId)
        {
            Id = new DeviceId(newId.Type, newId.Address);
            LogStart();

            if (Omni.Devices.TryGetValue(Id.Type, out var device))
                DeviceReference = device;
        }

        // ── Идентификация ──────────────────────────────────────────────
        [ObservableProperty] private DeviceId id;
        [ObservableProperty] private bool manualMode;
        [ObservableProperty] private bool secondMessages;
        public DeviceTemplate DeviceReference { get; }

        public string Name => ToString();

        public ImageSource Img
        {
            get
            {
                var imagesDir = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "Images");
                var imageName = DeviceReference?.ImageName;
                var path = !string.IsNullOrEmpty(imageName)
                    ? Path.Combine(imagesDir, $"{imageName}.jpg")
                    : null;
                if (path == null || !File.Exists(path))
                    path = Path.Combine(imagesDir, "noimg.jpg");
                return new BitmapImage(new Uri(path));
            }
        }

        // ── Версии ─────────────────────────────────────────────────────
        [ObservableProperty] private DateOnly productionDate;
        [ObservableProperty] public BindingList<int> serial = new() { 0, 0, 0 };
        [ObservableProperty] public BindingList<int> firmware = new() { 0, 0, 0, 0 };
        [ObservableProperty] public BindingList<int> bootFirmware = new() { 0, 0, 0, 0 };

        // ── Данные устройства ──────────────────────────────────────────
        public CommonParameters Parameters { get; set; } = new();
        public UpdatableList<StatusVariable> Status { get; } = new();
        public UpdatableList<ReadedParameter> ReadParameters { get; } = new();
        public UpdatableList<ReadedBlackBoxValue> BbValues { get; } = new();
        public BindingList<BbError> BbErrors { get; } = new();
        public bool[] SupportedVariables { get; } = new bool[200];

        // ── Специализированные данные (используются при обработке сообщений) ──
        public virtual Timberline20OmniViewModel TimberlineParams { get; } = new();
        public virtual ACInverterViewModel ACInverterParams { get; } = new();
        public virtual ModemViewModel ModemParams { get; } = new();
        public virtual GenericLoadTrippleViewModel GenericLoadTripple { get; } = new();
        public virtual OverrideStateClass OverrideState { get; } = new();

        public partial class OverrideStateClass : ObservableObject
        {
            [ObservableProperty] public bool blowerOverriden;
            [ObservableProperty] public bool fuelPumpOverriden;
            [ObservableProperty] public bool glowPlugOverriden;
            [ObservableProperty] public bool relayOverriden;
            [ObservableProperty] public bool pumpOverriden;
            [ObservableProperty] public int blowerOverridenRevs;
            [ObservableProperty] public int fuelPumpOverridenFrequencyX100;
            [ObservableProperty] public int glowPlugOverridenPower;
            [ObservableProperty] public bool relayOverridenState;
            [ObservableProperty] public bool pumpOverridenState;
        }

        // ── Лог ────────────────────────────────────────────────────────
        public List<double[]> LogData = new();
        [ObservableProperty] private bool isLogWriting = true;
        [ObservableProperty] private int logCurrentPos;

        private const int LogShortLength = 7200;   // 2 часа
        private const int LogLongLength  = 86400;  // 24 часа
        private const int LogExpandAt    = 6900;   // расширяем за ~5 минут до конца двухчасового буфера

        public void LogTick()
        {
            // Снимок истории раз в секунду для водопадной таблицы
            foreach (var sv in Status)
                sv.TakeHistorySnapshot();

            if (!IsLogWriting || LogData.Count == 0) return;

            if (LogData[0].Length == LogShortLength && LogCurrentPos >= LogExpandAt)
            {
                var expanded = new List<double[]>(LogData.Count);
                for (var i = 0; i < LogData.Count; i++)
                {
                    var newArr = new double[LogLongLength];
                    Array.Copy(LogData[i], newArr, LogCurrentPos);
                    expanded.Add(newArr);
                }
                LogData = expanded;
            }

            if (LogCurrentPos < LogData[0].Length)
            {
                foreach (var sv in Status)
                    if (sv.Id < LogData.Count)
                        LogData[sv.Id][LogCurrentPos] = sv.Value;
                LogCurrentPos++;
            }
            else
            {
                saveLog();
                LogStart();
            }
        }

        public void saveLog()
        {
            var path = Environment.GetFolderPath(Environment.SpecialFolder.Desktop) + "\\" +
                       DeviceReference.Name + "_" + DateTime.Now.ToString("HH-mm-ss_dd-MM-yy") + ".csv";
            using var sw = new StreamWriter(path);
            foreach (var v in Status)
                sw.Write(GetString($"vars_{v.Id}") + ";");
            sw.WriteLine();
            for (var i = 0; i < LogCurrentPos; i++)
            {
                foreach (var v in Status)
                    sw.Write(LogData[v.Id][i].ToString(v.AssignedParameter.OutputFormat) + ";");
                sw.WriteLine();
            }
            sw.Flush();
        }

        public void LogInit(int length = LogShortLength)
        {
            LogCurrentPos = 0;
            LogData = new List<double[]>();
            for (var i = 0; i < 300; i++)
                LogData.Add(new double[length]);
        }

        public void LogStart() { LogInit(); IsLogWriting = true; }
        public void LogStop() { IsLogWriting = false; LogData = new List<double[]>(); }

        // ── Флаги чтения данных (используются Omni.cs) ─────────────────
        public bool flagGetParamDone = false;
        public bool flagGetBbDone = false;
        public bool waitForBb = false;

        // ── Транспорт ──────────────────────────────────────────────────
        public void Transmit(CanMessage msg)
        {
            MainWindowViewModel.Instance?.CanAdapter.Transmit(msg);
        }

        public static void TransmitStatic(CanMessage msg)
        {
            MainWindowViewModel.Instance?.CanAdapter.Transmit(msg);
        }

        // Доступ к общей шине/списку устройств - используется командами загрузчика (см. ниже
        // и BootloaderDeviceViewModel), которым нужен не только Transmit, но и ConnectedDevices/
        // CurrentTask/SelectedConnectedDevice. Через кэшированный MainWindowViewModel.Instance,
        // а не Application.Current.MainWindow.DataContext - последнее обращается к Window
        // (DispatcherObject) и бросает исключение при вызове из фонового потока (а команды
        // загрузчика выполняются через Task.Run).
        protected static Omni Bus => MainWindowViewModel.Instance.OmniInstance;

        // ── Переход в загрузчик ────────────────────────────────────────
        // Единственная кнопка, которая остаётся на странице обычного устройства - всё
        // остальное (заливка hex, verify, дамп памяти) теперь живёт на странице загрузчика
        // (BootloaderDeviceViewModel), т.к. набор доступных операций зависит от конкретного
        // варианта загрузчика (см. Generation/IsPu28 там), а не только от типа устройства.
        //
        // Когда устройство переходит в загрузчик, оно физически меняет свой CAN-адрес на
        // Type=123, поэтому в ConnectedDevices оно появляется как СОВЕРШЕННО НОВАЯ запись -
        // текущий объект (this) её не увидит и не сможет забрать. PendingBootloaderOriginType/
        // PendingBootloaderOriginFirmware - способ передать данные об исходном устройстве в тот
        // будущий объект (см. RunAutoUpdate в BootloaderDeviceViewModel.cs).
        public static int? PendingBootloaderOriginType { get; private set; }
        public static BindingList<int> PendingBootloaderOriginFirmware { get; private set; }

        [RelayCommand]
        private void SwitchToBootLoader()
        {
            if (BootloaderAlreadyOnBus())
            {
                MessageBox.Show(GetString("t_bootloader_already_on_bus"), GetString("t_bootloader_conflict_title"),
                    MessageBoxButton.OK, MessageBoxImage.Warning);
                return;
            }
            if (VulnerableMbcOnBus(this))
            {
                MessageBox.Show(GetString("t_vulnerable_mbc_on_bus"), GetString("t_vulnerable_mbc_title"),
                    MessageBoxButton.OK, MessageBoxImage.Warning);
                return;
            }

            PendingBootloaderOriginType = Id.Type;
            PendingBootloaderOriginFirmware = new BindingList<int>(Firmware.ToList());

            OmniMessage msg = new();
            msg.Pgn = 1;
            msg.ReceiverId.Address = Id.Address;
            msg.ReceiverId.Type = Id.Type;
            msg.Data[0] = 0;
            msg.Data[1] = 22;
            msg.Data[2] = 0;
            Transmit(msg.ToCanMessage());
        }

        private static bool BootloaderAlreadyOnBus() => Bus.ConnectedDevices.Any(d => d.Id.Type == 123);

        // HCU (MBC-2, device type 125) firmware 125.0.0.5 - 125.0.0.15 has a CAN filter bug:
        // it wrongly accepts any frame addressed to ReceiverType 126 (Control device / remote
        // panel) as if it were its own, including the "enter bootloader" command. Flashing the
        // panel with such an MBC-2 on the bus makes both jump into the bootloader at once, and
        // the two bootloaders answering the flash protocol simultaneously bricks the flash.
        // Fixed properly in firmware (per-command target check) starting with 125.0.0.16.
        private const int VulnerableMbcDeviceType = 125;
        private const int VulnerableMbcMinBuild = 5;
        private const int VulnerableMbcMaxBuild = 15;

        // excludeDevice: the device actually being switched to bootloader is not itself a risk
        // (its own "enter bootloader" command is addressed to its own type, never to 126), and
        // must be excluded so that updating a vulnerable MBC-2 to fix it isn't blocked by itself.
        private static bool VulnerableMbcOnBus(DeviceViewModel excludeDevice) =>
            Bus.ConnectedDevices.Any(d =>
                d != excludeDevice &&
                d.Id.Type == VulnerableMbcDeviceType &&
                d.Firmware[3] >= VulnerableMbcMinBuild &&
                d.Firmware[3] <= VulnerableMbcMaxBuild);

        public void ExecuteCommand(int cmdNum, params byte[] data)
        {
            OmniMessage msg = new();
            msg.TransmitterId.Type = 126;
            msg.TransmitterId.Address = 6;
            msg.ReceiverId.Type = Id.Type;
            msg.ReceiverId.Address = Id.Address;
            msg.Pgn = 1;
            msg.Data = new byte[8];
            msg.Data[0] = (byte)(cmdNum >> 8);
            msg.Data[1] = (byte)(cmdNum & 0xFF);
            for (var i = 0; i < data.Length; i++)
                msg.Data[i + 2] = data[i];
            Transmit(msg.ToCanMessage());
        }

        public static void ExecuteCommandOnDevice(Tuple<DeviceId, int, byte[]> arg)
        {
            OmniMessage msg = new();
            msg.TransmitterId.Type = 126;
            msg.TransmitterId.Address = 6;
            msg.ReceiverId.Address = arg.Item1.Address;
            msg.ReceiverId.Type = arg.Item1.Type;
            msg.Pgn = 1;
            msg.Data = new byte[8];
            msg.Data[0] = (byte)(arg.Item2 >> 8);
            msg.Data[1] = (byte)(arg.Item2 & 0xFF);
            for (var i = 0; i < arg.Item3.Length; i++)
                msg.Data[i + 2] = arg.Item3[i];
            TransmitStatic(msg.ToCanMessage());
        }

        public bool WaitForFlag(ref bool flag, int delay)
        {
            var wd = 0;
            while (!flag && wd < delay) { wd++; System.Threading.Thread.Sleep(1); }
            if (!flag) { flag = false; return false; }
            flag = false;
            return true;
        }

        // ── Фабрика ────────────────────────────────────────────────────
        public static DeviceViewModel Create(DeviceId id)
        {
            if (!Omni.Devices.TryGetValue(id.Type, out var template))
                return new DeviceViewModel(id);

            return template.DevType switch
            {
                DeviceType_t.Binar or DeviceType_t.Planar => new HeaterDeviceViewModel(id),
                DeviceType_t.Hcu                          => new HcuDeviceViewModel(id),
                DeviceType_t.AcInverter                   => new AcInverterDeviceViewModel(id),
                DeviceType_t.AcPanel                      => new AcPanelDeviceViewModel(id),
                DeviceType_t.GenericLoadTripple            => new GenericLoadTrippleDeviceViewModel(id),
                DeviceType_t.PressureSensor               => new PressureSensorDeviceViewModel(id),
                DeviceType_t.BootLoader                   => new BootloaderDeviceViewModel(id),
                DeviceType_t.Modem                        => new ModemDeviceViewModel(id),
                _                                         => new DeviceViewModel(id),
            };
        }

        // ── Переопределения ────────────────────────────────────────────
        public override string ToString() =>
            DeviceReference != null
                ? $"{DeviceReference.Name}({Id.Address})"
                : $"Device #<{Id.Type}>({Id.Address})";

        public override bool Equals(object obj)
        {
            if (obj == null || obj.GetType() != typeof(DeviceViewModel)) return false;
            return Id.Equals((obj as DeviceViewModel).Id);
        }

        public override int GetHashCode() => Id.GetHashCode();
    }
}
