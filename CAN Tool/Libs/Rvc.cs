using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Runtime.Serialization;
using System.IO;
using Can_Adapter;
using System.Formats.Asn1;
using System.Text.Json;
using System.Windows.Controls;
using CAN_Tool.ViewModels.Base;
using CAN_Tool.ViewModels;
using System.ComponentModel;
using OmniProtocol;

namespace RVC
{
    static class RVC
    {
        static RVC()
        {

        }

        public static void SeedData()
        {
            DGN newDgn;

            newDgn = new DGN(true) { Dgn = 0x1FF9C, Name = "THERMOSTAT_AMBIENT_STATUS" };
            newDgn.Parameters.Add(new Parameter { Name = "Ambient temperature", ShortName = "Tamb", Type = parameterType.temperature, Size = parameterSize.uint16, firstByte = 1 });
            DGNs.Add(newDgn.Dgn, newDgn);

            newDgn = new DGN(true) { Dgn = 0x1FFE4, Name = "FURNACE_STATUS" };

            newDgn.Parameters.Add(new Parameter
            {
                Name = "Operating mode",
                ShortName = "Op mode",
                Type = parameterType.list,
                Size = parameterSize.uint2,
                firstByte = 1,
                Meanings = { [0] = "Automatic", [1] = "Manual" }
            });
            newDgn.Parameters.Add(new Parameter
            {
                Name = "Heat Source",
                ShortName = "Heat Src",
                Type = parameterType.list,
                Size = parameterSize.uint6,
                firstByte = 1,
                firstBit = 2,
                Meanings = { [0] = "Combustion", [1] = "AC power primary", [2] = "AC power secondary", [3] = "Engine Heat" }
            });

            newDgn.Parameters.Add(new Parameter { Name = "Circulation fan speed", ShortName = "Fan%", Type = parameterType.percent, firstByte = 2, firstBit = 0 });
            newDgn.Parameters.Add(new Parameter { Name = "Heat output level", ShortName = "Heat%", Type = parameterType.percent, firstByte = 3, firstBit = 0 });
            newDgn.Parameters.Add(new Parameter { Name = "Dead band", ShortName = "Db", Type = parameterType.custom, firstByte = 4, coefficient = 0.1, Unit = "C" });
            newDgn.Parameters.Add(new Parameter { Name = "Dead band level 2", ShortName = "Db2", Type = parameterType.custom, firstByte = 5, coefficient = 0.1, Unit = "C" });
            newDgn.Parameters.Add(new Parameter { Name = "Zone overcurrent status", ShortName = "Zone OC", Type = parameterType.boolean, Size = parameterSize.uint2, firstByte = 6, firstBit = 0 });
            newDgn.Parameters.Add(new Parameter { Name = "Zone undercurrent status", ShortName = "Zone UC", Type = parameterType.boolean, Size = parameterSize.uint2, firstByte = 6, firstBit = 2 });
            newDgn.Parameters.Add(new Parameter
            {
                Name = "Zone temperature status",
                ShortName = "Zone t Status",
                Type = parameterType.boolean,
                Size = parameterSize.uint2,
                firstByte = 6,
                firstBit = 4,
                Meanings = { [0] = "Normal", [1] = "Warning" }
            });
            newDgn.Parameters.Add(new Parameter
            {
                Name = "Zone analog input status",
                ShortName = "Zone an",
                Type = parameterType.boolean,
                Size = parameterSize.uint2,
                firstByte = 6,
                firstBit = 6,
                Meanings = { [0] = "Off(Inactive)", [1] = "On(Active)" }
            });
            DGNs.Add(newDgn.Dgn, newDgn);

            newDgn = new DGN(true) { Dgn = 0x1FFE3, Name = "FURNACE_COMMAND" };

            newDgn.Parameters.Add(new Parameter
            {
                Name = "Operating mode",
                ShortName = "Op mode",
                Type = parameterType.list,
                Size = parameterSize.uint2,
                firstByte = 1,
                Meanings = { [0] = "Automatic", [1] = "Manual" }
            });
            newDgn.Parameters.Add(new Parameter
            {
                Name = "Heat Source",
                ShortName = "Heat Src",
                Type = parameterType.list,
                Size = parameterSize.uint6,
                firstByte = 1,
                firstBit = 2,
                Meanings = { [0] = "Combustion", [1] = "AC power primary", [2] = "AC power secondary", [3] = "Engine Heat" }
            });

            newDgn.Parameters.Add(new Parameter { Name = "Circulation fan speed", ShortName = "Fan%", Type = parameterType.percent, firstByte = 2, firstBit = 0 });
            newDgn.Parameters.Add(new Parameter { Name = "Heat output level", ShortName = "Heat%", Type = parameterType.percent, firstByte = 3, firstBit = 0 });
            newDgn.Parameters.Add(new Parameter { Name = "Dead band", ShortName = "Db", Type = parameterType.custom, firstByte = 4, coefficient = 0.1, Unit = "C" });
            newDgn.Parameters.Add(new Parameter { Name = "Dead band level 2", ShortName = "Db2", Type = parameterType.custom, firstByte = 5, coefficient = 0.1, Unit = "C" });

            DGNs.Add(newDgn.Dgn, newDgn);

            newDgn = new DGN(true) { Dgn = 0x1FFF7, Name = "WATERHEATER_STATUS" };

            newDgn.Parameters.Add(new Parameter
            {
                Name = "Operating modes",
                ShortName = "Mode",
                Type = parameterType.list,
                Size = parameterSize.uint8,
                firstByte = 1,
                Meanings = { [0] = "off", [1] = "combustion", [2] = "electric", [3] = "gas/electric (both)", [4] = "test combustion (forced on)", [5] = "test combustion (forced on)", [6] = "test electric (forced on)" }
            });

            newDgn.Parameters.Add(new Parameter { Name = "Set point temperature", ShortName = "SP T", Type = parameterType.temperature, Size = parameterSize.uint16, firstByte = 2 });

            newDgn.Parameters.Add(new Parameter { Name = "Water temperature", ShortName = "Twater", Type = parameterType.temperature, Size = parameterSize.uint16, firstByte = 4 });

            newDgn.Parameters.Add(new Parameter
            {
                Name = "Thermostat status",
                Type = parameterType.boolean,
                Size = parameterSize.uint2,
                firstByte = 6,
                firstBit = 0,
                Meanings = { [0] = "set point met", [1] = "set point not met (heat is being applied)" }
            });

            newDgn.Parameters.Add(new Parameter
            {
                Name = "Burner status",
                Type = parameterType.boolean,
                Size = parameterSize.uint2,
                firstByte = 6,
                firstBit = 2,
                Meanings = { [0] = "off", [1] = "burner is lit" }
            });

            newDgn.Parameters.Add(new Parameter
            {
                Name = "AC element status",
                Type = parameterType.boolean,
                Size = parameterSize.uint2,
                firstByte = 6,
                firstBit = 4,
                Meanings = { [0] = "AC element is inactive", [1] = "AC element is active)" }
            });


            newDgn.Parameters.Add(new Parameter
            {
                Name = "High temperature limit switch status",
                Type = parameterType.boolean,
                Size = parameterSize.uint2,
                firstByte = 6,
                firstBit = 6,
                Meanings = { [0] = "limit switch not tripped", [1] = "limit switch tripped" }
            });

            newDgn.Parameters.Add(new Parameter
            {
                Name = "Failure to ignite status",
                Type = parameterType.boolean,
                Size = parameterSize.uint2,
                firstByte = 7,
                firstBit = 0,
                Meanings = { [0] = "no failure", [1] = "device has failed to ignite" }
            });

            newDgn.Parameters.Add(new Parameter
            {
                Name = "AC power failure status",
                Type = parameterType.boolean,
                Size = parameterSize.uint2,
                firstByte = 7,
                firstBit = 2,
                Meanings = { [0] = "AC power present", [1] = "AC power not present" }
            });

            newDgn.Parameters.Add(new Parameter
            {
                Name = "DC power failure status",
                Type = parameterType.boolean,
                Size = parameterSize.uint2,
                firstByte = 7,
                firstBit = 4,
                Meanings = { [0] = "DC power present", [1] = "DC power not present" }
            });

            newDgn.Parameters.Add(new Parameter
            {
                Name = "DC power warning status",
                Type = parameterType.boolean,
                Size = parameterSize.uint2,
                firstByte = 7,
                firstBit = 6,
                Meanings = { [0] = "DC power sufficient", [1] = "DC power warning" }
            });

            DGNs.Add(newDgn.Dgn, newDgn);

            newDgn = new DGN(true) { Dgn = 0x1FFF6, Name = "WATERHEATER_COMMAND" };

            newDgn.Parameters.Add(new Parameter
            {
                Name = "Operating modes",
                ShortName = "Mode",
                Type = parameterType.list,
                Size = parameterSize.uint8,
                firstByte = 1,
                Meanings = { [0] = "off", [1] = "combustion", [2] = "electric", [3] = "gas/electric (both)", [4] = "test combustion (forced on)", [5] = "test combustion (forced on)", [6] = "test electric (forced on)" }
            });

            newDgn.Parameters.Add(new Parameter { Name = "Set point temperature", ShortName = "SP T", Type = parameterType.temperature, Size = parameterSize.uint16, firstByte = 2 });
            newDgn.Parameters.Add(new Parameter { Name = "Electric Element Level", ShortName = "Elec lvl", Type = parameterType.natural, Size = parameterSize.uint4, firstByte = 6 });

            DGNs.Add(newDgn.Dgn, newDgn);

            newDgn = new DGN(true) { Dgn = 0x1FFE2, Name = "THERMOSTAT_STATUS_1" };

            newDgn.Parameters.Add(new Parameter
            {
                Name = "Operating mode",
                Type = parameterType.list,
                Size = parameterSize.uint4,
                firstByte = 1,
                Meanings = { [0] = "Off", [1] = "Cool", [2] = "Heat", [3] = "Auto heat/Cool", [4] = "Fan only", [5] = "Aux heat", [6] = "Window Defrost/Dehumidify" }
            });

            newDgn.Parameters.Add(new Parameter
            {
                Name = "Fan mode",
                Type = parameterType.boolean,
                Size = parameterSize.uint2,
                firstByte = 1,
                firstBit = 4,
                Meanings = { [0] = "Auto", [1] = "On" }
            });

            newDgn.Parameters.Add(new Parameter
            {
                Name = "Schedule mode",
                Type = parameterType.boolean,
                Size = parameterSize.uint2,
                firstByte = 1,
                firstBit = 6,
                Meanings = { [0] = "Disabled", [1] = "Enabled" }
            });

            newDgn.Parameters.Add(new Parameter
            {
                Name = "Fan speed",
                Type = parameterType.percent,
                Size = parameterSize.uint8,
                firstByte = 2,
            });

            newDgn.Parameters.Add(new Parameter
            {
                Name = "Setopint temp - Heat",
                Type = parameterType.temperature,
                Size = parameterSize.uint16,
                firstByte = 3,
            });

            newDgn.Parameters.Add(new Parameter
            {
                Name = "Setopint temp - Cool",
                Type = parameterType.temperature,
                Size = parameterSize.uint16,
                firstByte = 5,
            });

            DGNs.Add(newDgn.Dgn, newDgn);


            newDgn = new DGN(true) { Dgn = 0x1FEF9, Name = "THERMOSTAT_COMMAND_1" };

            newDgn.Parameters.Add(new Parameter
            {
                Name = "Operating mode",
                Type = parameterType.list,
                Size = parameterSize.uint4,
                firstByte = 1,
                Meanings = { [0] = "Off", [1] = "Cool", [2] = "Heat", [3] = "Auto heat/Cool", [4] = "Fan only", [5] = "Aux heat", [6] = "Window Defrost/Dehumidify" }
            });

            newDgn.Parameters.Add(new Parameter
            {
                Name = "Fan mode",
                Type = parameterType.boolean,
                Size = parameterSize.uint2,
                firstByte = 1,
                firstBit = 4,
                Meanings = { [0] = "Auto", [1] = "On" }
            });

            newDgn.Parameters.Add(new Parameter
            {
                Name = "Schedule mode",
                Type = parameterType.boolean,
                Size = parameterSize.uint2,
                firstByte = 1,
                firstBit = 6,
                Meanings = { [0] = "Disabled", [1] = "Enabled" }
            });

            newDgn.Parameters.Add(new Parameter
            {
                Name = "Fan speed",
                Type = parameterType.percent,
                Size = parameterSize.uint8,
                firstByte = 2,
            });

            newDgn.Parameters.Add(new Parameter
            {
                Name = "Setopint temp - Heat",
                Type = parameterType.temperature,
                Size = parameterSize.uint16,
                firstByte = 3,
            });

            newDgn.Parameters.Add(new Parameter
            {
                Name = "Setopint temp - Cool",
                Type = parameterType.temperature,
                Size = parameterSize.uint16,
                firstByte = 5,
            });

            DGNs.Add(newDgn.Dgn, newDgn);


            newDgn = new DGN(true) { Dgn = 0x1FEFA, Name = "THERMOSTAT_STATUS_2" };
            newDgn.Parameters.Add(new Parameter
            {
                Name = "Current schedule instatnce",
                Type = parameterType.list,
                Size = parameterSize.uint8,
                firstByte = 1,
                Meanings = { [0] = "Sleep", [1] = "Wake", [2] = "Away", [3] = "Return", [250] = "Storage" }
            });

            newDgn.Parameters.Add(new Parameter
            {
                Name = "Number of schedule instances",
                Type = parameterType.custom,
                Size = parameterSize.uint8,
                firstByte = 2,
            });


            newDgn.Parameters.Add(new Parameter
            {
                Name = "Reduced noise mode",
                Type = parameterType.boolean,
                Size = parameterSize.uint2,
                firstByte = 3,
                Meanings = { [0] = "Disabled", [1] = "Endabled" }
            });

            DGNs.Add(newDgn.Dgn, newDgn);

            newDgn = new DGN(true) { Dgn = 0x1FEF8, Name = "THERMOSTAT_COMMAND_2" };
            newDgn.Parameters.Add(new Parameter
            {
                Name = "Current schedule instatnce",
                Type = parameterType.list,
                Size = parameterSize.uint8,
                firstByte = 1,
                Meanings = { [0] = "Sleep", [1] = "Wake", [2] = "Away", [3] = "Return", [250] = "Storage", [251] = "Reset to \"current\" instance" }
            });

            newDgn.Parameters.Add(new Parameter
            {
                Name = "Number of schedule instances",
                Type = parameterType.custom,
                Size = parameterSize.uint8,
                firstByte = 2,
            });


            newDgn.Parameters.Add(new Parameter
            {
                Name = "Reduced noise mode",
                Type = parameterType.boolean,
                Size = parameterSize.uint2,
                firstByte = 3,
                Meanings = { [0] = "Disabled", [1] = "Endabled" }
            });

            DGNs.Add(newDgn.Dgn, newDgn);

            newDgn = new DGN(true) { Dgn = 0x1FEF7, Name = "THERMOSTAT_SCHEDULE_STATUS_1" };

            newDgn.Parameters.Add(new Parameter
            {
                Name = "Schedule mode instance",
                Type = parameterType.list,
                Size = parameterSize.uint8,
                firstByte = 1,
                Id = true,
                Meanings = { [0] = "Sleep", [1] = "Wake", [2] = "Away", [3] = "Return", [250] = "Storage" }
            });

            newDgn.Parameters.Add(new Parameter
            {
                Name = "Start hour",
                Type = parameterType.custom,
                Size = parameterSize.uint8,
                firstByte = 2,
            });

            newDgn.Parameters.Add(new Parameter
            {
                Name = "Start minute",
                Type = parameterType.custom,
                Size = parameterSize.uint8,
                firstByte = 3,
            });

            newDgn.Parameters.Add(new Parameter
            {
                Name = "Setopint temp - Heat",
                Type = parameterType.temperature,
                Size = parameterSize.uint16,
                firstByte = 4,
            });

            newDgn.Parameters.Add(new Parameter
            {
                Name = "Setopint temp - Cool",
                Type = parameterType.temperature,
                Size = parameterSize.uint16,
                firstByte = 6,
            });

            DGNs.Add(newDgn.Dgn, newDgn);

            newDgn = new DGN(true) { Dgn = 0x1FEF5, Name = "THERMOSTAT_SCHEDULE_COMMAND_1", Parameters = DGNs[0x1FEF7].Parameters };

            DGNs.Add(newDgn.Dgn, newDgn);

            newDgn = new DGN(true) { Dgn = 0x1FEF6, Name = "THERMOSTAT_SCHEDULE_STATUS_2" };

            newDgn.Parameters.Add(new Parameter
            {
                Name = "Schedule mode instance",
                Type = parameterType.list,
                Size = parameterSize.uint8,
                firstByte = 1,
                Id = true,
                Meanings = { [0] = "Sleep", [1] = "Wake", [2] = "Away", [3] = "Return", [250] = "Storage" }
            });

            newDgn.Parameters.Add(new Parameter
            {
                Name = "Sunday",
                Type = parameterType.boolean,
                Size = parameterSize.uint2,
                firstByte = 2,
                firstBit = 0,
                Meanings = { [0] = "Not scheduled for this day", [1] = "Schedule applies to this day" }
            });



            newDgn.Parameters.Add(new Parameter
            {
                Name = "Monday",
                Type = parameterType.boolean,
                Size = parameterSize.uint2,
                firstByte = 2,
                firstBit = 2,
                Meanings = { [0] = "Not scheduled for this day", [1] = "Schedule applies to this day" }
            });

            newDgn.Parameters.Add(new Parameter
            {
                Name = "Tuesday",
                Type = parameterType.list,
                Size = parameterSize.uint2,
                firstByte = 2,
                firstBit = 4,
                Meanings = { [0] = "Not scheduled for this day", [1] = "Schedule applies to this day" }
            });

            newDgn.Parameters.Add(new Parameter
            {
                Name = "Wednesday",
                Type = parameterType.boolean,
                Size = parameterSize.uint2,
                firstByte = 2,
                firstBit = 6,
                Meanings = { [0] = "Not scheduled for this day", [1] = "Schedule applies to this day" }
            });

            newDgn.Parameters.Add(new Parameter
            {
                Name = "Thursday",
                Type = parameterType.boolean,
                Size = parameterSize.uint2,
                firstByte = 3,
                firstBit = 0,
                Meanings = { [0] = "Not scheduled for this day", [1] = "Schedule applies to this day" }
            });

            newDgn.Parameters.Add(new Parameter
            {
                Name = "Friday",
                Type = parameterType.boolean,
                Size = parameterSize.uint2,
                firstByte = 3,
                firstBit = 2,
                Meanings = { [0] = "Not scheduled for this day", [1] = "Schedule applies to this day" }
            });

            newDgn.Parameters.Add(new Parameter
            {
                Name = "Saturday",
                Type = parameterType.boolean,
                Size = parameterSize.uint2,
                firstByte = 3,
                firstBit = 4,
                Meanings = { [0] = "Not scheduled for this day", [1] = "Schedule applies to this day" }
            });

            DGNs.Add(newDgn.Dgn, newDgn);

            newDgn = new DGN(true) { Dgn = 0x1FEF4, Name = "THERMOSTAT_SCHEDULE_COMMAND_2", Parameters = DGNs[0x1FEF6].Parameters };

            DGNs.Add(newDgn.Dgn, newDgn);

            newDgn = new DGN(true) { Dgn = 0x1FE97, Name = "CIRCULATION_PUMP_STATUS" };

            newDgn.Parameters.Add(new Parameter
            {
                Name = "Output status",
                Type = parameterType.list,
                Size = parameterSize.uint4,
                firstByte = 1,
                Meanings = { [0] = "Off", [1] = "On", [5] = "Test (Forced On)" }
            });

            newDgn.Parameters.Add(new Parameter
            {
                Name = "Pump Overcurrent Status",
                Type = parameterType.boolean,
                Size = parameterSize.uint2,
                firstByte = 2,
                Meanings = { [0] = "No overcurrent detected", [1] = "Overcurrent detected" }
            });
            newDgn.Parameters.Add(new Parameter
            {
                Name = "Pump Undercurrent Status",
                Type = parameterType.boolean,
                Size = parameterSize.uint2,
                firstByte = 2,
                firstBit = 2,
                Meanings = { [0] = "No undercurrent detected", [1] = "Undercurrent detected" }
            });

            newDgn.Parameters.Add(new Parameter
            {
                Name = "Pump Temperature Status",
                Type = parameterType.boolean,
                Size = parameterSize.uint2,
                firstByte = 1,
                firstBit = 4,
                Meanings = { [0] = "Temperature normal", [1] = "Temperature warning" }
            });
            DGNs.Add(newDgn.Dgn, newDgn);

            newDgn = new DGN() { Dgn = 0x1FFFF, Name = "DATE_TIME_STATUS" };

            newDgn.Parameters.Add(new Parameter
            {
                Name = "Year",
                Type = parameterType.natural
            });

            newDgn.Parameters.Add(new Parameter
            {
                Name = "Month",
                Type = parameterType.natural,
                firstByte = 1
            });

            newDgn.Parameters.Add(new Parameter
            {
                Name = "Day of month",
                Type = parameterType.natural,
                firstByte = 2
            });

            newDgn.Parameters.Add(new Parameter
            {
                Name = "Day of week",
                Type = parameterType.natural,
                firstByte = 3
            });

            newDgn.Parameters.Add(new Parameter
            {
                Name = "Hour",
                Type = parameterType.natural,
                firstByte = 4
            });

            newDgn.Parameters.Add(new Parameter
            {
                Name = "Minute",
                Type = parameterType.natural,
                firstByte = 5
            });

            newDgn.Parameters.Add(new Parameter
            {
                Name = "Second",
                Type = parameterType.natural,
                firstByte = 6
            });

            DGNs.Add(newDgn.Dgn, newDgn);

            newDgn = new DGN() { Dgn = 0x1FE99, Name = "WATERHEATER_STATUS_2" };

            newDgn.Parameters.Add(new Parameter()
            {
                Name = "Electric Element Level",
                Size = parameterSize.uint4,
                firstByte = 1
            });

            newDgn.Parameters.Add(new Parameter()
            {
                Name = "Max Electric Element Leve",
                Size = parameterSize.uint4,
                firstByte = 1,
                firstBit = 4
            });

            newDgn.Parameters.Add(new Parameter()
            {
                Name = "Engine Preheat",
                Size = parameterSize.uint4,
                firstByte = 2,
                Type = parameterType.list,
                Meanings = { [0] = "Off", [1] = "On", [5] = "Test(Forced On)" }
            });

            newDgn.Parameters.Add(new Parameter()
            {
                Name = "Coolant Level Warning",
                Size = parameterSize.uint2,
                firstByte = 2,
                firstBit = 4,
                Type = parameterType.boolean,
                Meanings = { [0] = "Coolant level sufficient", [1] = "Coolant level low" }
            });

            newDgn.Parameters.Add(new Parameter()
            {
                Name = "Hot Water Priority",
                Size = parameterSize.uint2,
                firstByte = 2,
                firstBit = 4,
                Type = parameterType.list,
                Meanings = { [0] = "Domestic water priority", [1] = "Heating priority" }
            });
            DGNs.Add(newDgn.Dgn, newDgn);
        }

