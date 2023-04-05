using RVC;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection.Emit;
using System.Text;
using System.Threading.Tasks;

namespace RVC
{
    static class RVC
    {
        public static Dictionary<int, DGN> DGNs = new Dictionary<int, DGN>();

        static RVC()
        {
            SeedData();
        }
        private static Dictionary<uint, string> meansMaker(params string[] meanings)
        { 
        var ret = new Dictionary<uint, string>();
            uint cnt = 0;
            foreach (var meaning in meanings)
                ret.Add(cnt++, meaning);
            return ret;
        }
        public static void SeedData()
        {
            DGN newDgn;

            newDgn = new DGN(1) { Dgn = 0x1FF9C, Name = "THERMOSTAT_AMBIENT_STATUS" };
            newDgn.Parameters.Add(new Parameter { Name = "Ambient temperature", ShortName = "Tamb", Type = parameterType.temperature, Size = paramSize.uint16, firstByte = 1 });
            DGNs.Add(newDgn.Dgn, newDgn);

            newDgn = new DGN(1) { Dgn = 0x1FFE4, Name = "FURNACE_STATUS" };

            newDgn.Parameters.Add(new Parameter
            {
                Name = "Operating mode",
                ShortName = "Op mode",
                Type = parameterType.list,
                Size = paramSize.uint2,
                firstByte = 1,
                Meanings = { [0] = "Automatic", [1] = "Manual" }
            });
            newDgn.Parameters.Add(new Parameter
            {
                Name = "Heat Source",
                ShortName = "Heat Src",
                Type = parameterType.list,
                Size = paramSize.uint6,
                firstByte = 1,
                firstBit = 2,
                Meanings = { [0] = "Combustion", [1] = "AC power primary", [2] = "AC power secondary", [3] = "Engine Heat" }
            });

            newDgn.Parameters.Add(new Parameter { Name = "Circulation fan speed", ShortName = "Fan%", Type = parameterType.percent, firstByte = 2, firstBit = 0 });
            newDgn.Parameters.Add(new Parameter { Name = "Heat output level", ShortName = "Heat%", Type = parameterType.percent, firstByte = 3, firstBit = 0 });
            newDgn.Parameters.Add(new Parameter { Name = "Dead band", ShortName = "Db", Type = parameterType.custom, firstByte = 4, coefficient = 0.1, Unit = "C" });
            newDgn.Parameters.Add(new Parameter { Name = "Dead band level 2", ShortName = "Db2", Type = parameterType.custom, firstByte = 5, coefficient = 0.1, Unit = "C" });
            newDgn.Parameters.Add(new Parameter { Name = "Zone overcurrent status", ShortName = "Zone OC", Type = parameterType.boolean, Size = paramSize.uint2, firstByte = 6, firstBit = 0 });
            newDgn.Parameters.Add(new Parameter { Name = "Zone undercurrent status", ShortName = "Zone UC", Type = parameterType.boolean, Size = paramSize.uint2, firstByte = 6, firstBit = 2 });
            newDgn.Parameters.Add(new Parameter
            {
                Name = "Zone temperature status",
                ShortName = "Zone t Status",
                Type = parameterType.boolean,
                Size = paramSize.uint2,
                firstByte = 6,
                firstBit = 4,
                Meanings = { [0] = "Normal", [1] = "Warning" }
            });
            newDgn.Parameters.Add(new Parameter
            {
                Name = "Zone analog input status",
                ShortName = "Zone an",
                Type = parameterType.boolean,
                Size = paramSize.uint2,
                firstByte = 6,
                firstBit = 6,
                Meanings = { [0] = "Off(Inactive)", [1] = "On(Active)" }
            });
            DGNs.Add(newDgn.Dgn, newDgn);

            newDgn = new DGN(1) { Dgn = 0x1FFE3, Name = "FURNACE_COMMAND" };

            newDgn.Parameters.Add(new Parameter
            {
                Name = "Operating mode",
                ShortName = "Op mode",
                Type = parameterType.list,
                Size = paramSize.uint2,
                firstByte = 1,
                Meanings = { [0] = "Automatic", [1] = "Manual" }
            });
            newDgn.Parameters.Add(new Parameter
            {
                Name = "Heat Source",
                ShortName = "Heat Src",
                Type = parameterType.list,
                Size = paramSize.uint6,
                firstByte = 1,
                firstBit = 2,
                Meanings = { [0] = "Combustion", [1] = "AC power primary", [2] = "AC power secondary", [3] = "Engine Heat" }
            });

            newDgn.Parameters.Add(new Parameter { Name = "Circulation fan speed", ShortName = "Fan%", Type = parameterType.percent, firstByte = 2, firstBit = 0 });
            newDgn.Parameters.Add(new Parameter { Name = "Heat output level", ShortName = "Heat%", Type = parameterType.percent, firstByte = 3, firstBit = 0 });
            newDgn.Parameters.Add(new Parameter { Name = "Dead band", ShortName = "Db", Type = parameterType.custom, firstByte = 4, coefficient = 0.1, Unit = "C" });
            newDgn.Parameters.Add(new Parameter { Name = "Dead band level 2", ShortName = "Db2", Type = parameterType.custom, firstByte = 5, coefficient = 0.1, Unit = "C" });

            DGNs.Add(newDgn.Dgn, newDgn);

            newDgn = new DGN(1) { Dgn = 0x1FFF7, Name = "WATERHEATER_STATUS" };

            newDgn.Parameters.Add(new Parameter
            {
                Name = "Operating modes",
                ShortName = "Mode",
                Type = parameterType.list,
                Size = paramSize.uint8,
                firstByte = 1,
                Meanings = { [0] = "off", [1] = "combustion", [2] = "electric", [3] = "gas/electric (both)", [4] = "test combustion (forced on)", [5] = "test combustion (forced on)", [6] = "test electric (forced on)" }
            });

            newDgn.Parameters.Add(new Parameter { Name = "Set point temperature", ShortName = "SP T", Type = parameterType.temperature, Size = paramSize.uint16, firstByte = 2 });

            newDgn.Parameters.Add(new Parameter { Name = "Water temperature", ShortName = "Twater", Type = parameterType.temperature, Size = paramSize.uint16, firstByte = 4 });

            newDgn.Parameters.Add(new Parameter
            {
                Name = "Thermostat status",
                Type = parameterType.boolean,
                Size = paramSize.uint2,
                firstByte = 6,
                firstBit = 0,
                Meanings = { [0] = "set point met", [1] = "set point not met (heat is being applied)" }
            });

            newDgn.Parameters.Add(new Parameter
            {
                Name = "Burner status",
                Type = parameterType.boolean,
                Size = paramSize.uint2,
                firstByte = 6,
                firstBit = 2,
                Meanings = { [0] = "off", [1] = "burner is lit" }
            });

            newDgn.Parameters.Add(new Parameter
            {
                Name = "AC element status",
                Type = parameterType.boolean,
                Size = paramSize.uint2,
                firstByte = 6,
                firstBit = 4,
                Meanings = { [0] = "AC element is inactive", [1] = "AC element is active)" }
            });


            newDgn.Parameters.Add(new Parameter
            {
                Name = "High temperature limit switch status",
                Type = parameterType.boolean,
                Size = paramSize.uint2,
                firstByte = 6,
                firstBit = 6,
                Meanings = { [0] = "limit switch not tripped", [1] = "limit switch tripped" }
            });

            newDgn.Parameters.Add(new Parameter
            {
                Name = "Failure to ignite status",
                Type = parameterType.boolean,
                Size = paramSize.uint2,
                firstByte = 7,
                firstBit = 0,
                Meanings = { [0] = "no failure", [1] = "device has failed to ignite" }
            });

            newDgn.Parameters.Add(new Parameter
            {
                Name = "AC power failure status",
                Type = parameterType.boolean,
                Size = paramSize.uint2,
                firstByte = 7,
                firstBit = 2,
                Meanings = { [0] = "AC power present", [1] = "AC power not present" }
            });

            newDgn.Parameters.Add(new Parameter
            {
                Name = "DC power failure status",
                Type = parameterType.boolean,
                Size = paramSize.uint2,
                firstByte = 7,
                firstBit = 4,
                Meanings = { [0] = "DC power present", [1] = "DC power not present" }
            });

            newDgn.Parameters.Add(new Parameter
            {
                Name = "DC power warning status",
                Type = parameterType.boolean,
                Size = paramSize.uint2,
                firstByte = 7,
                firstBit = 6,
                Meanings = { [0] = "DC power sufficient", [1] = "DC power warning" }
            });

            DGNs.Add(newDgn.Dgn, newDgn);

            newDgn = new DGN(1) { Dgn = 0x1FFF6, Name = "WATERHEATER_COMMAND" };

            newDgn.Parameters.Add(new Parameter
            {
                Name = "Operating modes",
                ShortName = "Mode",
                Type = parameterType.list,
                Size = paramSize.uint8,
                firstByte = 1,
                Meanings = { [0] = "off", [1] = "combustion", [2] = "electric", [3] = "gas/electric (both)", [4] = "test combustion (forced on)", [5] = "test combustion (forced on)", [6] = "test electric (forced on)" }
            });

            newDgn.Parameters.Add(new Parameter { Name = "Set point temperature", ShortName = "SP T", Type = parameterType.temperature, Size = paramSize.uint16, firstByte = 2 });
            newDgn.Parameters.Add(new Parameter { Name = "Electric Element Level", ShortName = "Elec lvl", Type = parameterType.natural, Size = paramSize.uint4, firstByte = 6 });

            DGNs.Add(newDgn.Dgn, newDgn);

            newDgn = new DGN(1) { Dgn = 0x1FFE2, Name = "THERMOSTAT_STATUS_1" };

            newDgn.Parameters.Add(new Parameter
            {
                Name = "Operating mode",
                Type = parameterType.list,
                Size = paramSize.uint4,
                firstByte = 1,
                Meanings = { [0] = "Off", [1] = "Cool", [2] = "Heat", [3] = "Auto heat/Cool", [4] = "Fan only", [5] = "Aux heat", [6] = "Window Defrost/Dehumidify" }
            });

            newDgn.Parameters.Add(new Parameter
            {
                Name = "Fan mode",
                Type = parameterType.boolean,
                Size = paramSize.uint2,
                firstByte = 1,
                firstBit = 4,
                Meanings = { [0] = "Auto", [1] = "On" }
            });

            newDgn.Parameters.Add(new Parameter
            {
                Name = "Schedule mode",
                Type = parameterType.boolean,
                Size = paramSize.uint2,
                firstByte = 1,
                firstBit = 6,
                Meanings = { [0] = "Disabled", [1] = "Enabled" }
            });

            newDgn.Parameters.Add(new Parameter
            {
                Name = "Fan speed",
                Type = parameterType.percent,
                Size = paramSize.uint8,
                firstByte = 2,
            });

            newDgn.Parameters.Add(new Parameter
            {
                Name = "Setopint temp - Heat",
                Type = parameterType.temperature,
                Size = paramSize.uint16,
                firstByte = 3,
            });

            newDgn.Parameters.Add(new Parameter
            {
                Name = "Setopint temp - Cool",
                Type = parameterType.temperature,
                Size = paramSize.uint16,
                firstByte = 5,
            });

            DGNs.Add(newDgn.Dgn, newDgn);


            newDgn = new DGN(1) { Dgn = 0x1FEF9, Name = "THERMOSTAT_COMMAND_1" };

            newDgn.Parameters.Add(new Parameter
            {
                Name = "Operating mode",
                Type = parameterType.list,
                Size = paramSize.uint4,
                firstByte = 1,
                Meanings = { [0] = "Off", [1] = "Cool", [2] = "Heat", [3] = "Auto heat/Cool", [4] = "Fan only", [5] = "Aux heat", [6] = "Window Defrost/Dehumidify" }
            });

            newDgn.Parameters.Add(new Parameter
            {
                Name = "Fan mode",
                Type = parameterType.boolean,
                Size = paramSize.uint2,
                firstByte = 1,
                firstBit = 4,
                Meanings = { [0] = "Auto", [1] = "On" }
            });

            newDgn.Parameters.Add(new Parameter
            {
                Name = "Schedule mode",
                Type = parameterType.boolean,
                Size = paramSize.uint2,
                firstByte = 1,
                firstBit = 6,
                Meanings = { [0] = "Disabled", [1] = "Enabled" }
            });

            newDgn.Parameters.Add(new Parameter
            {
                Name = "Fan speed",
                Type = parameterType.percent,
                Size = paramSize.uint8,
                firstByte = 2,
            });

            newDgn.Parameters.Add(new Parameter
            {
                Name = "Setopint temp - Heat",
                Type = parameterType.temperature,
                Size = paramSize.uint16,
                firstByte = 3,
            });

            newDgn.Parameters.Add(new Parameter
            {
                Name = "Setopint temp - Cool",
                Type = parameterType.temperature,
                Size = paramSize.uint16,
                firstByte = 5,
            });

            DGNs.Add(newDgn.Dgn, newDgn);


            newDgn = new DGN(1) { Dgn = 0x1FEFA, Name = "THERMOSTAT_STATUS_2" };
            newDgn.Parameters.Add(new Parameter
            {
                Name = "Current schedule instatnce",
                Type = parameterType.list,
                Size = paramSize.uint8,
                firstByte = 1,
                Meanings = { [0] = "Sleep", [1] = "Wake", [2] = "Away", [3] = "Return", [250] = "Storage" }
            });

            newDgn.Parameters.Add(new Parameter
            {
                Name = "Number of schedule instances",
                Type = parameterType.custom,
                Size = paramSize.uint8,
                firstByte = 2,
            });


            newDgn.Parameters.Add(new Parameter
            {
                Name = "Reduced noise mode",
                Type = parameterType.boolean,
                Size = paramSize.uint2,
                firstByte = 3,
                Meanings = { [0] = "Disabled", [1] = "Endabled" }
            });

            DGNs.Add(newDgn.Dgn, newDgn);

            newDgn = new DGN(1) { Dgn = 0x1FEF8, Name = "THERMOSTAT_COMMAND_2" };
            newDgn.Parameters.Add(new Parameter
            {
                Name = "Current schedule instatnce",
                Type = parameterType.list,
                Size = paramSize.uint8,
                firstByte = 1,
                Meanings = { [0] = "Sleep", [1] = "Wake", [2] = "Away", [3] = "Return", [250] = "Storage", [251] = "Reset to \"current\" instance" }
            });

            newDgn.Parameters.Add(new Parameter
            {
                Name = "Number of schedule instances",
                Type = parameterType.custom,
                Size = paramSize.uint8,
                firstByte = 2,
            });


            newDgn.Parameters.Add(new Parameter
            {
                Name = "Reduced noise mode",
                Type = parameterType.boolean,
                Size = paramSize.uint2,
                firstByte = 3,
                Meanings = { [0] = "Disabled", [1] = "Endabled" }
            });

            DGNs.Add(newDgn.Dgn, newDgn);

            newDgn = new DGN(2) { Dgn = 0x1FEF7, Name = "THERMOSTAT_SCHEDULE_STATUS_1" };

            newDgn.Parameters.Add(new Parameter
            {
                Name = "Schedule mode instance",
                Type = parameterType.list,
                Size = paramSize.uint8,
                firstByte = 1,
                Id = true,
                Meanings = { [0] = "Sleep", [1] = "Wake", [2] = "Away", [3] = "Return", [250] = "Storage" }
            });

            newDgn.Parameters.Add(new Parameter
            {
                Name = "Start hour",
                Type = parameterType.custom,
                Size = paramSize.uint8,
                firstByte = 2,
            });

            newDgn.Parameters.Add(new Parameter
            {
                Name = "Start minute",
                Type = parameterType.custom,
                Size = paramSize.uint8,
                firstByte = 3,
            });

            newDgn.Parameters.Add(new Parameter
            {
                Name = "Setopint temp - Heat",
                Type = parameterType.temperature,
                Size = paramSize.uint16,
                firstByte = 4,
            });

            newDgn.Parameters.Add(new Parameter
            {
                Name = "Setopint temp - Cool",
                Type = parameterType.temperature,
                Size = paramSize.uint16,
                firstByte = 6,
            });

            DGNs.Add(newDgn.Dgn, newDgn);

            newDgn = new DGN(1) { Dgn = 0x1FEF5, Name = "THERMOSTAT_SCHEDULE_COMMAND_1", Parameters = DGNs[0x1FEF7].Parameters };

            DGNs.Add(newDgn.Dgn, newDgn);

            newDgn = new DGN(2) { Dgn = 0x1FEF6, Name = "THERMOSTAT_SCHEDULE_STATUS_2" };

            newDgn.Parameters.Add(new Parameter
            {
                Name = "Schedule mode instance",
                Type = parameterType.list,
                Size = paramSize.uint8,
                firstByte = 1,
                Id = true,
                Meanings = { [0] = "Sleep", [1] = "Wake", [2] = "Away", [3] = "Return", [250] = "Storage" }
            });

            newDgn.Parameters.Add(new Parameter
            {
                Name = "Sunday",
                Type = parameterType.boolean,
                Size = paramSize.uint2,
                firstByte = 2,
                firstBit = 0,
                Meanings = { [0] = "Not scheduled for this day", [1] = "Schedule applies to this day" }
            });



            newDgn.Parameters.Add(new Parameter
            {
                Name = "Monday",
                Type = parameterType.boolean,
                Size = paramSize.uint2,
                firstByte = 2,
                firstBit = 2,
                Meanings = { [0] = "Not scheduled for this day", [1] = "Schedule applies to this day" }
            });

            newDgn.Parameters.Add(new Parameter
            {
                Name = "Tuesday",
                Type = parameterType.list,
                Size = paramSize.uint2,
                firstByte = 2,
                firstBit = 4,
                Meanings = { [0] = "Not scheduled for this day", [1] = "Schedule applies to this day" }
            });

            newDgn.Parameters.Add(new Parameter
            {
                Name = "Wednesday",
                Type = parameterType.boolean,
                Size = paramSize.uint2,
                firstByte = 2,
                firstBit = 6,
                Meanings = { [0] = "Not scheduled for this day", [1] = "Schedule applies to this day" }
            });

            newDgn.Parameters.Add(new Parameter
            {
                Name = "Thursday",
                Type = parameterType.boolean,
                Size = paramSize.uint2,
                firstByte = 3,
                firstBit = 0,
                Meanings = { [0] = "Not scheduled for this day", [1] = "Schedule applies to this day" }
            });

            newDgn.Parameters.Add(new Parameter
            {
                Name = "Friday",
                Type = parameterType.boolean,
                Size = paramSize.uint2,
                firstByte = 3,
                firstBit = 2,
                Meanings = { [0] = "Not scheduled for this day", [1] = "Schedule applies to this day" }
            });

            newDgn.Parameters.Add(new Parameter
            {
                Name = "Saturday",
                Type = parameterType.boolean,
                Size = paramSize.uint2,
                firstByte = 3,
                firstBit = 4,
                Meanings = { [0] = "Not scheduled for this day", [1] = "Schedule applies to this day" }
            });

            DGNs.Add(newDgn.Dgn, newDgn);

            newDgn = new DGN(1) { Dgn = 0x1FEF4, Name = "THERMOSTAT_SCHEDULE_COMMAND_2", Parameters = DGNs[0x1FEF6].Parameters };

            DGNs.Add(newDgn.Dgn, newDgn);

            newDgn = new DGN(1) { Dgn = 0x1FE97, Name = "CIRCULATION_PUMP_STATUS" };

            newDgn.Parameters.Add(new Parameter
            {
                Name = "Output status",
                Type = parameterType.list,
                Size = paramSize.uint4,
                firstByte = 1,
                Meanings = { [0] = "Off", [1] = "On", [5] = "Test (Forced On)" }
            });

            newDgn.Parameters.Add(new Parameter
            {
                Name = "Pump Overcurrent Status",
                Type = parameterType.boolean,
                Size = paramSize.uint2,
                firstByte = 2,
                Meanings = { [0] = "No overcurrent detected", [1] = "Overcurrent detected" }
            });
            newDgn.Parameters.Add(new Parameter
            {
                Name = "Pump Undercurrent Status",
                Type = parameterType.boolean,
                Size = paramSize.uint2,
                firstByte = 2,
                firstBit = 2,
                Meanings = { [0] = "No undercurrent detected", [1] = "Undercurrent detected" }
            });

            newDgn.Parameters.Add(new Parameter
            {
                Name = "Pump Temperature Status",
                Type = parameterType.boolean,
                Size = paramSize.uint2,
                firstByte = 1,
                firstBit = 4,
                Meanings = { [0] = "Temperature normal", [1] = "Temperature warning" }
            });
            DGNs.Add(newDgn.Dgn, newDgn);

            newDgn = new DGN(0) { Dgn = 0x1FFFF, Name = "DATE_TIME_STATUS" };

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
                Size = paramSize.uint4,
                firstByte = 1
            });

