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
using static Omni;
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
        internal int StartByte;   //Начальный байт в пакете
        internal int StartBit;    //Начальный бит в байте
        internal int BitLength;   //Длина параметра в битах
        internal bool Signed; //Число со знаком
        public string Name { set; get; }     //Имя параметра
        internal double a = 1;         //коэффициент приведения
        internal double b = 0;         //смещение

        public UnitType UnitT { get; set; } = UnitType.None;

        internal Dictionary<int, string> Meanings { set; get; } = new();
        internal Func<int, string> GetMeaning; //Принимает на вход сырое значение, возвращает строку с расшифровкой значения параметра
        internal Func<byte[], string> CustomDecoder; //Если для декодирования нужен весь пакет данных
        internal int PackNumber; //Номер пакета в мультипакете
        internal int Var; //Соответствующая переменная из paramsName.h
        public double DefaultValue; //Для конструктора комманд
        public bool AnswerOnly; //Присутствует только в ответе, не задаётся в комманде

        public string OutputFormat
        {
            get
            {
                if (UnitT == UnitType.Temp && App.Settings.UseImperial)
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
                    case UnitType.None: return "";
                    case UnitType.Temp:
                        if (App.Settings.UseImperial)
                            return "°F";
                        else
                            return "°C";
                    case UnitType.Volt: return GetString("u_voltage");
                    case UnitType.Percent: return "%";
                    case UnitType.Flow:
                        if (App.Settings.UseImperial)
                            return GetString("u_hal_per_minute");
                        else
                            return GetString("u_litre_per_minute");
                    case UnitType.Current: return GetString("u_ampere");
                    case UnitType.Rpm: return GetString("u_rpm");
                    case UnitType.Pressure:
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