        public static Dictionary<int, DGN> DGNs = new Dictionary<int, DGN>();

    }


    public enum parameterSize
    {
        uint2, //uint2
        uint4,
        uint6,
        uint8,
        uint16,
        uint32,
    };

    public enum parameterType
    {
        boolean,
        list,
        instance,
        percent,
        temperature,
        voltage,
        amperage,
        hertz,
        watts,
        natural,
        custom
    };

    public class DGN
    {
        public int Dgn;
        public string Name;
        public int MaxBroadcastGap = 5000;
        public int MinBroadcastGap = 500;
        public List<Parameter> Parameters;
        public bool HasInstance = false;

        public DGN(bool addInstance = false)
        {
            HasInstance = true;
            Parameters = new List<Parameter>();
            if (addInstance) this.Parameters.Add(new Parameter { Name = "Instance", ShortName = "#", Type = parameterType.instance, Size = parameterSize.uint8, firstByte = 0, Id = true });
        }

        public string Decode(byte[] data)
        {
            string ret = Name + ":{;";
            foreach (Parameter p in Parameters)
            {
                ret += p.ToString(data) + "; ";
            }
            ret += "}";
            return ret;
        }


    }

    public class Parameter
    {
        public string Name;
        public string ShortName;
        public parameterSize Size = parameterSize.uint8;
        public parameterType Type = parameterType.natural;
        public byte firstByte = 0;
        public byte firstBit = 0;
        public Dictionary<uint, string> Meanings;
        public double coefficient = 1;
        public double shift = 0;
        public string Unit = "";
        public bool Id = false;
        public Control assignedLabel;
        public Control assignedField;
        public Parameter()
        {
            Meanings = new Dictionary<uint, string>();
        }

