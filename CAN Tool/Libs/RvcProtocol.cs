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
        static readonly Dictionary<int, string> defMeaningsYesNo = new() { { 0, "t_no" }, { 1, "t_yes" }, { 2, "t_error" }, { 3, "t_no_data" } };
        static readonly Dictionary<int, string> defMeaningsOnOff = new() { { 0, "t_off" }, { 1, "t_on" }, { 2, "t_error" }, { 3, "t_no_data" } };
        static readonly Dictionary<int, string> defMeaningsEnabledDisabled = new() { { 0, "t_disabled" }, { 1, "t_enabled" }, { 2, "t_error" }, { 3, "t_no_data" } };

        public static Dictionary<int, DGN> DGNs = new Dictionary<int, DGN>();

        static RVC()
        {
            SeedData();
        }
        private static Dictionary<uint, string> mnsMkr(params string[] meanings)
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
            newDgn.Parameters.Add(new ("Ambient temperature") {ShortName = "Tamb", Type = paramTyp.temperature, Size = 16, fstByte = 1 });
            DGNs.Add(newDgn.Dgn, newDgn);

            newDgn = new DGN(1) { Dgn = 0x1FFE4, Name = "FURNACE_STATUS" };

            newDgn.Parameters.Add(new ("Operating mode") { ShortName = "Op mode", Type = paramTyp.natural, Size = 2, fstByte = 1, Meanings = new() { [0] = "Automatic", [1] = "Manual" }});
            newDgn.Parameters.Add(new ("Heat Source") {ShortName = "Heat Src",Type = paramTyp.natural, Size = 6, fstByte = 1, frstFit = 2,
                Meanings = new() { [0] = "Combustion", [1] = "AC power primary", [2] = "AC power secondary", [3] = "Engine Heat" }});

            newDgn.Parameters.Add(new ("Circulation fan speed") {ShortName = "Fan%", Type = paramTyp.percent, fstByte = 2, frstFit = 0 });
            newDgn.Parameters.Add(new ("Heat output level") {ShortName = "Heat%", Type = paramTyp.percent, fstByte = 3, frstFit = 0 });
            newDgn.Parameters.Add(new ("Dead band") {ShortName = "Db", Type = paramTyp.custom, fstByte = 4, coefficient = 0.1, Unit = "C" });
            newDgn.Parameters.Add(new ("Dead band level 2") {ShortName = "Db2", Type = paramTyp.custom, fstByte = 5, coefficient = 0.1, Unit = "C" });
            newDgn.Parameters.Add(new("Zone overcurrent status") {ShortName = "Zone OC", Type = paramTyp.boolean, Size = 2, fstByte = 6, frstFit = 0 });
            newDgn.Parameters.Add(new ("Zone undercurrent status") {ShortName = "Zone UC", Type = paramTyp.boolean, Size = 2, fstByte = 6, frstFit = 2 });
            newDgn.Parameters.Add(new ("Zone temperature status") {ShortName = "Zone t Status",Type = paramTyp.boolean,Size = 2,fstByte = 6,frstFit = 4,
                Meanings = new() { [0] = "Normal", [1] = "Warning" }});
            newDgn.Parameters.Add(new ("Zone analog input status") {ShortName = "Zone an",Type = paramTyp.boolean,Size = 2,fstByte = 6,frstFit = 6,
                Meanings = new() { [0] = "Off(Inactive)", [1] = "On(Active)" }});
            DGNs.Add(newDgn.Dgn, newDgn);

            newDgn = new DGN(1) { Dgn = 0x1FFE3, Name = "FURNACE_COMMAND" };

            newDgn.Parameters.Add(new ("Operating mode") {ShortName = "Op mode",Type = paramTyp.natural,Size = 2,fstByte = 1,Meanings = new() { [0] = "Automatic", [1] = "Manual" }});
            newDgn.Parameters.Add(new Parameter
            {
                Name = "Heat Source",
                ShortName = "Heat Src",
                Type = paramTyp.natural,
                Size = 6,
                fstByte = 1,
                frstFit = 2,
                Meanings = new() { [0] = "Combustion", [1] = "AC power primary", [2] = "AC power secondary", [3] = "Engine Heat" }
            });

            newDgn.Parameters.Add(new Parameter { Name = "Circulation fan speed", ShortName = "Fan%", Type = paramTyp.percent, fstByte = 2, frstFit = 0 });
            newDgn.Parameters.Add(new Parameter { Name = "Heat output level", ShortName = "Heat%", Type = paramTyp.percent, fstByte = 3, frstFit = 0 });
            newDgn.Parameters.Add(new Parameter { Name = "Dead band", ShortName = "Db", Type = paramTyp.custom, fstByte = 4, coefficient = 0.1, Unit = "C" });
            newDgn.Parameters.Add(new Parameter { Name = "Dead band level 2", ShortName = "Db2", Type = paramTyp.custom, fstByte = 5, coefficient = 0.1, Unit = "C" });

            DGNs.Add(newDgn.Dgn, newDgn);

            newDgn = new DGN(1) { Dgn = 0x1FFF7, Name = "WATERHEATER_STATUS" };

            newDgn.Parameters.Add(new Parameter
            {
                Name = "Operating modes",
                ShortName = "Mode",
                Type = paramTyp.natural,
                Size = 8,
                fstByte = 1,
                Meanings = new() { [0] = "off", [1] = "combustion", [2] = "electric", [3] = "gas/electric (both)", [4] = "test combustion (forced on)", [5] = "test combustion (forced on)", [6] = "test electric (forced on)" }
            });

            newDgn.Parameters.Add(new Parameter { Name = "Set point temperature", ShortName = "SP T", Type = paramTyp.temperature, Size = 16, fstByte = 2 });

            newDgn.Parameters.Add(new Parameter { Name = "Water temperature", ShortName = "Twater", Type = paramTyp.temperature, Size = 16, fstByte = 4 });

            newDgn.Parameters.Add(new Parameter
            {
                Name = "Thermostat status",
                Type = paramTyp.boolean,
                Size = 2,
                fstByte = 6,
                frstFit = 0,
                Meanings = new() { [0] = "set point met", [1] = "set point not met (heat is being applied)" }
            });

            newDgn.Parameters.Add(new Parameter
            {
                Name = "Burner status",
                Type = paramTyp.boolean,
                Size = 2,
                fstByte = 6,
                frstFit = 2,
                Meanings = new() { [0] = "off", [1] = "burner is lit" }
            });

            newDgn.Parameters.Add(new Parameter
            {
                Name = "AC element status",
                Type = paramTyp.boolean,
                Size = 2,
                fstByte = 6,
                frstFit = 4,
                Meanings = new() { [0] = "AC element is inactive", [1] = "AC element is active)" }
            });


            newDgn.Parameters.Add(new Parameter
            {
                Name = "High temperature limit switch status",
                Type = paramTyp.boolean,
                Size = 2,
                fstByte = 6,
                frstFit = 6,
                Meanings = new() { [0] = "limit switch not tripped", [1] = "limit switch tripped" }
            });

            newDgn.Parameters.Add(new Parameter
            {
                Name = "Failure to ignite status",
                Type = paramTyp.boolean,
                Size = 2,
                fstByte = 7,
                frstFit = 0,
                Meanings = new() { [0] = "no failure", [1] = "device has failed to ignite" }
            });

            newDgn.Parameters.Add(new Parameter
            {
                Name = "AC power failure status",
                Type = paramTyp.boolean,
                Size = 2,
                fstByte = 7,
                frstFit = 2,
                Meanings = new() { [0] = "AC power present", [1] = "AC power not present" }
            });

            newDgn.Parameters.Add(new Parameter
            {
                Name = "DC power failure status",
                Type = paramTyp.boolean,
                Size = 2,
                fstByte = 7,
                frstFit = 4,
                Meanings = new() { [0] = "DC power present", [1] = "DC power not present" }
            });

            newDgn.Parameters.Add(new Parameter
            {
                Name = "DC power warning status",
                Type = paramTyp.boolean,
                Size = 2,
                fstByte = 7,
                frstFit = 6,
                Meanings = new() { [0] = "DC power sufficient", [1] = "DC power warning" }
            });

            DGNs.Add(newDgn.Dgn, newDgn);

            newDgn = new DGN(1) { Dgn = 0x1FFF6, Name = "WATERHEATER_COMMAND" };

            newDgn.Parameters.Add(new Parameter
            {
                Name = "Operating modes",
                ShortName = "Mode",
                Type = paramTyp.natural,
                Size = 8,
                fstByte = 1,
                Meanings = new() { [0] = "off", [1] = "combustion", [2] = "electric", [3] = "gas/electric (both)", [4] = "test combustion (forced on)", [5] = "test combustion (forced on)", [6] = "test electric (forced on)" }
            });

            newDgn.Parameters.Add(new Parameter { Name = "Set point temperature", ShortName = "SP T", Type = paramTyp.temperature, Size = 16, fstByte = 2 });
            newDgn.Parameters.Add(new Parameter { Name = "Electric Element Level", ShortName = "Elec lvl", Type = paramTyp.natural, Size = 4, fstByte = 6 });

            DGNs.Add(newDgn.Dgn, newDgn);

            newDgn = new DGN(1) { Dgn = 0x1FFE2, Name = "THERMOSTAT_STATUS_1" };

            newDgn.Parameters.Add(new Parameter
            {
                Name = "Operating mode",
                Type = paramTyp.natural,
                Size = 4,
                fstByte = 1,
                Meanings = new() { [0] = "Off", [1] = "Cool", [2] = "Heat", [3] = "Auto heat/Cool", [4] = "Fan only", [5] = "Aux heat", [6] = "Window Defrost/Dehumidify" }
            });

            newDgn.Parameters.Add(new()
            {
                Name = "Fan mode",
                Type = paramTyp.boolean,
                Size = 2,
                fstByte = 1,
                frstFit = 4,
                Meanings = new() { [0] = "Auto", [1] = "On" }
            });

            newDgn.Parameters.Add(new()
            {
                Name = "Schedule mode",
                Type = paramTyp.boolean,
                Size = 2,
                fstByte = 1,
                frstFit = 6,
                Meanings = new() { [0] = "Disabled", [1] = "Enabled" }
            });

            newDgn.Parameters.Add(new()
            {
                Name = "Fan speed",
                Type = paramTyp.percent,
                Size = 8,
                fstByte = 2,
            });

            newDgn.Parameters.Add(new()
            {
                Name = "Setopint temp - Heat",
                Type = paramTyp.temperature,
                Size = 16,
                fstByte = 3,
            });

            newDgn.Parameters.Add(new()
            {
                Name = "Setopint temp - Cool",
                Type = paramTyp.temperature,
                Size = 16,
                fstByte = 5,
            });

            DGNs.Add(newDgn.Dgn, newDgn);


            newDgn = new DGN(1) { Dgn = 0x1FEF9, Name = "THERMOSTAT_COMMAND_1" };

            newDgn.Parameters.Add(new()
            {
                Name = "Operating mode",
                Type = paramTyp.natural,
                Size = 4,
                fstByte = 1,
                Meanings = new() { [0] = "Off", [1] = "Cool", [2] = "Heat", [3] = "Auto heat/Cool", [4] = "Fan only", [5] = "Aux heat", [6] = "Window Defrost/Dehumidify" }
            });

            newDgn.Parameters.Add(new()
            {
                Name = "Fan mode",
                Type = paramTyp.boolean,
                Size = 2,
                fstByte = 1,
                frstFit = 4,
                Meanings = new() { [0] = "Auto", [1] = "On" }
            });

            newDgn.Parameters.Add(new()
            {
                Name = "Schedule mode",
                Type = paramTyp.boolean,
                Size = 2,
                fstByte = 1,
                frstFit = 6,
                Meanings = new() { [0] = "Disabled", [1] = "Enabled" }
            });

            newDgn.Parameters.Add(new()
            {
                Name = "Fan speed",
                Type = paramTyp.percent,
                Size = 8,
                fstByte = 2,
            });

            newDgn.Parameters.Add(new()
            {
                Name = "Setopint temp - Heat",
                Type = paramTyp.temperature,
                Size = 16,
                fstByte = 3,
            });

            newDgn.Parameters.Add(new()
            {
                Name = "Setopint temp - Cool",
                Type = paramTyp.temperature,
                Size = 16,
                fstByte = 5,
            });

            DGNs.Add(newDgn.Dgn, newDgn);


            newDgn = new DGN(1) { Dgn = 0x1FEFA, Name = "THERMOSTAT_STATUS_2" };
            newDgn.Parameters.Add(new()
            {
                Name = "Current schedule instatnce",
                Type = paramTyp.natural,
                Size = 8,
                fstByte = 1,
                Meanings = new() { [0] = "Sleep", [1] = "Wake", [2] = "Away", [3] = "Return", [250] = "Storage" }
            });

            newDgn.Parameters.Add(new()
            {
                Name = "Number of schedule instances",
                Type = paramTyp.custom,
                Size = 8,
                fstByte = 2,
            });


            newDgn.Parameters.Add(new()
            {
                Name = "Reduced noise mode",
                Type = paramTyp.boolean,
                Size = 2,
                fstByte = 3,
                Meanings = new() { [0] = "Disabled", [1] = "Endabled" }
            });

            DGNs.Add(newDgn.Dgn, newDgn);

            newDgn = new DGN(1) { Dgn = 0x1FEF8, Name = "THERMOSTAT_COMMAND_2" };
            newDgn.Parameters.Add(new()
            {
                Name = "Current schedule instatnce",
                Type = paramTyp.natural,
                Size = 8,
                fstByte = 1,
                Meanings = new() { [0] = "Sleep", [1] = "Wake", [2] = "Away", [3] = "Return", [250] = "Storage", [251] = "Reset to \"current\" instance" }
            });

            newDgn.Parameters.Add(new()
            {
                Name = "Number of schedule instances",
                Type = paramTyp.custom,
                Size = 8,
                fstByte = 2,
            });


            newDgn.Parameters.Add(new()
            {
                Name = "Reduced noise mode",
                Type = paramTyp.boolean,
                Size = 2,
                fstByte = 3,
                Meanings = new() { [0] = "Disabled", [1] = "Endabled" }
            });

            DGNs.Add(newDgn.Dgn, newDgn);

            newDgn = new DGN(2) { Dgn = 0x1FEF7, Name = "THERMOSTAT_SCHEDULE_STATUS_1" };

            newDgn.Parameters.Add(new()
            {
                Name = "Schedule mode instance",
                Type = paramTyp.natural,
                Size = 8,
                fstByte = 1,
                Id = true,
                Meanings = new() { [0] = "Sleep", [1] = "Wake", [2] = "Away", [3] = "Return", [250] = "Storage" }
            });

            newDgn.Parameters.Add(new()
            {
                Name = "Start hour",
                Type = paramTyp.custom,
                Size = 8,
                fstByte = 2,
            });

            newDgn.Parameters.Add(new()
            {
                Name = "Start minute",
                Type = paramTyp.custom,
                Size = 8,
                fstByte = 3,
            });

            newDgn.Parameters.Add(new()
            {
                Name = "Setopint temp - Heat",
                Type = paramTyp.temperature,
                Size = 16,
                fstByte = 4,
            });

            newDgn.Parameters.Add(new()
            {
                Name = "Setopint temp - Cool",
                Type = paramTyp.temperature,
                Size = 16,
                fstByte = 6,
            });

            DGNs.Add(newDgn.Dgn, newDgn);

            newDgn = new DGN(1) { Dgn = 0x1FEF5, Name = "THERMOSTAT_SCHEDULE_COMMAND_1", Parameters = DGNs[0x1FEF7].Parameters };

            DGNs.Add(newDgn.Dgn, newDgn);

            newDgn = new DGN(2) { Dgn = 0x1FEF6, Name = "THERMOSTAT_SCHEDULE_STATUS_2" };

            newDgn.Parameters.Add(new()
            {
                Name = "Schedule mode instance",
                Type = paramTyp.natural,
                Size = 8,
                fstByte = 1,
                Id = true,
                Meanings = new() { [0] = "Sleep", [1] = "Wake", [2] = "Away", [3] = "Return", [250] = "Storage" }
            });

            newDgn.Parameters.Add(new()
            {
                Name = "Sunday",
                Type = paramTyp.boolean,
                Size = 2,
                fstByte = 2,
                frstFit = 0,
                Meanings = new() { [0] = "Not scheduled for this day", [1] = "Schedule applies to this day" }
            });



            newDgn.Parameters.Add(new()
            {
                Name = "Monday",
                Type = paramTyp.boolean,
                Size = 2,
                fstByte = 2,
                frstFit = 2,
                Meanings = new() { [0] = "Not scheduled for this day", [1] = "Schedule applies to this day" }
            });

            newDgn.Parameters.Add(new()
            {
                Name = "Tuesday",
                Type = paramTyp.natural,
                Size = 2,
                fstByte = 2,
                frstFit = 4,
                Meanings = new() { [0] = "Not scheduled for this day", [1] = "Schedule applies to this day" }
            });

            newDgn.Parameters.Add(new()
            {
                Name = "Wednesday",
                Type = paramTyp.boolean,
                Size = 2,
                fstByte = 2,
                frstFit = 6,
                Meanings = new() { [0] = "Not scheduled for this day", [1] = "Schedule applies to this day" }
            });

            newDgn.Parameters.Add(new()
            {
                Name = "Thursday",
                Type = paramTyp.boolean,
                Size = 2,
                fstByte = 3,
                frstFit = 0,
                Meanings = new() { [0] = "Not scheduled for this day", [1] = "Schedule applies to this day" }
            });

            newDgn.Parameters.Add(new()
            {
                Name = "Friday",
                Type = paramTyp.boolean,
                Size = 2,
                fstByte = 3,
                frstFit = 2,
                Meanings = new() { [0] = "Not scheduled for this day", [1] = "Schedule applies to this day" }
            });

            newDgn.Parameters.Add(new()
            {
                Name = "Saturday",
                Type = paramTyp.boolean,
                Size = 2,
                fstByte = 3,
                frstFit = 4,
                Meanings = new() { [0] = "Not scheduled for this day", [1] = "Schedule applies to this day" }
            });

            DGNs.Add(newDgn.Dgn, newDgn);

            newDgn = new DGN(1) { Dgn = 0x1FEF4, Name = "THERMOSTAT_SCHEDULE_COMMAND_2", Parameters = DGNs[0x1FEF6].Parameters };

            DGNs.Add(newDgn.Dgn, newDgn);

            newDgn = new DGN(1) { Dgn = 0x1FE97, Name = "CIRCULATION_PUMP_STATUS" };

            newDgn.Parameters.Add(new()
            {
                Name = "Output status",
                Type = paramTyp.natural,
                Size = 4,
                fstByte = 1,
                Meanings = new() { [0] = "Off", [1] = "On", [5] = "Test (Forced On)" }
            });

            newDgn.Parameters.Add(new()
            {
                Name = "Pump Overcurrent Status",
                Type = paramTyp.boolean,
                Size = 2,
                fstByte = 2,
                Meanings = new() { [0] = "No overcurrent detected", [1] = "Overcurrent detected" }
            });
            newDgn.Parameters.Add(new()
            {
                Name = "Pump Undercurrent Status",
                Type = paramTyp.boolean,
                Size = 2,
                fstByte = 2,
                frstFit = 2,
                Meanings = new() { [0] = "No undercurrent detected", [1] = "Undercurrent detected" }
            });

            newDgn.Parameters.Add(new()
            {
                Name = "Pump Temperature Status",
                Type = paramTyp.boolean,
                Size = 2,
                fstByte = 1,
                frstFit = 4,
                Meanings = new() { [0] = "Temperature normal", [1] = "Temperature warning" }
            });
            DGNs.Add(newDgn.Dgn, newDgn);

            newDgn = new DGN(0) { Dgn = 0x1FFFF, Name = "DATE_TIME_STATUS" };

            newDgn.Parameters.Add(new()
            {
                Name = "Year",
                Type = paramTyp.natural
            });

            newDgn.Parameters.Add(new()
            {
                Name = "Month",
                Type = paramTyp.natural,
                fstByte = 1
            });

            newDgn.Parameters.Add(new()
            {
                Name = "Day of month",
                Type = paramTyp.natural,
                fstByte = 2
            });

            newDgn.Parameters.Add(new ()
            {
                Name = "Day of week",
                Type = paramTyp.natural,
                fstByte = 3
            });

            newDgn.Parameters.Add(new ()
            {
                Name = "Hour",
                Type = paramTyp.natural,
                fstByte = 4
            });

            newDgn.Parameters.Add(new ()
            {
                Name = "Minute",
                Type = paramTyp.natural,
                fstByte = 5
            });

            newDgn.Parameters.Add(new ()
            {
                Name = "Second",
                Type = paramTyp.natural,
                fstByte = 6
            });

            DGNs.Add(newDgn.Dgn, newDgn);

            newDgn = new DGN() { Dgn = 0x1FE99, Name = "WATERHEATER_STATUS_2" };

            newDgn.Parameters.Add(new ()
            {
                Name = "Electric Element Level",
                Size = 4,
                fstByte = 1
            });

            newDgn.Parameters.Add(new ()
            {
                Name = "Max Electric Element Leve",
                Size = 4,
                fstByte = 1,
                frstFit = 4
            });

            newDgn.Parameters.Add(new ()
            {
                Name = "Engine Preheat",
                Size = 4,
                fstByte = 2,
                Type = paramTyp.natural,
                Meanings = new() { [0] = "Off", [1] = "On", [5] = "Test(Forced On)" }
            });

            newDgn.Parameters.Add(new ()
            {
                Name = "Coolant Level Warning",
                Size = 2,
                fstByte = 2,
                frstFit = 4,
                Type = paramTyp.boolean,
                Meanings = new() { [0] = "Coolant level sufficient", [1] = "Coolant level low" }
            });

            newDgn.Parameters.Add(new ()
            {
                Name = "Hot Water Priority",
                Size = 2,
                fstByte = 2,
                frstFit = 4,
                Type = paramTyp.natural,
                Meanings = new() { [0] = "Domestic water priority", [1] = "Heating priority" }
            });
            DGNs.Add(newDgn.Dgn, newDgn);

            newDgn = new() { Name = "GENERATOR_DEMAND_STATUS", Dgn = 0xFF80, idLength = 0 };
            newDgn.Parameters.Add(new Parameter() { Name = "Generator demand", fstByte = 0, frstFit = 0, Size = 2, Meanings = mnsMkr("No demand for generator", "Generator is demanded") } );
            newDgn.Parameters.Add(new Parameter() { Name = "Internal generator demand", fstByte = 0, frstFit = 2, Size = 2, Meanings =  mnsMkr("No internal demand", "Internal AGS criterion is demanding generator") });
            newDgn.Parameters.Add(new Parameter() { Name = "Network generator demand", fstByte = 0, frstFit = 4, Size = 2, Meanings =  mnsMkr("No demand from other network nodes", "Network device is demanding generator") });
            newDgn.Parameters.Add(new Parameter() { Name = "External activity detected", fstByte = 0, frstFit = 6, Size = 2, Meanings = mnsMkr("Automatic starting is allowed", "Automatic starting is disabled due to the detection of external activity") });
            newDgn.Parameters.Add(new Parameter() { Name = "Manual override detected", fstByte = 1, frstFit = 0, Size = 2, Meanings =  mnsMkr("Normal Operation", "Manual Override") });
            newDgn.Parameters.Add(new Parameter() { Name = "Quiet time", fstByte = 1, frstFit = 2, Size = 2, Meanings =  mnsMkr("Unit is not in Quiet Time", "Unit is in Quiet Time") });
            newDgn.Parameters.Add(new Parameter() { Name = "Quiet time override", fstByte = 1, frstFit = 4, Size = 2, Meanings =  mnsMkr("Normal operation", "Quiet Time is being overridden") });
            newDgn.Parameters.Add(new Parameter() { Name = "Generator lock", fstByte = 0, frstFit = 6, Size = 2, Meanings =  mnsMkr("Normal operation", "Genset is locked. Node will not start generator for any reason") });
            newDgn.Parameters.Add(new Parameter() { Name = "Network generator demand", fstByte = 0, frstFit = 4, Size = 2, Meanings =  mnsMkr("No demand from other network nodes", "Network device is demanding generator") });
            newDgn.Parameters.Add(new Parameter() { Name = "Quiet time begin hour", fstByte = 2, frstFit = 0, Unit="h"});
            DGNs.Add(newDgn.Dgn, newDgn);
        }

     

    }


}
