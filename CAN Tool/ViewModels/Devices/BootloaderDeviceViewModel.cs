using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using Microsoft.Win32;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using System.Windows;
using static CAN_Tool.Libs.Helper;

namespace OmniProtocol
{
    // Общий буфер одного фрагмента прошивки/дампа памяти (адрес + данные) - используется всеми
    // поколениями протокола загрузчика (Gen1/Gen2/Gen3/ПУ28).
    internal class CodeFragment
    {
        public CodeFragment(int len)
        {
            Data = new byte[len];
        }
        public uint StartAddress;
        public int Length;
        public byte[] Data;
    }

    // Страница устройства, находящегося в режиме загрузчика (Id.Type == 123 - см.
    // DeviceViewModel.Create/omnidata.json). Физический загрузчик всегда отвечает под этим
    // типом независимо от того, каким устройством он был до перехода в загрузчик, поэтому
    // единственный способ понять, какой именно это загрузчик и что он умеет - разобрать байты
    // версии BootFirmware:
    //   [0] = 123 (тип "загрузчик", всегда фиксирован)
    //   [1] = напряжение/вариант устройства
    //   [2] = подтип (3 = ПУ28 - есть управление внешней flash-микросхемой)
    //   [3] = номер сборки загрузчика (определяет поколение протокола, см. Generation)
    //
    // Код разложен по partial-файлам один в один со старыми #region из бывшего
    // FirmwarePageViewModel: .Gen1.cs (PGN100/101, сборка ≤4), .Gen2.cs (PGN105/106, всё
    // остальное), .Gen3.cs (PGN110/111, сборка ≥13), .Pu28.cs (внешняя flash, PGN107-109,
    // видна только когда IsPu28).
    public partial class BootloaderDeviceViewModel : DeviceViewModel
    {
        public BootloaderDeviceViewModel(DeviceId id) : base(id) { }

        // ── Поколение протокола / вариант ──────────────────────────────
        private const byte Gen1MaxBuild = 4;

        public int Generation => BootFirmware[3] <= Gen1MaxBuild ? 1 : BootFirmware[3] >= Gen3MinBootBuild ? 3 : 2;
        public bool IsPu28 => BootFirmware[0] == 123 && BootFirmware[2] == 3;

        // ── Флаги протокола фрагментов (общие для PGN105/Gen2 и PGN110/Gen3 - формат ответов
        // идентичен, см. Omni.cs.DecodeFragmentProtocolResponse) ───────────────────────────
        public bool flagEraseDone = false;
        public bool flagSetAdrDone = false;
        public bool flagProgramDone = false;
        public bool flagDataGetDone = false;
        public bool flagVerifyDone = false;
        public bool flagReadDone = false;
        public uint fragmentAddress = 0;
        public int receivedFragmentLength = 0;
        public uint receivedFragmentCrc = 0;
        public uint verifyResultCrc = 0;
        public bool verifyResultOk = false;
        public uint readResultData = 0;
        public bool readResultOk = false;

        // ── Лог ────────────────────────────────────────────────────────
        [ObservableProperty]
        private string log;

        private void LogWrite(string str)
        {
            Log = str + Log;
        }

        private void LogWriteLine(string str)
        {
            Log = str + Environment.NewLine + Log;
        }

        // ── Hex-файл (общий парсер и поле fragments - используются всеми поколениями) ──
        [ObservableProperty]
        private int fragmentSize = 512;

        private List<CodeFragment> fragments = new();
        private string lastHexFilePath = "";

        [RelayCommand]
        private void LoadHex()
        {
            OpenFileDialog dialog = new();
            dialog.Filter = "Hex Files|*.hex";
            if (!(bool)dialog.ShowDialog()) return;
            lastHexFilePath = dialog.FileName;
            fragments = ParseHexFile(lastHexFilePath, FragmentSize);
            LogWriteLine($"Hex is loaded, contains {fragments.Count} fragments.");
        }

        private List<CodeFragment> ParseHexFile(string path, int maxFragmentSize)
        {
            fragments.Clear();
            return ParseHexFile(path, maxFragmentSize, fragments);
        }

        private List<CodeFragment> ParseHexFile(string path, int maxFragmentSize, List<CodeFragment> target)
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
        // Также используется VerifyFirmwareBytes (ниже) - она сверяет побайтно, независимо от
        // поколения.
        private List<CodeFragment> ParseHexFileRaw(string path, int maxFragmentSize)
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