        public uint RawData(byte[] data)
        {
            switch (Size)
            {
                case parameterSize.uint2: return (uint)(data[firstByte] >> firstBit) & 0x3;
                case parameterSize.uint4: return (uint)(data[firstByte] >> firstBit) & 0xF;
                case parameterSize.uint6: return (uint)(data[firstByte] >> firstBit) & 0x3F;
                case parameterSize.uint8: return (data[firstByte]);
                case parameterSize.uint16: return (uint)(data[firstByte] + data[firstByte + 1] * 256);
                case parameterSize.uint32: return (uint)(data[firstByte] + data[firstByte + 1] * 0x100 + data[firstByte + 2] * 0x10000 + data[firstByte + 3] * 0x1000000);
                default: throw new ArgumentException("Bad parameter size");
            }
        }

        public string ToString(byte[] data, bool useFarenheit = false)
        {
            string retString = Name + ": ";

            uint rawData = 0;
            double tempValue = 0;

            switch (Size)
            {
                case parameterSize.uint2: rawData = (uint)((data[firstByte] >> firstBit) & 0x3); break;
                case parameterSize.uint4: rawData = (uint)((data[firstByte] >> firstBit) & 0xF); break;
                case parameterSize.uint6: rawData = (uint)((data[firstByte] >> firstBit) & 0x3F); break;
                case parameterSize.uint8: rawData = (uint)(data[firstByte]); break;
                case parameterSize.uint16: rawData = (uint)(data[firstByte] + data[firstByte + 1] * 256); break;
                case parameterSize.uint32: rawData = (uint)(data[firstByte] + data[firstByte + 1] * 0x100 + data[firstByte + 2] * 0x10000 + data[firstByte + 3] * 0x1000000); break;
                default: throw new ArgumentException("Bad parameter size");
            }

            switch (Size)
            {
                case parameterSize.uint2:
                    if (rawData == 2) return retString += "Error";
                    if (rawData == 3) return retString += "No Data";
                    break;
                case parameterSize.uint4:
                    if (rawData == 14) return retString += "Error";
                    if (rawData == 15) return retString += "No Data";
                    break;
                case parameterSize.uint6:
                    if (rawData == 62) return retString += "Error";
                    if (rawData == 63) return retString += "No Data";
                    break;
                case parameterSize.uint8:
                    if (rawData == byte.MaxValue - 1) return retString += "Error";
                    if (rawData == byte.MaxValue) return retString += "No Data";
                    break;
                case parameterSize.uint16:
                    if (rawData == UInt16.MaxValue - 1) return retString += "Error";
                    if (rawData == UInt16.MaxValue) return retString += "No Data";
                    break;
                case parameterSize.uint32:
                    if (rawData == UInt32.MaxValue - 1) return retString += "Error";
                    if (rawData == UInt32.MaxValue) return retString += "No Data";
                    break;
            }

            switch (Type)
            {
                case parameterType.percent:
                    tempValue = rawData / 2;
                    retString += tempValue.ToString() + "%";
                    break;
                case parameterType.instance:
                    tempValue = rawData;
                    retString += (rawData == 0) ? "For everyone" : "#" + tempValue.ToString();
                    break;
                case parameterType.hertz:
                    tempValue = rawData;
                    retString += tempValue.ToString() + " Hz";
                    break;
                case parameterType.watts:
                    tempValue = rawData;
                    retString += tempValue.ToString() + " W";
                    break;
                case parameterType.amperage:
                    switch (Size)
                    {
                        case parameterSize.uint8: tempValue = rawData; break;
                        case parameterSize.uint16: tempValue = -1600 + rawData * 0.05; break;
                        case parameterSize.uint32: tempValue = -2000000000.0 + rawData * 0.001; break;
                        default: throw new Exception("Wrong size for Amperage field");
                    }
                    retString += tempValue.ToString() + "A";
                    break;
                case parameterType.voltage:
                    switch (Size)
                    {
                        case parameterSize.uint8: tempValue = rawData; break;
                        case parameterSize.uint16: tempValue = rawData * 0.05; break;
                        default: throw new Exception("Wrong size for Voltage field");
                    }
                    retString += tempValue.ToString() + "V";
                    break;

                case parameterType.temperature:
                    switch (Size)
                    {
                        case parameterSize.uint8: tempValue = -40 + rawData; break;
                        case parameterSize.uint16: tempValue = -273 + rawData * 0.03125; break;
                        default: throw new Exception("Wrong size for Temperature field");
                    }
                    if (useFarenheit) tempValue = tempValue * 9.0 / 5.0 + 32;
                    retString += tempValue.ToString() + (useFarenheit ? "F" : "C");
                    break;
                case parameterType.custom:
                    tempValue = rawData;
                    retString += $"{tempValue * coefficient + shift} {Unit}";
                    break;
                case parameterType.boolean:
                    switch (rawData)
                    {
                        case 0: retString += (Meanings.Count == 2) ? Meanings[0] : "False"; break;
                        case 1: retString += (Meanings.Count == 2) ? Meanings[1] : "True"; break;
                        case 2: retString += "Error"; break;
                        case 3: retString += "Not supported"; break;
                    }
                    break;
                case parameterType.list:
                    if (!Meanings.ContainsKey(rawData))
                        retString += $"no meaning for \"{rawData}!\"";
                    else
                        retString += Meanings[rawData];
                    break;
                case parameterType.natural:
                    retString += rawData.ToString();
                    break;

                default: throw new ArgumentException("Bad parameter type");
            }

            return retString;
        }
    }
    public sealed class RvcMessage:ViewModel,IComparable,IUpdatable<RvcMessage>
    {
        private byte priority;

