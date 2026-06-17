namespace OmniProtocol
{
    public class ModemDeviceViewModel : DeviceViewModel
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
    }
}
