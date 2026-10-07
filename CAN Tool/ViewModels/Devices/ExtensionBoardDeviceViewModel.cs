namespace OmniProtocol
{
    public class ExtensionBoardDeviceViewModel : DeviceViewModel
    {
        public ExtensionBoardDeviceViewModel(DeviceId id) : base(id)
        {
            ExtensionBoard.LoadCommandRequested += SendLoadCommand;
        }

        public override ExtensionBoardViewModel ExtensionBoard { get; } = new();

        // Прошивка платы расширения применяет все три канала из каждого PGN 43 (маски «не менять» нет),
        // поэтому в сообщение всегда кладутся значения всех каналов; канал 4 в прошивке не используется.
        private void SendLoadCommand(int[] promille)
        {
            OmniMessage m = new();
            m.ReceiverId.Type = Id.Type;
            m.ReceiverId.Address = Id.Address;
            m.Pgn = 43;
            m.Data = new byte[] { 0xFF, 0xFF, 0xFF, 0xFF, 0xFF, 0xFF, 0xFF, 0xFF };
            for (var i = 0; i < 3; i++)
            {
                m.Data[i * 2] = (byte)(promille[i] >> 8);
                m.Data[i * 2 + 1] = (byte)(promille[i] & 0xFF);
            }
            Transmit(m.ToCanMessage());
        }
    }
}