        [AffectsTo(nameof(Verboseinfo))]
        public byte Priority { set => Set(ref priority, value); get => priority; }

        private int dgn;
        [AffectsTo(nameof(Verboseinfo))]
        public int Dgn { set => Set(ref dgn, value); get => dgn; }

        private byte sourceAdress;
        [AffectsTo(nameof(Verboseinfo))]
        public byte SourceAdress { set => Set(ref sourceAdress, value); get => sourceAdress; }
        private byte[] data = new byte[8];
        [AffectsTo(nameof(Verboseinfo),nameof(Instance),nameof(DataAsText))]
        public byte[] Data { set => Set(ref data, value); get => data; }
        public byte Instance => Data[0];
        
        public string DataAsText => $"{Data[0]:X02} {Data[1]:X02} {Data[2]:X02} {Data[3]:X02} {Data[4]:X02} {Data[5]:X02} {Data[6]:X02} {Data[7]:X02}";

        public IEnumerable<Parameter> Parameters => (RVC.DGNs.ContainsKey(Dgn)) ? RVC.DGNs[Dgn].Parameters : null;
        public RvcMessage(CanMessage msg)
        {
            if (msg == null)
                throw new ArgumentNullException("Source CAN Message can't be null");
            if (msg.DLC != 8)
                throw new ArgumentException("DLC of Source Message must be 8");
            if (msg.IDE == false)
                throw new ArgumentException("RV-C supports only extended CAN frame format (IDE=1)");
            if (msg.RTR == true)
                throw new ArgumentException("RV-C do not supports data requests (RTR must be 0)");
            Priority = (byte)((msg.Id >> 26) & 7);
            Dgn = msg.Id >> 8 & 0x1FFFF;
            SourceAdress = (byte)(msg.Id & 0xFF);
            Data = msg.Data;
        }

