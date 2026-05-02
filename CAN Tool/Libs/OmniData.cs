using CAN_Tool.Libs;
using CommunityToolkit.Mvvm.ComponentModel;
using System.Collections.Generic;
using static CAN_Tool.Libs.Helper;

namespace OmniProtocol
{
    public enum UnitType_t { None, Temp, Volt, Current, Pressure, Flow, Rpm, Rps, Percent, Second, Minute, Hour, Day, Month, Year, Frequency, Power }

    public enum DeviceType_t { Binar, Planar, Hcu, ValveControl, BootLoader, CookingPanel, ExtensionBoard, PressureSensor, GenericLoadSingle, GenericLoadTripple, AcInverter, AcPanel, Modem, Panel }

    public enum LoadMode_t { Off = 0, Toggle = 1, Pwm = 2 };

    public enum zoneType_t { Disconnected = 0, Furnace = 1, Defrosting = 2, Radiator = 3 };

    public enum zoneState_t { Off = 0, Heat = 1, Fan = 2 };

    //public enum acMode_t { ac_off, ac_cool, ac_dry, ac_eco, ac_night, ac_max, ac_manual, ac_error };

    //public enum acFanMode_t { fan_auto, fan_1, fan_2, fan_3, fan_4, fan_5};

    public partial class Omni : ObservableObject
    {
        public static Dictionary<int, DeviceTemplate> Devices { set; get; } = new();
        public static Dictionary<int, PgnClass> Pgns { get; } = new();
        public static List<OmniPgnParameter> Parameters { get; } = new();
        public static Dictionary<int, OmniCommand> Commands { get; } = new();