            newDgn.Parameters.Add(new Parameter()
            {
                Name = "Max Electric Element Leve",
                Size = paramSize.uint4,
                firstByte = 1,
                firstBit = 4
            });

            newDgn.Parameters.Add(new Parameter()
            {
                Name = "Engine Preheat",
                Size = paramSize.uint4,
                firstByte = 2,
                Type = parameterType.list,
                Meanings = { [0] = "Off", [1] = "On", [5] = "Test(Forced On)" }
            });

            newDgn.Parameters.Add(new Parameter()
            {
                Name = "Coolant Level Warning",
                Size = paramSize.uint2,
                firstByte = 2,
                firstBit = 4,
                Type = parameterType.boolean,
                Meanings = { [0] = "Coolant level sufficient", [1] = "Coolant level low" }
            });

            newDgn.Parameters.Add(new Parameter()
            {
                Name = "Hot Water Priority",
                Size = paramSize.uint2,
                firstByte = 2,
                firstBit = 4,
                Type = parameterType.list,
                Meanings = { [0] = "Domestic water priority", [1] = "Heating priority" }
            });
            DGNs.Add(newDgn.Dgn, newDgn);

            newDgn = new() { Name = "GENERATOR_DEMAND_STATUS", Dgn = 0xFF80, idLength = 0 };
            newDgn.Parameters.Add(new Parameter() { Name = "Generator demand", firstByte = 0, firstBit = 0, Size = paramSize.uint2, Meanings = meansMaker("No demand for generator", "Generator is demanded") } );
            newDgn.Parameters.Add(new Parameter() { Name = "Internal generator demand", firstByte = 0, firstBit = 2, Size = paramSize.uint2, Meanings = meansMaker("No internal demand", "Internal AGS criterion is demanding generator") });
            newDgn.Parameters.Add(new Parameter() { Name = "Network generator demand", firstByte = 0, firstBit = 4, Size = paramSize.uint2, Meanings = meansMaker("No demand from other network nodes", "Network device is demanding generator") });
            newDgn.Parameters.Add(new Parameter() { Name = "External activity detected", firstByte = 0, firstBit = 6, Size = paramSize.uint2, Meanings = meansMaker("Automatic starting is allowed", "Automatic starting is disabled due to the\r\ndetection of external activity") });
            newDgn.Parameters.Add(new Parameter() { Name = "Manual override detected", firstByte = 1, firstBit = 0, Size = paramSize.uint2, Meanings = meansMaker("Normal Operation", "Manual Override") });
            newDgn.Parameters.Add(new Parameter() { Name = "Quiet time", firstByte = 1, firstBit = 2, Size = paramSize.uint2, Meanings = meansMaker("Unit is not in Quiet Time", "Unit is in Quiet Time") });
            newDgn.Parameters.Add(new Parameter() { Name = "Quiet time override", firstByte = 1, firstBit = 4, Size = paramSize.uint2, Meanings = meansMaker("Normal operation", "Quiet Time is being overridden") });
            newDgn.Parameters.Add(new Parameter() { Name = "Generator lock", firstByte = 0, firstBit = 6, Size = paramSize.uint2, Meanings = meansMaker("Normal operation", "Genset is locked. Node will not start\r\ngenerator for any reason") });
            newDgn.Parameters.Add(new Parameter() { Name = "Network generator demand", firstByte = 0, firstBit = 4, Size = paramSize.uint2, Meanings = meansMaker("No demand from other network nodes", "Network device is demanding generator") });
            newDgn.Parameters.Add(new Parameter() { Name = "Quiet time begin hour", firstByte = 2, firstBit = 0, Unit="h"});
            DGNs.Add(newDgn.Dgn, newDgn);
        }

     

    }


}
