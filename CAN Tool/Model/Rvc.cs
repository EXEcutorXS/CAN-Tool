using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Runtime.Serialization;
using Newtonsoft.Json;
using System.IO;
using Can_Adapter;
using System.ComponentModel;

namespace RVC
{
    static class RVC
    {
        static RVC()
        {

        }

        public static void SeedData()
        {
            DGN newDGN;

            newDGN = new DGN(true) { DgnId = 0x1FF9C, Name = "THERMOSTAT_AMBIENT_STATUS" };
            newDGN.Parameters.Add(new RVCParameter { Name = "Ambient temperature", ShortName = "Tamb", Type = parameterType.temperature, Size = parameterSize.uint16, firstByte = 1 });
            DGNs.Add(newDGN.DgnId, newDGN);

            newDGN = new DGN(true) { DgnId = 0x1FFE4, Name = "FURNACE_STATUS" };

            newDGN.Parameters.Add(new RVCParameter
            {
                Name = "Operating mode",
                ShortName = "Op mode",
                Type = parameterType.list,
                Size = parameterSize.uint2,
                firstByte = 1,
                Meanings = { [0] = "Automatic", [1] = "Manual" }
            });
            newDGN.Parameters.Add(new RVCParameter
            {
                Name = "Heat Source",
                ShortName = "Heat Src",
                Type = parameterType.list,
                Size = parameterSize.uint6,
                firstByte = 1,
                firstBit = 2,
                Meanings = { [0] = "Combustion", [1] = "AC power primary", [2] = "AC power secondary", [3] = "Engine Heat" }
            });

            newDGN.Parameters.Add(new RVCParameter { Name = "Circulation fan speed", ShortName = "Fan%", Type = parameterType.percent, firstByte = 2, firstBit = 0 });
            newDGN.Parameters.Add(new RVCParameter { Name = "Heat output level", ShortName = "Heat%", Type = parameterType.percent, firstByte = 3, firstBit = 0 });
            newDGN.Parameters.Add(new RVCParameter { Name = "Dead band", ShortName = "Db", Type = parameterType.custom, firstByte = 4, coefficient = 0.1, Unit = "C" });
            newDGN.Parameters.Add(new RVCParameter { Name = "Dead band level 2", ShortName = "Db2", Type = parameterType.custom, firstByte = 5, coefficient = 0.1, Unit = "C" });
            newDGN.Parameters.Add(new RVCParameter { Name = "Zone overcurrent status", ShortName = "Zone OC", Type = parameterType.boolean, Size = parameterSize.uint2, firstByte = 6, firstBit = 0 });
            newDGN.Parameters.Add(new RVCParameter { Name = "Zone undercurrent status", ShortName = "Zone UC", Type = parameterType.boolean, Size = parameterSize.uint2, firstByte = 6, firstBit = 2 });
            newDGN.Parameters.Add(new RVCParameter
            {
                Name = "Zone temperature status",
                ShortName = "Zone t Status",
                Type = parameterType.boolean,
                Size = parameterSize.uint2,
                firstByte = 6,
                firstBit = 4,
                Meanings = { [0] = "Normal", [1] = "Warning" }
            });
            newDGN.Parameters.Add(new RVCParameter
            {
                Name = "Zone analog input status",
                ShortName = "Zone an",
                Type = parameterType.boolean,
                Size = parameterSize.uint2,
                firstByte = 6,
                firstBit = 6,
                Meanings = { [0] = "Off(Inactive)", [1] = "On(Active)" }
            });
            DGNs.Add(newDGN.DgnId, newDGN);

            newDGN = new DGN(true) { DgnId = 0x1FFE3, Name = "FURNACE_COMMAND" };

            newDGN.Parameters.Add(new RVCParameter
            {
                Name = "Operating mode",
                ShortName = "Op mode",
                Type = parameterType.list,
                Size = parameterSize.uint2,
                firstByte = 1,
                Meanings = { [0] = "Automatic", [1] = "Manual" }
            });
            newDGN.Parameters.Add(new RVCParameter
            {
                Name = "Heat Source",
                ShortName = "Heat Src",
                Type = parameterType.list,
                Size = parameterSize.uint6,
                firstByte = 1,
                firstBit = 2,
                Meanings = { [0] = "Combustion", [1] = "AC power primary", [2] = "AC power secondary", [3] = "Engine Heat" }
            });

            newDGN.Parameters.Add(new RVCParameter { Name = "Circulation fan speed", ShortName = "Fan%", Type = parameterType.percent, firstByte = 2, firstBit = 0 });
            newDGN.Parameters.Add(new RVCParameter { Name = "Heat output level", ShortName = "Heat%", Type = parameterType.percent, firstByte = 3, firstBit = 0 });
            newDGN.Parameters.Add(new RVCParameter { Name = "Dead band", ShortName = "Db", Type = parameterType.custom, firstByte = 4, coefficient = 0.1, Unit = "C" });
            newDGN.Parameters.Add(new RVCParameter { Name = "Dead band level 2", ShortName = "Db2", Type = parameterType.custom, firstByte = 5, coefficient = 0.1, Unit = "C" });

            DGNs.Add(newDGN.DgnId, newDGN);

            newDGN = new DGN(true) { DgnId = 0x1FFF7, Name = "WATERHEATER_STATUS" };

            newDGN.Parameters.Add(new RVCParameter
            {
                Name = "Operating modes",
                ShortName = "Mode",
                Type = parameterType.list,
                Size = parameterSize.uint8,
                firstByte = 1,
                Meanings = { [0] = "off", [1] = "combustion", [2] = "electric", [3] = "gas/electric (both)", [4] = "test combustion (forced on)", [5] = "test combustion (forced on)", [6] = "test electric (forced on)" }
            });

            newDGN.Parameters.Add(new RVCParameter { Name = "Set point temperature", ShortName = "SP T", Type = parameterType.temperature, Size = parameterSize.uint16, firstByte = 2 });

            newDGN.Parameters.Add(new RVCParameter { Name = "Water temperature", ShortName = "Twater", Type = parameterType.temperature, Size = parameterSize.uint16, firstByte = 4 });

            newDGN.Parameters.Add(new RVCParameter
            {
                Name = "Thermostat status",
                Type = parameterType.boolean,
                Size = parameterSize.uint2,
                firstByte = 6,
                firstBit = 0,
                Meanings = { [0] = "set point met", [1] = "set point not met (heat is being applied)" }
            });

            newDGN.Parameters.Add(new RVCParameter
            {
                Name = "Burner status",
                Type = parameterType.boolean,
                Size = parameterSize.uint2,
                firstByte = 6,
                firstBit = 2,
                Meanings = { [0] = "off", [1] = "burner is lit" }
            });

            newDGN.Parameters.Add(new RVCParameter
            {
                Name = "AC element status",
                Type = parameterType.boolean,
                Size = parameterSize.uint2,
                firstByte = 6,
                firstBit = 4,
                Meanings = { [0] = "AC element is inactive", [1] = "AC element is active)" }
            });


            newDGN.Parameters.Add(new RVCParameter
            {
                Name = "High temperature limit switch status",
                Type = parameterType.boolean,
                Size = parameterSize.uint2,
                firstByte = 6,
                firstBit = 6,
                Meanings = { [0] = "limit switch not tripped", [1] = "limit switch tripped" }
            });

            newDGN.Parameters.Add(new RVCParameter
            {
                Name = "Failure to ignite status",
                Type = parameterType.boolean,
                Size = parameterSize.uint2,
                firstByte = 7,
                firstBit = 0,
                Meanings = { [0] = "no failure", [1] = "device has failed to ignite" }
            });

            newDGN.Parameters.Add(new RVCParameter
            {
                Name = "AC power failure status",
                Type = parameterType.boolean,
                Size = parameterSize.uint2,
                firstByte = 7,
                firstBit = 2,
                Meanings = { [0] = "AC power present", [1] = "AC power not present" }
            });

            newDGN.Parameters.Add(new RVCParameter
            {
                Name = "DC power failure status",
                Type = parameterType.boolean,
                Size = parameterSize.uint2,
                firstByte = 7,
                firstBit = 4,
                Meanings = { [0] = "DC power present", [1] = "DC power not present" }
            });

            newDGN.Parameters.Add(new RVCParameter
            {
                Name = "DC power warning status",
                Type = parameterType.boolean,
                Size = parameterSize.uint2,
                firstByte = 7,
                firstBit = 6,
                Meanings = { [0] = "DC power sufficient", [1] = "DC power warning" }
            });

            DGNs.Add(newDGN.DgnId, newDGN);

            newDGN = new DGN(true) { DgnId = 0x1FFF6, Name = "WATERHEATER_COMMAND" };

            newDGN.Parameters.Add(new RVCParameter
            {
                Name = "Operating modes",
                ShortName = "Mode",
                Type = parameterType.list,
                Size = parameterSize.uint8,
                firstByte = 1,
                Meanings = { [0] = "off", [1] = "combustion", [2] = "electric", [3] = "gas/electric (both)", [4] = "test combustion (forced on)", [5] = "test combustion (forced on)", [6] = "test electric (forced on)" }
            });

            newDGN.Parameters.Add(new RVCParameter { Name = "Set point temperature", ShortName = "SP T", Type = parameterType.temperature, Size = parameterSize.uint16, firstByte = 2 });
            newDGN.Parameters.Add(new RVCParameter { Name = "Electric Element Level", ShortName = "Elec lvl", Type = parameterType.natural, Size = parameterSize.uint4, firstByte = 6 });

            DGNs.Add(newDGN.DgnId, newDGN);

            newDGN = new DGN(true) { DgnId = 0x1FFE2, Name = "THERMOSTAT_STATUS_1" };

            newDGN.Parameters.Add(new RVCParameter
            {
                Name = "Operating mode",
                Type = parameterType.list,
                Size = parameterSize.uint4,
                firstByte = 1,
                Meanings = { [0] = "Off", [1] = "Cool", [2] = "Heat", [3] = "Auto heat/Cool", [4] = "Fan only", [5] = "Aux heat", [6] = "Window Defrost/Dehumidify" }
            });

            newDGN.Parameters.Add(new RVCParameter
            {
                Name = "Fan mode",
                Type = parameterType.boolean,
                Size = parameterSize.uint2,
                firstByte = 1,
                firstBit = 4,
                Meanings = { [0] = "Auto", [1] = "On" }
            });

            newDGN.Parameters.Add(new RVCParameter
            {
                Name = "Schedule mode",
                Type = parameterType.boolean,
                Size = parameterSize.uint2,
                firstByte = 1,
                firstBit = 6,
                Meanings = { [0] = "Disabled", [1] = "Enabled" }
            });

            newDGN.Parameters.Add(new RVCParameter
            {
                Name = "Fan speed",
                Type = parameterType.percent,
                Size = parameterSize.uint8,
                firstByte = 2,
            });

            newDGN.Parameters.Add(new RVCParameter
            {
                Name = "Setopint temp - Heat",
                Type = parameterType.temperature,
                Size = parameterSize.uint16,
                firstByte = 3,
            });

            newDGN.Parameters.Add(new RVCParameter
            {
                Name = "Setopint temp - Cool",
                Type = parameterType.temperature,
                Size = parameterSize.uint16,
                firstByte = 5,
            });

            DGNs.Add(newDGN.DgnId, newDGN);


            newDGN = new DGN(true) { DgnId = 0x1FEF9, Name = "THERMOSTAT_COMMAND_1" };

            newDGN.Parameters.Add(new RVCParameter
            {
                Name = "Operating mode",
                Type = parameterType.list,
                Size = parameterSize.uint4,
                firstByte = 1,
                Meanings = { [0] = "Off", [1] = "Cool", [2] = "Heat", [3] = "Auto heat/Cool", [4] = "Fan only", [5] = "Aux heat", [6] = "Window Defrost/Dehumidify" }
            });

            newDGN.Parameters.Add(new RVCParameter
            {
                Name = "Fan mode",
                Type = parameterType.boolean,
                Size = parameterSize.uint2,
                firstByte = 1,
                firstBit = 4,
                Meanings = { [0] = "Auto", [1] = "On" }
            });

            newDGN.Parameters.Add(new RVCParameter
            {
                Name = "Schedule mode",
                Type = parameterType.boolean,
                Size = parameterSize.uint2,
                firstByte = 1,
                firstBit = 6,
                Meanings = { [0] = "Disabled", [1] = "Enabled" }
            });

            newDGN.Parameters.Add(new RVCParameter
            {
                Name = "Fan speed",
                Type = parameterType.percent,
                Size = parameterSize.uint8,
                firstByte = 2,
            });

            newDGN.Parameters.Add(new RVCParameter
            {
                Name = "Setopint temp - Heat",
                Type = parameterType.temperature,
                Size = parameterSize.uint16,
                firstByte = 3,
            });

            newDGN.Parameters.Add(new RVCParameter
            {
                Name = "Setopint temp - Cool",
                Type = parameterType.temperature,
                Size = parameterSize.uint16,
                firstByte = 5,
            });

            DGNs.Add(newDGN.DgnId, newDGN);


            newDGN = new DGN(true) { DgnId = 0x1FEFA, Name = "THERMOSTAT_STATUS_2" };
            newDGN.Parameters.Add(new RVCParameter
            {
                Name = "Current schedule instatnce",
                Type = parameterType.list,
                Size = parameterSize.uint8,
                firstByte = 1,
                Meanings = { [0] = "Sleep", [1] = "Wake", [2] = "Away", [3] = "Return", [250] = "Storage" }
            });

            newDGN.Parameters.Add(new RVCParameter
            {
                Name = "Number of schedule instances",
                Type = parameterType.custom,
                Size = parameterSize.uint8,
                firstByte = 2,
            });


            newDGN.Parameters.Add(new RVCParameter
            {
                Name = "Reduced noise mode",
                Type = parameterType.boolean,
                Size = parameterSize.uint2,
                firstByte = 3,
                Meanings = { [0] = "Disabled", [1] = "Endabled" }
            });

            DGNs.Add(newDGN.DgnId, newDGN);

            newDGN = new DGN(true) { DgnId = 0x1FEF8, Name = "THERMOSTAT_COMMAND_2" };
            newDGN.Parameters.Add(new RVCParameter
            {
                Name = "Current schedule instatnce",
                Type = parameterType.list,
                Size = parameterSize.uint8,
                firstByte = 1,
                Meanings = { [0] = "Sleep", [1] = "Wake", [2] = "Away", [3] = "Return", [250] = "Storage", [251] = "Reset to \"current\" instance" }
            });

            newDGN.Parameters.Add(new RVCParameter
            {
                Name = "Number of schedule instances",
                Type = parameterType.custom,
                Size = parameterSize.uint8,
                firstByte = 2,
            });


            newDGN.Parameters.Add(new RVCParameter
            {
                Name = "Reduced noise mode",
                Type = parameterType.boolean,
                Size = parameterSize.uint2,
                firstByte = 3,
                Meanings = { [0] = "Disabled", [1] = "Endabled" }
            });

            DGNs.Add(newDGN.DgnId, newDGN);

            newDGN = new DGN(true) { DgnId = 0x1FEF7, Name = "THERMOSTAT_SCHEDULE_STATUS_1" };

            newDGN.Parameters.Add(new RVCParameter
            {
                Name = "Schedule mode instance",
                Type = parameterType.list,
                Size = parameterSize.uint8,
                firstByte = 1,
                Id = true,
                Meanings = { [0] = "Sleep", [1] = "Wake", [2] = "Away", [3] = "Return", [250] = "Storage" }
            });

            newDGN.Parameters.Add(new RVCParameter
            {
                Name = "Start hour",
                Type = parameterType.custom,
                Size = parameterSize.uint8,
                firstByte = 2,
            });

            newDGN.Parameters.Add(new RVCParameter
            {
                Name = "Start minute",
                Type = parameterType.custom,
                Size = parameterSize.uint8,
                firstByte = 3,
            });

            newDGN.Parameters.Add(new RVCParameter
            {
                Name = "Setopint temp - Heat",
                Type = parameterType.temperature,
                Size = parameterSize.uint16,
                firstByte = 4,
            });

            newDGN.Parameters.Add(new RVCParameter
            {
                Name = "Setopint temp - Cool",
                Type = parameterType.temperature,
                Size = parameterSize.uint16,
                firstByte = 6,
            });

            DGNs.Add(newDGN.DgnId, newDGN);

            newDGN = new DGN(true) { DgnId = 0x1FEF5, Name = "THERMOSTAT_SCHEDULE_COMMAND_1", Parameters = DGNs[0x1FEF7].Parameters };

            DGNs.Add(newDGN.DgnId, newDGN);

            newDGN = new DGN(true) { DgnId = 0x1FEF6, Name = "THERMOSTAT_SCHEDULE_STATUS_2" };

            newDGN.Parameters.Add(new RVCParameter
            {
                Name = "Schedule mode instance",
                Type = parameterType.list,
                Size = parameterSize.uint8,
                firstByte = 1,
                Id = true,
                Meanings = { [0] = "Sleep", [1] = "Wake", [2] = "Away", [3] = "Return", [250] = "Storage" }
            });

            newDGN.Parameters.Add(new RVCParameter
            {
                Name = "Sunday",
                Type = parameterType.boolean,
                Size = parameterSize.uint2,
                firstByte = 2,
                firstBit = 0,
                Meanings = { [0] = "Not scheduled for this day", [1] = "Schedule applies to this day" }
            });



            newDGN.Parameters.Add(new RVCParameter
            {
                Name = "Monday",
                Type = parameterType.boolean,
                Size = parameterSize.uint2,
                firstByte = 2,
                firstBit = 2,
                Meanings = { [0] = "Not scheduled for this day", [1] = "Schedule applies to this day" }
            });

            newDGN.Parameters.Add(new RVCParameter
            {
                Name = "Tuesday",
                Type = parameterType.list,
                Size = parameterSize.uint2,
                firstByte = 2,
                firstBit = 4,
                Meanings = { [0] = "Not scheduled for this day", [1] = "Schedule applies to this day" }
            });

            newDGN.Parameters.Add(new RVCParameter
            {
                Name = "Wednesday",
                Type = parameterType.boolean,
                Size = parameterSize.uint2,
                firstByte = 2,
                firstBit = 6,
                Meanings = { [0] = "Not scheduled for this day", [1] = "Schedule applies to this day" }
            });

            newDGN.Parameters.Add(new RVCParameter
            {
                Name = "Thursday",
                Type = parameterType.boolean,
                Size = parameterSize.uint2,
                firstByte = 3,
                firstBit = 0,
                Meanings = { [0] = "Not scheduled for this day", [1] = "Schedule applies to this day" }
            });

            newDGN.Parameters.Add(new RVCParameter
            {
                Name = "Friday",
                Type = parameterType.boolean,
                Size = parameterSize.uint2,
                firstByte = 3,
                firstBit = 2,
                Meanings = { [0] = "Not scheduled for this day", [1] = "Schedule applies to this day" }
            });

            newDGN.Parameters.Add(new RVCParameter
            {
                Name = "Saturday",
                Type = parameterType.boolean,
                Size = parameterSize.uint2,
                firstByte = 3,
                firstBit = 4,
                Meanings = { [0] = "Not scheduled for this day", [1] = "Schedule applies to this day" }
            });

            DGNs.Add(newDGN.DgnId, newDGN);

            newDGN = new DGN(true) { DgnId = 0x1FEF4, Name = "THERMOSTAT_SCHEDULE_COMMAND_2", Parameters = DGNs[0x1FEF6].Parameters };

            DGNs.Add(newDGN.DgnId, newDGN);

            newDGN = new DGN(true) { DgnId = 0x1FE97, Name = "CIRCULATION_PUMP_STATUS" };

            newDGN.Parameters.Add(new RVCParameter
            {
                Name = "Output status",
                Type = parameterType.list,
                Size = parameterSize.uint4,
                firstByte = 1,
                Meanings = { [0] = "Off", [1] = "On", [5] = "Test (Forced On)" }
            });

            newDGN.Parameters.Add(new RVCParameter
            {
                Name = "Pump Overcurrent Status",
                Type = parameterType.boolean,
                Size = parameterSize.uint2,
                firstByte = 2,
                Meanings = { [0] = "No overcurrent detected", [1] = "Overcurrent detected" }
            });
            newDGN.Parameters.Add(new RVCParameter
            {
                Name = "Pump Undercurrent Status",
                Type = parameterType.boolean,
                Size = parameterSize.uint2,
                firstByte = 2,
                firstBit = 2,
                Meanings = { [0] = "No undercurrent detected", [1] = "Undercurrent detected" }
            });

            newDGN.Parameters.Add(new RVCParameter
            {
                Name = "Pump Temperature Status",
                Type = parameterType.boolean,
                Size = parameterSize.uint2,
                firstByte = 1,
                firstBit = 4,
                Meanings = { [0] = "Temperature normal", [1] = "Temperature warning" }
            });
            DGNs.Add(newDGN.DgnId, newDGN);

            newDGN = new DGN() { DgnId = 0x1FFFF, Name = "DATE_TIME_STATUS" };

            newDGN.Parameters.Add(new RVCParameter
            {
                Name = "Year",
                Type = parameterType.natural
            });

            newDGN.Parameters.Add(new RVCParameter
            {
                Name = "Month",
                Type = parameterType.natural,
                firstByte = 1
            });

            newDGN.Parameters.Add(new RVCParameter
            {
                Name = "Day of month",
                Type = parameterType.natural,
                firstByte = 2
            });

            newDGN.Parameters.Add(new RVCParameter
            {
                Name = "Day of week",
                Type = parameterType.natural,
                firstByte = 3
            });

            newDGN.Parameters.Add(new RVCParameter
            {
                Name = "Hour",
                Type = parameterType.natural,
                firstByte = 4
            });

            newDGN.Parameters.Add(new RVCParameter
            {
                Name = "Minute",
                Type = parameterType.natural,
                firstByte = 5
            });

            newDGN.Parameters.Add(new RVCParameter
            {
                Name = "Second",
                Type = parameterType.natural,
                firstByte = 6
            });

            DGNs.Add(newDGN.DgnId, newDGN);

            newDGN = new DGN() { DgnId = 0x1FE99, Name = "WATERHEATER_STATUS_2" };

            newDGN.Parameters.Add(new RVCParameter()
            {
                Name = "Electric Element Level",
                Size = parameterSize.uint4,
                firstByte = 1
            });

            newDGN.Parameters.Add(new RVCParameter()
            {
                Name = "Max Electric Element Leve",
                Size = parameterSize.uint4,
                firstByte = 1,
                firstBit = 4
            });

            newDGN.Parameters.Add(new RVCParameter()
            {
                Name = "Engine Preheat",
                Size = parameterSize.uint4,
                firstByte = 2,
                Type = parameterType.list,
                Meanings = { [0] = "Off", [1] = "On", [5] = "Test(Forced On)" }
            });

            newDGN.Parameters.Add(new RVCParameter()
            {
                Name = "Coolant Level Warning",
                Size = parameterSize.uint2,
                firstByte = 2,
                firstBit = 4,
                Type = parameterType.boolean,
                Meanings = { [0] = "Coolant level sufficient", [1] = "Coolant level low" }
            });

            newDGN.Parameters.Add(new RVCParameter()
            {
                Name = "Hot Water Priority",
                Size = parameterSize.uint2,
                firstByte = 2,
                firstBit = 4,
                Type = parameterType.list,
                Meanings = { [0] = "Domestic water priority", [1] = "Heating priority" }
            });
            DGNs.Add(newDGN.DgnId, newDGN);

            newDGN = new DGN(true) { DgnId = 0x1FFBB, Name = "DC_DIMMER_STATUS_1" };
            newDGN.Parameters.Add(new RVCParameter()
            {
                Name = "Master brightness",
                Size = parameterSize.uint8,
                firstByte = 1,
                Type = parameterType.percent
            }
            );

            newDGN.Parameters.Add(new RVCParameter()
            {
                Name = "Red brightnessd",
                Size = parameterSize.uint8,
                firstByte = 2,
                Type = parameterType.percent
            }
            );

            newDGN.Parameters.Add(new RVCParameter()
            {
                Name = "Green brightness",
                Size = parameterSize.uint8,
                firstByte = 3,
                Type = parameterType.percent
            }
            );

            newDGN.Parameters.Add(new RVCParameter()
            {
                Name = "Blue brightness",
                Size = parameterSize.uint8,
                firstByte = 4,
                Type = parameterType.percent
            }
            );

            newDGN.Parameters.Add(new RVCParameter()
            {
                Name = "On duration",
                Size = parameterSize.uint4,
                firstByte = 5,
                Type = parameterType.natural,
                Unit = "s",
                Meanings = { [0] = "Always On" }
            });

            newDGN.Parameters.Add(new RVCParameter()
            {
                Name = "Off duration",
                Size = parameterSize.uint4,
                firstByte = 5,
                firstBit = 4,
                Type = parameterType.natural,
                Unit = "s",
                Meanings = { [0] = "One Shot" }
            });

            newDGN.Parameters.Add(new RVCParameter()
            {
                Name = "White brightness",
                Size = parameterSize.uint8,
                firstByte = 6,
                Type = parameterType.percent
            });
            
            DGNs.Add(newDGN.DgnId, newDGN);

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
        public int DgnId;
        public string Name = "";
        public int MaxBroadcastGap = 5000;
        public int MinBroadcastGap = 500;
        public List<RVCParameter> Parameters;

        public DGN(bool addInstance = false)
        {
            Parameters = new List<RVCParameter>();
            if (addInstance) this.Parameters.Add(new RVCParameter { Name = "Instance", ShortName = "#", Type = parameterType.instance, Size = parameterSize.uint8, firstByte = 0, Id = true });
        }
        public string Decode(byte[] data)
        {
            string ret = Name + ":{;";
            foreach (RVCParameter p in Parameters)
            {
                ret += p.ToString(data) + "; ";
            }
            ret += "}";
            return ret;
        }


    }
    public class RVCParameter
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
        