        public void SeedStaticData()
        {

            Devices = new Dictionary<int, DeviceTemplate>() {
            { 0,   new() { Id = 0 }} ,
            { 1,   new() { Id = 1,   DevType = DeviceType_t.Binar,         ImageName = "ts14_mini"     }} , // 14ТС-Мини
            { 2,   new() { Id = 2,   DevType = DeviceType_t.Planar,        ImageName = "planar2"       }} , // Планар 2
            { 3,   new() { Id = 3,   DevType = DeviceType_t.Planar,        ImageName = "planar44"      }} , // Планар 44Д
            { 4,   new() { Id = 4,   DevType = DeviceType_t.Binar                                      }} , // 30ТСД
            { 5,   new() { Id = 5,   DevType = DeviceType_t.Binar                                      }} , // 30ТСГ
            { 6,   new() { Id = 6,   DevType = DeviceType_t.Binar,  MaxBlower = 200, ImageName = "binar5s"      }} , // Binar-5S бензин
            { 7,   new() { Id = 7,   DevType = DeviceType_t.Planar,        ImageName = "planar8"       }} , // Планар 8Д
            { 8,   new() { Id = 8,   DevType = DeviceType_t.Binar                                      }} , // OB-8
            { 9,   new() { Id = 9,   DevType = DeviceType_t.Planar,        ImageName = "planar4"       }} , // Планар 4Д
            { 10,  new() { Id = 10,  DevType = DeviceType_t.Binar,  MaxBlower = 200, ImageName = "binar5s"      }} , // Binar-5S дизель
            { 11,  new() { Id = 11,  DevType = DeviceType_t.Planar,        ImageName = "planar9"       }} , // Планар-9Д, ОВ-8ДК
            { 12,  new() { Id = 12,  DevType = DeviceType_t.Planar,        ImageName = "planar44"      }} , // Планар-44Б
            { 13,  new() { Id = 13,  DevType = DeviceType_t.Planar,        ImageName = "planar4"       }} , // Планар-4Б
            { 14,  new() { Id = 14,  DevType = DeviceType_t.CookingPanel                               }} , // Термикс
            { 15,  new() { Id = 15,  DevType = DeviceType_t.Planar,        ImageName = "planar44"      }} , // Планар-44Г
            { 16,  new() { Id = 16,  DevType = DeviceType_t.Binar                                      }} , // ОВ-4
            { 17,  new() { Id = 17,  DevType = DeviceType_t.Binar,         ImageName = "ts14"          }} , // 14ТСД-10
            { 18,  new() { Id = 18,  DevType = DeviceType_t.Planar,        ImageName = "planar2"       }} , // Планар 2Б
            { 19,  new() { Id = 19,  DevType = DeviceType_t.ValveControl                               }} , // Блок управления клапанами
            { 20,  new() { Id = 20,  DevType = DeviceType_t.Planar                                     }} , // Планар-6Д
            { 21,  new() { Id = 21,  DevType = DeviceType_t.Binar,         ImageName = "ts14"          }} , // 14ТС-10
            { 22,  new() { Id = 22,  DevType = DeviceType_t.Binar                                      }} , // 30SP впрысковый
            { 23,  new() { Id = 23,  DevType = DeviceType_t.Binar,  MaxBlower = 90, MaxFuelPump = 4.7, ImageName = "binar5_compact" }} , // Бинар 5Б-Компакт
            { 25,  new() { Id = 25,  DevType = DeviceType_t.Binar,         ImageName = "sp35"          }} , // 35SP впрысковый
            { 27,  new() { Id = 27,  DevType = DeviceType_t.Binar,  MaxBlower = 90, MaxFuelPump = 4.3, ImageName = "binar5_compact" }} , // Бинар 5Д-Компакт
            { 29,  new() { Id = 29,  DevType = DeviceType_t.Binar,         ImageName = "binar5_compact"}} , // Бинар 6Г-Компакт
            { 31,  new() { Id = 31,  DevType = DeviceType_t.Binar                                      }} , // 14ТСГ-Мини
            { 32,  new() { Id = 32,  DevType = DeviceType_t.Binar                                      }} , // 30SPG
            { 34,  new() { Id = 34,  DevType = DeviceType_t.Binar,  MaxFuelPump = 8, MaxBlower = 140, ImageName = "binar10"       }} , // Binar-10Д
            { 35,  new() { Id = 35,  DevType = DeviceType_t.Binar,  MaxFuelPump = 8, MaxBlower = 140, ImageName = "binar10"       }} , // Binar-10Б
            { 37,  new() { Id = 37,  DevType = DeviceType_t.ExtensionBoard,  ImageName = "ext_board"   }} , // Плата расширения
            { 39,  new() { Id = 39,  DevType = DeviceType_t.Planar                                     }} , // Военные изделия
            { 40,  new() { Id = 40,  DevType = DeviceType_t.Binar,         ImageName = "ts15g"         }} , // 15ТСГ-ГАЗ
            { 41,  new() { Id = 41,  DevType = DeviceType_t.GenericLoadSingle, ImageName = "ext_board"  }} , // Одноканальная плата нагрузки
            { 42,  new() { Id = 42,  DevType = DeviceType_t.GenericLoadTripple, ImageName = "load3ch"  }} , // Трёхканальная плата нагрузки
            { 43,  new() { Id = 43,  DevType = DeviceType_t.Binar,  MaxBlower = 90, MaxFuelPump = 4.3, ImageName = "binar_silent" }} , // Тихий бинар дизель
            { 44,  new() { Id = 44,  DevType = DeviceType_t.Binar,  MaxBlower = 90, MaxFuelPump = 4.7, ImageName = "binar_silent" }} , // Тихий бинар бензин
            { 50,  new() { Id = 50,  DevType = DeviceType_t.AcInverter,    ImageName = "ac_driver"     }} , // Драйвер компрессора
            { 60,  new() { Id = 60,  DevType = DeviceType_t.PressureSensor, ImageName="pressure"       }} , // Датчик давления
            { 121, new() { Id = 121, DevType = DeviceType_t.Modem,         ImageName = "modem"         }} , // CAN-Модем
            { 123, new() { Id = 123, DevType = DeviceType_t.BootLoader,    ImageName = "bootloader"    }} , // Bootloader
            { 124, new() { Id = 124, DevType = DeviceType_t.AcPanel,       ImageName = "ac_panel"      }} , // Плата управления кондиционером
            { 125, new() { Id = 125, DevType = DeviceType_t.Hcu,           ImageName = "hcu"           }} , // HCU
            { 126, new() { Id = 126, DevType = DeviceType_t.Panel,         ImageName = "hcu"           }} , // Устройство управления
            { 255, new() { Id = 255 }}
        };

            OmniDataLoader.Load(Pgns, Parameters, Commands);
        }
    }
}
#if NEVER
            Pgns.Add(1, new() { id = 1, name = "t_control_command" });
            Pgns.Add(2, new() { id = 2, name = "t_received_command_ack" });
            Pgns.Add(3, new() { id = 3, name = "t_spn_request" });
            Pgns.Add(4, new() { id = 4, name = "t_spn_answer" });
            Pgns.Add(5, new() { id = 5, name = "t_parameter_write" });
            Pgns.Add(6, new() { id = 6, name = "t_pgn_request" });
            Pgns.Add(7, new() { id = 7, name = "t_flash_conf_read_write" });
            Pgns.Add(8, new() { id = 8, name = "t_black_box_operation" });
            Pgns.Add(9, new() { id = 9, name = "t_pwm_data" });
            Pgns.Add(10, new() { id = 10, name = "t_stage_mode_failures" });
            Pgns.Add(11, new() { id = 11, name = "t_voltage_pressure_current" });
            Pgns.Add(12, new() { id = 12, name = "t_blower_fp_plug_relay" });
            Pgns.Add(13, new() { id = 13, name = "t_liquid_heater_temperatures" });
            Pgns.Add(14, new() { id = 14, name = "t_flame_process" });
            Pgns.Add(15, new() { id = 15, name = "t_adc0-3" });
            Pgns.Add(16, new() { id = 16, name = "t_adc4-7" });
            Pgns.Add(17, new() { id = 17, name = "t_adc8-11" });
            Pgns.Add(18, new() { id = 18, name = "t_firmware_version" });
            Pgns.Add(19, new() { id = 19, name = "t_hcu_Parameters", multiPack = true });
            Pgns.Add(20, new() { id = 20, name = "t_failures" });
            Pgns.Add(21, new() { id = 21, name = "t_hcu_status" });
            Pgns.Add(22, new() { id = 22, name = "t_zone_control" });
            Pgns.Add(23, new() { id = 23, name = "t_fan_setpoints" });
            Pgns.Add(24, new() { id = 24, name = "t_fan_current_speed" });
            Pgns.Add(25, new() { id = 25, name = "t_daytime_setpoins" });
            Pgns.Add(26, new() { id = 26, name = "t_nighttime_setpoints" });
            Pgns.Add(27, new() { id = 27, name = "t_fan_manual_control" });
            Pgns.Add(28, new() { id = 28, name = "t_total_working_time" });
            Pgns.Add(29, new() { id = 29, name = "t_Параметры давления", multiPack = true });
            Pgns.Add(30, new() { id = 30, name = "t_remote_wire_engine_air_temp" });
            Pgns.Add(31, new() { id = 31, name = "t_working_time" });
            Pgns.Add(32, new() { id = 32, name = "t_liquid_heater_setup" });
            Pgns.Add(33, new() { id = 33, name = "t_serial_number", multiPack = true });
            Pgns.Add(34, new() { id = 34, name = "t_read_flash_by_address_req" });
            Pgns.Add(35, new() { id = 35, name = "t_read_flash_by_address_ans" });
            Pgns.Add(36, new() { id = 36, name = "t_valves_status_probe_valve_failures" });
            Pgns.Add(37, new() { id = 37, name = "t_air_heater_temperatures", multiPack = true });
            Pgns.Add(38, new() { id = 38, name = "t_panel_temperature" });
            Pgns.Add(39, new() { id = 39, name = "t_drivers_status" });
            Pgns.Add(40, new() { id = 40, name = "t_date_time" });
            Pgns.Add(41, new() { id = 41, name = "t_day_night_backlight" });
            Pgns.Add(42, new() { id = 42, name = "t_pump_control" });
            Pgns.Add(43, new() { id = 43, name = "t_generic_board_pwm_command" });
            Pgns.Add(44, new() { id = 44, name = "t_generic_board_pwm_status" });
            Pgns.Add(45, new() { id = 45, name = "t_generic_board_temp" });
            Pgns.Add(46, new() { id = 46, name = "t_hcu_error_codes" });
            Pgns.Add(47, new() { id = 47, name = "t_actuator_override" });
            Pgns.Add(48, new() { id = 48, name = "t_test_report" });
            Pgns.Add(49, new() { id = 49, name = "t_generic_load_control" });
            Pgns.Add(50, new() { id = 50, name = "t_compressor_control", multiPack = true });
            Pgns.Add(51, new() { id = 51, name = "t_ac_control", multiPack = true });
            Pgns.Add(52, new() { id = 52, name = "t_ac_manual_control", multiPack = true });
            Pgns.Add(55, new() { id = 55, name = "t_tank_levels" });
            Pgns.Add(56, new() { id = 56, name = "t_furnace_control" });
            Pgns.Add(57, new() { id = 57, name = "t_extra_heater_Parameters" });
            Pgns.Add(99, new() { id = 99, name = "t_debug_pack" });
            Pgns.Add(100, new() { id = 100, name = "t_memory_control_old", multiPack = true });
            Pgns.Add(101, new() { id = 101, name = "t_buffer_data_transmitting_old" });
            Pgns.Add(105, new() { id = 105, name = "t_memory_control" });
            Pgns.Add(106, new() { id = 106, name = "t_buffer_data_transmitting" });


            Commands.Add(0, new() { Id = 0 });
            Commands.Add(1, new() { Id = 1 });
            Commands.Add(3, new() { Id = 3 });
            Commands.Add(4, new() { Id = 4 });
            Commands.Add(5, new() { Id = 5 });
            Commands.Add(6, new() { Id = 6 });
            Commands.Add(7, new() { Id = 7 });
            Commands.Add(8, new() { Id = 8 });
            Commands.Add(9, new() { Id = 9 });
            Commands.Add(10, new() { Id = 10 });
            Commands.Add(19, new() { Id = 19 });
            Commands.Add(20, new() { Id = 20 });
            Commands.Add(21, new() { Id = 21 });
            Commands.Add(22, new() { Id = 22 });
            Commands.Add(45, new() { Id = 45 });
            Commands.Add(65, new() { Id = 65 });
            Commands.Add(66, new() { Id = 66 });
            Commands.Add(67, new() { Id = 67 });
            Commands.Add(68, new() { Id = 68 });
            Commands.Add(69, new() { Id = 69 });
            Commands.Add(70, new() { Id = 70 });



            Commands[0].Parameters.Add(new() { StartByte = 2, BitLength = 8, GetMeaning = i => (GetString("t_device") + ": " + GetString($"d_{i}") + ";"), AnswerOnly = true }); ;
            Commands[0].Parameters.Add(new() { StartByte = 3, BitLength = 8, Meanings = { { 0, "t_12 volts" }, { 1, "t_24_volts" } }, AnswerOnly = true });
            Commands[0].Parameters.Add(new() { StartByte = 4, BitLength = 8, Name = "t_firmware", AnswerOnly = true });
            Commands[0].Parameters.Add(new() { StartByte = 5, BitLength = 8, Name = "t_modification", AnswerOnly = true });

            Commands[1].Parameters.Add(new() { StartByte = 2, BitLength = 16, Name = "t_working_time", UnitT = UnitType_t.Minute });

            Commands[4].Parameters.Add(new() { StartByte = 2, BitLength = 16, Name = "t_working_time", UnitT = UnitType_t.Minute });

            Commands[6].Parameters.Add(new() { StartByte = 4, BitLength = 4, Name = "t_working_mode", Meanings = { { 0, "t_regular" }, { 1, "t_eco" }, { 2, "t_additional_heater" }, { 3, "t_preheater" }, { 4, "t_heating_systems" } } });
            Commands[6].Parameters.Add(new() { StartByte = 4, StartBit = 4, BitLength = 4, Name = "t_additional_heater_mode", Meanings = { { 0, "t_off" }, { 1, "t_auto" }, { 2, "t_manual" } } });
            Commands[6].Parameters.Add(new() { StartByte = 5, BitLength = 16, Name = "t_temp_setpoint", UnitT = UnitType_t.Temp });
            Commands[6].Parameters.Add(new() { StartByte = 7, BitLength = 2, Name = "t_pump_in_idle", Meanings = DefMeaningsOnOff });
            Commands[6].Parameters.Add(new() { StartByte = 7, BitLength = 2, StartBit = 2, Name = "t_pump_while_engine_running", Meanings = DefMeaningsOnOff });

            Commands[7].Parameters.Add(new() { StartByte = 2, BitLength = 8, Name = "t_power_level" });

            Commands[7].Parameters.Add(new() { StartByte = 3, BitLength = 16, Name = "t_going_up_temperature", AnswerOnly = true });
            Commands[7].Parameters.Add(new() { StartByte = 4, BitLength = 16, Name = "t_going_down_temperature", AnswerOnly = true });

            Commands[8].Parameters.Add(new() { StartByte = 2, BitLength = 2, StartBit = 0, Name = "t_valve_1_state", Meanings = DefMeaningsOnOff });
            Commands[8].Parameters.Add(new() { StartByte = 2, BitLength = 2, StartBit = 2, Name = "t_valve_2_state", Meanings = DefMeaningsOnOff });
            Commands[8].Parameters.Add(new() { StartByte = 2, BitLength = 2, StartBit = 4, Name = "t_valve_3_state", Meanings = DefMeaningsOnOff });
            Commands[8].Parameters.Add(new() { StartByte = 2, BitLength = 2, StartBit = 6, Name = "t_valve_4_state", Meanings = DefMeaningsOnOff });
            Commands[8].Parameters.Add(new() { StartByte = 3, BitLength = 2, StartBit = 0, Name = "t_valve_5_state", Meanings = DefMeaningsOnOff });
            Commands[8].Parameters.Add(new() { StartByte = 3, BitLength = 2, StartBit = 2, Name = "t_valve_6_state", Meanings = DefMeaningsOnOff });
            Commands[8].Parameters.Add(new() { StartByte = 3, BitLength = 2, StartBit = 4, Name = "t_valve_7_state", Meanings = DefMeaningsOnOff });
            Commands[8].Parameters.Add(new() { StartByte = 3, BitLength = 2, StartBit = 6, Name = "t_valve_8_state", Meanings = DefMeaningsOnOff });
            Commands[8].Parameters.Add(new() { StartByte = 4, BitLength = 1, StartBit = 0, Meanings = { { 0, "t_do_not_clear_codes" }, { 1, "t_clear_error_codes" } } });

            Commands[9].Parameters.Add(new() { StartByte = 2, BitLength = 16, Name = "t_working_time", UnitT = UnitType_t.Minute });
            Commands[9].Parameters.Add(new() { StartByte = 4, BitLength = 4, Name = "t_working_mode", Meanings = { { 0, "t_not_used" }, { 1, "t_work_by_pcb_temp" }, { 2, "t_work_by_panel_sensor_temp" }, { 3, "t_work_by_external_sensor" }, { 4, "t_work_by_power" } } });
            Commands[9].Parameters.Add(new() { StartByte = 4, StartBit = 4, BitLength = 2, Name = "t_enable_idle_while_working_by_temp_sensor", Meanings = DefMeaningsAllow });
            Commands[9].Parameters.Add(new() { StartByte = 4, StartBit = 6, BitLength = 2, Name = "t_enable_blower_while_idle", Meanings = DefMeaningsAllow });
            Commands[9].Parameters.Add(new() { StartByte = 5, BitLength = 16, Name = "t_set_room_temperature", UnitT = UnitType_t.Temp });
            Commands[9].Parameters.Add(new() { StartByte = 7, BitLength = 4, Name = "t_power_setpoint" });

            Commands[10].Parameters.Add(new() { StartByte = 2, BitLength = 16, Name = "t_working_time", UnitT = UnitType_t.Temp });

            Commands[19].Parameters.Add(new() { StartByte = 2, BitLength = 8, Name = "t_set_power_level" });

            Commands[20].Parameters.Add(new() { StartByte = 2, BitLength = 16, Name = "t_1st_tcouple_cal", AnswerOnly = true });
            Commands[20].Parameters.Add(new() { StartByte = 4, BitLength = 16, Name = "t_2nd_tcouple_cal", AnswerOnly = true });

            Commands[21].Parameters.Add(new() { StartByte = 2, BitLength = 8, Name = "t_prescaler" });
            Commands[21].Parameters.Add(new() { StartByte = 3, BitLength = 8, Name = "t_pwm_period" });
            Commands[21].Parameters.Add(new() { StartByte = 5, BitLength = 8, Name = "t_required_freq", UnitT = UnitType_t.Frequency });

            Commands[22].Parameters.Add(new() { StartByte = 2, StartBit = 0, BitLength = 8, Name = "t_action_after_reset", Meanings = { { 0, "t_stay_in_boot" }, { 1, "t_to_main_program_without_delay" }, { 2, "t_5_sec_in_boot" } } });

            Commands[45].Parameters.Add(new() { StartByte = 2, StartBit = 0, BitLength = 2, Name = "t_mask_all", Meanings = DefMeaningsYesNo });
            Commands[45].Parameters.Add(new() { StartByte = 2, StartBit = 2, BitLength = 2, Name = "t_mask_fp_failures", Meanings = DefMeaningsYesNo });
            Commands[45].Parameters.Add(new() { StartByte = 2, StartBit = 4, BitLength = 2, Name = "t_mask_flamebreak_fails", Meanings = DefMeaningsYesNo });
            Commands[45].Parameters.Add(new() { StartByte = 2, StartBit = 6, BitLength = 2, Name = "t_mask_glow_plug_failures", Meanings = DefMeaningsYesNo });
            Commands[45].Parameters.Add(new() { StartByte = 3, StartBit = 0, BitLength = 2, Name = "t_mask_blower_failures", Meanings = DefMeaningsYesNo });
            Commands[45].Parameters.Add(new() { StartByte = 3, StartBit = 2, BitLength = 2, Name = "t_mask_sensors_failures", Meanings = DefMeaningsYesNo });
            Commands[45].Parameters.Add(new() { StartByte = 3, StartBit = 4, BitLength = 2, Name = "t_mask_pump_failures", Meanings = DefMeaningsYesNo });
            Commands[45].Parameters.Add(new() { StartByte = 3, StartBit = 6, BitLength = 2, Name = "t_mask_overheating_failures", Meanings = DefMeaningsYesNo });

            Commands[65].Parameters.Add(new() { StartByte = 2, StartBit = 0, BitLength = 8, Name = "", Meanings = { { 7, "t_liquid_temp" }, { 10, "t_overheat_temp" }, { 12, "t_flame_temperature" }, { 13, "t_body_temp" }, { 27, "t_air_temp" } } });
            Commands[65].Parameters.Add(new() { StartByte = 3, BitLength = 16, Name = "t_temp_value", UnitT = UnitType_t.Temp });

            Commands[67].Parameters.Add(new() { StartByte = 2, StartBit = 0, BitLength = 2, Name = "", Meanings = { { 0, "t_leave_m" }, { 1, "t_ener_m" } } });
            Commands[67].Parameters.Add(new() { StartByte = 2, StartBit = 2, BitLength = 2, Name = "", Meanings = { { 0, "t_leave_t" }, { 1, "t_enter_t" } } });

            Commands[68].Parameters.Add(new() { StartByte = 2, StartBit = 0, BitLength = 2, Name = "t_pump_state", Meanings = DefMeaningsOnOff });
            Commands[68].Parameters.Add(new() { StartByte = 3, StartBit = 0, BitLength = 8, Name = "t_blower_revs", UnitT = UnitType_t.Rps });
            Commands[68].Parameters.Add(new() { StartByte = 4, StartBit = 0, BitLength = 8, Name = "t_glow_plug", UnitT = UnitType_t.Percent });
            Commands[68].Parameters.Add(new() { StartByte = 5, StartBit = 0, BitLength = 16, Name = "t_fuel_pump_freq", a = 0.01, UnitT = UnitType_t.Frequency });

            Commands[69].Parameters.Add(new() { StartByte = 2, StartBit = 0, BitLength = 8, Name = "t_exec_dev_type", Meanings = { { 0, "t_fpx10" }, { 1, "t_relay01" }, { 2, "t_glow_plug_perc" }, { 3, "t_pump_perc" }, { 4, "t_blower_perc" }, { 23, "t_blower_revs_rps" } } });
            Commands[69].Parameters.Add(new() { StartByte = 3, StartBit = 0, BitLength = 16, Name = "Значение" });

            Commands[70].Parameters.Add(new() { StartByte = 2, StartBit = 0, BitLength = 2, Name = "t_fuel_pump_state", Meanings = DefMeaningsOnOff });
            Commands[70].Parameters.Add(new() { StartByte = 2, StartBit = 2, BitLength = 2, Name = "t_relay_state", Meanings = DefMeaningsOnOff });
            Commands[70].Parameters.Add(new() { StartByte = 2, StartBit = 4, BitLength = 2, Name = "t_glow_plug_state", Meanings = DefMeaningsOnOff });
            Commands[70].Parameters.Add(new() { StartByte = 2, StartBit = 6, BitLength = 2, Name = "t_pump_state", Meanings = DefMeaningsOnOff });
            Commands[70].Parameters.Add(new() { StartByte = 3, StartBit = 0, BitLength = 2, Name = "t_blower_state", Meanings = DefMeaningsOnOff });




            Parameters.Add(new() { Pgn = 3, Name = "SPN", BitLength = 16, StartByte = 0 });

            Parameters.Add(new() { Pgn = 4, Name = "SPN", BitLength = 16, StartByte = 0 });
            Parameters.Add(new() { Pgn = 4, Name = "Значение", BitLength = 32, StartBit = 0, StartByte = 2 });

            Parameters.Add(new() { Pgn = 5, Name = "SPN", BitLength = 16, StartByte = 0 });
            Parameters.Add(new() { Pgn = 5, Name = "Значение", BitLength = 32, StartBit = 0, StartByte = 2 });

            Parameters.Add(new() { Pgn = 6, Name = "Pgn", BitLength = 16, StartByte = 0, GetMeaning = x => { if (Pgns.ContainsKey(x)) return Pgns[x].name; else return "Нет такого Pgn"; } });

            Parameters.Add(new() { Pgn = 7, Name = "Команда", BitLength = 8, StartBit = 0, StartByte = 0, Meanings = { { 0, "Стереть конфигурацию" }, { 1, "Запись параметра в ОЗУ" }, { 2, "Запись всех параметров во Flash" }, { 3, "Чтение параметра по номеру" }, { 4, "Успешный ответ на запрос" }, { 5, "Невозможно выполнить" } } });
            Parameters.Add(new() { Pgn = 7, Name = "Запрошенная команда", BitLength = 8, StartBit = 0, StartByte = 1, Meanings = { { 0, "Стереть конфигурацию" }, { 1, "Запись параметра в ОЗУ" }, { 2, "Запись всех параметров во Flash" }, { 3, "Чтение параметра по номеру" }, { 255, "" } } });
            Parameters.Add(new() { Pgn = 7, Name = "Параметр", BitLength = 16, StartBit = 0, StartByte = 2, GetMeaning = x => GetString($"par_{x}") });
            Parameters.Add(new() { Pgn = 7, Name = "Value", BitLength = 32, StartBit = 0, StartByte = 4, AnswerOnly = true });

            Parameters.Add(new() { Pgn = 8, Name = "Команда", BitLength = 4, StartBit = 0, StartByte = 0, Meanings = { { 0, "Стереть ЧЯ" }, { 3, "Чтение ЧЯ" }, { 4, "Ответ" }, { 6, "Чтение параметра (из paramsname.h)" } } });
            Parameters.Add(new() { Pgn = 8, Name = "Тип:", BitLength = 2, StartBit = 4, StartByte = 0, Meanings = { { 0, "Общие данные" }, { 1, "Неисправности" } } });
            Parameters.Add(new() { Pgn = 8, Name = "Номер пары", CustomDecoder = d => { if ((d[0] & 0xF) == 3) return "Номер пары:" + (d[4] * 0x100 + d[5]).ToString() + ";"; else return ""; } });
            Parameters.Add(new() { Pgn = 8, Name = "Номер параметра", CustomDecoder = d => { if ((d[0] & 0xF) == 6) return "Номер параметра:" + (d[4] * 0x100 + d[5]).ToString() + ";"; else return ""; } });
            Parameters.Add(new() { Pgn = 8, Name = "Число пар", CustomDecoder = d => { if ((d[0] & 0xF) == 6) return "Запрошено пар:" + (d[6] * 0x100 + d[7]).ToString() + ";"; else return ""; } });
            Parameters.Add(new() { Pgn = 8, Name = "Номер параметра", CustomDecoder = d => { if (d[0] == 4) return "Параметр:" + (d[2] * 256 + d[3]).ToString() + ";"; else return ""; } });
            Parameters.Add(new() { Pgn = 8, Name = "Значение параметра", CustomDecoder = d => { if (d[0] == 4) return "Значение:" + (d[4] * 0x1000000 + d[5] * 0x10000 + d[6] * 0x100 + d[7]).ToString() + ";"; else return ""; }, AnswerOnly = true });

            Parameters.Add(new() { Pgn = 9, Name = "Текущий ШИМ", BitLength = 16, a = 0.01, UnitT = UnitType_t.Percent, Var = 133 });

            Parameters.Add(new() { Pgn = 10, Name = "Стадия", BitLength = 8, StartByte = 0, Meanings = Stages, Var = 1 });
            Parameters.Add(new() { Pgn = 10, Name = "Режим", BitLength = 8, StartByte = 1, Var = 2 });
            Parameters.Add(new() { Pgn = 10, Name = "Код неисправности", BitLength = 8, StartByte = 2, Var = 24, GetMeaning = x => GetString($"e_{x}") });
            Parameters.Add(new() { Pgn = 10, Name = "Помпа неисправна", BitLength = 2, StartByte = 3, Meanings = DefMeaningsYesNo });
            Parameters.Add(new() { Pgn = 10, Name = "Код предупреждения", BitLength = 8, StartByte = 4 });
            Parameters.Add(new() { Pgn = 10, Name = "Количество морганий", BitLength = 8, StartByte = 5, Var = 25 });
            Parameters.Add(new() { Pgn = 10, Name = "Желаемый режим мощности", BitLength = 8, StartByte = 6, Var = 132 });
            Parameters.Add(new() { Pgn = 10, Name = "Вариант розжига", BitLength = 4, StartByte = 7 });
            Parameters.Add(new() { Pgn = 10, Name = "Число срывов пламени", BitLength = 4, StartByte = 7, StartBit = 4 });

            Parameters.Add(new() { Pgn = 11, Name = "Напряжение питания", BitLength = 16, StartByte = 0, a = 0.1, UnitT = UnitType_t.Volt, Var = 5 });
            Parameters.Add(new() { Pgn = 11, Name = "Атмосферное давление", BitLength = 8, StartByte = 2, UnitT = UnitType_t.Pressure });
            Parameters.Add(new() { Pgn = 11, Name = "Ток двигателя, значения АЦП", BitLength = 16, StartByte = 3 });
            Parameters.Add(new() { Pgn = 11, Name = "Ток двигателя, мА", BitLength = 16, StartByte = 5, UnitT = UnitType_t.Current, a = 0.001, Var = 128 });

            Parameters.Add(new() { Pgn = 12, Name = "Заданные обороты нагнетателя воздуха", BitLength = 8, StartByte = 0, UnitT = UnitType_t.Rps, Var = 15 });
            Parameters.Add(new() { Pgn = 12, Name = "Измеренные обороты нагнетателя воздуха,", BitLength = 8, StartByte = 1, UnitT = UnitType_t.Rps, Var = 16 });
            Parameters.Add(new() { Pgn = 12, Name = "Заданная частота ТН", BitLength = 16, StartByte = 2, a = 0.01, UnitT = UnitType_t.Frequency, Var = 17 });
            Parameters.Add(new() { Pgn = 12, Name = "Реализованная частота ТН", BitLength = 16, StartByte = 4, a = 0.01, UnitT = UnitType_t.Frequency, Var = 18 });
            Parameters.Add(new() { Pgn = 12, Name = "Мощность свечи", BitLength = 8, StartByte = 6, UnitT = UnitType_t.Percent, Var = 21 });
            Parameters.Add(new() { Pgn = 12, Name = "Состояние помпы", BitLength = 2, StartByte = 7, Meanings = DefMeaningsOnOff, Var = 46 });
            Parameters.Add(new() { Pgn = 12, Name = "Состояние реле печки кабины", BitLength = 2, StartByte = 7, StartBit = 2, Meanings = DefMeaningsOnOff, Var = 45 });
            Parameters.Add(new() { Pgn = 12, Name = "Состояние состояние канала сигнализации", BitLength = 2, StartByte = 7, StartBit = 4, Meanings = DefMeaningsOnOff, Var = 47 });

            Parameters.Add(new() { Pgn = 13, Name = "Температура ИП", BitLength = 16, StartByte = 0, UnitT = UnitType_t.Temp, Signed = true, Var = 6 });
            Parameters.Add(new() { Pgn = 13, Name = "Температура платы/процессора", BitLength = 8, StartByte = 2, b = -75, UnitT = UnitType_t.Temp, Var = 59 });
            Parameters.Add(new() { Pgn = 13, Name = "Температура жидкости", BitLength = 8, StartByte = 3, b = -75, UnitT = UnitType_t.Temp, Var = 40 });
            Parameters.Add(new() { Pgn = 13, Name = "Температура перегрева", BitLength = 8, StartByte = 4, b = -75, UnitT = UnitType_t.Temp, Var = 41 });

            Parameters.Add(new() { Pgn = 14, Name = "Минимальная температура пламени перед розжигом", BitLength = 16, StartByte = 0, UnitT = UnitType_t.Temp, Var = 36, Signed = true });
            Parameters.Add(new() { Pgn = 14, Name = "Граница срыва пламени", BitLength = 16, StartByte = 2, UnitT = UnitType_t.Temp, Var = 37, Signed = true });
            Parameters.Add(new() { Pgn = 14, Name = "Граница срыва пламени на прогреве", BitLength = 16, StartByte = 4, UnitT = UnitType_t.Temp, Signed = true });
            Parameters.Add(new() { Pgn = 14, Name = "Скорость изменения температуры ИП", BitLength = 16, StartByte = 6, UnitT = UnitType_t.Temp, Signed = true });

            Parameters.Add(new() { Pgn = 15, Name = "0 канал АЦП ", BitLength = 16, StartByte = 0, Var = 49 });
            Parameters.Add(new() { Pgn = 15, Name = "1 канал АЦП ", BitLength = 16, StartByte = 2, Var = 50 });
            Parameters.Add(new() { Pgn = 15, Name = "2 канал АЦП ", BitLength = 16, StartByte = 4, Var = 51 });
            Parameters.Add(new() { Pgn = 15, Name = "3 канал АЦП ", BitLength = 16, StartByte = 6, Var = 52 });

            Parameters.Add(new() { Pgn = 16, Name = "4 канал АЦП ", BitLength = 16, StartByte = 0, Var = 53 });
            Parameters.Add(new() { Pgn = 16, Name = "5 канал АЦП ", BitLength = 16, StartByte = 2, Var = 54 });
            Parameters.Add(new() { Pgn = 16, Name = "6 канал АЦП ", BitLength = 16, StartByte = 4, Var = 55 });
            Parameters.Add(new() { Pgn = 16, Name = "7 канал АЦП ", BitLength = 16, StartByte = 6, Var = 56 });

            Parameters.Add(new() { Pgn = 17, Name = "8 канал АЦП ", BitLength = 16, StartByte = 0, Var = 57 });
            Parameters.Add(new() { Pgn = 17, Name = "9 канал АЦП ", BitLength = 16, StartByte = 2, Var = 58 });
            Parameters.Add(new() { Pgn = 17, Name = "10 канал АЦП ", BitLength = 16, StartByte = 4 });
            Parameters.Add(new() { Pgn = 17, Name = "11 канал АЦП ", BitLength = 16, StartByte = 6 });

            Parameters.Add(new() { Pgn = 18, Name = "Вид изделия", BitLength = 8, StartByte = 0, CustomDecoder = i => $"{i[0]}.{i[1]}.{i[2]}.{i[3]}\r\n" });
            Parameters.Add(new() { Pgn = 18, Name = "Дата релиза", BitLength = 24, StartByte = 5, GetMeaning = v => $"{v >> 16}.{(v >> 8) & 0xF}.{v & 0xFF}\r\n" });

            // PGN 19 - PackNumber 1
            Parameters.Add(new() { Pgn = 19, Name = "Подогреватель", BitLength = 2, StartBit = 0, StartByte = 1, PackNumber = 1, Meanings = DefMeaningsOnOff });
            Parameters.Add(new() { Pgn = 19, Name = "Запрос включения помпы", BitLength = 2, StartBit = 2, StartByte = 1, PackNumber = 1, Meanings = DefMeaningsOnOff });
            Parameters.Add(new() { Pgn = 19, Name = "Кнопка воды (для котла)", BitLength = 2, StartBit = 4, StartByte = 1, PackNumber = 1, Meanings = DefMeaningsOnOff });
            Parameters.Add(new() { Pgn = 19, Name = "Быстрый нагрев воды", BitLength = 2, StartBit = 6, StartByte = 1, PackNumber = 1, Meanings = DefMeaningsOnOff });
            Parameters.Add(new() { Pgn = 19, Name = "Помпа подогревателя статус", BitLength = 2, StartByte = 7, PackNumber = 1, Meanings = DefMeaningsOnOff });
            Parameters.Add(new() { Pgn = 19, Name = "Температура бака", BitLength = 8, StartByte = 2, b = -75, UnitT = UnitType_t.Temp, PackNumber = 1 });
            Parameters.Add(new() { Pgn = 19, Name = "Атмосферное давление", BitLength = 8, StartByte = 3, UnitT = UnitType_t.Temp, PackNumber = 1 });
            Parameters.Add(new() { Pgn = 19, Name = "Сработал датчик бытовой воды", BitLength = 2, StartByte = 4, PackNumber = 1, Meanings = DefMeaningsYesNo, Var = 108 });
            Parameters.Add(new() { Pgn = 19, Name = "Кнопка воды(для HCU)", BitLength = 2, StartBit = 2, StartByte = 4, PackNumber = 1, Meanings = DefMeaningsOnOff });
            Parameters.Add(new() { Pgn = 19, Name = "Доступен тёплый пол", BitLength = 2, StartByte = 5, StartBit = 6, PackNumber = 1, Meanings = DefMeaningsOnOff });
            Parameters.Add(new() { Pgn = 19, Name = "Доступен предпусковой подогрев", BitLength = 2, StartByte = 5, StartBit = 6, PackNumber = 1, Meanings = DefMeaningsOnOff });
            Parameters.Add(new() { Pgn = 19, Name = "Доп помпа 1 статус", BitLength = 2, StartByte = 6, StartBit = 0, PackNumber = 1, Meanings = DefMeaningsOnOff });
            Parameters.Add(new() { Pgn = 19, Name = "Доп помпа 2 статус", BitLength = 2, StartByte = 6, StartBit = 2, PackNumber = 1, Meanings = DefMeaningsOnOff });
            Parameters.Add(new() { Pgn = 19, Name = "Доп помпа 3 статус", BitLength = 2, StartByte = 6, StartBit = 4, PackNumber = 1, Meanings = DefMeaningsOnOff });
            Parameters.Add(new() { Pgn = 19, Name = "Помпа 4 статус", BitLength = 2, StartByte = 6, StartBit = 6, PackNumber = 1, Meanings = DefMeaningsOnOff });
            Parameters.Add(new() { Pgn = 19, Name = "Помпа 1 статус", BitLength = 2, StartByte = 7, StartBit = 2, PackNumber = 1, Meanings = DefMeaningsOnOff });
            Parameters.Add(new() { Pgn = 19, Name = "Помпа 2 статус", BitLength = 2, StartByte = 7, StartBit = 4, PackNumber = 1, Meanings = DefMeaningsOnOff });
            Parameters.Add(new() { Pgn = 19, Name = "Помпа 3 статус", BitLength = 2, StartByte = 7, StartBit = 6, PackNumber = 1, Meanings = DefMeaningsOnOff });

            // PGN 19 - PackNumber 2
            Parameters.Add(new() { Pgn = 19, Name = "Уставка температуры жидкости подогревателя для перехода в ждущий.", BitLength = 8, StartByte = 1, b = -75, PackNumber = 2, UnitT = UnitType_t.Temp });
            Parameters.Add(new() { Pgn = 19, Name = "Уставка температуры жидкости подогревателя для выхода из ждущего.", BitLength = 8, StartByte = 2, b = -75, PackNumber = 2, UnitT = UnitType_t.Temp });
            Parameters.Add(new() { Pgn = 19, Name = "Уставка температуры жидкости подогревателя для выхода из ждущего при разборе воды.", BitLength = 8, StartByte = 3, b = -75, PackNumber = 2, UnitT = UnitType_t.Temp });
            Parameters.Add(new() { Pgn = 19, Name = "Уставка температуры бака для перехода в ждущий.", BitLength = 8, StartByte = 4, b = -75, PackNumber = 2, UnitT = UnitType_t.Temp });
            Parameters.Add(new() { Pgn = 19, Name = "Уставка температуры бака для выхода из ждущего.", BitLength = 8, StartByte = 5, b = -75, PackNumber = 2, UnitT = UnitType_t.Temp });
            Parameters.Add(new() { Pgn = 19, Name = "Уставка температуры бака для выхода из ждущего при разборе воды.", BitLength = 8, StartByte = 6, b = -75, PackNumber = 2, UnitT = UnitType_t.Temp });
            Parameters.Add(new() { Pgn = 19, Name = "t_power_limiter", BitLength = 4, StartByte = 7 });

            // PGN 19 - PackNumber 3
            Parameters.Add(new() { Pgn = 19, Name = "Уставка температуры для тёплого пола", BitLength = 8, StartByte = 1, b = -75, PackNumber = 3, UnitT = UnitType_t.Temp });
            Parameters.Add(new() { Pgn = 19, Name = "Гистерезис работы тёплого пола ", BitLength = 8, StartByte = 2, PackNumber = 3, UnitT = UnitType_t.Temp });
            Parameters.Add(new() { Pgn = 19, Name = "Уставка предпускового подогрева", BitLength = 8, StartByte = 3, b = -75, PackNumber = 3, UnitT = UnitType_t.Temp });
            Parameters.Add(new() { Pgn = 19, Name = "Ограничение работы по времени предпускового подогрева, мин", BitLength = 16, StartByte = 4, PackNumber = 3, UnitT = UnitType_t.Minute });
            Parameters.Add(new() { Pgn = 19, Name = "Ограничение работы системы,часы", BitLength = 8, StartByte = 6, PackNumber = 3, UnitT = UnitType_t.Hour });
            Parameters.Add(new() { Pgn = 19, Name = "Ограничение работы помп,минуты", BitLength = 8, StartByte = 7, PackNumber = 3, UnitT = UnitType_t.Minute });

            // PGN 19 - PackNumber 4
            Parameters.Add(new() { Pgn = 19, Name = "Подключенная зона 1", BitLength = 8, StartByte = 1, PackNumber = 4, Meanings = { { 0, "Не подключена" }, { 1, "Зависимые отопители" }, { 2, "Защита от замерзания" }, { 3, "Пассивное отопление" } } });
            Parameters.Add(new() { Pgn = 19, Name = "Подключенная зона 2", BitLength = 8, StartByte = 2, PackNumber = 4, Meanings = { { 0, "Не подключена" }, { 1, "Зависимые отопители" }, { 2, "Защита от замерзания" }, { 3, "Пассивное отопление" } } });
            Parameters.Add(new() { Pgn = 19, Name = "Подключенная зона 3", BitLength = 8, StartByte = 3, PackNumber = 4, Meanings = { { 0, "Не подключена" }, { 1, "Зависимые отопители" }, { 2, "Защита от замерзания" }, { 3, "Пассивное отопление" } } });
            Parameters.Add(new() { Pgn = 19, Name = "Подключенная зона 4", BitLength = 8, StartByte = 4, PackNumber = 4, Meanings = { { 0, "Не подключена" }, { 1, "Зависимые отопители" }, { 2, "Защита от замерзания" }, { 3, "Пассивное отопление" } } });
            Parameters.Add(new() { Pgn = 19, Name = "Подключенная зона 5", BitLength = 8, StartByte = 5, PackNumber = 4, Meanings = { { 0, "Не подключена" }, { 1, "Зависимые отопители" }, { 2, "Защита от замерзания" }, { 3, "Пассивное отопление" } } });

            // PGN 19 - PackNumber 5
            Parameters.Add(new() { Pgn = 19, Name = "Наработка ТЭНа,с", BitLength = 32, StartByte = 1, PackNumber = 5, UnitT = UnitType_t.Second });
            Parameters.Add(new() { Pgn = 19, Name = "Статус тёплого пола", BitLength = 2, StartByte = 5, PackNumber = 5, Meanings = DefMeaningsOnOff });
            Parameters.Add(new() { Pgn = 19, Name = "Статус предпускового подогрева", BitLength = 2, StartByte = 5, StartBit = 2, PackNumber = 5, Meanings = DefMeaningsOnOff });

            // PGN 19 - PackNumber 6
            Parameters.Add(new() { Pgn = 19, Name = "Текущая температура пола", BitLength = 8, StartByte = 1, b = -75, PackNumber = 6, UnitT = UnitType_t.Temp });
            Parameters.Add(new() { Pgn = 19, Name = "Текущая температура двигателя", BitLength = 8, StartByte = 2, b = -75, PackNumber = 6, UnitT = UnitType_t.Temp });
            Parameters.Add(new() { Pgn = 19, Name = "Подстройка датчика зоны 1", BitLength = 8, StartByte = 3, b = -75, PackNumber = 6, UnitT = UnitType_t.Temp });
            Parameters.Add(new() { Pgn = 19, Name = "Подстройка датчика зоны 2", BitLength = 8, StartByte = 4, b = -75, PackNumber = 6, UnitT = UnitType_t.Temp });
            Parameters.Add(new() { Pgn = 19, Name = "Подстройка датчика зоны 3", BitLength = 8, StartByte = 5, b = -75, PackNumber = 6, UnitT = UnitType_t.Temp });
            Parameters.Add(new() { Pgn = 19, Name = "Подстройка датчика зоны 4", BitLength = 8, StartByte = 6, b = -75, PackNumber = 6, UnitT = UnitType_t.Temp });
            Parameters.Add(new() { Pgn = 19, Name = "Подстройка датчика зоны 5", BitLength = 8, StartByte = 7, b = -75, PackNumber = 6, UnitT = UnitType_t.Temp });

            Parameters.Add(new() { Pgn = 20, Name = "Код неисправности", BitLength = 8, StartByte = 0 });
            Parameters.Add(new() { Pgn = 20, Name = "Количество морганий", BitLength = 8, StartByte = 1 });
            Parameters.Add(new() { Pgn = 20, Name = "Байт неисправностей 1", BitLength = 8, StartByte = 2 });
            Parameters.Add(new() { Pgn = 20, Name = "Байт неисправностей 2", BitLength = 8, StartByte = 3 });
            Parameters.Add(new() { Pgn = 20, Name = "Байт неисправностей 3", BitLength = 8, StartByte = 4 });
            Parameters.Add(new() { Pgn = 20, Name = "Байт неисправностей 4", BitLength = 8, StartByte = 5 });
            Parameters.Add(new() { Pgn = 20, Name = "Байт неисправностей 5", BitLength = 8, StartByte = 6 });
            Parameters.Add(new() { Pgn = 20, Name = "Байт неисправностей 6", BitLength = 8, StartByte = 7 });

            Parameters.Add(new() { Pgn = 21, Name = "Опорное напряжение процессора", BitLength = 8, StartByte = 0, UnitT = UnitType_t.Volt, a = 0.1 });
            Parameters.Add(new() { Pgn = 21, Name = "Температура процессора", BitLength = 8, StartByte = 1, UnitT = UnitType_t.Temp, b = -75 });
            Parameters.Add(new() { Pgn = 21, Name = "Температура бака", BitLength = 8, StartByte = 2, UnitT = UnitType_t.Temp, b = -75, Var = 106 });
            Parameters.Add(new() { Pgn = 21, Name = "Температура теплообменника", BitLength = 8, StartByte = 3, UnitT = UnitType_t.Temp, b = -75 });
            Parameters.Add(new() { Pgn = 21, Name = "Температура наружного воздуха", BitLength = 8, StartByte = 4, UnitT = UnitType_t.Temp, b = -75, Var = 107 });
            Parameters.Add(new() { Pgn = 21, Name = "Иконка подогревателя", BitLength = 8, StartByte = 5, Meanings = { { 0, "Ожидание" }, { 1, "Продувка" }, { 2, "Розжиг" }, { 3, "Работа на мощности" } } });
            Parameters.Add(new() { Pgn = 21, Name = "Уровень жидкости в баке", BitLength = 8, StartByte = 6, Var = 105 });
            Parameters.Add(new() { Pgn = 21, Name = "Режим хранения", BitLength = 2, StartByte = 7, Meanings = DefMeaningsYesNo });
            Parameters.Add(new() { Pgn = 21, Name = "ТЭН активен", StartBit = 2, BitLength = 2, StartByte = 7, Meanings = DefMeaningsYesNo });
            Parameters.Add(new() { Pgn = 21, Name = "ТЭН отключен", StartBit = 4, BitLength = 2, StartByte = 7, Meanings = DefMeaningsYesNo });

            Parameters.Add(new() { Pgn = 22, Name = "Зона 1", BitLength = 2, StartByte = 0, StartBit = 0, Meanings = DefMeaningsOnOff, Var = 65 });
            Parameters.Add(new() { Pgn = 22, Name = "Зона 2", BitLength = 2, StartByte = 0, StartBit = 2, Meanings = DefMeaningsOnOff, Var = 66 });
            Parameters.Add(new() { Pgn = 22, Name = "Зона 3", BitLength = 2, StartByte = 0, StartBit = 4, Meanings = DefMeaningsOnOff, Var = 67 });
            Parameters.Add(new() { Pgn = 22, Name = "Зона 4", BitLength = 2, StartByte = 0, StartBit = 6, Meanings = DefMeaningsOnOff, Var = 68 });
            Parameters.Add(new() { Pgn = 22, Name = "Зона 5", BitLength = 2, StartByte = 1, StartBit = 0, Meanings = DefMeaningsOnOff, Var = 69 });
            Parameters.Add(new() { Pgn = 22, Name = "Температура зоны 1", BitLength = 8, StartByte = 2, UnitT = UnitType_t.Temp, b = -75, Var = 70 });
            Parameters.Add(new() { Pgn = 22, Name = "Температура зоны 2", BitLength = 8, StartByte = 3, UnitT = UnitType_t.Temp, b = -75, Var = 71 });
            Parameters.Add(new() { Pgn = 22, Name = "Температура зоны 3", BitLength = 8, StartByte = 4, UnitT = UnitType_t.Temp, b = -75, Var = 72 });
            Parameters.Add(new() { Pgn = 22, Name = "Температура зоны 4", BitLength = 8, StartByte = 5, UnitT = UnitType_t.Temp, b = -75, Var = 73 });
            Parameters.Add(new() { Pgn = 22, Name = "Температура зоны 5", BitLength = 8, StartByte = 6, UnitT = UnitType_t.Temp, b = -75, Var = 74 });
            Parameters.Add(new() { Pgn = 22, Name = "Кнопка Подогреватель", BitLength = 2, StartByte = 7, StartBit = 0, Meanings = DefMeaningsOnOff, Var = 119 });
            Parameters.Add(new() { Pgn = 22, Name = "Кнопка ТЭН", BitLength = 2, StartByte = 7, StartBit = 2, Meanings = DefMeaningsOnOff, Var = 120 });
            Parameters.Add(new() { Pgn = 22, Name = "Кнопка Тёплый пол", BitLength = 2, StartByte = 7, StartBit = 4, Meanings = DefMeaningsOnOff });
            Parameters.Add(new() { Pgn = 22, Name = "Кнопка Предпусковой подогрев", BitLength = 2, StartByte = 7, StartBit = 5, Meanings = DefMeaningsOnOff });

            Parameters.Add(new() { Pgn = 23, Name = "Зад. ШИМ вентилятора зоны 1", BitLength = 8, StartByte = 0, UnitT = UnitType_t.Percent, Var = 100 });
            Parameters.Add(new() { Pgn = 23, Name = "Зад. ШИМ вентилятора зоны 2", BitLength = 8, StartByte = 1, UnitT = UnitType_t.Percent, Var = 101 });
            Parameters.Add(new() { Pgn = 23, Name = "Зад. ШИМ вентилятора зоны 3", BitLength = 8, StartByte = 2, UnitT = UnitType_t.Percent, Var = 102 });
            Parameters.Add(new() { Pgn = 23, Name = "Зад. ШИМ вентилятора зоны 4", BitLength = 8, StartByte = 3, UnitT = UnitType_t.Percent, Var = 103 });
            Parameters.Add(new() { Pgn = 23, Name = "Зад. ШИМ вентилятора зоны 5", BitLength = 8, StartByte = 4, UnitT = UnitType_t.Percent, Var = 104 });

            Parameters.Add(new() { Pgn = 24, Name = "Ступень скорости вентилятора зоны 1", BitLength = 4, StartByte = 0, });
            Parameters.Add(new() { Pgn = 24, Name = "Ступень скорости вентилятора зоны 2", BitLength = 4, StartByte = 0, StartBit = 4 });
            Parameters.Add(new() { Pgn = 24, Name = "Ступень скорости вентилятора зоны 3", BitLength = 4, StartByte = 1, });
            Parameters.Add(new() { Pgn = 24, Name = "Ступень скорости вентилятора зоны 4", BitLength = 4, StartByte = 1, StartBit = 4 });
            Parameters.Add(new() { Pgn = 24, Name = "Ступень скорости вентилятора зоны 5", BitLength = 4, StartByte = 2, });
            Parameters.Add(new() { Pgn = 24, Name = "Тек. ШИМ вентилятора зоны 1", BitLength = 8, StartByte = 3, UnitT = UnitType_t.Percent, Var = 95 });
            Parameters.Add(new() { Pgn = 24, Name = "Тек. ШИМ вентилятора зоны 2", BitLength = 8, StartByte = 4, UnitT = UnitType_t.Percent, Var = 96 });
            Parameters.Add(new() { Pgn = 24, Name = "Тек. ШИМ вентилятора зоны 3", BitLength = 8, StartByte = 5, UnitT = UnitType_t.Percent, Var = 97 });
            Parameters.Add(new() { Pgn = 24, Name = "Тек. ШИМ вентилятора зоны 4", BitLength = 8, StartByte = 6, UnitT = UnitType_t.Percent, Var = 98 });
            Parameters.Add(new() { Pgn = 24, Name = "Тек. ШИМ вентилятора зоны 5", BitLength = 8, StartByte = 7, UnitT = UnitType_t.Percent, Var = 99 });

            Parameters.Add(new() { Pgn = 25, Name = "Дневная уставка зоны 1", BitLength = 8, StartByte = 0, UnitT = UnitType_t.Temp, b = -75, Var = 75 });
            Parameters.Add(new() { Pgn = 25, Name = "Дневная уставка зоны 2", BitLength = 8, StartByte = 1, UnitT = UnitType_t.Temp, b = -75, Var = 76 });
            Parameters.Add(new() { Pgn = 25, Name = "Дневная уставка зоны 3", BitLength = 8, StartByte = 2, UnitT = UnitType_t.Temp, b = -75, Var = 77 });
            Parameters.Add(new() { Pgn = 25, Name = "Дневная уставка зоны 4", BitLength = 8, StartByte = 3, UnitT = UnitType_t.Temp, b = -75, Var = 78 });
            Parameters.Add(new() { Pgn = 25, Name = "Дневная уставка зоны 5", BitLength = 8, StartByte = 4, UnitT = UnitType_t.Temp, b = -75, Var = 79 });

            Parameters.Add(new() { Pgn = 26, Name = "Ночная уставка зоны 1", BitLength = 8, StartByte = 0, UnitT = UnitType_t.Temp, b = -75, Var = 80 });
            Parameters.Add(new() { Pgn = 26, Name = "Ночная уставка зоны 2", BitLength = 8, StartByte = 1, UnitT = UnitType_t.Temp, b = -75, Var = 81 });
            Parameters.Add(new() { Pgn = 26, Name = "Ночная уставка зоны 3", BitLength = 8, StartByte = 2, UnitT = UnitType_t.Temp, b = -75, Var = 82 });
            Parameters.Add(new() { Pgn = 26, Name = "Ночная уставка зоны 4", BitLength = 8, StartByte = 3, UnitT = UnitType_t.Temp, b = -75, Var = 83 });
            Parameters.Add(new() { Pgn = 26, Name = "Ночная уставка зоны 5", BitLength = 8, StartByte = 4, UnitT = UnitType_t.Temp, b = -75, Var = 84 });

            Parameters.Add(new() { Pgn = 27, Name = "Ручная уставка ШИМ зоны 1", BitLength = 8, StartByte = 0, UnitT = UnitType_t.Percent, Var = 90 });
            Parameters.Add(new() { Pgn = 27, Name = "Ручная уставка ШИМ зоны 2", BitLength = 8, StartByte = 1, UnitT = UnitType_t.Percent, Var = 91 });
            Parameters.Add(new() { Pgn = 27, Name = "Ручная уставка ШИМ зоны 3", BitLength = 8, StartByte = 2, UnitT = UnitType_t.Percent, Var = 92 });
            Parameters.Add(new() { Pgn = 27, Name = "Ручная уставка ШИМ зоны 4", BitLength = 8, StartByte = 3, UnitT = UnitType_t.Percent, Var = 93 });
            Parameters.Add(new() { Pgn = 27, Name = "Ручная уставка ШИМ зоны 5", BitLength = 8, StartByte = 4, UnitT = UnitType_t.Percent, Var = 94 });
            Parameters.Add(new() { Pgn = 27, Name = "Зона 1 Ручной режим", BitLength = 2, StartByte = 5, StartBit = 0, Meanings = DefMeaningsOnOff, Var = 85 });
            Parameters.Add(new() { Pgn = 27, Name = "Зона 2 Ручной режим", BitLength = 2, StartByte = 5, StartBit = 2, Meanings = DefMeaningsOnOff, Var = 86 });
            Parameters.Add(new() { Pgn = 27, Name = "Зона 3 Ручной режим", BitLength = 2, StartByte = 5, StartBit = 4, Meanings = DefMeaningsOnOff, Var = 87 });
            Parameters.Add(new() { Pgn = 27, Name = "Зона 4 Ручной режим", BitLength = 2, StartByte = 5, StartBit = 6, Meanings = DefMeaningsOnOff, Var = 88 });
            Parameters.Add(new() { Pgn = 27, Name = "Зона 5 Ручной режим", BitLength = 2, StartByte = 6, StartBit = 0, Meanings = DefMeaningsOnOff, Var = 89 });

            Parameters.Add(new() { Pgn = 28, Name = "Общее время на всех режимах", BitLength = 32, StartByte = 0, UnitT = UnitType_t.Second });
            Parameters.Add(new() { Pgn = 28, Name = "Общее время работы (кроме ожидания команды)", BitLength = 32, StartByte = 4, UnitT = UnitType_t.Second });

            Parameters.Add(new() { Pgn = 29, Name = "Атмосферное давление", BitLength = 8, StartByte = 1, UnitT = UnitType_t.Pressure, PackNumber = 1 });
            Parameters.Add(new() { Pgn = 29, Name = "Среднее максимальное значение давления", BitLength = 24, StartByte = 2, UnitT = UnitType_t.Pressure, a = 0.001, PackNumber = 1 });
            Parameters.Add(new() { Pgn = 29, Name = "Среднее минимальное значение давления", BitLength = 24, StartByte = 4, UnitT = UnitType_t.Pressure, a = 0.001, PackNumber = 1 });
            Parameters.Add(new() { Pgn = 29, Name = "Разница между max и min  значениями", BitLength = 16, StartByte = 1, a = 0.01, UnitT = UnitType_t.Pressure, PackNumber = 2 });
            Parameters.Add(new() { Pgn = 29, Name = "Флаг появления пламени по пульсации давления", BitLength = 2, StartByte = 3, Meanings = DefMeaningsYesNo, PackNumber = 2 });
            Parameters.Add(new() { Pgn = 29, Name = "Использовать датчик давления для определения горения", BitLength = 2, StartByte = 3, Meanings = DefMeaningsYesNo, PackNumber = 2, StartBit = 2 });
            Parameters.Add(new() { Pgn = 29, Name = "Использовать датчик давления для топливной коррекции", BitLength = 2, StartByte = 3, Meanings = DefMeaningsYesNo, PackNumber = 2, StartBit = 4 });
            Parameters.Add(new() { Pgn = 29, Name = "Атмосферное давление", BitLength = 24, StartByte = 4, UnitT = UnitType_t.Pressure, a = 0.001, PackNumber = 2, Var = 60 });

            Parameters.Add(new() { Pgn = 31, Name = "Время работы", BitLength = 32, StartByte = 0, UnitT = UnitType_t.Second, Var = 3 });
            Parameters.Add(new() { Pgn = 31, Name = "Время работы на режиме", BitLength = 32, StartByte = 4, UnitT = UnitType_t.Second, Var = 4 });

            Parameters.Add(new() { Pgn = 32, Name = "t_work_time_minutes", BitLength = 16, StartByte = 0, UnitT = UnitType_t.Minute });
            Parameters.Add(new() { Pgn = 32, Name = "t_heater_mode", BitLength = 4, StartByte = 2, Meanings = { { 0, "t_regular" }, { 1, "t_eco" }, { 2, "t_additional_heater" }, { 3, "t_heating" }, { 4, "t_heating_systems" } } });

            Parameters.Add(new() { Pgn = 40, Name = "t_year", BitLength = 8, StartByte = 0, UnitT = UnitType_t.Year, Var = 118 });
            Parameters.Add(new() { Pgn = 40, Name = "t_month", BitLength = 8, StartByte = 1, UnitT = UnitType_t.Month, Var = 117 });
            Parameters.Add(new() { Pgn = 40, Name = "t_day", BitLength = 8, StartByte = 2, UnitT = UnitType_t.Day, Var = 116 });
            Parameters.Add(new() { Pgn = 40, Name = "t_hour", BitLength = 8, StartByte = 3, UnitT = UnitType_t.Hour, Var = 115 });
            Parameters.Add(new() { Pgn = 40, Name = "t_minute", BitLength = 8, StartByte = 4, UnitT = UnitType_t.Minute, Var = 114 });
            Parameters.Add(new() { Pgn = 40, Name = "t_second", BitLength = 8, StartByte = 5, UnitT = UnitType_t.Second, Var = 113 });

            Parameters.Add(new() { Pgn = 41, Name = "t_day_start_hour", BitLength = 8, StartByte = 0, UnitT = UnitType_t.Hour });
            Parameters.Add(new() { Pgn = 41, Name = "t_day_start_minute", BitLength = 8, StartByte = 1, UnitT = UnitType_t.Minute });
            Parameters.Add(new() { Pgn = 41, Name = "t_night_start_hour", BitLength = 8, StartByte = 2, UnitT = UnitType_t.Hour });
            Parameters.Add(new() { Pgn = 41, Name = "t_hight_start_minute", BitLength = 8, StartByte = 3, UnitT = UnitType_t.Minute });
            Parameters.Add(new() { Pgn = 41, Name = "t_daytime_backlight", BitLength = 8, StartByte = 4, UnitT = UnitType_t.Percent });
            Parameters.Add(new() { Pgn = 41, Name = "t_nighttime_backlight", BitLength = 8, StartByte = 5, UnitT = UnitType_t.Percent });
            Parameters.Add(new() { Pgn = 41, Name = "t_display_sleep_time", BitLength = 16, StartByte = 6, UnitT = UnitType_t.Second });

            Parameters.Add(new() { Pgn = 42, Name = "t_pump_heater_btn", BitLength = 8, StartByte = 0, Meanings = DefMeaningsOnOff });
            Parameters.Add(new() { Pgn = 42, Name = "t_pump_1_btn", BitLength = 8, StartByte = 1, Meanings = DefMeaningsOnOff });
            Parameters.Add(new() { Pgn = 42, Name = "t_pump_2_btn", BitLength = 8, StartByte = 2, Meanings = DefMeaningsOnOff });
            Parameters.Add(new() { Pgn = 42, Name = "t_pump_3_btn", BitLength = 8, StartByte = 3, Meanings = DefMeaningsOnOff });
            Parameters.Add(new() { Pgn = 42, Name = "t_pump_aux_1_btn", BitLength = 8, StartByte = 4, Meanings = DefMeaningsOnOff });
            Parameters.Add(new() { Pgn = 42, Name = "t_pump_aux_2_btn", BitLength = 8, StartByte = 5, Meanings = DefMeaningsOnOff });
            Parameters.Add(new() { Pgn = 42, Name = "t_pump_aux_3_btn", BitLength = 8, StartByte = 6, Meanings = DefMeaningsOnOff });

            Parameters.Add(new() { Pgn = 43, Name = "t_channel1_pwm_value", BitLength = 16, StartByte = 0 });
            Parameters.Add(new() { Pgn = 43, Name = "t_channel2_pwm_value", BitLength = 16, StartByte = 2 });
            Parameters.Add(new() { Pgn = 43, Name = "t_channel3_pwm_value", BitLength = 16, StartByte = 4 });
            Parameters.Add(new() { Pgn = 43, Name = "t_channel4_pwm_value", BitLength = 16, StartByte = 6 });

            Parameters.Add(new() { Pgn = 44, Name = "t_channel1_pwm_value", BitLength = 16, StartByte = 0 });
            Parameters.Add(new() { Pgn = 44, Name = "t_channel2_pwm_value", BitLength = 16, StartByte = 2 });
            Parameters.Add(new() { Pgn = 44, Name = "t_channel3_pwm_value", BitLength = 16, StartByte = 4 });
            Parameters.Add(new() { Pgn = 44, Name = "t_channel4_pwm_value", BitLength = 16, StartByte = 6 });

            Parameters.Add(new() { Pgn = 45, Name = "t_channel1_temperature", BitLength = 16, StartByte = 0, a = 0.1, UnitT = UnitType_t.Temp, Signed = true });
            Parameters.Add(new() { Pgn = 45, Name = "t_channel2_temperature", BitLength = 16, StartByte = 2, a = 0.1, UnitT = UnitType_t.Temp, Signed = true });
            Parameters.Add(new() { Pgn = 45, Name = "t_channel3_temperature", BitLength = 16, StartByte = 4, a = 0.1, UnitT = UnitType_t.Temp, Signed = true });
            Parameters.Add(new() { Pgn = 45, Name = "t_channel4_temperature", BitLength = 16, StartByte = 6, a = 0.1, UnitT = UnitType_t.Temp, Signed = true });

            Parameters.Add(new() { Pgn = 46, Name = "t_error_code1", BitLength = 8, StartByte = 0, GetMeaning = (x) => GetString($"e_{x}") });
            Parameters.Add(new() { Pgn = 46, Name = "t_error_code2", BitLength = 8, StartByte = 1, GetMeaning = (x) => GetString($"e_{x}") });
            Parameters.Add(new() { Pgn = 46, Name = "t_error_code3", BitLength = 8, StartByte = 2, GetMeaning = (x) => GetString($"e_{x}") });
            Parameters.Add(new() { Pgn = 46, Name = "t_error_code4", BitLength = 8, StartByte = 3, GetMeaning = (x) => GetString($"e_{x}") });
            Parameters.Add(new() { Pgn = 46, Name = "t_error_code5", BitLength = 8, StartByte = 4, GetMeaning = (x) => GetString($"e_{x}") });
            Parameters.Add(new() { Pgn = 46, Name = "t_error_code6", BitLength = 8, StartByte = 5, GetMeaning = (x) => GetString($"e_{x}") });
            Parameters.Add(new() { Pgn = 46, Name = "t_error_code7", BitLength = 8, StartByte = 6, GetMeaning = (x) => GetString($"e_{x}") });
            Parameters.Add(new() { Pgn = 46, Name = "t_error_code8", BitLength = 8, StartByte = 7, GetMeaning = (x) => GetString($"e_{x}") });

            Parameters.Add(new() { Pgn = 47, Name = "t_fuel_pump_overriden", BitLength = 2, StartByte = 0, Meanings = DefMeaningsYesNo });
            Parameters.Add(new() { Pgn = 47, Name = "t_relay_overriden", BitLength = 2, StartByte = 0, StartBit = 2, Meanings = DefMeaningsYesNo });
            Parameters.Add(new() { Pgn = 47, Name = "t_glow_plug_overriden", BitLength = 2, StartByte = 0, StartBit = 4, Meanings = DefMeaningsYesNo });
            Parameters.Add(new() { Pgn = 47, Name = "t_pump_overriden", BitLength = 2, StartByte = 0, StartBit = 6, Meanings = DefMeaningsYesNo });
            Parameters.Add(new() { Pgn = 47, Name = "t_blower_overriden", BitLength = 2, StartByte = 1, StartBit = 0, Meanings = DefMeaningsYesNo });
            Parameters.Add(new() { Pgn = 47, Name = "t_pump_state", BitLength = 2, StartByte = 2, StartBit = 0, Meanings = DefMeaningsOnOff });
            Parameters.Add(new() { Pgn = 47, Name = "t_relay_state", BitLength = 2, StartByte = 2, StartBit = 2, Meanings = DefMeaningsOnOff });
            Parameters.Add(new() { Pgn = 47, Name = "t_overriden_blower_revs", BitLength = 8, StartByte = 3, UnitT = UnitType_t.Rps });
            Parameters.Add(new() { Pgn = 47, Name = "t_overriden_glow_plug_power", BitLength = 8, StartByte = 4, UnitT = UnitType_t.Percent });
            Parameters.Add(new() { Pgn = 47, Name = "t_overriden_fuel_pump_frequency", BitLength = 16, StartByte = 5, a = 0.01, UnitT = UnitType_t.Frequency });

            Parameters.Add(new() { Pgn = 49, Name = "t_load_channel1", BitLength = 2, StartByte = 0, StartBit = 0, Meanings = { { 0, "t_off" }, { 1, "t_toggle" }, { 2, "t_pwm" } } });
            Parameters.Add(new() { Pgn = 49, Name = "t_load_channel2", BitLength = 2, StartByte = 0, StartBit = 2, Meanings = { { 0, "t_off" }, { 1, "t_toggle" }, { 2, "t_pwm" } } });
            Parameters.Add(new() { Pgn = 49, Name = "t_load_channel3", BitLength = 2, StartByte = 0, StartBit = 4, Meanings = { { 0, "t_off" }, { 1, "t_toggle" }, { 2, "t_pwm" } } });

            Parameters.Add(new() { Pgn = 50, Name = "t_compressor_control_type", BitLength = 8, StartByte = 1, Meanings = { { 0, "t_pwm" }, { 1, "t_revolutions" }, { 2, "t_power" } }, PackNumber = 1 });
            Parameters.Add(new() { Pgn = 50, Name = "t_compressor_rev_set", BitLength = 8, StartByte = 2, UnitT = UnitType_t.Rps, Var = 134, PackNumber = 1 });
            Parameters.Add(new() { Pgn = 50, Name = "t_compressor_rev_measured", BitLength = 8, StartByte = 3, UnitT = UnitType_t.Rps, Var = 135, PackNumber = 1 });
            Parameters.Add(new() { Pgn = 50, Name = "t_compressor_pwm_set", BitLength = 16, a = 0.01, StartByte = 4, UnitT = UnitType_t.Percent, Var = 146, PackNumber = 1 });
            Parameters.Add(new() { Pgn = 50, Name = "t_compressor_power_set", BitLength = 16, StartByte = 6, UnitT = UnitType_t.Power, PackNumber = 1 });
            Parameters.Add(new() { Pgn = 50, Name = "t_compressor_current", BitLength = 16, StartByte = 1, UnitT = UnitType_t.Current, a = 0.01, Var = 138, PackNumber = 2 });
            Parameters.Add(new() { Pgn = 50, Name = "t_condensor_current", BitLength = 16, StartByte = 3, UnitT = UnitType_t.Current, a = 0.01, Var = 139, PackNumber = 2 });
            Parameters.Add(new() { Pgn = 50, Name = "t_mcu_temp", BitLength = 8, StartByte = 5, UnitT = UnitType_t.Temp, b = -75, Var = 59, PackNumber = 2 });
            Parameters.Add(new() { Pgn = 50, Name = "t_pcb_temp", BitLength = 8, StartByte = 6, UnitT = UnitType_t.Temp, b = -75, Var = 145, PackNumber = 2 });
            Parameters.Add(new() { Pgn = 50, Name = "t_high_pressure", BitLength = 16, StartByte = 1, UnitT = UnitType_t.Pressure, PackNumber = 3, a = 0.01, Var = 141 });
            Parameters.Add(new() { Pgn = 50, Name = "t_low_pressure", BitLength = 16, StartByte = 3, UnitT = UnitType_t.Pressure, PackNumber = 3, a = 0.01, Var = 142 });
            Parameters.Add(new() { Pgn = 50, Name = "t_ac_press_sensor", BitLength = 8, StartByte = 5, Meanings = DefMeaningsAllow, PackNumber = 3 });
            Parameters.Add(new() { Pgn = 50, Name = "t_condensor_control_type", BitLength = 8, StartByte = 1, Meanings = { { 0, "t_pwm" }, { 1, "t_revolutions" }, { 2, "t_power" } }, PackNumber = 4 });
            Parameters.Add(new() { Pgn = 50, Name = "t_condensor_rev_set", BitLength = 8, StartByte = 2, UnitT = UnitType_t.Rps, PackNumber = 4 });
            Parameters.Add(new() { Pgn = 50, Name = "t_condensor_rev_measured", BitLength = 8, StartByte = 3, UnitT = UnitType_t.Rps, PackNumber = 4 });
            Parameters.Add(new() { Pgn = 50, Name = "t_condensor_pwm_set", BitLength = 16, a = 0.01, StartByte = 4, UnitT = UnitType_t.Percent, Var = 136, PackNumber = 4 });
            Parameters.Add(new() { Pgn = 50, Name = "t_condensor_power_set", BitLength = 16, a = 0.01, StartByte = 6, UnitT = UnitType_t.Power, PackNumber = 4 });

            Parameters.Add(new() { Pgn = 51, Name = "t_ac_mode", BitLength = 8, StartByte = 1, Meanings = { { 0, "t_off" }, { 1, "t_cool" }, { 2, "t_dry" }, { 3, "t_eco" }, { 4, "t_night" }, { 5, "t_power" }, { 6, "t_manual" } }, PackNumber = 1 });
            Parameters.Add(new() { Pgn = 51, Name = "t_ac_temp_setpoint", BitLength = 8, StartByte = 2, b = -75, UnitT = UnitType_t.Temp, Var = 143, PackNumber = 1 });
            Parameters.Add(new() { Pgn = 51, Name = "t_ac_fan_mode", BitLength = 8, StartByte = 3, Meanings = { { 0, "t_auto" }, { 1, "t_1st_speed" }, { 2, "t_2nd_speed" }, { 3, "t_3rd_speed" }, { 4, "t_4th_speed" }, { 5, "t_5th_speed" } }, PackNumber = 1, Var = 144 });
            Parameters.Add(new() { Pgn = 51, Name = "t_ac_panel_current", BitLength = 16, StartByte = 4, PackNumber = 1, Var = 140, a = 0.01, UnitT = UnitType_t.Current });
            Parameters.Add(new() { Pgn = 51, Name = "t_evap_control_type", BitLength = 8, StartByte = 1, Meanings = { { 0, "t_pwm" }, { 1, "t_revolutions" }, { 2, "t_power" } }, PackNumber = 2 });
            Parameters.Add(new() { Pgn = 51, Name = "t_evap_rev_set", BitLength = 8, StartByte = 2, UnitT = UnitType_t.Rps, PackNumber = 2 });
            Parameters.Add(new() { Pgn = 51, Name = "t_evap_rev_measured", BitLength = 8, StartByte = 3, UnitT = UnitType_t.Rps, PackNumber = 2 });
            Parameters.Add(new() { Pgn = 51, Name = "t_evap_pwm_set", BitLength = 16, a = 0.01, StartByte = 4, UnitT = UnitType_t.Percent, Var = 137, PackNumber = 2 });
            Parameters.Add(new() { Pgn = 51, Name = "t_evap_power_set", BitLength = 16, a = 0.01, StartByte = 6, UnitT = UnitType_t.Power, PackNumber = 2 });
            Parameters.Add(new() { Pgn = 51, Name = "t_ac_cabin_air_t", BitLength = 16, StartByte = 1, UnitT = UnitType_t.Temp, PackNumber = 3, Var = 149, a = 0.01, Signed = true });
            Parameters.Add(new() { Pgn = 51, Name = "t_ac_evap_t", BitLength = 16, StartByte = 3, UnitT = UnitType_t.Temp, PackNumber = 3, Var = 148, a = 0.01, Signed = true });
            Parameters.Add(new() { Pgn = 51, Name = "t_ac_aux1_t", BitLength = 16, StartByte = 5, UnitT = UnitType_t.Temp, PackNumber = 3, Var = 150, a = 0.01, Signed = true });

            Parameters.Add(new() { Pgn = 52, Name = "t_ac_man_evap_pwm_set", BitLength = 16, StartByte = 1, UnitT = UnitType_t.Percent, a = 0.01 });
            Parameters.Add(new() { Pgn = 52, Name = "t_ac_man_cond_pwm_set", BitLength = 16, StartByte = 3, UnitT = UnitType_t.Percent, a = 0.01 });
            Parameters.Add(new() { Pgn = 52, Name = "t_ac_man_comp_pwm_set", BitLength = 16, StartByte = 5, UnitT = UnitType_t.Percent, a = 0.01 });

            Parameters.Add(new() { Pgn = 55, Name = "t_tank1_level", BitLength = 8, StartByte = 0, UnitT = UnitType_t.Percent });
            Parameters.Add(new() { Pgn = 55, Name = "t_tank2_level", BitLength = 8, StartByte = 1, UnitT = UnitType_t.Percent });
            Parameters.Add(new() { Pgn = 55, Name = "t_tank3_level", BitLength = 8, StartByte = 2, UnitT = UnitType_t.Percent });
            Parameters.Add(new() { Pgn = 55, Name = "t_tank4_level", BitLength = 8, StartByte = 3, UnitT = UnitType_t.Percent });
            Parameters.Add(new() { Pgn = 55, Name = "t_tank1_content", BitLength = 4, StartByte = 4, Meanings = { { 0, "t_off" }, { 1, "t_white_tank" }, { 2, "t_grey_tank" }, { 3, "t_black_tank" }, { 4, "t_fuel_tank" } } });
            Parameters.Add(new() { Pgn = 55, Name = "t_tank1_resistance", BitLength = 4, StartByte = 4, StartBit = 4, Meanings = { { 0, "t_0-190_ohm" }, { 1, "t_240-33_ohm" }, { 2, "t_short_full" }, { 3, "t_open_full" } } });
            Parameters.Add(new() { Pgn = 55, Name = "t_tank2_content", BitLength = 4, StartByte = 5, Meanings = { { 0, "t_off" }, { 1, "t_white_tank" }, { 2, "t_grey_tank" }, { 3, "t_black_tank" }, { 4, "t_fuel_tank" } } });
            Parameters.Add(new() { Pgn = 55, Name = "t_tank2_resistance", BitLength = 4, StartByte = 5, StartBit = 4, Meanings = { { 0, "t_0-190_ohm" }, { 1, "t_240-33_ohm" }, { 2, "t_short_full" }, { 3, "t_open_full" } } });
            Parameters.Add(new() { Pgn = 55, Name = "t_tank3_content", BitLength = 4, StartByte = 6, Meanings = { { 0, "t_off" }, { 1, "t_white_tank" }, { 2, "t_grey_tank" }, { 3, "t_black_tank" }, { 4, "t_fuel_tank" } } });
            Parameters.Add(new() { Pgn = 55, Name = "t_tank3_resistance", BitLength = 4, StartByte = 6, StartBit = 4, Meanings = { { 0, "t_0-190_ohm" }, { 1, "t_240-33_ohm" }, { 2, "t_short_full" }, { 3, "t_open_full" } } });
            Parameters.Add(new() { Pgn = 55, Name = "t_tank4_content", BitLength = 4, StartByte = 7, Meanings = { { 0, "t_off" }, { 1, "t_white_tank" }, { 2, "t_grey_tank" }, { 3, "t_black_tank" }, { 4, "t_fuel_tank" } } });
            Parameters.Add(new() { Pgn = 55, Name = "t_tank4_resistance", BitLength = 4, StartByte = 7, StartBit = 4, Meanings = { { 0, "t_0-190_ohm" }, { 1, "t_240-33_ohm" }, { 2, "t_short_full" }, { 3, "t_open_full" } } });

            Parameters.Add(new() { Pgn = 57, Name = "t_power_limiter", BitLength = 4, StartByte = 0 });

            Parameters.Add(new() { Pgn = 99, Name = "t_channel_pwm1", BitLength = 8, StartByte = 1, UnitT = UnitType_t.Percent });
            Parameters.Add(new() { Pgn = 99, Name = "t_channel_pwm2", BitLength = 8, StartByte = 2, UnitT = UnitType_t.Percent });
            Parameters.Add(new() { Pgn = 99, Name = "t_channel_pwm3", BitLength = 8, StartByte = 3, UnitT = UnitType_t.Percent });
            Parameters.Add(new() { Pgn = 99, Name = "t_temperature_1", BitLength = 16, Signed = true, StartByte = 1, UnitT = UnitType_t.Temp, PackNumber = 1, Var = 129 });
            Parameters.Add(new() { Pgn = 99, Name = "t_temperature_2", BitLength = 16, Signed = true, StartByte = 3, UnitT = UnitType_t.Temp, PackNumber = 1, Var = 130 });
            Parameters.Add(new() { Pgn = 99, Name = "t_pressure", BitLength = 24, StartByte = 5, a = 0.001, UnitT = UnitType_t.Pressure, Var = 131 });

            Parameters.Add(new() { Pgn = 100, Name = "Начальный адрес", BitLength = 24, StartByte = 1, PackNumber = 2, GetMeaning = r => $"{GetString("t_starting_address")}: 0X{(r + 0x8000000):X}" });
            Parameters.Add(new() { Pgn = 100, Name = "Длина данных", BitLength = 32, StartByte = 4, PackNumber = 2 });
            Parameters.Add(new() { Pgn = 100, Name = "Длина данных", BitLength = 24, StartByte = 1, PackNumber = 4 });
            Parameters.Add(new() { Pgn = 100, Name = "CRC", BitLength = 32, StartByte = 4, PackNumber = 4, GetMeaning = r => $"CRC: 0X{(r):X}" });
            Parameters.Add(new() { Pgn = 100, Name = "Адрес фрагмента", BitLength = 32, StartByte = 2, PackNumber = 5, GetMeaning = r => $"{GetString("t_fragment_address")}: 0X{r:X}" });

            Parameters.Add(new() { Pgn = 101, Name = "Первое слово", BitLength = 32, StartByte = 0, GetMeaning = r => $"1st: 0X{(r):X}" });
            Parameters.Add(new() { Pgn = 101, Name = "Второе слово", BitLength = 32, StartByte = 4, GetMeaning = r => $"2nd: 0X{(r):X}" });

        }
    }
}
#endif
