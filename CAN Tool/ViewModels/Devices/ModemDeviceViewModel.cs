using CommunityToolkit.Mvvm.Input;

namespace OmniProtocol
{
    public partial class ModemDeviceViewModel : DeviceViewModel
    {
        public ModemDeviceViewModel(DeviceId id) : base(id) { }

        public void ToggleOnlySmsMode() => SendSettingFlag(0, !ModemParams.OnlySmsMode);
        public void ToggleFaultReport() => SendSettingFlag(2, !ModemParams.FaultReport);
        public void ToggleCmdAck()      => SendSettingFlag(4, !ModemParams.CmdAck);

        // PGN 60, sub-packet 1: 4 флага x 2 бита (00=off,01=on,11=без изменений).
        // Меняем только нужное поле, остальные оставляем "11" — те же поля, что и в статусе.
        private void SendSettingFlag(int bitShift, bool value)
        {
            byte d1 = 0b11111111;
            d1 &= (byte)~(0b11 << bitShift);
            d1 |= (byte)((value ? 1 : 0) << bitShift);

            var msg = new OmniMessage
            {
                Pgn = 60,
                ReceiverId = Id,
                Data = new byte[] { 1, d1, 0xFF, 0xFF, 0xFF, 0xFF, 0xFF, 0xFF }
            };
            Transmit(msg.ToCanMessage());
        }

        // Force2gOnly (D[2]) / AllowRoaming (D[3]) - плоские байты вне 2-битной схемы D[1] (там
        // больше нет свободных пар бит) - см. ModemSettings::SendByteSetting в
        // C:\source\PU28-Timberline\User\Activity\ModemSettings.cpp. 0xFF = "без изменений".
        [RelayCommand]
        private void ToggleForce2gOnly() => SendByteSetting(2, !ModemParams.Force2gOnly);

        [RelayCommand]
        private void ToggleAllowRoaming() => SendByteSetting(3, !ModemParams.AllowRoaming);

        private void SendByteSetting(int byteIndex, bool value)
        {
            var data = new byte[] { 1, 0xFF, 0xFF, 0xFF, 0xFF, 0xFF, 0xFF, 0xFF };
            data[byteIndex] = (byte)(value ? 1 : 0);

            var msg = new OmniMessage { Pgn = 60, ReceiverId = Id, Data = data };
            Transmit(msg.ToCanMessage());
        }

        // PGN1, (D[0]<<8)+D[1]==30 - см. ModemInternetInfo::SendAutoRegisterTrigger в
        // C:\source\PU28-Timberline\User\Activity\ModemInternetInfo.cpp и
        // Modem::startAutoRegister() на стороне модема.
        [RelayCommand]
        private void AutoRegister() => ExecuteCommand(30);
    }
}
