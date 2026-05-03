using System;
using CAN_Tool.Libs;
using CommunityToolkit.Mvvm.ComponentModel;
using static CAN_Tool.Libs.Helper;


namespace OmniProtocol
{

    public class DeviceTemplate
    {
        public int Id;
        public string Name => GetString($"d_{Id}");
        public DeviceType_t DevType { set; get; }

        public int MaxBlower { get; set; } = 130; //Максимальное значение скорости нагнетателя
        public double MaxFuelPump { get; set; } = 4; //Максимальное значение ТН
        public int BBErrorsLen { get; set; } = 512; //Длина ЧЯ для ошибок
        public string ImageName { get; set; } = string.Empty;

        public override string ToString() => Name;
    }

    public partial class ReadedBlackBoxValue : ObservableObject, IUpdatable<ReadedBlackBoxValue>, IComparable
    {
        [ObservableProperty] private int id;

        [ObservableProperty] private uint value;

        public void Update(ReadedBlackBoxValue item) => Value = item.Value;

        public bool IsSimiliarTo(ReadedBlackBoxValue item) => Id == item.Id;

        public int CompareTo(object obj) => Id - (obj as ReadedBlackBoxValue).Id;

        public string Description => GetString($"bb_{Id}");
    }

}
