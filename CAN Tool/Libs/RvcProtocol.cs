using OmniProtocol;
using System.Collections.Generic;


namespace RVC
{
    static class RVC
    {
        static readonly Dictionary<int, string> defMeaningsYesNo = new() { { 0, "t_no" }, { 1, "t_yes" }, { 2, "t_error" }, { 3, "t_no_data" } };
        static readonly Dictionary<int, string> defMeaningsOnOff = new() { { 0, "t_off" }, { 1, "t_on" }, { 2, "t_error" }, { 3, "t_no_data" } };
        static readonly Dictionary<int, string> defMeaningsEnabledDisabled = new() { { 0, "t_disabled" }, { 1, "t_enabled" }, { 2, "t_error" }, { 3, "t_no_data" } };
        /*
        static readonly Dictionary<int, string> operatingStatusesSimple = new() {
            { 0, "Device is disabled and not operating. Generally a fault condition or the result of a manual override" },
            { 1, "Device is disabled, but is running. Generally a fault conditions or the result of a manual override." },
            { 2, "Device is not operating, but will accept commands to operate.This is the'normal' OFF condition." },
            { 3, "Device is operating and will accept command. This is the 'normal' ON condition." } };

        static readonly Dictionary<int, string> operatingStatusesIntel = new() {
            { 0, "Device is disabled and not operating" },
            { 1, "Device is disabled, but is running. Generally a fault condition or the result of a manual override." },
            { 2, "Device is enabled, but is waiting for some conditions to be fulfilled before it will start running." },
            { 3, "Device is enabled and running." } };
        */
        public static Dictionary<int, DGN> DGNs = new Dictionary<int, DGN>();