        // ── Версия / базовые команды (доступны на любом поколении) ─────
        [RelayCommand]
        private async Task RequestBootLoaderVersion()
        {
            OmniMessage msg = new();
            msg.Pgn = 6;
            msg.ReceiverId.Type = 123;
            msg.Data[0] = 0;
            msg.Data[1] = 18;
            Transmit(msg.ToCanMessage());
        }

        [RelayCommand]
        private async Task SwitchToMainProgram()
        {
            OmniMessage msg = new();
            msg.Pgn = 1;
            msg.ReceiverId.Type = 123;
            msg.Data[0] = 0;
            msg.Data[1] = 22;
            msg.Data[2] = 1;
            Transmit(msg.ToCanMessage());
        }

        [RelayCommand]
        private async Task GetVersion()
        {
            OmniMessage msg = new();
            msg.Pgn = 6;
            msg.ReceiverId.Address = Id.Address;
            msg.ReceiverId.Type = Id.Type;
            msg.Data[0] = 0;
            msg.Data[1] = 18;
            Transmit(msg.ToCanMessage());
        }

        // ── Verify bytes: побайтная сверка через чтение слов (PGN105/8). Работает на любом
        // поколении, в отличие от VerifyFirmware (CRC32), которая требует Gen3. Оригинальная
        // команда никогда не проверяла версию загрузчика - оставляем как есть. ──
        private bool ReadWordAtAddress(uint address, out uint data)
        {
            data = 0;
            OmniMessage msg = new()
            {
                Pgn = 105,
                ReceiverId = new(123, 0),
                Data =
                {
                    [0] = 8,
                    [1] = (byte)(address >> 24),
                    [2] = (byte)(address >> 16),
                    [3] = (byte)(address >> 8),
                    [4] = (byte)(address)
                }
            };

            for (var attempt = 0; attempt < 4; attempt++)
            {
                flagReadDone = false;
                Transmit(msg.ToCanMessage());
                if (!WaitForFlag(ref flagReadDone, 500)) continue;
                if (!readResultOk) return false;
                data = readResultData;
                return true;
            }
            return false;
        }

        private bool VerifyFlashOld(List<CodeFragment> rawFragmentsArg)
        {
            if (rawFragmentsArg == null || rawFragmentsArg.Count == 0)
            {
                LogWriteLine("Verify: no fragments");
                return false;
            }

            if (!Bus.CurrentTask.Capture("Verifying")) return false;
            LogWriteLine("=== Byte-by-byte verification ===");

            var totalBytes = rawFragmentsArg.Sum(f => f.Length);
            var checkedBytes = 0;
            var mismatchCount = 0;

            foreach (var f in rawFragmentsArg)
            {
                var wordCount = (f.Length + 3) / 4;
                for (var w = 0; w < wordCount; w++)
                {
                    if (Bus.CurrentTask.Cts.IsCancellationRequested)
                    {
                        Bus.CurrentTask.OnFail("Cancelled");
                        return false;
                    }

                    var addr = f.StartAddress + (uint)(w * 4);
                    if (!ReadWordAtAddress(addr, out var flashWord))
                    {
                        LogWriteLine($"Verify: no response reading 0x{addr:X08}");
                        Bus.CurrentTask.OnFail("Verification aborted: no response");
                        return false;
                    }

                    var byteOffset = w * 4;
                    uint expectedWord = 0;
                    for (var b = 0; b < 4; b++)
                    {
                        var byteVal = (byteOffset + b < f.Length) ? f.Data[byteOffset + b] : (byte)0xFF;
                        expectedWord |= (uint)byteVal << (b * 8);
                    }

                    if (flashWord != expectedWord)
                    {
                        LogWriteLine($"  [!] 0x{addr:X08}: hex=0x{expectedWord:X08}  flash=0x{flashWord:X08}");
                        mismatchCount++;
                    }

                    checkedBytes += Math.Min(4, f.Length - byteOffset);
                    Bus.CurrentTask.PercentComplete = checkedBytes * 100 / totalBytes;
                }
            }

            if (mismatchCount == 0)
                LogWriteLine($"=== Verification passed: {totalBytes} bytes, no mismatches ===");
            else
                LogWriteLine($"=== Verification done: {totalBytes} bytes, {mismatchCount} mismatch(es) ===");

            Bus.CurrentTask.OnDone();
            return mismatchCount == 0;
        }

