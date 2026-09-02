using CommunityToolkit.Mvvm.Input;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using System.Windows;
using static CAN_Tool.Libs.Helper;

namespace OmniProtocol
{
    // Общий буфер одного фрагмента прошивки/дампа памяти (адрес + данные) - используется всеми
    // поколениями протокола загрузчика (Gen1/Gen2/Gen3/ПУ28). Публичный, т.к. используется в
    // сигнатурах protected/public членов DeviceViewModel (fragments, ParseHexFile, FlashFragments).
    public class CodeFragment
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

        // Уже находимся в загрузчике - переходить в него больше некуда (кнопка "To Bootloader"
        // из общего блока-шапки скрывается для этой страницы, см. CanEnterBootloader).
        public override bool CanEnterBootloader => false;

        // Id.Type тут всегда 123 (виртуальный тип загрузчика) - список прошивок с сервера
        // нужно запрашивать по исходному типу устройства, который был до перехода в загрузчик.
        public override int FirmwareQueryType => PendingBootloaderOriginType ?? Id.Type;

        // ── Поколение протокола / вариант ──────────────────────────────
        private const byte Gen1MaxBuild = 4;

        public int Generation => BootFirmware[3] <= Gen1MaxBuild ? 1 : BootFirmware[3] >= Gen3MinBootBuild ? 3 : 2;
        public bool IsPu28 => BootFirmware[0] == 123 && BootFirmware[2] == 3;

        // Единая точка входа для DeviceViewModel.RunAutoUpdate - выбирает нужный протокол по
        // фактическому поколению загрузчика и прошивает.
        public void FlashFragments(List<CodeFragment> frags)
        {
            switch (Generation)
            {
                case 1:
                    UpdateFirmwareOld(frags);
                    break;
                case 3:
                    UpdateFirmware110(frags);
                    break;
                default:
                    UpdateFirmware(frags);
                    break;
            }
        }

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

        // ── Версия / базовые команды (доступны на любом поколении) ─────
        // internal (не private) - DeviceViewModel.RunAutoUpdate вызывает эти методы напрямую,
        // а не через сгенерированный XxxCommand.Execute(): AsyncRelayCommand.Execute поднимает
        // CanExecuteChanged, который синхронно трогает Button.Command (DependencyProperty) -
        // а RunAutoUpdate выполняется в фоновом потоке (Task.Run), что бросает "The calling
        // thread cannot access this object because a different thread owns it".
        [RelayCommand]
        internal async Task RequestBootLoaderVersion()
        {
            OmniMessage msg = new();
            msg.Pgn = 6;
            msg.ReceiverId.Type = 123;
            msg.Data[0] = 0;
            msg.Data[1] = 18;
            Transmit(msg.ToCanMessage());
        }

        [RelayCommand]
        internal async Task SwitchToMainProgram()
        {
            OmniMessage msg = new();
            msg.Pgn = 1;
            msg.ReceiverId.Type = 123;
            msg.Data[0] = 0;
            msg.Data[1] = 22;
            msg.Data[2] = 1;
            Transmit(msg.ToCanMessage());

            // Формально загрузчик в этот момент перестаёт существовать на шине под Type=123
            // (устройство переходит обратно на свой родной тип/адрес) - убираем эту запись из
            // списка сразу же, не дожидаясь тайм-аута опроса.
            RunOnUi(() => Bus.ConnectedDevices.Remove(this));
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
                    Bus.CurrentTask.UpdatePercent(checkedBytes * 100 / totalBytes);
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

    }
}