        static RVC()
        {
            SeedData();
        }
        private static Dictionary<int, string> mnsMkr(params string[] meanings)
        {
            var ret = new Dictionary<int, string>();
            int cnt = 0;
            foreach (var meaning in meanings)
                ret.Add(cnt++, meaning);
            return ret;
        }
        public static void SeedData()
        {
            DGN newDgn;

            newDgn = new DGN(1) { Dgn = 0x1FF9C, Name = "THERMOSTAT_AMBIENT_STATUS" };
            newDgn.Parameters.Add(new("Ambient temperature") { ShortName = "Tamb", Type = paramTyp.temperature, Size = 16, frstByte = 1 });
            DGNs.Add(newDgn.Dgn, newDgn);

            newDgn = new DGN(1) { Dgn = 0x1FFE4, Name = "FURNACE_STATUS" };

            newDgn.Parameters.Add(new("Operating mode") { ShortName = "Op mode", Type = paramTyp.natural, Size = 2, frstByte = 1, Meanings = new() { [0] = "Automatic", [1] = "Manual" } });
            newDgn.Parameters.Add(new("Heat Source")
            {
                ShortName = "Heat Src",
                Type = paramTyp.natural,
                Size = 6,
                frstByte = 1,
                frstBit = 2,
                Meanings = new() { [0] = "Combustion", [1] = "AC power primary", [2] = "AC power secondary", [3] = "Engine Heat" }
            });

            newDgn.Parameters.Add(new("Circulation fan speed") { ShortName = "Fan%", Type = paramTyp.percent, frstByte = 2, frstBit = 0 });
            newDgn.Parameters.Add(new("Heat output level") { ShortName = "Heat%", Type = paramTyp.percent, frstByte = 3, frstBit = 0 });
            newDgn.Parameters.Add(new("Dead band") { ShortName = "Db", Type = paramTyp.custom, frstByte = 4, coefficient = 0.1, Unit = "C" });
            newDgn.Parameters.Add(new("Dead band level 2") { ShortName = "Db2", Type = paramTyp.custom, frstByte = 5, coefficient = 0.1, Unit = "C" });
            newDgn.Parameters.Add(new("Zone overcurrent status") { ShortName = "Zone OC", Type = paramTyp.boolean, Size = 2, frstByte = 6, frstBit = 0 });
            newDgn.Parameters.Add(new("Zone undercurrent status") { ShortName = "Zone UC", Type = paramTyp.boolean, Size = 2, frstByte = 6, frstBit = 2 });
            newDgn.Parameters.Add(new("Zone temperature status")
            {
                ShortName = "Zone t Status",
                Type = paramTyp.boolean,
                Size = 2,
                frstByte = 6,
                frstBit = 4,
                Meanings = new() { [0] = "Normal", [1] = "Warning" }
            });
            newDgn.Parameters.Add(new("Zone analog input status")
            {
                ShortName = "Zone an",
                Type = paramTyp.boolean,
                Size = 2,
                frstByte = 6,
                frstBit = 6,
                Meanings = new() { [0] = "Off(Inactive)", [1] = "On(Active)" }
            });
            DGNs.Add(newDgn.Dgn, newDgn);

            newDgn = new DGN(1) { Dgn = 0x1FFE3, Name = "FURNACE_COMMAND" };

            newDgn.Parameters.Add(new("Operating mode") { ShortName = "Op mode", Type = paramTyp.natural, Size = 2, frstByte = 1, Meanings = new() { [0] = "Automatic", [1] = "Manual" } });
            newDgn.Parameters.Add(new Parameter
            {
                Name = "Heat Source",
                ShortName = "Heat Src",
                Type = paramTyp.natural,
                Size = 6,
                frstByte = 1,
                frstBit = 2,
                Meanings = new() { [0] = "Combustion", [1] = "AC power primary", [2] = "AC power secondary", [3] = "Engine Heat" }
            });

            newDgn.Parameters.Add(new Parameter { Name = "Circulation fan speed", ShortName = "Fan%", Type = paramTyp.percent, frstByte = 2, frstBit = 0 });
            newDgn.Parameters.Add(new Parameter { Name = "Heat output level", ShortName = "Heat%", Type = paramTyp.percent, frstByte = 3, frstBit = 0 });
            newDgn.Parameters.Add(new Parameter { Name = "Dead band", ShortName = "Db", Type = paramTyp.custom, frstByte = 4, coefficient = 0.1, Unit = "C" });
            newDgn.Parameters.Add(new Parameter { Name = "Dead band level 2", ShortName = "Db2", Type = paramTyp.custom, frstByte = 5, coefficient = 0.1, Unit = "C" });

            DGNs.Add(newDgn.Dgn, newDgn);

            newDgn = new DGN(1) { Dgn = 0x1FFF7, Name = "WATERHEATER_STATUS" };

            newDgn.Parameters.Add(new Parameter
            {
                Name = "Operating modes",
                ShortName = "Mode",
                Type = paramTyp.natural,
                Size = 8,
                frstByte = 1,
                Meanings = new() { [0] = "off", [1] = "combustion", [2] = "electric", [3] = "gas/electric (both)", [4] = "test combustion (forced on)", [5] = "test combustion (forced on)", [6] = "test electric (forced on)" }
            });

            newDgn.Parameters.Add(new Parameter { Name = "Set point temperature", ShortName = "SP T", Type = paramTyp.temperature, Size = 16, frstByte = 2 });

            newDgn.Parameters.Add(new Parameter { Name = "Water temperature", ShortName = "Twater", Type = paramTyp.temperature, Size = 16, frstByte = 4 });

            newDgn.Parameters.Add(new Parameter
            {
                Name = "Thermostat status",
                Type = paramTyp.boolean,
                Size = 2,
                frstByte = 6,
                frstBit = 0,
                Meanings = new() { [0] = "set point met", [1] = "set point not met (heat is being applied)" }
            });

            newDgn.Parameters.Add(new Parameter
            {
                Name = "Burner status",
                Type = paramTyp.boolean,
                Size = 2,
                frstByte = 6,
                frstBit = 2,
                Meanings = new() { [0] = "off", [1] = "burner is lit" }
            });

            newDgn.Parameters.Add(new Parameter
            {
                Name = "AC element status",
                Type = paramTyp.boolean,
                Size = 2,
                frstByte = 6,
                frstBit = 4,
                Meanings = new() { [0] = "AC element is inactive", [1] = "AC element is active)" }
            });


            newDgn.Parameters.Add(new Parameter
            {
                Name = "High temperature limit switch status",
                Type = paramTyp.boolean,
                Size = 2,
                frstByte = 6,
                frstBit = 6,
                Meanings = new() { [0] = "limit switch not tripped", [1] = "limit switch tripped" }
            });

            newDgn.Parameters.Add(new Parameter
            {
                Name = "Failure to ignite status",
                Type = paramTyp.boolean,
                Size = 2,
                frstByte = 7,
                frstBit = 0,
                Meanings = new() { [0] = "no failure", [1] = "device has failed to ignite" }
            });

            newDgn.Parameters.Add(new Parameter
            {
                Name = "AC power failure status",
                Type = paramTyp.boolean,
                Size = 2,
                frstByte = 7,
                frstBit = 2,
                Meanings = new() { [0] = "AC power present", [1] = "AC power not present" }
            });

            newDgn.Parameters.Add(new Parameter
            {
                Name = "DC power failure status",
                Type = paramTyp.boolean,
                Size = 2,
                frstByte = 7,
                frstBit = 4,
                Meanings = new() { [0] = "DC power present", [1] = "DC power not present" }
            });

            newDgn.Parameters.Add(new Parameter
            {
                Name = "DC power warning status",
                Type = paramTyp.boolean,
                Size = 2,
                frstByte = 7,
                frstBit = 6,
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
                frstByte = 1,
                Meanings = new() { [0] = "off", [1] = "combustion", [2] = "electric", [3] = "gas/electric (both)", [4] = "test combustion (forced on)", [5] = "test combustion (forced on)", [6] = "test electric (forced on)" }
            });

            newDgn.Parameters.Add(new Parameter { Name = "Set point temperature", ShortName = "SP T", Type = paramTyp.temperature, Size = 16, frstByte = 2 });
            newDgn.Parameters.Add(new Parameter { Name = "Electric Element Level", ShortName = "Elec lvl", Type = paramTyp.natural, Size = 4, frstByte = 6 });

            DGNs.Add(newDgn.Dgn, newDgn);

            newDgn = new DGN(1) { Dgn = 0x1FFE2, Name = "THERMOSTAT_STATUS_1" };

            newDgn.Parameters.Add(new Parameter
            {
                Name = "Operating mode",
                Type = paramTyp.natural,
                Size = 4,
                frstByte = 1,
                Meanings = new() { [0] = "Off", [1] = "Cool", [2] = "Heat", [3] = "Auto heat/Cool", [4] = "Fan only", [5] = "Aux heat", [6] = "Window Defrost/Dehumidify" }
            });

            newDgn.Parameters.Add(new()
            {
                Name = "Fan mode",
                Type = paramTyp.boolean,
                Size = 2,
                frstByte = 1,
                frstBit = 4,
                Meanings = new() { [0] = "Auto", [1] = "On" }
            });

            newDgn.Parameters.Add(new()
            {
                Name = "Schedule mode",
                Type = paramTyp.boolean,
                Size = 2,
                frstByte = 1,
                frstBit = 6,
                Meanings = new() { [0] = "Disabled", [1] = "Enabled" }
            });

            newDgn.Parameters.Add(new()
            {
                Name = "Fan speed",
                Type = paramTyp.percent,
                Size = 8,
                frstByte = 2,
            });

            newDgn.Parameters.Add(new()
            {
                Name = "Setopint temp - Heat",
                Type = paramTyp.temperature,
                Size = 16,
                frstByte = 3,
            });

            newDgn.Parameters.Add(new()
            {
                Name = "Setopint temp - Cool",
                Type = paramTyp.temperature,
                Size = 16,
                frstByte = 5,
            });

            DGNs.Add(newDgn.Dgn, newDgn);


            newDgn = new DGN(1) { Dgn = 0x1FEF9, Name = "THERMOSTAT_COMMAND_1" };

            newDgn.Parameters.Add(new()
            {
                Name = "Operating mode",
                Type = paramTyp.natural,
                Size = 4,
                frstByte = 1,
                Meanings = new() { [0] = "Off", [1] = "Cool", [2] = "Heat", [3] = "Auto heat/Cool", [4] = "Fan only", [5] = "Aux heat", [6] = "Window Defrost/Dehumidify" }
            });

            newDgn.Parameters.Add(new()
            {
                Name = "Fan mode",
                Type = paramTyp.boolean,
                Size = 2,
                frstByte = 1,
                frstBit = 4,
                Meanings = new() { [0] = "Auto", [1] = "On" }
            });

            newDgn.Parameters.Add(new()
            {
                Name = "Schedule mode",
                Type = paramTyp.boolean,
                Size = 2,
                frstByte = 1,
                frstBit = 6,
                Meanings = new() { [0] = "Disabled", [1] = "Enabled" }
            });

            newDgn.Parameters.Add(new()
            {
                Name = "Fan speed",
                Type = paramTyp.percent,
                Size = 8,
                frstByte = 2,
            });

            newDgn.Parameters.Add(new()
            {
                Name = "Setopint temp - Heat",
                Type = paramTyp.temperature,
                Size = 16,
                frstByte = 3,
            });

            newDgn.Parameters.Add(new()
            {
                Name = "Setopint temp - Cool",
                Type = paramTyp.temperature,
                Size = 16,
                frstByte = 5,
            });

            DGNs.Add(newDgn.Dgn, newDgn);


            newDgn = new DGN(1) { Dgn = 0x1FEFA, Name = "THERMOSTAT_STATUS_2" };
            newDgn.Parameters.Add(new()
            {
                Name = "Current schedule instatnce",
                Type = paramTyp.natural,
                Size = 8,
                frstByte = 1,
                Meanings = new() { [0] = "Sleep", [1] = "Wake", [2] = "Away", [3] = "Return", [250] = "Storage" }
            });

            newDgn.Parameters.Add(new()
            {
                Name = "Number of schedule instances",
                Type = paramTyp.custom,
                Size = 8,
                frstByte = 2,
            });


            newDgn.Parameters.Add(new()
            {
                Name = "Reduced noise mode",
                Type = paramTyp.boolean,
                Size = 2,
                frstByte = 3,
                Meanings = new() { [0] = "Disabled", [1] = "Endabled" }
            });

            DGNs.Add(newDgn.Dgn, newDgn);

            newDgn = new DGN(1) { Dgn = 0x1FEF8, Name = "THERMOSTAT_COMMAND_2" };
            newDgn.Parameters.Add(new()
            {
                Name = "Current schedule instatnce",
                Type = paramTyp.natural,
                Size = 8,
                frstByte = 1,
                Meanings = new() { [0] = "Sleep", [1] = "Wake", [2] = "Away", [3] = "Return", [250] = "Storage", [251] = "Reset to \"current\" instance" }
            });

            newDgn.Parameters.Add(new()
            {
                Name = "Number of schedule instances",
                Type = paramTyp.custom,
                Size = 8,
                frstByte = 2,
            });


            newDgn.Parameters.Add(new()
            {
                Name = "Reduced noise mode",
                Type = paramTyp.boolean,
                Size = 2,
                frstByte = 3,
                Meanings = new() { [0] = "Disabled", [1] = "Endabled" }
            });

            DGNs.Add(newDgn.Dgn, newDgn);

            newDgn = new DGN(2) { Dgn = 0x1FEF7, Name = "THERMOSTAT_SCHEDULE_STATUS_1" };

            newDgn.Parameters.Add(new()
            {
                Name = "Schedule mode instance",
                Type = paramTyp.natural,
                Size = 8,
                frstByte = 1,
                Id = true,
                Meanings = new() { [0] = "Sleep", [1] = "Wake", [2] = "Away", [3] = "Return", [250] = "Storage" }
            });

            newDgn.Parameters.Add(new()
            {
                Name = "Start hour",
                Type = paramTyp.custom,
                Size = 8,
                frstByte = 2,
            });

            newDgn.Parameters.Add(new()
            {
                Name = "Start minute",
                Type = paramTyp.custom,
                Size = 8,
                frstByte = 3,
            });

            newDgn.Parameters.Add(new()
            {
                Name = "Setopint temp - Heat",
                Type = paramTyp.temperature,
                Size = 16,
                frstByte = 4,
            });

            newDgn.Parameters.Add(new()
            {
                Name = "Setopint temp - Cool",
                Type = paramTyp.temperature,
                Size = 16,
                frstByte = 6,
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
                frstByte = 1,
                Id = true,
                Meanings = new() { [0] = "Sleep", [1] = "Wake", [2] = "Away", [3] = "Return", [250] = "Storage" }
            });

            newDgn.Parameters.Add(new()
            {
                Name = "Sunday",
                Type = paramTyp.boolean,
                Size = 2,
                frstByte = 2,
                frstBit = 0,
                Meanings = new() { [0] = "Not scheduled for this day", [1] = "Schedule applies to this day" }
            });



            newDgn.Parameters.Add(new()
            {
                Name = "Monday",
                Type = paramTyp.boolean,
                Size = 2,
                frstByte = 2,
                frstBit = 2,
                Meanings = new() { [0] = "Not scheduled for this day", [1] = "Schedule applies to this day" }
            });

            newDgn.Parameters.Add(new()
            {
                Name = "Tuesday",
                Type = paramTyp.natural,
                Size = 2,
                frstByte = 2,
                frstBit = 4,
                Meanings = new() { [0] = "Not scheduled for this day", [1] = "Schedule applies to this day" }
            });

            newDgn.Parameters.Add(new()
            {
                Name = "Wednesday",
                Type = paramTyp.boolean,
                Size = 2,
                frstByte = 2,
                frstBit = 6,
                Meanings = new() { [0] = "Not scheduled for this day", [1] = "Schedule applies to this day" }
            });

            newDgn.Parameters.Add(new()
            {
                Name = "Thursday",
                Type = paramTyp.boolean,
                Size = 2,
                frstByte = 3,
                frstBit = 0,
                Meanings = new() { [0] = "Not scheduled for this day", [1] = "Schedule applies to this day" }
            });

            newDgn.Parameters.Add(new()
            {
                Name = "Friday",
                Type = paramTyp.boolean,
                Size = 2,
                frstByte = 3,
                frstBit = 2,
                Meanings = new() { [0] = "Not scheduled for this day", [1] = "Schedule applies to this day" }
            });

            newDgn.Parameters.Add(new()
            {
                Name = "Saturday",
                Type = paramTyp.boolean,
                Size = 2,
                frstByte = 3,
                frstBit = 4,
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
                frstByte = 1,
                Meanings = new() { [0] = "Off", [1] = "On", [5] = "Test (Forced On)" }
            });

            newDgn.Parameters.Add(new()
            {
                Name = "Pump Overcurrent Status",
                Type = paramTyp.boolean,
                Size = 2,
                frstByte = 2,
                Meanings = new() { [0] = "No overcurrent detected", [1] = "Overcurrent detected" }
            });
            newDgn.Parameters.Add(new()
            {
                Name = "Pump Undercurrent Status",
                Type = paramTyp.boolean,
                Size = 2,
                frstByte = 2,
                frstBit = 2,
                Meanings = new() { [0] = "No undercurrent detected", [1] = "Undercurrent detected" }
            });

            newDgn.Parameters.Add(new()
            {
                Name = "Pump Temperature Status",
                Type = paramTyp.boolean,
                Size = 2,
                frstByte = 1,
                frstBit = 4,
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
                frstByte = 1
            });

            newDgn.Parameters.Add(new()
            {
                Name = "Day of month",
                Type = paramTyp.natural,
                frstByte = 2
            });

            newDgn.Parameters.Add(new()
            {
                Name = "Day of week",
                Type = paramTyp.natural,
                frstByte = 3
            });

            newDgn.Parameters.Add(new()
            {
                Name = "Hour",
                Type = paramTyp.natural,
                frstByte = 4
            });

            newDgn.Parameters.Add(new()
            {
                Name = "Minute",
                Type = paramTyp.natural,
                frstByte = 5
            });

            newDgn.Parameters.Add(new()
            {
                Name = "Second",
                Type = paramTyp.natural,
                frstByte = 6
            });

            DGNs.Add(newDgn.Dgn, newDgn);

            newDgn = new DGN() { Dgn = 0x1FEFB, Name = "FLOOR_HEAT_COMMAND" };
            newDgn.Parameters.Add(new()
            {
                Name = "Operating mode",
                Size = 2,
                frstByte = 1,
                Meanings = mnsMkr("Automatic","Manual")
            });

            newDgn.Parameters.Add(new()
            {
                Name = "Operating status",
                Size = 2,
                frstByte = 1,
                frstBit = 2,
                Meanings = defMeaningsOnOff
            });

            newDgn.Parameters.Add(new()
            {
                Name = "Heat element status",
                Size = 2,
                frstByte = 1,
                frstBit = 4,
                Meanings = defMeaningsOnOff
            });

            newDgn.Parameters.Add(new()
            {
                Name = "Schedule mode",
                Size = 2,
                frstByte = 1,
                frstBit = 6,
                Meanings = defMeaningsOnOff
            });

            newDgn.Parameters.Add(new()
            {
                Name = "Setpoint",
                Size = 16,
                frstByte = 2,
                Type = paramTyp.temperature
            });

            newDgn.Parameters.Add(new()
            {
                Name = "Dead band",
                Size = 8,
                frstByte = 4,
                coefficient = 0.1,
                Unit = "°C"
            }); ;

            DGNs.Add(newDgn.Dgn, newDgn);

            newDgn = new DGN() { Dgn = 0x1FE99, Name = "WATERHEATER_STATUS_2" };

            newDgn.Parameters.Add(new()
            {
                Name = "Electric Element Level",
                Size = 4,
                frstByte = 1
            });

            newDgn.Parameters.Add(new()
            {
                Name = "Max Electric Element Leve",
                Size = 4,
                frstByte = 1,
                frstBit = 4
            });

            newDgn.Parameters.Add(new()
            {
                Name = "Engine Preheat",
                Size = 4,
                frstByte = 2,
                Type = paramTyp.natural,
                Meanings = new() { [0] = "Off", [1] = "On", [5] = "Test(Forced On)" }
            });

            newDgn.Parameters.Add(new()
            {
                Name = "Coolant Level Warning",
                Size = 2,
                frstByte = 2,
                frstBit = 4,
                Type = paramTyp.boolean,
                Meanings = new() { [0] = "Coolant level sufficient", [1] = "Coolant level low" }
            });

            newDgn.Parameters.Add(new()
            {
                Name = "Hot Water Priority",
                Size = 2,
                frstByte = 2,
                frstBit = 6,
                Type = paramTyp.natural,
                Meanings = new() { [0] = "Domestic water priority", [1] = "Heating priority" }
            });
            DGNs.Add(newDgn.Dgn, newDgn);

            newDgn = new() { Name = "GENERATOR_DEMAND_STATUS", Dgn = 0xFF80, idLength = 0 };
            newDgn.Parameters.Add(new () { Name = "Generator demand", frstByte = 0, frstBit = 0, Size = 2, Meanings = mnsMkr("No demand for generator", "Generator is demanded") });
            newDgn.Parameters.Add(new () { Name = "Internal generator demand", frstByte = 0, frstBit = 2, Size = 2, Meanings = mnsMkr("No internal demand", "Internal AGS criterion is demanding generator") });
            newDgn.Parameters.Add(new () { Name = "Network generator demand", frstByte = 0, frstBit = 4, Size = 2, Meanings = mnsMkr("No demand from other network nodes", "Network device is demanding generator") });
            newDgn.Parameters.Add(new () { Name = "External activity detected", frstByte = 0, frstBit = 6, Size = 2, Meanings = mnsMkr("Automatic starting is allowed", "Automatic starting is disabled due to the detection of external activity") });
            newDgn.Parameters.Add(new () { Name = "Manual override detected", frstByte = 1, frstBit = 0, Size = 2, Meanings = mnsMkr("Normal Operation", "Manual Override") });
            newDgn.Parameters.Add(new () { Name = "Quiet time", frstByte = 1, frstBit = 2, Size = 2, Meanings = mnsMkr("Unit is not in Quiet Time", "Unit is in Quiet Time") });
            newDgn.Parameters.Add(new () { Name = "Quiet time override", frstByte = 1, frstBit = 4, Size = 2, Meanings = mnsMkr("Normal operation", "Quiet Time is being overridden") });
            newDgn.Parameters.Add(new () { Name = "Generator lock", frstByte = 0, frstBit = 6, Size = 2, Meanings = mnsMkr("Normal operation", "Genset is locked. Node will not start generator for any reason") });
            newDgn.Parameters.Add(new () { Name = "Network generator demand", frstByte = 0, frstBit = 4, Size = 2, Meanings = mnsMkr("No demand from other network nodes", "Network device is demanding generator") });
            newDgn.Parameters.Add(new () { Name = "Quiet time begin hour", frstByte = 2, frstBit = 0, Unit = "h" });
            DGNs.Add(newDgn.Dgn, newDgn);

            newDgn = new() { Name = "DM_RV", Dgn = 0x1FECA, idLength = 0 };
            newDgn.Parameters.Add(new () { Name = "Operating Status", frstByte = 0, frstBit = 0, Size = 2, Meanings = mnsMkr("Disabled","Enabled") });
            newDgn.Parameters.Add(new () { Name = "Operating Status", frstByte = 0, frstBit = 2, Size = 2, Meanings = mnsMkr("Standby/Idle","Running") });
            newDgn.Parameters.Add(new () { Name = "Yellow lamp status", frstByte = 0, frstBit = 4, Size = 2, Meanings = defMeaningsOnOff });
            newDgn.Parameters.Add(new () { Name = "Red lamp status", frstByte = 0, frstBit = 5, Size = 2, Meanings = defMeaningsOnOff });
            newDgn.Parameters.Add(new () { Name = "DSA", frstByte = 1, frstBit = 0, Id = true });
            newDgn.Parameters.Add(new () { Name = "SPN - MSB", frstByte = 2, frstBit = 0});
            newDgn.Parameters.Add(new () { Name = "SPN - ISB", frstByte = 3, frstBit = 0 });
            newDgn.Parameters.Add(new () { Name = "SPN - LSB", frstByte = 4, frstBit = 5, Size = 3 });
            newDgn.Parameters.Add(new () { Name = "FMI", frstByte = 4, frstBit = 0, Size = 5 });
            newDgn.Parameters.Add(new () { Name = "Occurrence count", frstByte = 5, frstBit = 0, Size = 6, Meanings = new() { { 0x7F, "Not available" } } });
            newDgn.Parameters.Add(new () { Name = "DSA extension", frstByte = 6, frstBit = 0,  Id = true });
            newDgn.Parameters.Add(new () { Name = "Bank select", frstByte = 7, frstBit = 0,  Meanings = new () { { 15, "Not supported" } } });
            DGNs.Add(newDgn.Dgn, newDgn);

            newDgn = new(1) { Name = "Timberline Extension", MultiPack=true, Dgn = 0x1EF65};
            newDgn.Parameters.Add(new() { multipackNum = 0x84, Name = "Solenoid", frstByte = 1, Size = 2, Meanings = defMeaningsOnOff });
            newDgn.Parameters.Add(new() { multipackNum = 0x84, Name = "Tank temperature", Type = paramTyp.temperature, frstByte = 2, Size = 16 });
            newDgn.Parameters.Add(new() { multipackNum = 0x84, Name = "Heater temperature", Type = paramTyp.temperature, frstByte = 4, Size = 16 });
            newDgn.Parameters.Add(new() { multipackNum = 0x84, Name = "Manual fan speed", Type = paramTyp.percent, frstByte = 6, Size = 8 });

            newDgn.Parameters.Add(new() { multipackNum = 0x85, Name = "System timer", Unit="s", frstByte = 1, Size = 24 });
            newDgn.Parameters.Add(new() { multipackNum = 0x85, Name = "Water priority timer", Unit = "s", frstByte = 4, Size = 16 });
            newDgn.Parameters.Add(new() { multipackNum = 0x85, Name = "Pump timer", Unit = "s", frstByte = 6, Size = 16 });

            newDgn.Parameters.Add(new() { multipackNum = 0x86, Name = "Total heater minutes", Unit = "m", frstByte = 1, Size = 24 });
            newDgn.Parameters.Add(new() { multipackNum = 0x86, Name = "Heater version 1", frstByte = 4, Size = 8 });
            newDgn.Parameters.Add(new() { multipackNum = 0x86, Name = "Heater version 2", frstByte = 5, Size = 8 });
            newDgn.Parameters.Add(new() { multipackNum = 0x86, Name = "Heater version 3", frstByte = 6, Size = 8 });
            newDgn.Parameters.Add(new() { multipackNum = 0x86, Name = "Heater version 4", frstByte = 7, Size = 8 });

            newDgn.Parameters.Add(new() { multipackNum = 0x87, Name = "Minutes since start", frstByte = 1, Size = 24, Unit = "min" });
            newDgn.Parameters.Add(new() { multipackNum = 0x87, Name = "Panel version 1", frstByte = 4, Size = 8 });
            newDgn.Parameters.Add(new() { multipackNum = 0x87, Name = "Panel version 2", frstByte = 5, Size = 8 });
            newDgn.Parameters.Add(new() { multipackNum = 0x87, Name = "Panel version 3", frstByte = 6, Size = 8 });
            newDgn.Parameters.Add(new() { multipackNum = 0x87, Name = "Panel version 4", frstByte = 7, Size = 8 });

            newDgn.Parameters.Add(new() { multipackNum = 0x88, Name = "HCU version 1", frstByte = 4, Size = 8 });
            newDgn.Parameters.Add(new() { multipackNum = 0x88, Name = "HCU version 2", frstByte = 5, Size = 8 });
            newDgn.Parameters.Add(new() { multipackNum = 0x88, Name = "HCU version 3", frstByte = 6, Size = 8 });
            newDgn.Parameters.Add(new() { multipackNum = 0x88, Name = "HCU version 4", frstByte = 7, Size = 8 });

            newDgn.Parameters.Add(new() { multipackNum = 0x89, Name = "System limitation", frstByte = 1, Size = 16,Unit = "min" });
            newDgn.Parameters.Add(new() { multipackNum = 0x89, Name = "Water limitation", frstByte = 3, Size = 8, Unit = "min" });

            newDgn.Parameters.Add(new() { multipackNum = 0x8A, Name = "System limitation", frstByte = 1, Size = 16, Unit = "min" });
            newDgn.Parameters.Add(new() { multipackNum = 0x8A, Name = "Water limitation", frstByte = 3, Size = 8, Unit = "min" });

            newDgn.Parameters.Add(new() { multipackNum = 0xA0, Name = "Tank Temperature", frstByte = 1, Size = 8, Type = paramTyp.temperature});
            newDgn.Parameters.Add(new() { multipackNum = 0xA0, Name = "Heater Temperature", frstByte = 2, Size = 8, Type = paramTyp.temperature });
            newDgn.Parameters.Add(new() { multipackNum = 0xA0, Name = "Zone 1 fan manual percent", frstByte = 3, Size = 8, Type = paramTyp.percent });
            newDgn.Parameters.Add(new() { multipackNum = 0xA0, Name = "Zone 2 fan manual percent", frstByte = 4, Size = 8, Type = paramTyp.percent });
            newDgn.Parameters.Add(new() { multipackNum = 0xA0, Name = "Zone 3 fan manual percent", frstByte = 5, Size = 8, Type = paramTyp.percent });
            newDgn.Parameters.Add(new() { multipackNum = 0xA0, Name = "Zone 4 fan manual percent", frstByte = 6, Size = 8, Type = paramTyp.percent });
            newDgn.Parameters.Add(new() { multipackNum = 0xA0, Name = "Zone 5 fan manual percent", frstByte = 7, Size = 8, Type = paramTyp.percent });

            newDgn.Parameters.Add(new() { multipackNum = 0xA1, Name = "System timer", frstByte = 1, Size = 24, Type = paramTyp.seconds });

            newDgn.Parameters.Add(new() { multipackNum = 0xA2, Name = "Loop 1 pump timer", frstByte = 1, Size = 16, Type = paramTyp.seconds });
            newDgn.Parameters.Add(new() { multipackNum = 0xA2, Name = "Loop 2 pump timer", frstByte = 3, Size = 16, Type = paramTyp.seconds });
            newDgn.Parameters.Add(new() { multipackNum = 0xA2, Name = "Heater pump timer", frstByte = 5, Size = 16, Type = paramTyp.seconds });
            
            newDgn.Parameters.Add(new() { multipackNum = 0xA3, Name = "AUX pump 1 timer", frstByte = 1, Size = 16, Type = paramTyp.seconds });
            newDgn.Parameters.Add(new() { multipackNum = 0xA3, Name = "AUX pump 2 timer", frstByte = 3, Size = 16, Type = paramTyp.seconds });
            newDgn.Parameters.Add(new() { multipackNum = 0xA3, Name = "AUX pump 3 timer", frstByte = 5, Size = 16, Type = paramTyp.seconds });

            newDgn.Parameters.Add(new() { multipackNum = 0xA4, Name = "Heater total minutes", frstByte = 1, Size = 24, Type = paramTyp.minutes });
            newDgn.Parameters.Add(new() { multipackNum = 0xA4, Name = "Heater version byte 1", frstByte = 4, Size = 8, Type = paramTyp.natural });
            newDgn.Parameters.Add(new() { multipackNum = 0xA4, Name = "Heater version byte 2", frstByte = 5, Size = 8, Type = paramTyp.natural });
            newDgn.Parameters.Add(new() { multipackNum = 0xA4, Name = "Heater version byte 3", frstByte = 6, Size = 8, Type = paramTyp.natural });
            newDgn.Parameters.Add(new() { multipackNum = 0xA4, Name = "Heater version byte 4", frstByte = 7, Size = 8, Type = paramTyp.natural });

            newDgn.Parameters.Add(new() { multipackNum = 0xA5, Name = "Panel version byte 1", frstByte = 4, Size = 8, Type = paramTyp.natural });
            newDgn.Parameters.Add(new() { multipackNum = 0xA5, Name = "Panel version byte 2", frstByte = 5, Size = 8, Type = paramTyp.natural });
            newDgn.Parameters.Add(new() { multipackNum = 0xA5, Name = "Panel version byte 3", frstByte = 6, Size = 8, Type = paramTyp.natural });
            newDgn.Parameters.Add(new() { multipackNum = 0xA5, Name = "Panel version byte 4", frstByte = 7, Size = 8, Type = paramTyp.natural });

            newDgn.Parameters.Add(new() { multipackNum = 0xA6, Name = "HCU version byte 1", frstByte = 4, Size = 8, Type = paramTyp.natural });
            newDgn.Parameters.Add(new() { multipackNum = 0xA6, Name = "HCU version byte 2", frstByte = 5, Size = 8, Type = paramTyp.natural });
            newDgn.Parameters.Add(new() { multipackNum = 0xA6, Name = "HCU version byte 3", frstByte = 6, Size = 8, Type = paramTyp.natural });
            newDgn.Parameters.Add(new() { multipackNum = 0xA6, Name = "HCU version byte 4", frstByte = 7, Size = 8, Type = paramTyp.natural });

            newDgn.Parameters.Add(new() { multipackNum = 0xA7, Name = "System time limit", frstByte = 1, Size = 1, Type = paramTyp.hours });
            newDgn.Parameters.Add(new() { multipackNum = 0xA7, Name = "Pump overridelimit", frstByte = 2, Size = 1, Type = paramTyp.minutes });



            DGNs.Add(newDgn.Dgn, newDgn);
        }



    }


}
