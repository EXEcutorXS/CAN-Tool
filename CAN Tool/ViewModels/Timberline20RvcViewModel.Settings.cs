using CAN_Tool.Libs;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using RVC;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using System.Windows;
using static CAN_Tool.Libs.Helper;

namespace CAN_Tool.ViewModels
{
    // Настройки MBC-2 по RV-C: проприетарный пакет 0xC0 (аналог PGN 7 внутренней шины).
    //   запрос (DGN 0xEF00 + SA устройства): D0 = 0xC0, D1 = команда, D2..D3 = номер параметра, D4..D7 = сырое значение (старший байт первым)
    //   ответ:                               D0 = 0xC1, D1 = команда, D2..D3 = номер, D4..D7 = значение
    // Команды: 0 - стереть сектор настроек, 1 - записать параметр в ОЗУ, 2 - записать все параметры во flash, 3 - прочитать параметр.
    // Ответ приходит на 0xEF00/0x1EF00 + свой SA или + SA запросившего (зависит от параметров MBC-2 604/605) - принимаем любой
    // проприетарный DGN от SA устройства.
    public partial class Timberline20RvcViewModel
    {
        private const byte PropSettings = 0xC0;
        private const byte PropSettingsAnswer = 0xC1;
        private const byte CmdErase = 0;
        private const byte CmdWriteRam = 1;
        private const byte CmdSaveFlash = 2;
        private const byte CmdRead = 3;
        private const int MaxParameterId = 700;           // как в Omni.ReadAllParameters: ~600 параметров + запас
        private const int AnswerTimeoutMs = 150;
        private const int Attempts = 3;

        public UpdatableList<ReadedParameter> SettingsParameters { get; } = new();

        private string settingsStatus = "";
        public string SettingsStatus { get => settingsStatus; set => SetProperty(ref settingsStatus, value); }

        private int settingsProgress;
        public int SettingsProgress { get => settingsProgress; set => SetProperty(ref settingsProgress, value); }

        private bool settingsBusy;
        public bool SettingsBusy
        {
            get => settingsBusy;
            set { if (SetProperty(ref settingsBusy, value)) OnPropertyChanged(nameof(SettingsIdle)); }
        }
        public bool SettingsIdle => !settingsBusy;

        private int singleParameterId;
        public int SingleParameterId
        {
            get => singleParameterId;
            set { if (SetProperty(ref singleParameterId, value)) OnPropertyChanged(nameof(SingleParameterName)); }
        }
        public string SingleParameterName => GotResource($"par_{SingleParameterId}") ? GetString($"par_{SingleParameterId}") : "";

        private uint singleParameterValue;
        public uint SingleParameterValue { get => singleParameterValue; set => SetProperty(ref singleParameterValue, value); }

        private TaskCompletionSource<uint> pendingAnswer;
        private byte pendingCommand;
        private ushort pendingNumber;
        private CancellationTokenSource settingsCts;

        // Вызывается из ProcessMesage. true - пакет был ответом на команду настроек.
        private bool ProcessSettingsAnswer(RvcMessage msg)
        {
            if ((msg.Dgn & 0xFF00) != 0xEF00) return false;          // 0xEF00 + DA и 0x1EF00 + DA
            if (msg.SourceAdress != SaToRequest) return false;
            var D = msg.Data;
            if (D[0] != PropSettingsAnswer) return false;

            var command = D[1];
            var number = (ushort)((D[2] << 8) | D[3]);
            var value = ((uint)D[4] << 24) | ((uint)D[5] << 16) | ((uint)D[6] << 8) | D[7];
            var pending = pendingAnswer;
            if (pending != null && command == pendingCommand && (number == pendingNumber || command == CmdErase || command == CmdSaveFlash))
                pending.TrySetResult(value);
            return true;
        }

        private void SendSettingsCommand(byte command, ushort number, uint value)
        {
            RvcMessage msg = new();
            msg.Dgn = (int)(0xEF00 + SaToRequest);
            msg.Priority = 6;
            msg.Data[0] = PropSettings;
            msg.Data[1] = command;
            msg.Data[2] = (byte)(number >> 8);
            msg.Data[3] = (byte)number;
            msg.Data[4] = (byte)(value >> 24);
            msg.Data[5] = (byte)(value >> 16);
            msg.Data[6] = (byte)(value >> 8);
            msg.Data[7] = (byte)value;
            NeedToTransmit?.Invoke(this, new NeedToTransmitEventArgs() { msgToTransmit = msg.ToCanMessage() });
        }

        // Команда с ожиданием ответа и повторами. null - устройство не ответило.
        private async Task<uint?> SettingsExchange(byte command, ushort number, uint value = 0xFFFFFFFF, int timeoutMs = AnswerTimeoutMs)
        {
            for (var attempt = 0; attempt < Attempts; attempt++)
            {
                var tcs = new TaskCompletionSource<uint>(TaskCreationOptions.RunContinuationsAsynchronously);
                pendingCommand = command;
                pendingNumber = number;
                pendingAnswer = tcs;
                SendSettingsCommand(command, number, value);
                var finished = await Task.WhenAny(tcs.Task, Task.Delay(timeoutMs));
                pendingAnswer = null;
                if (finished == tcs.Task) return tcs.Task.Result;
            }
            return null;
        }

        private bool BeginSettingsOperation()
        {
            if (SettingsBusy) return false;
            SettingsBusy = true;
            SettingsProgress = 0;
            settingsCts = new CancellationTokenSource();
            return true;
        }

