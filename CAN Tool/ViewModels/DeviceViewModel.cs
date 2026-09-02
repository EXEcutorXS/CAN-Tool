using CAN_Tool;
using CAN_Tool.Libs;
using CAN_Tool.ViewModels;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using Microsoft.Win32;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.IO;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
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

            Task.Run(RefreshAvailableServerFirmwares);
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

        // ── Прошивка с сервера (multihot.online, см. Libs/OnlineFirmwareService.cs) ──
        // Тип, по которому запрашивается список версий: обычно это Id.Type устройства, но на
        // странице загрузчика (BootloaderDeviceViewModel) Id.Type всегда 123 - там
        // переопределяется на исходный тип устройства (PendingBootloaderOriginType).
        public virtual int FirmwareQueryType => Id.Type;

        // Псевдо-версия в начале списка - выбрана по умолчанию. Означает "источник - локальный
        // hex-файл, не сервер": и ComboBox всегда показывает этот вариант, и AutoUpdateFirmware
        // (см. RunAutoUpdate) по нему понимает, что нужно спросить файл диалогом, как раньше,
        // а не качать с сервера.
        private static string FromFileOption => GetString("t_firmware_from_file");

        public ObservableCollection<string> AvailableServerFirmwares { get; } = new();
        [ObservableProperty] private string selectedServerFirmware;

        private void RefreshAvailableServerFirmwares()
        {
            var versions = OnlineFirmwareService.GetVersionsAsync(FirmwareQueryType).GetAwaiter().GetResult();
            RunOnUi(() =>
            {
                AvailableServerFirmwares.Clear();
                AvailableServerFirmwares.Add(FromFileOption);
                foreach (var v in versions)
                    AvailableServerFirmwares.Add(v);
                SelectedServerFirmware = FromFileOption;
            });
        }

        // Выбор версии в комбобоксе - как нажатие "Load hex", только источник не файл, а
        // сервер: скачивает бинарник, строит fragments из него напрямую (без Intel HEX -
        // сервер publish'ит уже готовый плоский .bin + адрес начала прошивки), и подставляет
        // синтетическое имя файла в ожидаемом ConfirmVersionMatch формате "<version>_....",
        // чтобы сверка версии (см. ConfirmVersionMatch) продолжала работать как обычно. Нужно и
        // для того, чтобы после выбора версии сразу были доступны кнопки "Прошить" конкретного
        // поколения на странице загрузчика (не только Auto Update, который качает версию заново
        // сам - см. RunAutoUpdate).
        partial void OnSelectedServerFirmwareChanged(string value)
        {
            if (string.IsNullOrEmpty(value) || value == FromFileOption) return;
            var queryType = FirmwareQueryType;
            Task.Run(() => LoadServerFirmware(queryType, value));
        }

        private bool LoadServerFirmware(int deviceType, string version)
        {
            try
            {
                LogWriteLine($"Downloading firmware {version} from server...");
                var (data, flashBase) = OnlineFirmwareService.DownloadFirmwareAsync(deviceType, version).GetAwaiter().GetResult();
                lastHexFilePath = $"{version}_server.hex";
                fragments = BuildFragmentsFromBinary(data, flashBase, FragmentSize);
                LogWriteLine($"Firmware {version} loaded from server, contains {fragments.Count} fragments.");
                return fragments.Count > 0;
            }
            catch (Exception ex)
            {
                LogWriteLine($"Failed to load firmware {version} from server: {ex.Message}");
                MessageBox.Show(ex.Message);
                return false;
            }
        }

        private static List<CodeFragment> BuildFragmentsFromBinary(byte[] data, uint baseAddress, int maxFragmentSize)
        {
            var result = new List<CodeFragment>();
            for (var offset = 0; offset < data.Length; offset += maxFragmentSize)
            {
                var len = Math.Min(maxFragmentSize, data.Length - offset);
                var fragment = new CodeFragment(maxFragmentSize) { StartAddress = baseAddress + (uint)offset, Length = len };
                Array.Copy(data, offset, fragment.Data, 0, len);
                result.Add(fragment);
            }
            return result;
        }

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

        // ── Лог ────────────────────────────────────────────────────────
        // Общий для всех устройств: страница загрузчика показывает его в текстовом поле, у
        // остальных устройств просто накапливается невидимо. Пишут в него методы, выполняющиеся
        // в фоновом потоке (Task.Run - AutoUpdateFirmware/UpdateFirmware*/EraseFlash* и т.д.),
        // а WPF-биндинг требует, чтобы изменение UI-привязанного свойства происходило в потоке,
        // которому принадлежит Dispatcher - иначе "The calling thread cannot access this object
        // because a different thread owns it".
        [ObservableProperty]
        private string log;

        protected static void RunOnUi(Action action)
        {
            var dispatcher = Application.Current?.Dispatcher;
            if (dispatcher == null || dispatcher.CheckAccess())
                action();
            else
                dispatcher.Invoke(action);
        }

        protected void LogWrite(string str) => RunOnUi(() => Log = str + Log);

        protected void LogWriteLine(string str) => RunOnUi(() => Log = str + Environment.NewLine + Log);

        // ── Hex-файл (общий парсер - используется всеми поколениями загрузчика) ─────────────
        [ObservableProperty]
        private int fragmentSize = 512;

        protected List<CodeFragment> fragments = new();
        protected string lastHexFilePath = "";

        [RelayCommand]
        private void LoadHex()
        {
            OpenFileDialog dialog = new();
            dialog.Filter = "Hex Files|*.hex";
            if (!(bool)dialog.ShowDialog()) return;
            if (!ValidateFirmwareFileName(dialog.FileName)) return;
            lastHexFilePath = dialog.FileName;
            fragments = ParseHexFile(lastHexFilePath, FragmentSize);
            LogWriteLine($"Hex is loaded, contains {fragments.Count} fragments.");
        }

        protected List<CodeFragment> ParseHexFile(string path, int maxFragmentSize)
        {
            fragments.Clear();
            return ParseHexFile(path, maxFragmentSize, fragments);
        }

        protected List<CodeFragment> ParseHexFile(string path, int maxFragmentSize, List<CodeFragment> target)
        {
            // Не логируем сюда построчно/пофрагментно: LogWriteLine делает Log = str + Log,
            // то есть каждый вызов копирует весь накопленный лог целиком - для файла на
            // несколько тысяч фрагментов (например, дамп в 1МБ при 256-байтных фрагментах)
            // это O(n^2) и превращает загрузку в дело на десятки секунд. Итоговое количество
            // фрагментов логируется один раз в LoadHex/LoadDumpHex после завершения парсинга.
            void AddFragment(CodeFragment fragment)
            {
                target.Add(fragment);
            }

            CodeFragment currentFragment = new(maxFragmentSize);
            uint pageAddress = 0;

            if (path is not { Length: > 0 }) return null;
            uint lastLineAddress = 0;
            using StreamReader sr = new(path);
            while (!sr.EndOfStream)
            {
                var line = sr.ReadLine()?[1..];
                var bytes = new byte[60];
                for (var i = 0; i < line?.Length / 2; i++)
                    bytes[i] = Convert.ToByte(line.Substring(i * 2, 2), 16);
                int recordLen = bytes[0];

                var lastLineSize = recordLen;
                switch (bytes[3])
                {
                    case 0:
                        var localAddress = (uint)(bytes[1] * 256 + bytes[2]);
                        if ((pageAddress + localAddress != lastLineAddress + lastLineSize) && (lastLineAddress != 0)) //Current line is not just after previous, fragment must be divided
                        {
                            AddFragment(currentFragment);
                            currentFragment = new CodeFragment(maxFragmentSize);
                        }
                        if (currentFragment.Length == 0) //First line in data fragment, saving address
                        {
                            // Length==0, not StartAddress==0: a fragment whose true start address
                            // is exactly 0 (the very first fragment of a file starting at 0x000000)
                            // would otherwise be indistinguishable from "not yet set", so every
                            // following line at address 0, 16, 32... would keep overwriting
                            // StartAddress - firmware then wrote a full 256-byte page starting
                            // mid-page, and the flash's own page-program wrap corrupted the data.
                            currentFragment.StartAddress = pageAddress + localAddress;
                        }
                        lastLineAddress = pageAddress + localAddress;
                        var gotNotReserveData = false;

                        for (var i = 0; i < recordLen; i++)
                        {
                            if (bytes[i + 4] == 0xff) continue;
                            gotNotReserveData = true;
                            break;
                        }

                        if (gotNotReserveData)
                            for (var i = 0; i < recordLen; i++)
                            {
                                currentFragment.Data[currentFragment.Length++] = bytes[i + 4];
                                if (currentFragment.Length != maxFragmentSize) continue;
                                AddFragment(currentFragment);
                                currentFragment = new CodeFragment(maxFragmentSize);
                            }

                        break;
                    case 4:
                        if (currentFragment.Length != 0)
                        {
                            AddFragment(currentFragment);
                            currentFragment = new CodeFragment(maxFragmentSize);
                        }
                        pageAddress = (uint)(bytes[4] * 256 + bytes[5]) << 16;
                        lastLineAddress = 0;
                        break;
                    case 1:
                        if (currentFragment.Length > 0)
                            AddFragment(currentFragment);
                        return target;

                }
            }
            return target;
        }

        // Parses hex file including ALL bytes (even 0xFF).
        // Required for old (Gen1) bootloader: its write pointer advances for every received
        // byte, so skipping reserved 0xFF blocks would misalign subsequent data in flash.
        // Также используется VerifyFirmwareBytes - она сверяет побайтно, независимо от поколения.
        protected List<CodeFragment> ParseHexFileRaw(string path, int maxFragmentSize)
        {
            var result = new List<CodeFragment>();
            CodeFragment current = new(maxFragmentSize);
            uint pageAddress = 0;
            uint lastLineAddress = 0;

            if (string.IsNullOrEmpty(path)) return result;
            using StreamReader sr = new(path);
            while (!sr.EndOfStream)
            {
                var line = sr.ReadLine()?[1..];
                var bytes = new byte[60];
                for (var i = 0; i < line?.Length / 2; i++)
                    bytes[i] = Convert.ToByte(line.Substring(i * 2, 2), 16);

                int recordLen = bytes[0];
                switch (bytes[3])
                {
                    case 0:
                        var localAddr = (uint)(bytes[1] * 256 + bytes[2]);
                        uint absAddr = pageAddress + localAddr;
                        if (lastLineAddress != 0 && absAddr != lastLineAddress + (uint)recordLen)
                        {
                            if (current.Length > 0) { result.Add(current); current = new(maxFragmentSize); }
                        }
                        if (current.Length == 0) // see ParseHexFile for why not StartAddress==0
                            current.StartAddress = absAddr;
                        lastLineAddress = absAddr;
                        for (var i = 0; i < recordLen; i++)
                        {
                            current.Data[current.Length++] = bytes[i + 4];
                            if (current.Length == maxFragmentSize)
                            { result.Add(current); current = new(maxFragmentSize); }
                        }
                        break;
                    case 4:
                        if (current.Length > 0) { result.Add(current); current = new(maxFragmentSize); }
                        pageAddress = (uint)(bytes[4] * 256 + bytes[5]) << 16;
                        lastLineAddress = 0;
                        break;
                    case 1:
                        if (current.Length > 0) result.Add(current);
                        return result;
                }
            }
            return result;
        }

        // ── Переход в загрузчик / авто-обновление ──────────────────────
        // Единая кнопка на странице обычного устройства и на странице загрузчика: если
        // устройство ещё не в загрузчике - сама переводит его туда и ждёт появления; если уже
        // в загрузчике (страница BootloaderDeviceViewModel) - просто перепрошивает. Раньше это
        // было разнесено (Switch to bootloader отдельно от Auto Update), но общий блок
        // изображение/версия/серийник/дата теперь один на всех страницах, и там нужны обе
        // кнопки сразу.
        //
        // Когда устройство переходит в загрузчик, оно физически меняет свой CAN-адрес на
        // Type=123, поэтому в ConnectedDevices оно появляется как СОВЕРШЕННО НОВАЯ запись -
        // текущий объект (this) её не увидит и не сможет забрать. PendingBootloaderOriginType/
        // PendingBootloaderOriginFirmware - способ передать данные об исходном устройстве в тот
        // будущий объект.
        public static int? PendingBootloaderOriginType { get; private set; }
        public static BindingList<int> PendingBootloaderOriginFirmware { get; private set; }

        // На странице загрузчика (BootloaderDeviceViewModel) переопределяется в false - там уже
        // некуда "входить".
        public virtual bool CanEnterBootloader => true;

        [RelayCommand]
        private void SwitchToBootLoader() => TrySwitchToBootLoader();

        private bool TrySwitchToBootLoader()
        {
            if (BootloaderAlreadyOnBus())
            {
                MessageBox.Show(GetString("t_bootloader_already_on_bus"), GetString("t_bootloader_conflict_title"),
                    MessageBoxButton.OK, MessageBoxImage.Warning);
                return false;
            }
            if (VulnerableMbcOnBus(this))
            {
                MessageBox.Show(GetString("t_vulnerable_mbc_on_bus"), GetString("t_vulnerable_mbc_title"),
                    MessageBoxButton.OK, MessageBoxImage.Warning);
                return false;
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

            // Формально устройство в этот момент перестаёт существовать на шине под своим
            // текущим адресом/типом (оно физически меняет их на Type=123) - убираем его из
            // списка сразу, не дожидаясь тайм-аута опроса. Когда оно объявится как загрузчик,
            // появится новая отдельная запись (см. Omni.ProcessOmniMessage).
            RunOnUi(() => Bus.ConnectedDevices.Remove(this));
            return true;
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
        private const int VulnerableMbcMaxBuild = 16;

        // excludeDevice: the device actually being switched to bootloader is not itself a risk
        // (its own "enter bootloader" command is addressed to its own type, never to 126), and
        // must be excluded so that updating a vulnerable MBC-2 to fix it isn't blocked by itself.
        private static bool VulnerableMbcOnBus(DeviceViewModel excludeDevice) =>
            Bus.ConnectedDevices.Any(d =>
                d != excludeDevice &&
                d.Id.Type == VulnerableMbcDeviceType &&
                d.Firmware[3] >= VulnerableMbcMinBuild &&
                d.Firmware[3] <= VulnerableMbcMaxBuild);

        [RelayCommand]
        private void AutoUpdateFirmware()
        {
            Task.Run(() => RunAutoUpdate());
        }

        private void RunAutoUpdate()
        {
            try
            {
                // Источник прошивки решает комбобокс: "Из файла" (по умолчанию) - как раньше,
                // диалог выбора hex-файла каждый раз заново (иначе повторное нажатие AUTO для
                // другого устройства на шине молча прошивает его тем же файлом, что и
                // предыдущее устройство, и может окирпичить его); любая реальная версия -
                // скачиваем именно её с сервера (тоже заново, той же логики ради).
                bool loaded;
                var selectedVersion = SelectedServerFirmware;
                if (string.IsNullOrEmpty(selectedVersion) || selectedVersion == FromFileOption)
                {
                    loaded = false;
                    Application.Current.Dispatcher.Invoke(() =>
                    {
                        OpenFileDialog dialog = new() { Filter = "Hex Files|*.hex" };
                        if ((bool)dialog.ShowDialog() && ValidateFirmwareFileName(dialog.FileName))
                        {
                            lastHexFilePath = dialog.FileName;
                            fragments = ParseHexFile(lastHexFilePath, FragmentSize);
                            loaded = fragments.Count > 0;
                        }
                    });
                }
                else
                {
                    loaded = LoadServerFirmware(FirmwareQueryType, selectedVersion);
                }
                if (!loaded) return;

                BootloaderDeviceViewModel bootDev;
                int originalDeviceType;

                if (this is BootloaderDeviceViewModel selfAsBoot)
                {
                    // Уже находимся на странице загрузчика - переводить некуда, сразу прошиваем.
                    bootDev = selfAsBoot;
                    originalDeviceType = PendingBootloaderOriginType ?? -1;
                }
                else
                {
                    // Обычное устройство - переводим его в загрузчик сами и ждём, пока оно
                    // появится в списке устройств уже как BootloaderDeviceViewModel (см.
                    // TrySwitchToBootLoader/DeviceViewModel.Create).
                    if (!TrySwitchToBootLoader()) return;
                    originalDeviceType = Id.Type;

                    LogWriteLine(GetString("t_auto_waiting_bootloader"));
                    DeviceViewModel found = null;
                    for (var i = 0; i < 150; i++) // 15 сек
                    {
                        Thread.Sleep(100);
                        Application.Current.Dispatcher.Invoke(() =>
                            found = Bus.ConnectedDevices.FirstOrDefault(d => d.Id.Type == 123));
                        if (found != null) break;
                    }

                    if (found is not BootloaderDeviceViewModel foundBoot)
                    {
                        LogWriteLine(GetString("t_auto_bootloader_timeout"));
                        return;
                    }
                    bootDev = foundBoot;
                    Application.Current.Dispatcher.Invoke(() => Bus.SelectedConnectedDevice = bootDev);
                }

                // Запрашиваем версию загрузчика и ждём ответа - от неё зависит, каким
                // протоколом (Generation) будем прошивать. Вызываем сам метод, а не
                // RequestBootLoaderVersionCommand.Execute() - см. комментарий у объявления
                // метода в BootloaderDeviceViewModel.cs про фоновый поток и CanExecuteChanged.
                _ = bootDev.RequestBootLoaderVersion();
                Thread.Sleep(500);

                bootDev.FlashFragments(fragments);

                // Возврат в основную программу
                _ = bootDev.SwitchToMainProgram();

                if (originalDeviceType < 0) return;

                // Ждём повторного появления исходного устройства и переизбираем его в списке
                LogWriteLine(GetString("t_auto_waiting_device"));
                DeviceViewModel originalDev = null;
                for (var i = 0; i < 150; i++) // 15 сек
                {
                    Thread.Sleep(100);
                    Application.Current.Dispatcher.Invoke(() =>
                        originalDev = Bus.ConnectedDevices.FirstOrDefault(d => d.Id.Type == originalDeviceType));
                    if (originalDev != null) break;
                }

                if (originalDev == null)
                    LogWriteLine(GetString("t_auto_device_timeout"));
                else
                {
                    Application.Current.Dispatcher.Invoke(() => Bus.SelectedConnectedDevice = originalDev);
                    LogWriteLine(GetString("t_auto_done"));
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show(ex.ToString());
            }
        }

        // Имя файла прошивки по соглашению начинается с версии, напр.
        // "126.0.4.19_STM_Main.hex" (см. также BrowseSlotHex в BootloaderDeviceViewModel.Pu28.cs).
        // Порядок байт совпадает с BootFirmware/Firmware: [0]=тип изделия, [1]=напряжение/вариант,
        // [2]=подтип, [3]=сборка.
        private static bool TryParseVersionFromFileName(string path, out byte[] version)
        {
            version = null;
            if (string.IsNullOrEmpty(path)) return false;
            var candidate = Path.GetFileNameWithoutExtension(path).Split('_')[0];
            var parts = candidate.Split('.');
            if (parts.Length != 4 || !parts.All(p => byte.TryParse(p, out _))) return false;
            version = parts.Select(byte.Parse).ToArray();
            return true;
        }

        // Проверяет выбранный hex-файл перед тем, как разрешить его грузить в fragments -
        // жёсткая блокировка (не "уверены?"), т.к. прошивка от другого изделия может
        // окирпичить устройство. Имя файла должно быть по соглашению ("<4 байта версии
        // через точку>_..."), а первые 2 байта (тип изделия и подтип/вариант) должны совпадать
        // с версией реально работавшего ПО: если мы ещё не в загрузчике - берём текущую живую
        // Firmware (это устройство и есть то самое изделие); если уже в загрузчике - берём
        // снимок версии, сделанный в момент перехода (PendingBootloaderOriginFirmware), т.к.
        // текущая Firmware у объекта-загрузчика не отражает исходное устройство. Если версия
        // устройства ещё неизвестна, сравнение пропускается - иначе любой первый выбор файла
        // для только что появившегося устройства блокировался бы просто из-за отсутствия данных
        // для сравнения.
        private bool ValidateFirmwareFileName(string path)
        {
            if (!TryParseVersionFromFileName(path, out var hexVersion))
            {
                MessageBox.Show(
                    string.Format(GetString("t_firmware_name_invalid"), Path.GetFileName(path)),
                    GetString("t_firmware_mismatch_title"),
                    MessageBoxButton.OK, MessageBoxImage.Error);
                return false;
            }

            var curVersion = this is BootloaderDeviceViewModel ? PendingBootloaderOriginFirmware : Firmware;
            if (curVersion == null || (curVersion[0] == 0 && curVersion[1] == 0)) return true; // версия устройства пока неизвестна

            if (curVersion[0] == hexVersion[0] && curVersion[1] == hexVersion[1]) return true;

            var curStr = string.Join(".", curVersion);
            var hexStr = string.Join(".", hexVersion);
            MessageBox.Show(
                string.Format(GetString("t_firmware_version_mismatch"), hexStr, curStr),
                GetString("t_firmware_mismatch_title"),
                MessageBoxButton.OK, MessageBoxImage.Error);
            return false;
        }

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

        public override bool Equals(object obj) => obj is DeviceViewModel other && Id.Equals(other.Id);

        public override int GetHashCode() => Id.GetHashCode();
    }
}
