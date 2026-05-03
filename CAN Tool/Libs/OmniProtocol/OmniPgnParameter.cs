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

    public class PgnClass
    {
        public int id;
        public string name = "";
        public bool multiPack;
        public List<OmniPgnParameter> parameters = new();
    }

    public class ConfigPreset : ObservableObject
    {
        public int DeviceType;
        public string VendorName;
        public string ModelName;
        public BindingList<Tuple<int, uint>> ParamList = new();

    }


    public class OmniPgnParameter
    {
        public int Pgn { get; set; }  // PGN this parameter belongs to
        internal int StartByte;   //РќР°С‡Р°Р»СЊРЅС‹Р№ Р±Р°Р№С‚ РІ РїР°РєРµС‚Рµ
        internal int StartBit;    //РќР°С‡Р°Р»СЊРЅС‹Р№ Р±РёС‚ РІ Р±Р°Р№С‚Рµ
        internal int BitLength;   //Р”Р»РёРЅР° РїР°СЂР°РјРµС‚СЂР° РІ Р±РёС‚Р°С…
        internal bool Signed; //Р§РёСЃР»Рѕ СЃРѕ Р·РЅР°РєРѕРј
        public string Name { set; get; }     //РРјСЏ РїР°СЂР°РјРµС‚СЂР°
        internal double a = 1;         //РєРѕСЌС„С„РёС†РёРµРЅС‚ РїСЂРёРІРµРґРµРЅРёСЏ
        internal double b = 0;         //СЃРјРµС‰РµРЅРёРµ

        public UnitType_t UnitT { get; set; } = UnitType_t.None;

        internal Dictionary<int, string> Meanings { set; get; } = new();
        internal Func<int, string> GetMeaning; //РџСЂРёРЅРёРјР°РµС‚ РЅР° РІС…РѕРґ СЃС‹СЂРѕРµ Р·РЅР°С‡РµРЅРёРµ, РІРѕР·РІСЂР°С‰Р°РµС‚ СЃС‚СЂРѕРєСѓ СЃ СЂР°СЃС€РёС„СЂРѕРІРєРѕР№ Р·РЅР°С‡РµРЅРёСЏ РїР°СЂР°РјРµС‚СЂР°
        internal Func<byte[], string> CustomDecoder; //Р•СЃР»Рё РґР»СЏ РґРµРєРѕРґРёСЂРѕРІР°РЅРёСЏ РЅСѓР¶РµРЅ РІРµСЃСЊ РїР°РєРµС‚ РґР°РЅРЅС‹С…
        internal int? PackNumber; //РќРѕРјРµСЂ РїР°РєРµС‚Р° РІ РјСѓР»СЊС‚РёРїР°РєРµС‚Рµ
        internal int Var; //РЎРѕРѕС‚РІРµС‚СЃС‚РІСѓСЋС‰Р°СЏ РїРµСЂРµРјРµРЅРЅР°СЏ РёР· paramsName.h
        public double DefaultValue; //Р”Р»СЏ РєРѕРЅСЃС‚СЂСѓРєС‚РѕСЂР° РєРѕРјРјР°РЅРґ
        public bool AnswerOnly; //РџСЂРёСЃСѓС‚СЃС‚РІСѓРµС‚ С‚РѕР»СЊРєРѕ РІ РѕС‚РІРµС‚Рµ, РЅРµ Р·Р°РґР°С‘С‚СЃСЏ РІ РєРѕРјРјР°РЅРґРµ

        public string OutputFormat
        {
            get
            {
                if (UnitT == UnitType_t.Temp && App.Settings.UseImperial)
                    return "0.0"; // For correct farenheit display
                else
                {
                    if (a >= 1)
                        return "0";
                    else if (a >= 0.1)
                        return "0.0";
                    else if (a >= 0.01)
                        return "0.00";
                    else
                        return "0.000";
                }
            }
        }

        public string Unit
        {
            get
            {
                switch (UnitT)
                {
                    case UnitType_t.None: return "";
                    case UnitType_t.Temp:
                        if (App.Settings.UseImperial)
                            return "°F";
                        else
                            return "°C";
                    case UnitType_t.Volt: return GetString("u_voltage");
                    case UnitType_t.Percent: return "%";
                    case UnitType_t.Flow:
                        if (App.Settings.UseImperial)
                            return GetString("u_hal_per_minute");
                        else
                            return GetString("u_litre_per_minute");
                    case UnitType_t.Current: return GetString("u_ampere");
                    case UnitType_t.Rpm: return GetString("u_rpm");
                    case UnitType_t.Pressure:
                        if (App.Settings.UseImperial)
                            return "PSI";
                        else
                            return GetString("u_kpa");
                    default: return "";
                }
            }
        }

        public string Decode(long rawValue)
        {
            StringBuilder retString = new();
            retString.Append(Name + ": ");
            if (CustomDecoder != null)
                return "Custom decoders not supported";

            if (GetMeaning != null)
                return GetMeaning((int)rawValue);
            if (Meanings != null && Meanings.ContainsKey((int)rawValue))
                retString.Append(rawValue.ToString() + " - " + GetString(Meanings[(int)rawValue]));
            else
            {
                if (rawValue == Math.Pow(2, BitLength) - 1)
                    retString.Append(GetString("t_no_data") + $" ({rawValue})");
                else
                {
                    double rawDouble = rawValue;
                    if (Signed && rawValue > Math.Pow(2, BitLength - 1))
                        rawDouble *= -1;
                    var value = ImperialConverter(rawDouble * a + b, UnitT);


                    retString.Append(value.ToString(OutputFormat) + " " + Unit);
                }
            }
            return retString.ToString();
        }

        public override string ToString() => Name;
    }

    public partial class OmniCommand : ObservableObject
    {
        [ObservableProperty] private int id;

        public string Name => GetString("c_" + Id.ToString());

        public List<OmniPgnParameter> Parameters { get; } = new();

        public override string ToString() => Name;

    }

}