        [RelayCommand]
        private void VerifyFirmwareBytes()
        {
            if (string.IsNullOrEmpty(lastHexFilePath))
            {
                MessageBox.Show(GetString("t_load_hex_first"));
                return;
            }
            var rawFragments = ParseHexFileRaw(lastHexFilePath, FragmentSize);
            Task.Run(() => VerifyFlashOld(rawFragments));
        }

        // ── Авто-обновление ──────────────────────────────────────────────
        [RelayCommand]
        private void AutoUpdateFirmware()
        {
            Task.Run(() => RunAutoUpdate());
        }

        private void RunAutoUpdate()
        {
            try
            {
                // Всегда запрашиваем hex при нажатии AUTO, даже если он уже был загружен ранее -
                // иначе повторное нажатие AUTO для другого устройства на шине молча прошивает
                // его тем же файлом, что и предыдущее устройство, и может окирпичить его.
                bool loaded = false;
                Application.Current.Dispatcher.Invoke(() =>
                {
                    OpenFileDialog dialog = new() { Filter = "Hex Files|*.hex" };
                    if ((bool)dialog.ShowDialog())
                    {
                        lastHexFilePath = dialog.FileName;
                        fragments = ParseHexFile(lastHexFilePath, FragmentSize);
                        loaded = fragments.Count > 0;
                    }
                });
                if (!loaded) return;

                // Проверяем версию прошиваемого hex против версии ПО, которое было на устройстве
                // непосредственно перед переходом в загрузчик (PendingBootloaderOriginFirmware,
                // см. DeviceViewModel.SwitchToBootLoader) - защита от прошивки бинарником другого
                // изделия (например, PU28 в MBC-2). Раньше это делалось непосредственно перед
                // самим переходом в загрузчик (когда устройство ещё отвечало под своим родным
                // адресом) - теперь переход и заливка hex разнесены по разным страницам, поэтому
                // сравниваем с сохранённым тогда снимком версии, а не с живым опросом устройства.
                if (!ConfirmVersionMatch()) return;

                // Запрашиваем версию загрузчика и ждём ответа (страница и так уже выбирает
                // Generation по последней известной версии, но лишний раз освежить не помешает)
                LogWriteLine(GetString("t_auto_waiting_bootloader"));
                _ = RequestBootLoaderVersion();
                Thread.Sleep(500);

                // Прошиваем (выбор протокола по фактическому поколению загрузчика)
                switch (Generation)
                {
                    case 1:
                        UpdateFirmwareOld(fragments);
                        break;
                    case 3:
                        UpdateFirmware110(fragments);
                        break;
                    default:
                        UpdateFirmware(fragments);
                        break;
                }

                // Возврат в основную программу
                _ = SwitchToMainProgram();

                var originalDeviceType = PendingBootloaderOriginType ?? -1;
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
                MessageBox.Show(ex.Message);
            }
        }

        // Имя файла прошивки по соглашению начинается с версии, напр.
        // "126.0.4.19_STM_Main.hex" (см. также BrowseSlotHex в .Pu28.cs). Порядок байт совпадает
        // с BootFirmware/Firmware: [0]=тип изделия, [1]=напряжение/вариант, [2]=подтип, [3]=сборка.
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

        // Сверяет версию, зашитую в имя выбранного hex-файла, с версией ПО, которое реально
        // работало на устройстве прямо перед переходом в загрузчик. Если первые 2 байта версии
        // (тип изделия и подтип/вариант) не совпадают - это явный признак, что выбран бинарник от
        // другого изделия, и такую прошивку легко можно окирпичить. Если версия имени файла не
        // распознана или версия устройства ещё неизвестна, сравнение пропускается - молча
        // проходить дальше без спроса не даём только в случае явного расхождения.
        private bool ConfirmVersionMatch()
        {
            if (!TryParseVersionFromFileName(lastHexFilePath, out var hexVersion)) return true;

            var curVersion = PendingBootloaderOriginFirmware;
            if (curVersion == null || (curVersion[0] == 0 && curVersion[1] == 0)) return true; // версия устройства неизвестна

            if (curVersion[0] == hexVersion[0] && curVersion[1] == hexVersion[1]) return true;

            var curStr = string.Join(".", curVersion);
            var hexStr = string.Join(".", hexVersion);
            var result = MessageBox.Show(
                string.Format(GetString("t_auto_version_mismatch"), curStr, hexStr),
                GetString("t_auto_version_mismatch_title"),
                MessageBoxButton.YesNo, MessageBoxImage.Warning);
            return result == MessageBoxResult.Yes;
        }
    }
}
