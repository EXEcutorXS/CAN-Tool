using OmniProtocol;
using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;


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
        public static List<Parameter> Parameters { set; get; }

        static RVC()
        {
            //SeedData();
            try
            {
                System.Diagnostics.Debug.WriteLine("RVC static constructor started");
                SeedData();
                System.Diagnostics.Debug.WriteLine("RVC static constructor completed");
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"RVC static constructor failed: {ex.Message}");
                System.Diagnostics.Debug.WriteLine($"Stack: {ex.StackTrace}");
                if (ex.InnerException != null)
                {
                    System.Diagnostics.Debug.WriteLine($"Inner: {ex.InnerException.Message}");
                    System.Diagnostics.Debug.WriteLine($"Inner Stack: {ex.InnerException.StackTrace}");
                }
                throw; // Re-throw to see in debugger
            }
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
            // Сначала создаем все DGN объекты без параметров
            var dgnsList = new List<DGN>
    {
        new DGN(1) { Dgn = 0x1FF9C, Name = "THERMOSTAT_AMBIENT_STATUS" },
        new DGN(1) { Dgn = 0x1FFE4, Name = "FURNACE_STATUS" },
        new DGN(1) { Dgn = 0x1FFE3, Name = "FURNACE_COMMAND" },
        new DGN(1) { Dgn = 0x1FFF7, Name = "WATERHEATER_STATUS" },
        new DGN(1) { Dgn = 0x1FFF6, Name = "WATERHEATER_COMMAND" },
        new DGN(1) { Dgn = 0x1FFE2, Name = "THERMOSTAT_STATUS_1" },
        new DGN(1) { Dgn = 0x1FEF9, Name = "THERMOSTAT_COMMAND_1" },
        new DGN(1) { Dgn = 0x1FEFA, Name = "THERMOSTAT_STATUS_2" },
        new DGN(1) { Dgn = 0x1FEF8, Name = "THERMOSTAT_COMMAND_2" },
        new DGN(2) { Dgn = 0x1FEF7, Name = "THERMOSTAT_SCHEDULE_STATUS_1" },
        new DGN(1) { Dgn = 0x1FEF5, Name = "THERMOSTAT_SCHEDULE_COMMAND_1" },
        new DGN(2) { Dgn = 0x1FEF6, Name = "THERMOSTAT_SCHEDULE_STATUS_2" },
        new DGN(1) { Dgn = 0x1FEF4, Name = "THERMOSTAT_SCHEDULE_COMMAND_2" },
        new DGN(1) { Dgn = 0x1FE97, Name = "CIRCULATION_PUMP_STATUS" },
        new DGN() { Dgn = 0x1FFFF, Name = "DATE_TIME_STATUS" },
        new DGN() { Dgn = 0x1FEFB, Name = "FLOOR_HEAT_COMMAND" },
        new DGN() { Dgn = 0x1FE99, Name = "WATERHEATER_STATUS_2" },
        new DGN() {  Dgn = 0xFF80, Name = "GENERATOR_DEMAND_STATUS",},
        new DGN() { Dgn = 0x1FECA, Name = "DM_RV"},
        new DGN(1) {Dgn = 0x1EF65, Name = "Timberline Extension", MultiPack = true,  }
    };

            // Добавляем DGN в словарь
            foreach (var dgn in dgnsList)
            {
                DGNs.TryAdd(dgn.Dgn, dgn);
            }

            // Теперь создаем все параметры с указанием DGN
            var allParameters = new List<Parameter>();

            // THERMOSTAT_AMBIENT_STATUS (0x1FF9C)
            allParameters.Add(new Parameter(0x1FF9C)
            {
                Name = "Ambient temperature",
                ShortName = "Tamb",
                Type = paramTyp.temperature,
                Size = 16,
                frstByte = 1
            });

            // FURNACE_STATUS (0x1FFE4)
            allParameters.Add(new Parameter(0x1FFE4)
            {
                Name = "Operating mode",
                ShortName = "Op mode",
                Type = paramTyp.natural,
                Size = 2,
                frstByte = 1,
                Meanings = new() { [0] = "Automatic", [1] = "Manual" }
            });
            allParameters.Add(new Parameter(0x1FFE4)
            {
                Name = "Heat Source",
                ShortName = "Heat Src",
                Type = paramTyp.natural,
                Size = 6,
                frstByte = 1,
                frstBit = 2,
                Meanings = new() { [0] = "Combustion", [1] = "AC power primary", [2] = "AC power secondary", [3] = "Engine Heat" }
            });
            allParameters.Add(new Parameter(0x1FFE4)
            {
                Name = "Circulation fan speed",
                ShortName = "Fan%",
                Type = paramTyp.percent,
                frstByte = 2,
                frstBit = 0
            });
            allParameters.Add(new Parameter(0x1FFE4)
            {
                Name = "Heat output level",
                ShortName = "Heat%",
                Type = paramTyp.percent,
                frstByte = 3,
                frstBit = 0
            });
            allParameters.Add(new Parameter(0x1FFE4)
            {
                Name = "Dead band",
                ShortName = "Db",
                Type = paramTyp.custom,
                frstByte = 4,
                coefficient = 0.1,
                Unit = "C"
            });
            allParameters.Add(new Parameter(0x1FFE4)
            {
                Name = "Dead band level 2",
                ShortName = "Db2",
                Type = paramTyp.custom,
                frstByte = 5,
                coefficient = 0.1,
                Unit = "C"
            });
            allParameters.Add(new Parameter(0x1FFE4)
            {
                Name = "Zone overcurrent status",
                ShortName = "Zone OC",
                Type = paramTyp.boolean,
                Size = 2,
                frstByte = 6,
                frstBit = 0
            });
            allParameters.Add(new Parameter(0x1FFE4)
            {
                Name = "Zone undercurrent status",
                ShortName = "Zone UC",
                Type = paramTyp.boolean,
                Size = 2,
                frstByte = 6,
                frstBit = 2
            });
            allParameters.Add(new Parameter(0x1FFE4)
            {
                Name = "Zone temperature status",
                ShortName = "Zone t Status",
                Type = paramTyp.boolean,
                Size = 2,
                frstByte = 6,
                frstBit = 4,
                Meanings = new() { [0] = "Normal", [1] = "Warning" }
            });
            allParameters.Add(new Parameter(0x1FFE4)
            {
                Name = "Zone analog input status",
                ShortName = "Zone an",
                Type = paramTyp.boolean,
                Size = 2,
                frstByte = 6,
                frstBit = 6,
                Meanings = new() { [0] = "Off(Inactive)", [1] = "On(Active)" }
            });

            // FURNACE_COMMAND (0x1FFE3)
            allParameters.Add(new Parameter(0x1FFE3)
            {
                Name = "Operating mode",
                ShortName = "Op mode",
                Type = paramTyp.natural,
                Size = 2,
                frstByte = 1,
                Meanings = new() { [0] = "Automatic", [1] = "Manual" }
            });
            allParameters.Add(new Parameter(0x1FFE3)
            {
                Name = "Heat Source",
                ShortName = "Heat Src",
                Type = paramTyp.natural,
                Size = 6,
                frstByte = 1,
                frstBit = 2,
                Meanings = new() { [0] = "Combustion", [1] = "AC power primary", [2] = "AC power secondary", [3] = "Engine Heat" }
            });
            allParameters.Add(new Parameter(0x1FFE3)
            {
                Name = "Circulation fan speed",
                ShortName = "Fan%",
                Type = paramTyp.percent,
                frstByte = 2,
                frstBit = 0
            });
            allParameters.Add(new Parameter(0x1FFE3)
            {
                Name = "Heat output level",
                ShortName = "Heat%",
                Type = paramTyp.percent,
                frstByte = 3,
                frstBit = 0
            });
            allParameters.Add(new Parameter(0x1FFE3)
            {
                Name = "Dead band",
                ShortName = "Db",
                Type = paramTyp.custom,
                frstByte = 4,
                coefficient = 0.1,
                Unit = "C"
            });
            allParameters.Add(new Parameter(0x1FFE3)
            {
                Name = "Dead band level 2",
                ShortName = "Db2",
                Type = paramTyp.custom,
                frstByte = 5,
                coefficient = 0.1,
                Unit = "C"
            });

            // WATERHEATER_STATUS (0x1FFF7)
            allParameters.Add(new Parameter(0x1FFF7)
            {
                Name = "Operating modes",
                ShortName = "Mode",
                Type = paramTyp.natural,
                Size = 8,
                frstByte = 1,
                Meanings = new() { [0] = "off", [1] = "combustion", [2] = "electric", [3] = "gas/electric (both)", [4] = "test combustion (forced on)", [5] = "test combustion (forced on)", [6] = "test electric (forced on)" }
            });
            allParameters.Add(new Parameter(0x1FFF7)
            {
                Name = "Set point temperature",
                ShortName = "SP T",
                Type = paramTyp.temperature,
                Size = 16,
                frstByte = 2
            });
            allParameters.Add(new Parameter(0x1FFF7)
            {
                Name = "Water temperature",
                ShortName = "Twater",
                Type = paramTyp.temperature,
                Size = 16,
                frstByte = 4
            });
            allParameters.Add(new Parameter(0x1FFF7)
            {
                Name = "Thermostat status",
                Type = paramTyp.boolean,
                Size = 2,
                frstByte = 6,
                frstBit = 0,
                Meanings = new() { [0] = "set point met", [1] = "set point not met (heat is being applied)" }
            });
            allParameters.Add(new Parameter(0x1FFF7)
            {
                Name = "Burner status",
                Type = paramTyp.boolean,
                Size = 2,
                frstByte = 6,
                frstBit = 2,
                Meanings = new() { [0] = "off", [1] = "burner is lit" }
            });
            allParameters.Add(new Parameter(0x1FFF7)
            {
                Name = "AC element status",
                Type = paramTyp.boolean,
                Size = 2,
                frstByte = 6,
                frstBit = 4,
                Meanings = new() { [0] = "AC element is inactive", [1] = "AC element is active)" }
            });
            allParameters.Add(new Parameter(0x1FFF7)
            {
                Name = "High temperature limit switch status",
                Type = paramTyp.boolean,
                Size = 2,
                frstByte = 6,
                frstBit = 6,
                Meanings = new() { [0] = "limit switch not tripped", [1] = "limit switch tripped" }
            });
            allParameters.Add(new Parameter(0x1FFF7)
            {
                Name = "Failure to ignite status",
                Type = paramTyp.boolean,
                Size = 2,
                frstByte = 7,
                frstBit = 0,
                Meanings = new() { [0] = "no failure", [1] = "device has failed to ignite" }
            });
            allParameters.Add(new Parameter(0x1FFF7)
            {
                Name = "AC power failure status",
                Type = paramTyp.boolean,
                Size = 2,
                frstByte = 7,
                frstBit = 2,
                Meanings = new() { [0] = "AC power present", [1] = "AC power not present" }
            });
            allParameters.Add(new Parameter(0x1FFF7)
            {
                Name = "DC power failure status",
                Type = paramTyp.boolean,
                Size = 2,
                frstByte = 7,
                frstBit = 4,
                Meanings = new() { [0] = "DC power present", [1] = "DC power not present" }
            });
            allParameters.Add(new Parameter(0x1FFF7)
            {
                Name = "DC power warning status",
                Type = paramTyp.boolean,
                Size = 2,
                frstByte = 7,
                frstBit = 6,
                Meanings = new() { [0] = "DC power sufficient", [1] = "DC power warning" }
            });

            // WATERHEATER_COMMAND (0x1FFF6)
            allParameters.Add(new Parameter(0x1FFF6)
            {
                Name = "Operating modes",
                ShortName = "Mode",
                Type = paramTyp.natural,
                Size = 8,
                frstByte = 1,
                Meanings = new() { [0] = "off", [1] = "combustion", [2] = "electric", [3] = "gas/electric (both)", [4] = "test combustion (forced on)", [5] = "test combustion (forced on)", [6] = "test electric (forced on)" }
            });
            allParameters.Add(new Parameter(0x1FFF6)
            {
                Name = "Set point temperature",
                ShortName = "SP T",
                Type = paramTyp.temperature,
                Size = 16,
                frstByte = 2
            });
            allParameters.Add(new Parameter(0x1FFF6)
            {
                Name = "Electric Element Level",
                ShortName = "Elec lvl",
                Type = paramTyp.natural,
                Size = 4,
                frstByte = 6
            });

            // THERMOSTAT_STATUS_1 (0x1FFE2)
            allParameters.Add(new Parameter(0x1FFE2)
            {
                Name = "Operating mode",
                Type = paramTyp.natural,
                Size = 4,
                frstByte = 1,
                Meanings = new() { [0] = "Off", [1] = "Cool", [2] = "Heat", [3] = "Auto heat/Cool", [4] = "Fan only", [5] = "Aux heat", [6] = "Window Defrost/Dehumidify" }
            });
            allParameters.Add(new Parameter(0x1FFE2)
            {
                Name = "Fan mode",
                Type = paramTyp.boolean,
                Size = 2,
                frstByte = 1,
                frstBit = 4,
                Meanings = new() { [0] = "Auto", [1] = "On" }
            });
            allParameters.Add(new Parameter(0x1FFE2)
            {
                Name = "Schedule mode",
                Type = paramTyp.boolean,
                Size = 2,
                frstByte = 1,
                frstBit = 6,
                Meanings = new() { [0] = "Disabled", [1] = "Enabled" }
            });
            allParameters.Add(new Parameter(0x1FFE2)
            {
                Name = "Fan speed",
                Type = paramTyp.percent,
                Size = 8,
                frstByte = 2,
            });
            allParameters.Add(new Parameter(0x1FFE2)
            {
                Name = "Setopint temp - Heat",
                Type = paramTyp.temperature,
                Size = 16,
                frstByte = 3,
            });
            allParameters.Add(new Parameter(0x1FFE2)
            {
                Name = "Setopint temp - Cool",
                Type = paramTyp.temperature,
                Size = 16,
                frstByte = 5,
            });

            // THERMOSTAT_COMMAND_1 (0x1FEF9)
            allParameters.Add(new Parameter(0x1FEF9)
            {
                Name = "Operating mode",
                Type = paramTyp.natural,
                Size = 4,
                frstByte = 1,
                Meanings = new() { [0] = "Off", [1] = "Cool", [2] = "Heat", [3] = "Auto heat/Cool", [4] = "Fan only", [5] = "Aux heat", [6] = "Window Defrost/Dehumidify" }
            });
            allParameters.Add(new Parameter(0x1FEF9)
            {
                Name = "Fan mode",
                Type = paramTyp.boolean,
                Size = 2,
                frstByte = 1,
                frstBit = 4,
                Meanings = new() { [0] = "Auto", [1] = "On" }
            });
            allParameters.Add(new Parameter(0x1FEF9)
            {
                Name = "Schedule mode",
                Type = paramTyp.boolean,
                Size = 2,
                frstByte = 1,
                frstBit = 6,
                Meanings = new() { [0] = "Disabled", [1] = "Enabled" }
            });
            allParameters.Add(new Parameter(0x1FEF9)
            {
                Name = "Fan speed",
                Type = paramTyp.percent,
                Size = 8,
                frstByte = 2,
            });
            allParameters.Add(new Parameter(0x1FEF9)
            {
                Name = "Setopint temp - Heat",
                Type = paramTyp.temperature,
                Size = 16,
                frstByte = 3,
            });
            allParameters.Add(new Parameter(0x1FEF9)
            {
                Name = "Setopint temp - Cool",
                Type = paramTyp.temperature,
                Size = 16,
                frstByte = 5,
            });

            // THERMOSTAT_STATUS_2 (0x1FEFA)
            allParameters.Add(new Parameter(0x1FEFA)
            {
                Name = "Current schedule instatnce",
                Type = paramTyp.natural,
                Size = 8,
                frstByte = 1,
                Meanings = new() { [0] = "Sleep", [1] = "Wake", [2] = "Away", [3] = "Return", [250] = "Storage" }
            });
            allParameters.Add(new Parameter(0x1FEFA)
            {
                Name = "Number of schedule instances",
                Type = paramTyp.custom,
                Size = 8,
                frstByte = 2,
            });
            allParameters.Add(new Parameter(0x1FEFA)
            {
                Name = "Reduced noise mode",
                Type = paramTyp.boolean,
                Size = 2,
                frstByte = 3,
                Meanings = new() { [0] = "Disabled", [1] = "Endabled" }
            });

            // THERMOSTAT_COMMAND_2 (0x1FEF8)
            allParameters.Add(new Parameter(0x1FEF8)
            {
                Name = "Current schedule instatnce",
                Type = paramTyp.natural,
                Size = 8,
                frstByte = 1,
                Meanings = new() { [0] = "Sleep", [1] = "Wake", [2] = "Away", [3] = "Return", [250] = "Storage", [251] = "Reset to \"current\" instance" }
            });
            allParameters.Add(new Parameter(0x1FEF8)
            {
                Name = "Number of schedule instances",
                Type = paramTyp.custom,
                Size = 8,
                frstByte = 2,
            });
            allParameters.Add(new Parameter(0x1FEF8)
            {
                Name = "Reduced noise mode",
                Type = paramTyp.boolean,
                Size = 2,
                frstByte = 3,
                Meanings = new() { [0] = "Disabled", [1] = "Endabled" }
            });

            // THERMOSTAT_SCHEDULE_STATUS_1 (0x1FEF7)
            allParameters.Add(new Parameter(0x1FEF7)
            {
                Name = "Schedule mode instance",
                Type = paramTyp.natural,
                Size = 8,
                frstByte = 1,
                Id = true,
                Meanings = new() { [0] = "Sleep", [1] = "Wake", [2] = "Away", [3] = "Return", [250] = "Storage" }
            });
            allParameters.Add(new Parameter(0x1FEF7)
            {
                Name = "Start hour",
                Type = paramTyp.custom,
                Size = 8,
                frstByte = 2,
            });
            allParameters.Add(new Parameter(0x1FEF7)
            {
                Name = "Start minute",
                Type = paramTyp.custom,
                Size = 8,
                frstByte = 3,
            });
            allParameters.Add(new Parameter(0x1FEF7)
            {
                Name = "Setopint temp - Heat",
                Type = paramTyp.temperature,
                Size = 16,
                frstByte = 4,
            });
            allParameters.Add(new Parameter(0x1FEF7)
            {
                Name = "Setopint temp - Cool",
                Type = paramTyp.temperature,
                Size = 16,
                frstByte = 6,
            });

            var sourceParamsForFEF5 = allParameters.Where(p => p.Dgn == 0x1FEF7).ToList();
            foreach (var param in sourceParamsForFEF5)
            {
                var clonedParam = new Parameter(0x1FEF5)
                {
                    Name = param.Name,
                    ShortName = param.ShortName,
                    Type = param.Type,
                    Size = param.Size,
                    frstByte = param.frstByte,
                    frstBit = param.frstBit,
                    Id = param.Id,
                    coefficient = param.coefficient,
                    Unit = param.Unit,
                    Meanings = param.Meanings != null ? new Dictionary<int, string>(param.Meanings) : null
                };
                allParameters.Add(clonedParam);
            }

            // THERMOSTAT_SCHEDULE_STATUS_2 (0x1FEF6)
            allParameters.Add(new Parameter(0x1FEF6)
            {
                Name = "Schedule mode instance",
                Type = paramTyp.natural,
                Size = 8,
                frstByte = 1,
                Id = true,
                Meanings = new() { [0] = "Sleep", [1] = "Wake", [2] = "Away", [3] = "Return", [250] = "Storage" }
            });
            allParameters.Add(new Parameter(0x1FEF6)
            {
                Name = "Sunday",
                Type = paramTyp.boolean,
                Size = 2,
                frstByte = 2,
                frstBit = 0,
                Meanings = new() { [0] = "Not scheduled for this day", [1] = "Schedule applies to this day" }
            });
            allParameters.Add(new Parameter(0x1FEF6)
            {
                Name = "Monday",
                Type = paramTyp.boolean,
                Size = 2,
                frstByte = 2,
                frstBit = 2,
                Meanings = new() { [0] = "Not scheduled for this day", [1] = "Schedule applies to this day" }
            });
            allParameters.Add(new Parameter(0x1FEF6)
            {
                Name = "Tuesday",
                Type = paramTyp.natural,
                Size = 2,
                frstByte = 2,
                frstBit = 4,
                Meanings = new() { [0] = "Not scheduled for this day", [1] = "Schedule applies to this day" }
            });
            allParameters.Add(new Parameter(0x1FEF6)
            {
                Name = "Wednesday",
                Type = paramTyp.boolean,
                Size = 2,
                frstByte = 2,
                frstBit = 6,
                Meanings = new() { [0] = "Not scheduled for this day", [1] = "Schedule applies to this day" }
            });
            allParameters.Add(new Parameter(0x1FEF6)
            {
                Name = "Thursday",
                Type = paramTyp.boolean,
                Size = 2,
                frstByte = 3,
                frstBit = 0,
                Meanings = new() { [0] = "Not scheduled for this day", [1] = "Schedule applies to this day" }
            });
            allParameters.Add(new Parameter(0x1FEF6)
            {
                Name = "Friday",
                Type = paramTyp.boolean,
                Size = 2,
                frstByte = 3,
                frstBit = 2,
                Meanings = new() { [0] = "Not scheduled for this day", [1] = "Schedule applies to this day" }
            });
            allParameters.Add(new Parameter(0x1FEF6)
            {
                Name = "Saturday",
                Type = paramTyp.boolean,
                Size = 2,
                frstByte = 3,
                frstBit = 4,
                Meanings = new() { [0] = "Not scheduled for this day", [1] = "Schedule applies to this day" }
            });

            // THERMOSTAT_SCHEDULE_COMMAND_2 (0x1FEF4) - копируем параметры из 0x1FEF6
            var fef6Params = allParameters.Where(p => p.Dgn == 0x1FEF6).ToList(); // ← ToList() создает копию
            foreach (var param in fef6Params)
            {
                var clonedParam = new Parameter(0x1FEF4)
                {
                    Name = param.Name,
                    ShortName = param.ShortName,
                    Type = param.Type,
                    Size = param.Size,
                    frstByte = param.frstByte,
                    frstBit = param.frstBit,
                    Id = param.Id,
                    coefficient = param.coefficient,
                    Unit = param.Unit,
                    Meanings = param.Meanings != null ? new Dictionary<int, string>(param.Meanings) : null
                };
                allParameters.Add(clonedParam);
            }

            // CIRCULATION_PUMP_STATUS (0x1FE97)
            allParameters.Add(new Parameter(0x1FE97)
            {
                Name = "Output status",
                Type = paramTyp.natural,
                Size = 4,
                frstByte = 1,
                Meanings = new() { [0] = "Off", [1] = "On", [5] = "Test (Forced On)" }
            });
            allParameters.Add(new Parameter(0x1FE97)
            {
                Name = "Pump Overcurrent Status",
                Type = paramTyp.boolean,
                Size = 2,
                frstByte = 2,
                Meanings = new() { [0] = "No overcurrent detected", [1] = "Overcurrent detected" }
            });
            allParameters.Add(new Parameter(0x1FE97)
            {
                Name = "Pump Undercurrent Status",
                Type = paramTyp.boolean,
                Size = 2,
                frstByte = 2,
                frstBit = 2,
                Meanings = new() { [0] = "No undercurrent detected", [1] = "Undercurrent detected" }
            });
            allParameters.Add(new Parameter(0x1FE97)
            {
                Name = "Pump Temperature Status",
                Type = paramTyp.boolean,
                Size = 2,
                frstByte = 1,
                frstBit = 4,
                Meanings = new() { [0] = "Temperature normal", [1] = "Temperature warning" }
            });

            // DATE_TIME_STATUS (0x1FFFF)
            allParameters.Add(new Parameter(0x1FFFF)
            {
                Name = "Year",
                Type = paramTyp.natural
            });
            allParameters.Add(new Parameter(0x1FFFF)
            {
                Name = "Month",
                Type = paramTyp.natural,
                frstByte = 1
            });
            allParameters.Add(new Parameter(0x1FFFF)
            {
                Name = "Day of month",
                Type = paramTyp.natural,
                frstByte = 2
            });
            allParameters.Add(new Parameter(0x1FFFF)
            {
                Name = "Day of week",
                Type = paramTyp.natural,
                frstByte = 3
            });
            allParameters.Add(new Parameter(0x1FFFF)
            {
                Name = "Hour",
                Type = paramTyp.natural,
                frstByte = 4
            });
            allParameters.Add(new Parameter(0x1FFFF)
            {
                Name = "Minute",
                Type = paramTyp.natural,
                frstByte = 5
            });
            allParameters.Add(new Parameter(0x1FFFF)
            {
                Name = "Second",
                Type = paramTyp.natural,
                frstByte = 6
            });

            // FLOOR_HEAT_COMMAND (0x1FEFB)
            allParameters.Add(new Parameter(0x1FEFB)
            {
                Name = "Operating mode",
                Size = 2,
                frstByte = 1,
                Meanings = mnsMkr("Automatic", "Manual")
            });
            allParameters.Add(new Parameter(0x1FEFB)
            {
                Name = "Operating status",
                Size = 2,
                frstByte = 1,
                frstBit = 2,
                Meanings = defMeaningsOnOff
            });
            allParameters.Add(new Parameter(0x1FEFB)
            {
                Name = "Heat element status",
                Size = 2,
                frstByte = 1,
                frstBit = 4,
                Meanings = defMeaningsOnOff
            });
            allParameters.Add(new Parameter(0x1FEFB)
            {
                Name = "Schedule mode",
                Size = 2,
                frstByte = 1,
                frstBit = 6,
                Meanings = defMeaningsOnOff
            });
            allParameters.Add(new Parameter(0x1FEFB)
            {
                Name = "Setpoint",
                Size = 16,
                frstByte = 2,
                Type = paramTyp.temperature
            });
            allParameters.Add(new Parameter(0x1FEFB)
            {
                Name = "Dead band",
                Size = 8,
                frstByte = 4,
                coefficient = 0.1,
                Unit = "°C"
            });

            // WATERHEATER_STATUS_2 (0x1FE99)
            allParameters.Add(new Parameter(0x1FE99)
            {
                Name = "Electric Element Level",
                Size = 4,
                frstByte = 1
            });
            allParameters.Add(new Parameter(0x1FE99)
            {
                Name = "Max Electric Element Leve",
                Size = 4,
                frstByte = 1,
                frstBit = 4
            });
            allParameters.Add(new Parameter(0x1FE99)
            {
                Name = "Engine Preheat",
                Size = 4,
                frstByte = 2,
                Type = paramTyp.natural,
                Meanings = new() { [0] = "Off", [1] = "On", [5] = "Test(Forced On)" }
            });
            allParameters.Add(new Parameter(0x1FE99)
            {
                Name = "Coolant Level Warning",
                Size = 2,
                frstByte = 2,
                frstBit = 4,
                Type = paramTyp.boolean,
                Meanings = new() { [0] = "Coolant level sufficient", [1] = "Coolant level low" }
            });
            allParameters.Add(new Parameter(0x1FE99)
            {
                Name = "Hot Water Priority",
                Size = 2,
                frstByte = 2,
                frstBit = 6,
                Type = paramTyp.natural,
                Meanings = new() { [0] = "Domestic water priority", [1] = "Heating priority" }
            });

            // GENERATOR_DEMAND_STATUS (0xFF80)
            allParameters.Add(new Parameter(0xFF80)
            {
                Name = "Generator demand",
                frstByte = 0,
                frstBit = 0,
                Size = 2,
                Meanings = mnsMkr("No demand for generator", "Generator is demanded")
            });
            allParameters.Add(new Parameter(0xFF80)
            {
                Name = "Internal generator demand",
                frstByte = 0,
                frstBit = 2,
                Size = 2,
                Meanings = mnsMkr("No internal demand", "Internal AGS criterion is demanding generator")
            });
            allParameters.Add(new Parameter(0xFF80)
            {
                Name = "Network generator demand",
                frstByte = 0,
                frstBit = 4,
                Size = 2,
                Meanings = mnsMkr("No demand from other network nodes", "Network device is demanding generator")
            });
            allParameters.Add(new Parameter(0xFF80)
            {
                Name = "External activity detected",
                frstByte = 0,
                frstBit = 6,
                Size = 2,
                Meanings = mnsMkr("Automatic starting is allowed", "Automatic starting is disabled due to the detection of external activity")
            });
            allParameters.Add(new Parameter(0xFF80)
            {
                Name = "Manual override detected",
                frstByte = 1,
                frstBit = 0,
                Size = 2,
                Meanings = mnsMkr("Normal Operation", "Manual Override")
            });
            allParameters.Add(new Parameter(0xFF80)
            {
                Name = "Quiet time",
                frstByte = 1,
                frstBit = 2,
                Size = 2,
                Meanings = mnsMkr("Unit is not in Quiet Time", "Unit is in Quiet Time")
            });
            allParameters.Add(new Parameter(0xFF80)
            {
                Name = "Quiet time override",
                frstByte = 1,
                frstBit = 4,
                Size = 2,
                Meanings = mnsMkr("Normal operation", "Quiet Time is being overridden")
            });
            allParameters.Add(new Parameter(0xFF80)
            {
                Name = "Generator lock",
                frstByte = 0,
                frstBit = 6,
                Size = 2,
                Meanings = mnsMkr("Normal operation", "Genset is locked. Node will not start generator for any reason")
            });
            allParameters.Add(new Parameter(0xFF80)
            {
                Name = "Network generator demand",
                frstByte = 0,
                frstBit = 4,
                Size = 2,
                Meanings = mnsMkr("No demand from other network nodes", "Network device is demanding generator")
            });
            allParameters.Add(new Parameter(0xFF80)
            {
                Name = "Quiet time begin hour",
                frstByte = 2,
                frstBit = 0,
                Unit = "h"
            });

            // DM_RV (0x1FECA)
            allParameters.Add(new Parameter(0x1FECA)
            {
                Name = "Operating Status",
                frstByte = 0,
                frstBit = 0,
                Size = 2,
                Meanings = mnsMkr("Disabled", "Enabled")
            });
            allParameters.Add(new Parameter(0x1FECA)
            {
                Name = "Operating Status",
                frstByte = 0,
                frstBit = 2,
                Size = 2,
                Meanings = mnsMkr("Standby/Idle", "Running")
            });
            allParameters.Add(new Parameter(0x1FECA)
            {
                Name = "Yellow lamp status",
                frstByte = 0,
                frstBit = 4,
                Size = 2,
                Meanings = defMeaningsOnOff
            });
            allParameters.Add(new Parameter(0x1FECA)
            {
                Name = "Red lamp status",
                frstByte = 0,
                frstBit = 5,
                Size = 2,
                Meanings = defMeaningsOnOff
            });
            allParameters.Add(new Parameter(0x1FECA)
            {
                Name = "DSA",
                frstByte = 1,
                frstBit = 0,
                Id = true
            });
            allParameters.Add(new Parameter(0x1FECA)
            {
                Name = "SPN - MSB",
                frstByte = 2,
                frstBit = 0
            });
            allParameters.Add(new Parameter(0x1FECA)
            {
                Name = "SPN - ISB",
                frstByte = 3,
                frstBit = 0
            });
            allParameters.Add(new Parameter(0x1FECA)
            {
                Name = "SPN - LSB",
                frstByte = 4,
                frstBit = 5,
                Size = 3
            });
            allParameters.Add(new Parameter(0x1FECA)
            {
                Name = "FMI",
                frstByte = 4,
                frstBit = 0,
                Size = 5
            });
            allParameters.Add(new Parameter(0x1FECA)
            {
                Name = "Occurrence count",
                frstByte = 5,
                frstBit = 0,
                Size = 6,
                Meanings = new() { { 0x7F, "Not available" } }
            });
            allParameters.Add(new Parameter(0x1FECA)
            {
                Name = "DSA extension",
                frstByte = 6,
                frstBit = 0,
                Id = true
            });
            allParameters.Add(new Parameter(0x1FECA)
            {
                Name = "Bank select",
                frstByte = 7,
                frstBit = 0,
                Meanings = new() { { 15, "Not supported" } }
            });

            // Timberline Extension (0x1EF65)
            allParameters.Add(new Parameter(0x1EF65)
            {
                multipackNum = 0x84,
                Name = "Solenoid",
                frstByte = 1,
                Size = 2,
                Meanings = defMeaningsOnOff
            });
            allParameters.Add(new Parameter(0x1EF65)
            {
                multipackNum = 0x84,
                Name = "Tank temperature",
                Type = paramTyp.temperature,
                frstByte = 2,
                Size = 16
            });
            allParameters.Add(new Parameter(0x1EF65)
            {
                multipackNum = 0x84,
                Name = "Heater temperature",
                Type = paramTyp.temperature,
                frstByte = 4,
                Size = 16
            });
            allParameters.Add(new Parameter(0x1EF65)
            {
                multipackNum = 0x84,
                Name = "Manual fan speed",
                Type = paramTyp.percent,
                frstByte = 6,
                Size = 8
            });
            allParameters.Add(new Parameter(0x1EF65)
            {
                multipackNum = 0x85,
                Name = "System timer",
                Unit = "s",
                frstByte = 1,
                Size = 24
            });
            allParameters.Add(new Parameter(0x1EF65)
            {
                multipackNum = 0x85,
                Name = "Water priority timer",
                Unit = "s",
                frstByte = 4,
                Size = 16
            });
            allParameters.Add(new Parameter(0x1EF65)
            {
                multipackNum = 0x85,
                Name = "Pump timer",
                Unit = "s",
                frstByte = 6,
                Size = 16
            });
            allParameters.Add(new Parameter(0x1EF65)
            {
                multipackNum = 0x86,
                Name = "Total heater minutes",
                Unit = "m",
                frstByte = 1,
                Size = 24
            });
            allParameters.Add(new Parameter(0x1EF65)
            {
                multipackNum = 0x86,
                Name = "Heater version 1",
                frstByte = 4,
                Size = 8
            });
            allParameters.Add(new Parameter(0x1EF65)
            {
                multipackNum = 0x86,
                Name = "Heater version 2",
                frstByte = 5,
                Size = 8
            });
            allParameters.Add(new Parameter(0x1EF65)
            {
                multipackNum = 0x86,
                Name = "Heater version 3",
                frstByte = 6,
                Size = 8
            });
            allParameters.Add(new Parameter(0x1EF65)
            {
                multipackNum = 0x86,
                Name = "Heater version 4",
                frstByte = 7,
                Size = 8
            });
            allParameters.Add(new Parameter(0x1EF65)
            {
                multipackNum = 0x87,
                Name = "Minutes since start",
                frstByte = 1,
                Size = 24,
                Unit = "min"
            });
            allParameters.Add(new Parameter(0x1EF65)
            {
                multipackNum = 0x87,
                Name = "Panel version 1",
                frstByte = 4,
                Size = 8
            });
            allParameters.Add(new Parameter(0x1EF65)
            {
                multipackNum = 0x87,
                Name = "Panel version 2",
                frstByte = 5,
                Size = 8
            });
            allParameters.Add(new Parameter(0x1EF65)
            {
                multipackNum = 0x87,
                Name = "Panel version 3",
                frstByte = 6,
                Size = 8
            });
            allParameters.Add(new Parameter(0x1EF65)
            {
                multipackNum = 0x87,
                Name = "Panel version 4",
                frstByte = 7,
                Size = 8
            });
            allParameters.Add(new Parameter(0x1EF65)
            {
                multipackNum = 0x88,
                Name = "HCU version 1",
                frstByte = 4,
                Size = 8
            });
            allParameters.Add(new Parameter(0x1EF65)
            {
                multipackNum = 0x88,
                Name = "HCU version 2",
                frstByte = 5,
                Size = 8
            });
            allParameters.Add(new Parameter(0x1EF65)
            {
                multipackNum = 0x88,
                Name = "HCU version 3",
                frstByte = 6,
                Size = 8
            });
            allParameters.Add(new Parameter(0x1EF65)
            {
                multipackNum = 0x88,
                Name = "HCU version 4",
                frstByte = 7,
                Size = 8
            });
            allParameters.Add(new Parameter(0x1EF65)
            {
                multipackNum = 0x89,
                Name = "System limitation",
                frstByte = 1,
                Size = 16,
                Unit = "min"
            });
            allParameters.Add(new Parameter(0x1EF65)
            {
                multipackNum = 0x89,
                Name = "Water limitation",
                frstByte = 3,
                Size = 8,
                Unit = "min"
            });
            allParameters.Add(new Parameter(0x1EF65)
            {
                multipackNum = 0x8A,
                Name = "System limitation",
                frstByte = 1,
                Size = 16,
                Unit = "min"
            });
            allParameters.Add(new Parameter(0x1EF65)
            {
                multipackNum = 0x8A,
                Name = "Water limitation",
                frstByte = 3,
                Size = 8,
                Unit = "min"
            });
            allParameters.Add(new Parameter(0x1EF65)
            {
                multipackNum = 0xA0,
                Name = "Tank Temperature",
                frstByte = 1,
                Size = 8,
                Type = paramTyp.temperature
            });
            allParameters.Add(new Parameter(0x1EF65)
            {
                multipackNum = 0xA0,
                Name = "Heater Temperature",
                frstByte = 2,
                Size = 8,
                Type = paramTyp.temperature
            });
            allParameters.Add(new Parameter(0x1EF65)
            {
                multipackNum = 0xA0,
                Name = "Zone 1 fan manual percent",
                frstByte = 3,
                Size = 8,
                Type = paramTyp.percent
            });
            allParameters.Add(new Parameter(0x1EF65)
            {
                multipackNum = 0xA0,
                Name = "Zone 2 fan manual percent",
                frstByte = 4,
                Size = 8,
                Type = paramTyp.percent
            });
            allParameters.Add(new Parameter(0x1EF65)
            {
                multipackNum = 0xA0,
                Name = "Zone 3 fan manual percent",
                frstByte = 5,
                Size = 8,
                Type = paramTyp.percent
            });
            allParameters.Add(new Parameter(0x1EF65)
            {
                multipackNum = 0xA0,
                Name = "Zone 4 fan manual percent",
                frstByte = 6,
                Size = 8,
                Type = paramTyp.percent
            });
            allParameters.Add(new Parameter(0x1EF65)
            {
                multipackNum = 0xA0,
                Name = "Zone 5 fan manual percent",
                frstByte = 7,
                Size = 8,
                Type = paramTyp.percent
            });
            allParameters.Add(new Parameter(0x1EF65)
            {
                multipackNum = 0xA1,
                Name = "System timer",
                frstByte = 1,
                Size = 24,
                Type = paramTyp.seconds
            });
            allParameters.Add(new Parameter(0x1EF65)
            {
                multipackNum = 0xA2,
                Name = "Loop 1 pump timer",
                frstByte = 1,
                Size = 16,
                Type = paramTyp.seconds
            });
            allParameters.Add(new Parameter(0x1EF65)
            {
                multipackNum = 0xA2,
                Name = "Loop 2 pump timer",
                frstByte = 3,
                Size = 16,
                Type = paramTyp.seconds
            });
            allParameters.Add(new Parameter(0x1EF65)
            {
                multipackNum = 0xA2,
                Name = "Heater pump timer",
                frstByte = 5,
                Size = 16,
                Type = paramTyp.seconds
            });
            allParameters.Add(new Parameter(0x1EF65)
            {
                multipackNum = 0xA3,
                Name = "AUX pump 1 timer",
                frstByte = 1,
                Size = 16,
                Type = paramTyp.seconds
            });
            allParameters.Add(new Parameter(0x1EF65)
            {
                multipackNum = 0xA3,
                Name = "AUX pump 2 timer",
                frstByte = 3,
                Size = 16,
                Type = paramTyp.seconds
            });
            allParameters.Add(new Parameter(0x1EF65)
            {
                multipackNum = 0xA3,
                Name = "AUX pump 3 timer",
                frstByte = 5,
                Size = 16,
                Type = paramTyp.seconds
            });
            allParameters.Add(new Parameter(0x1EF65)
            {
                multipackNum = 0xA4,
                Name = "Heater total minutes",
                frstByte = 1,
                Size = 24,
                Type = paramTyp.minutes
            });
            allParameters.Add(new Parameter(0x1EF65)
            {
                multipackNum = 0xA4,
                Name = "Heater version byte 1",
                frstByte = 4,
                Size = 8,
                Type = paramTyp.natural
            });
            allParameters.Add(new Parameter(0x1EF65)
            {
                multipackNum = 0xA4,
                Name = "Heater version byte 2",
                frstByte = 5,
                Size = 8,
                Type = paramTyp.natural
            });
            allParameters.Add(new Parameter(0x1EF65)
            {
                multipackNum = 0xA4,
                Name = "Heater version byte 3",
                frstByte = 6,
                Size = 8,
                Type = paramTyp.natural
            });
            allParameters.Add(new Parameter(0x1EF65)
            {
                multipackNum = 0xA4,
                Name = "Heater version byte 4",
                frstByte = 7,
                Size = 8,
                Type = paramTyp.natural
            });
            allParameters.Add(new Parameter(0x1EF65)
            {
                multipackNum = 0xA5,
                Name = "Panel version byte 1",
                frstByte = 4,
                Size = 8,
                Type = paramTyp.natural
            });
            allParameters.Add(new Parameter(0x1EF65)
            {
                multipackNum = 0xA5,
                Name = "Panel version byte 2",
                frstByte = 5,
                Size = 8,
                Type = paramTyp.natural
            });
            allParameters.Add(new Parameter(0x1EF65)
            {
                multipackNum = 0xA5,
                Name = "Panel version byte 3",
                frstByte = 6,
                Size = 8,
                Type = paramTyp.natural
            });
            allParameters.Add(new Parameter(0x1EF65)
            {
                multipackNum = 0xA5,
                Name = "Panel version byte 4",
                frstByte = 7,
                Size = 8,
                Type = paramTyp.natural
            });
            allParameters.Add(new Parameter(0x1EF65)
            {
                multipackNum = 0xA6,
                Name = "HCU version byte 1",
                frstByte = 4,
                Size = 8,
                Type = paramTyp.natural
            });
            allParameters.Add(new Parameter(0x1EF65)
            {
                multipackNum = 0xA6,
                Name = "HCU version byte 2",
                frstByte = 5,
                Size = 8,
                Type = paramTyp.natural
            });
            allParameters.Add(new Parameter(0x1EF65)
            {
                multipackNum = 0xA6,
                Name = "HCU version byte 3",
                frstByte = 6,
                Size = 8,
                Type = paramTyp.natural
            });
            allParameters.Add(new Parameter(0x1EF65)
            {
                multipackNum = 0xA6,
                Name = "HCU version byte 4",
                frstByte = 7,
                Size = 8,
                Type = paramTyp.natural
            });
            allParameters.Add(new Parameter(0x1EF65)
            {
                multipackNum = 0xA7,
                Name = "System time limit",
                frstByte = 1,
                Size = 1,
                Type = paramTyp.hours
            });
            allParameters.Add(new Parameter(0x1EF65)
            {
                multipackNum = 0xA7,
                Name = "Pump overridelimit",
                frstByte = 2,
                Size = 1,
                Type = paramTyp.minutes
            });
            allParameters.Add(new Parameter(0x1EF65)
            {
                multipackNum = 0xA7,
                Name = "Engine preheat setpoint",
                frstByte = 3,
                Size = 1,
                Type = paramTyp.temperature
            });
            allParameters.Add(new Parameter(0x1EF65)
            {
                multipackNum = 0xA7,
                Name = "Engine preheat duration",
                frstByte = 4,
                Size = 16,
                Type = paramTyp.minutes
            });
            allParameters.Add(new Parameter(0x1EF65)
            {
                multipackNum = 0xA8,
                Name = "System time limit",
                frstByte = 1,
                Size = 1,
                Type = paramTyp.hours
            });
            allParameters.Add(new Parameter(0x1EF65)
            {
                multipackNum = 0xA8,
                Name = "Pump overridelimit",
                frstByte = 2,
                Size = 1,
                Type = paramTyp.minutes
            });
            allParameters.Add(new Parameter(0x1EF65)
            {
                multipackNum = 0xA8,
                Name = "Engine preheat setpoint",
                frstByte = 3,
                Size = 1,
                Type = paramTyp.temperature
            });
            allParameters.Add(new Parameter(0x1EF65)
            {
                multipackNum = 0xA8,
                Name = "Engine preheat duration",
                frstByte = 4,
                Size = 16,
                Type = paramTyp.minutes
            });
            allParameters.Add(new Parameter(0x1EF65)
            {
                multipackNum = 0xA9,
                Name = "Domestic Water",
                frstByte = 1,
                Size = 2,
                Type = paramTyp.boolean
            });
            allParameters.Add(new Parameter(0x1EF65)
            {
                multipackNum = 0xA9,
                Name = "Element status",
                frstByte = 1,
                Size = 2,
                Type = paramTyp.boolean
            });
            allParameters.Add(new Parameter(0x1EF65)
            {
                multipackNum = 0xA9,
                Name = "Heater Icon",
                frstByte = 2,
                Size = 8,
                Meanings = mnsMkr("Idle", "Blowing", "Ignition", "Lit")
            });
            allParameters.Add(new Parameter(0x1EF65)
            {
                multipackNum = 0xA9,
                Name = "Liquid level",
                frstByte = 3,
                Size = 8,
                Type = paramTyp.natural
            });
            allParameters.Add(new Parameter(0x1EF65)
            {
                multipackNum = 0xA9,
                Name = "Engine estimated time",
                frstByte = 4,
                Size = 24,
                Type = paramTyp.seconds
            });
            allParameters.Add(new Parameter(0x1EF65)
            {
                multipackNum = 0xA9,
                Name = "Fuel type",
                frstByte = 7,
                Size = 8,
                Meanings = new() { { 0, "Diesel" }, { 1, "Gasoline" }, { 2, "Propane" } }
            });
            allParameters.Add(new Parameter(0x1EF65)
            {
                multipackNum = 0xAA,
                Name = "Zone 1 Type",
                frstByte = 1,
                Size = 8,
                Meanings = mnsMkr("Disconnected", "Furnace", "Freeze protection", "Radiator")
            });
            allParameters.Add(new Parameter(0x1EF65)
            {
                multipackNum = 0xAA,
                Name = "Zone 2 Type",
                frstByte = 2,
                Size = 8,
                Meanings = mnsMkr("Disconnected", "Furnace", "Freeze protection", "Radiator")
            });
            allParameters.Add(new Parameter(0x1EF65)
            {
                multipackNum = 0xAA,
                Name = "Zone 3 Type",
                frstByte = 3,
                Size = 8,
                Meanings = mnsMkr("Disconnected", "Furnace", "Freeze protection", "Radiator")
            });
            allParameters.Add(new Parameter(0x1EF65)
            {
                multipackNum = 0xAA,
                Name = "Zone 4 Type",
                frstByte = 4,
                Size = 8,
                Meanings = mnsMkr("Disconnected", "Furnace", "Freeze protection", "Radiator")
            });
            allParameters.Add(new Parameter(0x1EF65)
            {
                multipackNum = 0xAA,
                Name = "Zone 5 Type",
                frstByte = 5,
                Size = 8,
                Meanings = mnsMkr("Disconnected", "Furnace", "Freeze protection", "Radiator")
            });

            // Добавляем все параметры в глобальную коллекцию
            RVC.Parameters = allParameters;
        }
    }
}