        public RVCParameter()
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
                    if (!Meanings.ContainsKey(rawData))
                        retString += rawData.ToString();
                    else
                        retString += Meanings[rawData].ToString();
                    if (Unit.Length > 0) retString += " " + Unit;
                    break;

                default: throw new ArgumentException("Bad parameter type");
            }

            return retString;
        }
    }
    public class RvcMessage:CanMessage
    {

        public byte Instance => Data[0];
        public IEnumerable<RVCParameter> Parameters => RVC.DGNs.ContainsKey(DGN) ? RVC.DGNs[DGN].Parameters : null;
        public int Priority
        {
            get { return (ID>>26) & 0x7; }
            set
            {
                if (value > 7)
                    throw new ArgumentException("Priority can't be over 7");
                ID &= ~(0x3<<26);
                ID |= value<<26;
            }
        }
        public int DGN
        {
            get { return (ID >> 8) & 0x1FFFF; }
            set
            {
                if (value > 0x1FFFF)
                    throw new ArgumentException("DGN can't be over 0x1FFFF");
                ID &= ~(0x1FFFF << 8);
                ID |= value << 8;
            }
        }

        public int SourceAdress
        {
            get { return (ID ) & 0xFF; }
            set
            {
                if (value > 255)
                    throw new ArgumentException("DGN can't be over 255");
                ID &= ~(0xFF);
                ID |= value;
            }
        }


        public override bool Equals(object obj)
        {
            return base.Equals(obj);
        }

        public override string ToString()
        {

            string ret = $"{Priority} | {DGN:X05} {SourceAdress:D3} ||";
            foreach (var item in Data)
                ret += $" {item:X02} ";
            return ret;
        }

        public string VerboseInfo()
        {
            if (!RVC.DGNs.ContainsKey(DGN))
                return $"{DGN:X} is not supported";
            return RVC.DGNs[DGN].Decode(Data);
        }
        public override int GetHashCode()
        {
            return base.GetHashCode();
        }

        public int? getPosInList(List<RvcMessage> list)
        {
            bool thisOne = false;

            for (int i = 0; i < list.Count; i++)
            {
                RvcMessage m = list[i];
                if (m.DGN != DGN)
                    continue;

                thisOne = true;
                if (RVC.DGNs.ContainsKey(m.DGN))
                    foreach (RVCParameter p in m.Parameters)
                    {
                        if (p.Id == true && p.RawData(m.Data) != p.RawData(Data))
                            thisOne = false;
                    }
                if (thisOne) return i;

            }
            return null;
        }

        new public event PropertyChangedEventHandler PropertyChanged;
        public override void PropChanged(string propertyName)
        {
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
        }

    }
}
