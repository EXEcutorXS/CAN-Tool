using CommunityToolkit.Mvvm.ComponentModel;
using static CAN_Tool.Libs.Helper;

namespace OmniProtocol
{
    public enum StringEncoding_t { Ascii = 0, Utf8 = 1, Utf16 = 2, Win1251 = 3 }

    // Одна собираемая/собранная строка протокола PGN61/62 (см. Omni.cs
    // DecodeStringTransferAnnounce/DecodeStringTransferData, а также прошивку
    // C:\source\PU28-Timberline\User\Can\StringTransfer.h). Sender - тот, кто реально передаёт
    // байты строки (шлёт анонс PGN61 D[0]=1 и последующие пакеты PGN62), Receiver - адрес,
    // которому это адресовано. Text перестраивается заново при каждом новом пакете данных, так
    // что в UI видно, как строка собирается по кусочкам.
    public partial class StringTransferEntry : ObservableObject
    {
        public StringTransferEntry(DeviceId sender, DeviceId receiver, int stringId)
        {
            Sender = sender;
            Receiver = receiver;
            StringId = stringId;
        }

        public DeviceId Sender { get; }
        public DeviceId Receiver { get; }
        public int StringId { get; }

        // Человекочитаемое назначение StringId (IMEI, пароль модема и т.п.) - см.
        // Omni.StringIds/omnidata.json "stringIds". Числовой id сам по себе ничего не говорит:
        // таблица общая для модема и пульта (см. StringId enum в StringTransfer.h), а не часть
        // общего протокола OmniProtocol PGN, поэтому это отдельный справочник, а не Pgns/Commands.
        public string StringName => Omni.StringIds.TryGetValue(StringId, out var key) ? GetString(key) : StringId.ToString();

        [ObservableProperty] private StringEncoding_t encoding;
        [ObservableProperty] private string text = "";

        // Буфер сборки - выделяется по длине из анонса (PGN61) либо расширяется по факту
        // приходящих пакетов, если анонс не был виден. Не показывается в UI напрямую, только Text.
        internal byte[] Buffer = System.Array.Empty<byte>();

        // Длина строки из анонса (PGN61 D[4-5]) - это ровно strlen() на стороне отправителя (см.
        // StringTransfer.cpp::beginSend), без нуль-терминатора. -1, если анонс не был виден.
        // Важно не путать это с длиной Buffer: последний пакет PGN62 всегда несёт полные 5 байт,
        // и отправитель забивает байты сверх DeclaredLength значением 0xFF (см.
        // StringTransfer.cpp - uint8_t d[5] = {0xFF,...}) - если решить, что "строка = весь
        // буфер", в Text попадёт от 0 до 4 лишних символов '?' (0xFF в ASCII/Win1251 не валиден)
        // в зависимости от остатка DeclaredLength по модулю 5.
        internal int DeclaredLength = -1;
    }
}