        private void EndSettingsOperation(string status)
        {
            SettingsStatus = status;
            SettingsBusy = false;
            settingsCts?.Dispose();
            settingsCts = null;
        }

        private string NoAnswerText => string.Format(GetString("t_rvc_settings_no_answer"), SaToRequest);

        [RelayCommand]
        private async Task ReadAllSettings()
        {
            if (!BeginSettingsOperation()) return;
            SettingsParameters.Clear();
            SettingsStatus = GetString("t_rvc_settings_reading");
            var read = 0;
            for (var id = 0; id < MaxParameterId; id++)
            {
                if (settingsCts.IsCancellationRequested) { EndSettingsOperation(GetString("t_rvc_settings_cancelled")); return; }
                var value = await SettingsExchange(CmdRead, (ushort)id);
                if (value == null) { EndSettingsOperation(NoAnswerText); return; }
                if (value != 0xFFFFFFFF)                           // 0xFFFFFFFF - такого параметра нет
                {
                    SettingsParameters.TryToAdd(new ReadedParameter { Id = id, Value = value.Value });
                    read++;
                }
                SettingsProgress = (id + 1) * 100 / MaxParameterId;
            }
            EndSettingsOperation(string.Format(GetString("t_rvc_settings_read_done"), read));
        }

        // Записывает в ОЗУ все параметры таблицы, затем сохраняет во flash. Устройство отвечает значением после записи:
        // если оно отличается (вне диапазона, гистерезис уставок) - в таблице остаётся значение устройства.
        [RelayCommand]
        private async Task WriteAllSettings()
        {
            if (SettingsParameters.Count == 0) { SettingsStatus = GetString("t_rvc_settings_read_first"); return; }
            if (!BeginSettingsOperation()) return;
            SettingsStatus = GetString("t_rvc_settings_writing");
            var list = SettingsParameters.ToList();
            var corrected = 0;
            for (var i = 0; i < list.Count; i++)
            {
                if (settingsCts.IsCancellationRequested) { EndSettingsOperation(GetString("t_rvc_settings_cancelled")); return; }
                var p = list[i];
                var answer = await SettingsExchange(CmdWriteRam, (ushort)p.Id, p.Value);
                if (answer == null) { EndSettingsOperation(NoAnswerText); return; }
                if (answer.Value != p.Value) { p.Value = answer.Value; corrected++; }
                SettingsProgress = (i + 1) * 100 / list.Count;
            }
            if (await SettingsExchange(CmdSaveFlash, 0xFFFF, 0xFFFFFFFF, 1000) == null) { EndSettingsOperation(NoAnswerText); return; }
            EndSettingsOperation(string.Format(GetString("t_rvc_settings_write_done"), list.Count, corrected));
        }

        [RelayCommand]
        private async Task SaveSettingsToFlash()
        {
            if (!BeginSettingsOperation()) return;
            var answer = await SettingsExchange(CmdSaveFlash, 0xFFFF, 0xFFFFFFFF, 1000);
            EndSettingsOperation(answer == null ? NoAnswerText : GetString("t_rvc_settings_saved"));
        }

        [RelayCommand]
        private async Task EraseSettings()
        {
            if (MessageBox.Show(GetString("t_rvc_settings_erase_confirm"), GetString("t_rvc_settings_title"),
                                MessageBoxButton.YesNo, MessageBoxImage.Warning) != MessageBoxResult.Yes)
                return;
            if (!BeginSettingsOperation()) return;
            var answer = await SettingsExchange(CmdErase, 0xFFFF, 0xFFFFFFFF, 2000);
            if (answer != null) SettingsParameters.Clear();               // значения в таблице больше не актуальны
            EndSettingsOperation(answer == null ? NoAnswerText : GetString("t_rvc_settings_erased"));
        }

        [RelayCommand]
        private async Task ReadSingleSetting()
        {
            if (!BeginSettingsOperation()) return;
            var id = (ushort)SingleParameterId;
            var answer = await SettingsExchange(CmdRead, id);
            if (answer == null) { EndSettingsOperation(NoAnswerText); return; }
            if (answer.Value == 0xFFFFFFFF) { EndSettingsOperation(string.Format(GetString("t_rvc_settings_unknown"), id)); return; }
            SingleParameterValue = answer.Value;
            SettingsParameters.TryToAdd(new ReadedParameter { Id = id, Value = answer.Value });
            EndSettingsOperation(string.Format(GetString("t_rvc_settings_single_read"), id, answer.Value));
        }

        [RelayCommand]
        private async Task WriteSingleSetting()
        {
            if (!BeginSettingsOperation()) return;
            var id = (ushort)SingleParameterId;
            var answer = await SettingsExchange(CmdWriteRam, id, SingleParameterValue);
            if (answer == null) { EndSettingsOperation(NoAnswerText); return; }
            if (answer.Value == 0xFFFFFFFF) { EndSettingsOperation(string.Format(GetString("t_rvc_settings_unknown"), id)); return; }
            var requested = SingleParameterValue;
            SingleParameterValue = answer.Value;
            SettingsParameters.TryToAdd(new ReadedParameter { Id = id, Value = answer.Value });
            EndSettingsOperation(answer.Value == requested
                ? string.Format(GetString("t_rvc_settings_single_written"), id, answer.Value)
                : string.Format(GetString("t_rvc_settings_single_corrected"), id, requested, answer.Value));
        }

        [RelayCommand]
        private void CancelSettings() => settingsCts?.Cancel();
    }
}
