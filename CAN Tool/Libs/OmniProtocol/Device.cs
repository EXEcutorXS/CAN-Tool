using CAN_Tool.Libs;
using CAN_Tool.ViewModels;
using CAN_Tool;
using CommunityToolkit.Mvvm.ComponentModel;
using OmniProtocol;
using ScottPlot;
using System.Collections.Generic;
using System;
using System.ComponentModel;
using System.Diagnostics;
using System.Diagnostics.CodeAnalysis;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Threading;
using System.Windows.Media;
using static CAN_Tool.Libs.Helper;
using System.Windows;
using CommunityToolkit.Mvvm.Input;
using System.IO;
using Newtonsoft;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using System.Collections.Specialized;
using ScottPlot.WPF;
using System.Collections.ObjectModel;
using System.Windows.Markup;


namespace OmniProtocol
{

    public class DeviceTemplate
    {
        public int Id;
        public string Name => GetString($"d_{Id}");
        public DeviceType_t DevType { set; get; }

        public int MaxBlower { get; set; } = 130; //РњР°РєСЃРёРјР°Р»СЊРЅРѕРµ Р·РЅР°С‡РµРЅРёРµ СЃРєРѕСЂРѕСЃС‚Рё РЅР°РіРЅРµС‚Р°С‚РµР»СЏ
        public double MaxFuelPump { get; set; } = 4; //РњР°РєСЃРёРјР°Р»СЊРЅРѕРµ Р·РЅР°С‡РµРЅРёРµ РўРќ
        public int BBErrorsLen { get; set; } = 512; //Р”Р»РёРЅР° Р§РЇ РґР»СЏ РѕС€РёР±РѕРє
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