        public CanMessage GetCanMessage()
        {
            var msg = new CanMessage();
            msg.Id = Priority << 26 | Dgn << 8 | SourceAdress;
            msg.DLC = 8;
            msg.IDE = true;
            msg.RTR = false;
            msg.Data = Data;
            return msg;
        }

        public override bool Equals(object obj)
        {
            if (obj == null)
                return false;
            if (!(obj is RvcMessage))
                return false;
            var comp = obj as RvcMessage;
            return GetCanMessage().Equals(comp.GetCanMessage());
        }

        public override string ToString()
        {

            string ret = $"{Priority} | {Dgn:X05} {SourceAdress:D3} ||";
            foreach (var item in Data)
                ret += $" {item:X02} ";
            return ret;
        }

        public string Verboseinfo => PrintParameters();

        public int Id => throw new NotImplementedException();

        public string PrintParameters ()
        {
            if (!RVC.DGNs.ContainsKey(Dgn))
                return $"{Dgn:X} is not supported";
            return RVC.DGNs[Dgn].Decode(Data);
        }

        public override int GetHashCode()
        {
            return GetCanMessage().GetHashCode();
        }

        public int CompareTo(object other)
        {
            var o = other as RvcMessage;
            if (Dgn != o.Dgn)
                return Dgn - o.Dgn;
            else
                return (Data[0] - o.Data[0]);
        }

        public void Update(RvcMessage item)
        {
            item.Data.CopyTo(Data,0);
            SourceAdress = item.SourceAdress;
            Priority = item.Priority;
        }

        public bool IsSimmiliarTo(RvcMessage item)
        {
            if (Dgn != item.Dgn) return false;
            if (RVC.DGNs.ContainsKey(Dgn) && RVC.DGNs[Dgn].HasInstance)
                return Data[0] != item.Data[0];
            else
                return true;
        }
    }
}
