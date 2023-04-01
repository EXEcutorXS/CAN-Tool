using CAN_Tool.ViewModels.Base;
using System.Collections.Generic;
using static CAN_Tool.Libs.Helper;

namespace OmniProtocol
{
    public partial class Omni : ViewModel
    {
        public static void SeedStaticData()
        {
            #region Device names init
            Devices = new Dictionary<int, Device>() {
            { 0, new (){ID=0, } } ,
            { 1, new (){ID=1,DevType=DeviceType.Binar } } ,
            { 2, new (){ID=2,DevType=DeviceType.Planar}} ,
            { 3, new (){ID=3,DevType=DeviceType.Planar }} ,
            { 4, new (){ID=4, DevType=DeviceType.Binar}} ,
            { 5, new (){ID=5, DevType=DeviceType.Binar }} ,
            { 6, new (){ID=6, DevType=DeviceType.Binar}} ,
            { 7, new (){ID=7,DevType=DeviceType.Planar}} ,
            { 8, new (){ID=8, DevType=DeviceType.Binar}} ,
            { 9, new (){ID=9,DevType=DeviceType.Planar }} ,
            { 10, new (){ID=10, DevType=DeviceType.Binar}} ,
            { 11, new (){ID=11,DevType=DeviceType.Planar }} ,
            { 12, new (){ID=12,DevType=DeviceType.Planar }} ,
            { 13, new (){ID=13, DevType=DeviceType.Planar }} ,
            { 14, new (){ID=14, DevType=DeviceType.CookingPanel}} ,
            { 15, new (){ID=15, DevType=DeviceType.Planar }} ,
            { 16, new (){ID=16, DevType=DeviceType.Binar}} ,
            { 17, new (){ID=17, DevType=DeviceType.Binar }} ,
            { 18, new (){ID=18, DevType=DeviceType.Planar }} ,
            { 19, new (){ID=19, DevType=DeviceType.ValveControl}} ,
            { 20, new (){ID=20, DevType=DeviceType.Planar}} ,
            { 21, new (){ID=21, DevType=DeviceType.Binar}} ,
            { 22, new (){ID=22, DevType=DeviceType.Binar}} ,
            { 23, new (){ID=23, DevType=DeviceType.Binar,MaxBlower=90,MaxFuelPump=4}} ,
            { 25, new (){ID=25, DevType=DeviceType.Binar }} ,
            { 27, new (){ID=27, DevType=DeviceType.Binar, MaxBlower=90,MaxFuelPump=4}} ,
            { 29, new (){ID=29, DevType=DeviceType.Binar}} ,
            { 31, new (){ID=31, DevType=DeviceType.Binar }} ,
            { 32, new (){ID=32, DevType=DeviceType.Binar }} ,
            { 34, new (){ID=34, DevType=DeviceType.Binar, MaxBlower=90}} ,
            { 35, new (){ID=35, DevType=DeviceType.Binar, MaxBlower=90}} ,
            { 123, new (){ID=123, DevType=DeviceType.Bootloader }} ,
            { 126, new (){ID=126, DevType=DeviceType.HCU }},
            { 255, new (){ID=255}}
        };
            #endregion

            #region PGN names init
            PGNs.Add(0, new() { id = 0, name = "Empty_command" });
            PGNs.Add(1, new() { id = 1, name = "Control_command" });
            PGNs.Add(2, new() { id = 2, name = "received_command_ack" });
            PGNs.Add(3, new() { id = 3, name = "spn_request" });
            PGNs.Add(4, new() { id = 4, name = "spn_answer" });
            PGNs.Add(5, new() { id = 5, name = "parameter_write" });
            PGNs.Add(6, new() { id = 6, name = "pgn_request" });
            PGNs.Add(7, new() { id = 7, name = "flash_conf_read_write" });
            PGNs.Add(8, new() { id = 8, name = "black_box_operation" });
            PGNs.Add(10, new() { id = 10, name = "stage_mode_failures" });
            PGNs.Add(11, new() { id = 11, name = "voltage_pressure_current" });
            PGNs.Add(12, new() { id = 12, name = "blower_fp_plug_relay" });
            PGNs.Add(13, new() { id = 13, name = "liquid_heater_temperatures" });
            PGNs.Add(14, new() { id = 14, name = "flame_process" });
            PGNs.Add(15, new() { id = 15, name = "adc0-3" });
            PGNs.Add(16, new() { id = 16, name = "adc4-7" });
            PGNs.Add(17, new() { id = 17, name = "adc8-11" });
            PGNs.Add(18, new() { id = 18, name = "firmware_version" });
            PGNs.Add(19, new() { id = 19, name = "hcu_parameters", multipack = true });
            PGNs.Add(20, new() { id = 20, name = "failures" });
            PGNs.Add(21, new() { id = 21, name = "hcu_status" });
            PGNs.Add(22, new() { id = 22, name = "zone_control" });
            PGNs.Add(23, new() { id = 23, name = "fan_setpoints" });
            PGNs.Add(24, new() { id = 24, name = "fan_current_speed" });
            PGNs.Add(25, new() { id = 25, name = "daytime_setpoins" });
            PGNs.Add(26, new() { id = 26, name = "nighttime_setpoints" });
            PGNs.Add(27, new() { id = 27, name = "fan_manual_control" });
            PGNs.Add(28, new() { id = 28, name = "total_working_time" });
            PGNs.Add(29, new() { id = 29, name = "Параметры давления", multipack = true });
            PGNs.Add(30, new() { id = 30, name = "remote_wire_engine_air_temp" });
            PGNs.Add(31, new() { id = 31, name = "working_time" });
            PGNs.Add(32, new() { id = 32, name = "liquid_heater_setup" });
            PGNs.Add(33, new() { id = 33, name = "serial_number", multipack = true });
            PGNs.Add(34, new() { id = 34, name = "read_flash_by_address_req" });
            PGNs.Add(35, new() { id = 35, name = "read_flash_by_address_ans" });
            PGNs.Add(36, new() { id = 36, name = "valves _status_probe_valve_failures" });
            PGNs.Add(37, new() { id = 37, name = "air_heater_temperatures", multipack = true });
            PGNs.Add(38, new() { id = 38, name = "panel_temperature" });
            PGNs.Add(39, new() { id = 39, name = "drivers_status" });
            PGNs.Add(100, new() { id = 100, name = "memory_control_old", multipack = true });
            PGNs.Add(101, new() { id = 101, name = "buffer_data_transmitting_old" });
            PGNs.Add(105, new() { id = 105, name = "memory_control" });
            PGNs.Add(106, new() { id = 106, name = "buffer_data_transmitting" });
            #endregion

            #region Commands init
            commands.Add(0, new() { Id = 0, Name = "whos_here" });
            commands.Add(1, new() { Id = 1, Name = "start_device" });
            commands.Add(3, new() { Id = 3, Name = "stop_device" });
            commands.Add(4, new() { Id = 4, Name = "start_pump" });
            commands.Add(5, new() { Id = 5, Name = "reset_failures" });
            commands.Add(6, new() { Id = 6, Name = "liquid_heater_set_parameters" });
            commands.Add(7, new() { Id = 7, Name = "request_deltas" });
            commands.Add(8, new() { Id = 8, Name = "valves_control”" });
            commands.Add(9, new() { Id = 9, Name = "air_heater_set_parameters" });
            commands.Add(10, new() { Id = 10, Name = "start_vent" });
            commands.Add(20, new() { Id = 20, Name = "calibration" });
            commands.Add(21, new() { Id = 21, Name = "set_pwm_freq" });
            commands.Add(22, new() { Id = 22, Name = "reset_cpu" });
            commands.Add(45, new() { Id = 45, Name = "failures_mask" });
            commands.Add(65, new() { Id = 65, Name = "temperature_set" });
            commands.Add(66, new() { Id = 66, Name = "clear_error_codes" });
            commands.Add(67, new() { Id = 67, Name = "manual_test_mode_enter" });
            commands.Add(68, new() { Id = 68, Name = "manual_control" });
            commands.Add(69, new() { Id = 69, Name = "control_executive_devices" });
            commands.Add(70, new() { Id = 69, Name = "executive_devices_onoff" });//TODO исправить номер
            #endregion

            #region Command parameters init
            commands[0].Parameters.Add(new() { StartByte = 2, BitLength = 8, GetMeaning = i => ("device: " + GetString($"d_{i}") + ";"), AnswerOnly = true }); ;
            commands[0].Parameters.Add(new() { StartByte = 3, BitLength = 8, Meanings = { { 0, "12 volts" }, { 1, "24 volts" } }, AnswerOnly = true });
            commands[0].Parameters.Add(new() { StartByte = 4, BitLength = 8, Name = "firmware", AnswerOnly = true });
            commands[0].Parameters.Add(new() { StartByte = 5, BitLength = 8, Name = "modification", AnswerOnly = true });

            commands[1].Parameters.Add(new() { StartByte = 2, BitLength = 16, Name = "working_time", UnitT = UnitType.Minute });

            commands[3].Parameters.Add(new() { StartByte = 2, BitLength = 16, Name = "working_time", UnitT = UnitType.Minute });

            commands[10].Parameters.Add(new() { StartByte = 2, BitLength = 16, Name = "working_time", UnitT = UnitType.Minute });
            commands[6].Parameters.Add(new() { StartByte = 4, BitLength = 4, Name = "working_mode", Meanings = { { 0, "t_regular" }, { 1, "t_eco" }, { 2, "t_additional_heater" }, { 3, "t_heater" }, { 4, "t_heating_systems" } } });
            commands[6].Parameters.Add(new() { StartByte = 4, StartBit = 4, BitLength = 4, Name = "t_additional_heater_mode", Meanings = { { 0, "off" }, { 1, "t_auto" }, { 2, "t_manual" } } });
            commands[6].Parameters.Add(new() { StartByte = 5, BitLength = 16, Name = "t_temp_setpoint", UnitT = UnitType.Temp });
            commands[6].Parameters.Add(new() { StartByte = 7, BitLength = 2, Name = "t_pump_in_idle", Meanings = defMeaningsOnOff });
            commands[6].Parameters.Add(new() { StartByte = 7, BitLength = 2, StartBit = 2, Name = "t_pump_while_engine_running", Meanings = defMeaningsOnOff });

            commands[7].Parameters.Add(new() { StartByte = 2, BitLength = 8, Name = "t_power_level" });

            commands[7].Parameters.Add(new() { StartByte = 3, BitLength = 16, Name = "t_going_up_temperature", AnswerOnly = true });
            commands[7].Parameters.Add(new() { StartByte = 4, BitLength = 16, Name = "t_going_down_temperature", AnswerOnly = true });

            commands[8].Parameters.Add(new() { StartByte = 2, BitLength = 2, StartBit = 0, Name = "Состояние клапана 1", Meanings = defMeaningsOnOff });
            commands[8].Parameters.Add(new() { StartByte = 2, BitLength = 2, StartBit = 2, Name = "Состояние клапана 2", Meanings = defMeaningsOnOff });
            commands[8].Parameters.Add(new() { StartByte = 2, BitLength = 2, StartBit = 4, Name = "Состояние клапана 3", Meanings = defMeaningsOnOff });
            commands[8].Parameters.Add(new() { StartByte = 2, BitLength = 2, StartBit = 6, Name = "Состояние клапана 4", Meanings = defMeaningsOnOff });
            commands[8].Parameters.Add(new() { StartByte = 3, BitLength = 2, StartBit = 0, Name = "Состояние клапана 5", Meanings = defMeaningsOnOff });
            commands[8].Parameters.Add(new() { StartByte = 3, BitLength = 2, StartBit = 2, Name = "Состояние клапана 6", Meanings = defMeaningsOnOff });
            commands[8].Parameters.Add(new() { StartByte = 3, BitLength = 2, StartBit = 4, Name = "Состояние клапана 7", Meanings = defMeaningsOnOff });
            commands[8].Parameters.Add(new() { StartByte = 3, BitLength = 2, StartBit = 6, Name = "Состояние клапана 8", Meanings = defMeaningsOnOff });
            commands[8].Parameters.Add(new() { StartByte = 4, BitLength = 1, StartBit = 0, Meanings = { { 0, "Сбросить неисправности" } } });

            commands[9].Parameters.Add(new() { StartByte = 2, BitLength = 16, Name = "Время работы", UnitT = UnitType.Minute });
            commands[9].Parameters.Add(new() { StartByte = 4, BitLength = 4, Name = "Режим работы", Meanings = { { 0, "не используется" }, { 1, "работа по температуре платы" }, { 2, "работа по температуре пульта" }, { 3, "работа по температуре выносного датчика" }, { 4, "работа по мощности" } } });
            commands[9].Parameters.Add(new() { StartByte = 4, StartBit = 4, BitLength = 2, Name = "Разрешение/запрещение ждущего режима (при работе по датчику температуры)", Meanings = defMeaningsAllow });
            commands[9].Parameters.Add(new() { StartByte = 4, StartBit = 6, BitLength = 2, Name = "Разрешение вращения нагнетателя воздуха на ждущем режиме", Meanings = defMeaningsAllow });
            commands[9].Parameters.Add(new() { StartByte = 5, BitLength = 16, Name = "Уставка температуры помещения", UnitT = UnitType.Temp });
            commands[9].Parameters.Add(new() { StartByte = 7, BitLength = 4, Name = "Заданное значение мощности" });

            commands[10].Parameters.Add(new() { StartByte = 2, BitLength = 16, Name = "Время работы", UnitT = UnitType.Temp });

            commands[20].Parameters.Add(new() { StartByte = 2, BitLength = 16, Name = "Калибровочное значение термопары 1", AnswerOnly = true });
            commands[20].Parameters.Add(new() { StartByte = 4, BitLength = 16, Name = "Калибровочное значение термопары 2", AnswerOnly = true });

            commands[21].Parameters.Add(new() { StartByte = 2, BitLength = 8, Name = "Предделитель" });
            commands[21].Parameters.Add(new() { StartByte = 3, BitLength = 8, Name = "Период ШИМ" });
            commands[21].Parameters.Add(new() { StartByte = 5, BitLength = 8, Name = "Требуемая частота", UnitT = UnitType.Frequency });

            commands[22].Parameters.Add(new() { StartByte = 2, StartBit = 0, BitLength = 8, Name = "Действие после перезагрузки", Meanings = { { 0, "Остаться в загрузчике" }, { 1, "Переход в основную программу без зедержки" }, { 2, "5 секунд в загрузчике" } } });

            commands[45].Parameters.Add(new() { StartByte = 2, StartBit = 0, BitLength = 2, Name = "Игнорирование всех неисправностей", Meanings = defMeaningsYesNo });
            commands[45].Parameters.Add(new() { StartByte = 2, StartBit = 2, BitLength = 2, Name = "Игнорирование неисправностей ТН", Meanings = defMeaningsYesNo });
            commands[45].Parameters.Add(new() { StartByte = 2, StartBit = 4, BitLength = 2, Name = "Игнорирование срывов пламени ", Meanings = defMeaningsYesNo });
            commands[45].Parameters.Add(new() { StartByte = 2, StartBit = 6, BitLength = 2, Name = "Игнорирование неисправностей свечи", Meanings = defMeaningsYesNo });
            commands[45].Parameters.Add(new() { StartByte = 3, StartBit = 0, BitLength = 2, Name = "Игнорирование неисправностей НВ", Meanings = defMeaningsYesNo });
            commands[45].Parameters.Add(new() { StartByte = 3, StartBit = 2, BitLength = 2, Name = "Игнорирование неисправностей датчиков", Meanings = defMeaningsYesNo });
            commands[45].Parameters.Add(new() { StartByte = 3, StartBit = 4, BitLength = 2, Name = "Игнорирование неисправностей помпы", Meanings = defMeaningsYesNo });
            commands[45].Parameters.Add(new() { StartByte = 3, StartBit = 6, BitLength = 2, Name = "Игнорирование перегревов", Meanings = defMeaningsYesNo });

            commands[65].Parameters.Add(new() { StartByte = 2, StartBit = 0, BitLength = 8, Name = "", Meanings = { { 7, "Температура жидкости" }, { 10, "Температура перегрева" }, { 12, "Температура пламени" }, { 13, "Температура корпуса" }, { 27, "Температура воздуха" } } });
            commands[65].Parameters.Add(new() { StartByte = 3, BitLength = 16, Name = "Значение температуры", UnitT = UnitType.Temp });

            commands[67].Parameters.Add(new() { StartByte = 2, StartBit = 0, BitLength = 2, Name = "", Meanings = { { 0, "Выход из режима М" }, { 1, "Вход в режим М" } } });
            commands[67].Parameters.Add(new() { StartByte = 2, StartBit = 2, BitLength = 2, Name = "", Meanings = { { 0, "Выход из режима Т" }, { 1, "Вход в режим Т" } } });

            commands[68].Parameters.Add(new() { StartByte = 2, StartBit = 0, BitLength = 2, Name = "Состояние помпы", Meanings = defMeaningsOnOff });
            commands[68].Parameters.Add(new() { StartByte = 3, StartBit = 0, BitLength = 8, Name = "Обороты нагнетателя", UnitT = UnitType.Rps });
            commands[68].Parameters.Add(new() { StartByte = 4, StartBit = 0, BitLength = 8, Name = "Мощность свечи", UnitT = UnitType.Percent });
            commands[68].Parameters.Add(new() { StartByte = 5, StartBit = 0, BitLength = 16, Name = "Частота ТН", a = 0.01, UnitT = UnitType.Frequency });

            commands[69].Parameters.Add(new() { StartByte = 2, StartBit = 0, BitLength = 8, Name = "Тип устройства", Meanings = { { 0, "ТН, Гц*10" }, { 1, "Реле(0/1)" }, { 2, "Свеча, %" }, { 3, "Помпа,%" }, { 4, "Шим НВ,%" }, { 23, "Обороты НВ, об/с" } } });
            commands[69].Parameters.Add(new() { StartByte = 3, StartBit = 0, BitLength = 16, Name = "Значение" });

            commands[70].Parameters.Add(new() { StartByte = 2, StartBit = 0, BitLength = 2, Name = "Состояние ТН", Meanings = defMeaningsOnOff });
            commands[70].Parameters.Add(new() { StartByte = 2, StartBit = 2, BitLength = 2, Name = "Состояние реле", Meanings = defMeaningsOnOff });
            commands[70].Parameters.Add(new() { StartByte = 2, StartBit = 4, BitLength = 2, Name = "Состояние свечи", Meanings = defMeaningsOnOff });
            commands[70].Parameters.Add(new() { StartByte = 2, StartBit = 6, BitLength = 2, Name = "Состояние помпы", Meanings = defMeaningsOnOff });
            commands[70].Parameters.Add(new() { StartByte = 3, StartBit = 0, BitLength = 2, Name = "Состояние НВ", Meanings = defMeaningsOnOff });
            #endregion

            #region PGN parameters initialise

            PGNs[3].parameters.Add(new() { Name = "SPN", BitLength = 16, StartByte = 0 });

            PGNs[4].parameters.Add(new() { Name = "SPN", BitLength = 16, StartByte = 0 });
            PGNs[4].parameters.Add(new() { Name = "Значение", BitLength = 32, StartBit = 0, StartByte = 2 });

            PGNs[5].parameters.Add(new() { Name = "SPN", BitLength = 16, StartByte = 0 });
            PGNs[5].parameters.Add(new() { Name = "Значение", BitLength = 32, StartBit = 0, StartByte = 2 });

            PGNs[6].parameters.Add(new() { Name = "PGN", BitLength = 16, StartByte = 0, GetMeaning = x => { if (PGNs.ContainsKey(x)) return PGNs[x].name; else return "Нет такого PGN"; } });

            PGNs[7].parameters.Add(new() { Name = "Команда", BitLength = 8, StartBit = 0, StartByte = 0, Meanings = { { 0, "Стереть конфигурацию" }, { 1, "Запись параметра в ОЗУ" }, { 2, "Запись всех параметров во Flash" }, { 3, "Чтение параметра по номеру" }, { 4, "Успешный ответ на запрос" }, { 5, "Невозможно выполнить" } } });
            PGNs[7].parameters.Add(new() { Name = "Запрошенная команда", BitLength = 8, StartBit = 0, StartByte = 1, Meanings = { { 0, "Стереть конфигурацию" }, { 1, "Запись параметра в ОЗУ" }, { 2, "Запись всех параметров во Flash" }, { 3, "Чтение параметра по номеру" }, { 255, "" } } });
            PGNs[7].parameters.Add(new() { Name = "Параметр", BitLength = 16, StartBit = 0, StartByte = 2, GetMeaning = x => GetString($"par_{x}") });
            PGNs[7].parameters.Add(new() { Name = "Value", BitLength = 32, StartBit = 0, StartByte = 4, AnswerOnly = true });

            PGNs[8].parameters.Add(new() { Name = "Команда", BitLength = 4, StartBit = 0, StartByte = 0, Meanings = { { 0, "Стереть ЧЯ" }, { 3, "Чтение ЧЯ" }, { 4, "Ответ" }, { 6, "Чтение параметра (из paramsname.h)" } } });
            PGNs[8].parameters.Add(new() { Name = "Тип:", BitLength = 2, StartBit = 4, StartByte = 0, Meanings = { { 0, "Общие данные" }, { 1, "Неисправности" } } });
            PGNs[8].parameters.Add(new() { Name = "Номер пары", CustomDecoder = d => { if ((d[0] & 0xF) == 3) return "Номер пары:" + (d[4] * 0x100 + d[5]).ToString() + ";"; else return ""; } });
            PGNs[8].parameters.Add(new() { Name = "Номер параметра", CustomDecoder = d => { if ((d[0] & 0xF) == 6) return "Номер параметра:" + (d[4] * 0x100 + d[5]).ToString() + ";"; else return ""; } });
            PGNs[8].parameters.Add(new() { Name = "Число пар", CustomDecoder = d => { if ((d[0] & 0xF) == 6) return "Запрошено пар:" + (d[6] * 0x100 + d[7]).ToString() + ";"; else return ""; } });
            PGNs[8].parameters.Add(new() { Name = "Номер параметра", CustomDecoder = d => { if (d[0] == 4) return "Параметр:" + (d[2] * 256 + d[3]).ToString() + ";"; else return ""; } });
            PGNs[8].parameters.Add(new() { Name = "Значение параметра", CustomDecoder = d => { if (d[0] == 4) return "Значение:" + (d[4] * 0x1000000 + d[5] * 0x10000 + d[6] * 0x100 + d[7]).ToString() + ";"; else return ""; }, AnswerOnly = true });


            PGNs[10].parameters.Add(new() { Name = "Стадия", BitLength = 8, StartByte = 0, Meanings = Stages, Var = 1 });
            PGNs[10].parameters.Add(new() { Name = "Режим", BitLength = 8, StartByte = 1, Var = 2 });
            PGNs[10].parameters.Add(new() { Name = "Код неисправности", BitLength = 8, StartByte = 2, Var = 24, GetMeaning = x => GetString($"e_{x}") });
            PGNs[10].parameters.Add(new() { Name = "Помпа неисправна", BitLength = 2, StartByte = 3, Meanings = defMeaningsYesNo });
            PGNs[10].parameters.Add(new() { Name = "Код предупреждения", BitLength = 8, StartByte = 4 });
            PGNs[10].parameters.Add(new() { Name = "Количество морганий", BitLength = 8, StartByte = 5, Var = 25 });

            PGNs[11].parameters.Add(new() { Name = "Напряжение питания", BitLength = 16, StartByte = 0, a = 0.1, UnitT = UnitType.Volt, Var = 5 });
            PGNs[11].parameters.Add(new() { Name = "Атмосферное давление", BitLength = 8, StartByte = 2, UnitT = UnitType.Pressure });
            PGNs[11].parameters.Add(new() { Name = "Ток двигателя, значения АЦП", BitLength = 16, StartByte = 3 });

            PGNs[12].parameters.Add(new() { Name = "Заданные обороты нагнетателя воздуха", BitLength = 8, StartByte = 0, UnitT = UnitType.Rps, Var = 15 });
            PGNs[12].parameters.Add(new() { Name = "Измеренные обороты нагнетателя воздуха,", BitLength = 8, StartByte = 1, UnitT = UnitType.Rps, Var = 16 });
            PGNs[12].parameters.Add(new() { Name = "Заданная частота ТН", BitLength = 16, StartByte = 2, a = 0.01, UnitT = UnitType.Frequency, Var = 17 });
            PGNs[12].parameters.Add(new() { Name = "Реализованная частота ТН", BitLength = 16, StartByte = 4, a = 0.01, UnitT = UnitType.Frequency, Var = 18 });
            PGNs[12].parameters.Add(new() { Name = "Мощность свечи", BitLength = 8, StartByte = 6, UnitT = UnitType.Percent, Var = 21 });
            PGNs[12].parameters.Add(new() { Name = "Состояние помпы", BitLength = 2, StartByte = 7, Meanings = defMeaningsOnOff, Var = 46 });
            PGNs[12].parameters.Add(new() { Name = "Состояние реле печки кабины", BitLength = 2, StartByte = 7, StartBit = 2, Meanings = defMeaningsOnOff, Var = 45 });
            PGNs[12].parameters.Add(new() { Name = "Состояние состояние канала сигнализации", BitLength = 2, StartByte = 7, StartBit = 4, Meanings = defMeaningsOnOff, Var = 47 });

            PGNs[13].parameters.Add(new() { Name = "Температура ИП", BitLength = 16, StartByte = 0, UnitT = UnitType.Temp, Var = 6 });
            PGNs[13].parameters.Add(new() { Name = "Температура платы/процессора", BitLength = 8, StartByte = 2, b = -75, UnitT = UnitType.Temp, Var = 59 });
            PGNs[13].parameters.Add(new() { Name = "Температура жидкости", BitLength = 8, StartByte = 3, b = -75, UnitT = UnitType.Temp, Var = 40 });
            PGNs[13].parameters.Add(new() { Name = "Температура перегрева", BitLength = 8, StartByte = 4, b = -75, UnitT = UnitType.Temp, Var = 41 });

            PGNs[14].parameters.Add(new() { Name = "Минимальная температура пламени перед розжигом", BitLength = 16, StartByte = 0, UnitT = UnitType.Temp, Var = 36, Signed = true });
            PGNs[14].parameters.Add(new() { Name = "Граница срыва пламени", BitLength = 16, StartByte = 2, UnitT = UnitType.Temp, Var = 37, Signed = true });
            PGNs[14].parameters.Add(new() { Name = "Граница срыва пламени на прогреве", BitLength = 16, StartByte = 4, UnitT = UnitType.Temp, Signed = true });
            PGNs[14].parameters.Add(new() { Name = "Скорость изменения температуры ИП", BitLength = 16, StartByte = 6, UnitT = UnitType.Temp, Signed = true });


            PGNs[15].parameters.Add(new() { Name = "0 канал АЦП ", BitLength = 16, StartByte = 0, Var = 49 });
            PGNs[15].parameters.Add(new() { Name = "1 канал АЦП ", BitLength = 16, StartByte = 2, Var = 50 });
            PGNs[15].parameters.Add(new() { Name = "2 канал АЦП ", BitLength = 16, StartByte = 4, Var = 51 });
            PGNs[15].parameters.Add(new() { Name = "3 канал АЦП ", BitLength = 16, StartByte = 6, Var = 52 });

            PGNs[16].parameters.Add(new() { Name = "4 канал АЦП ", BitLength = 16, StartByte = 0, Var = 53 });
            PGNs[16].parameters.Add(new() { Name = "5 канал АЦП ", BitLength = 16, StartByte = 2, Var = 54 });
            PGNs[16].parameters.Add(new() { Name = "6 канал АЦП ", BitLength = 16, StartByte = 4, Var = 55 });
            PGNs[16].parameters.Add(new() { Name = "7 канал АЦП ", BitLength = 16, StartByte = 6, Var = 56 });

            PGNs[17].parameters.Add(new() { Name = "8 канал АЦП ", BitLength = 16, StartByte = 0, Var = 57 });
            PGNs[17].parameters.Add(new() { Name = "9 канал АЦП ", BitLength = 16, StartByte = 2, Var = 58 });
            PGNs[17].parameters.Add(new() { Name = "10 канал АЦП ", BitLength = 16, StartByte = 4 });
            PGNs[17].parameters.Add(new() { Name = "11 канал АЦП ", BitLength = 16, StartByte = 6 });

            PGNs[18].parameters.Add(new() { Name = "Вид изделия", BitLength = 8, StartByte = 0, GetMeaning = i => Devices[i]?.Name });
            PGNs[18].parameters.Add(new() { Name = "Напряжение питания", BitLength = 8, StartByte = 1, Meanings = { { 0, "12 Вольт" }, { 1, "24 Вольта" } } });
            PGNs[18].parameters.Add(new() { Name = "Версия ПО", BitLength = 8, StartByte = 2 });
            PGNs[18].parameters.Add(new() { Name = "Модификация ПО", BitLength = 8, StartByte = 3 });
            PGNs[18].parameters.Add(new() { Name = "Дата релиза", BitLength = 24, StartByte = 5, GetMeaning = v => $"{v >> 16}.{(v >> 8) & 0xF}.{v & 0xFF}" });
            //PGNs[18].parameters.Add(new () { Name = "День ", BitLength = 8, StartByte = 5 });    Не красиво выглядит...луше одной строкой
            //PGNs[18].parameters.Add(new () { Name = "Месяц", BitLength = 8, StartByte = 6 });
            //PGNs[18].parameters.Add(new () { Name = "Год", BitLength = 8, StartByte = 7 });

            PGNs[19].parameters.Add(new() { Name = "Подогреватель", BitLength = 2, StartBit = 0, StartByte = 1, PackNumber = 1, Meanings = defMeaningsOnOff });
            PGNs[19].parameters.Add(new() { Name = "Помпы", BitLength = 2, StartBit = 2, StartByte = 1, PackNumber = 1, Meanings = defMeaningsOnOff });
            PGNs[19].parameters.Add(new() { Name = "Вода", BitLength = 2, StartBit = 4, StartByte = 1, PackNumber = 1, Meanings = defMeaningsOnOff });
            PGNs[19].parameters.Add(new() { Name = "Быстрый нагрев воды", BitLength = 2, StartBit = 6, StartByte = 1, PackNumber = 1, Meanings = defMeaningsOnOff });
            PGNs[19].parameters.Add(new() { Name = "Температура бака", BitLength = 8, StartByte = 2, b = -75, UnitT = UnitType.Temp, PackNumber = 1 });
            PGNs[19].parameters.Add(new() { Name = "Атмосферное давление", BitLength = 8, StartByte = 3, UnitT = UnitType.Temp, PackNumber = 1 });
            PGNs[19].parameters.Add(new() { Name = "Сработал датчик бытовой воды", BitLength = 2, StartByte = 4, PackNumber = 1, Meanings = defMeaningsYesNo, Var = 108 });

            PGNs[19].parameters.Add(new() { Name = "Уставка температуры жидкости подогревателя для перехода в ждущий.", BitLength = 8, StartByte = 1, b = -75, PackNumber = 2, UnitT = UnitType.Temp });
            PGNs[19].parameters.Add(new() { Name = "Уставка температуры жидкости подогревателя для выхода из ждущего.", BitLength = 8, StartByte = 2, b = -75, PackNumber = 2, UnitT = UnitType.Temp });
            PGNs[19].parameters.Add(new() { Name = "Уставка температуры жидкости подогревателя для выхода из ждущего при разборе воды.", BitLength = 8, StartByte = 3, b = -75, PackNumber = 2, UnitT = UnitType.Temp });
            PGNs[19].parameters.Add(new() { Name = "Уставка температуры бака для перехода в ждущий.", BitLength = 8, StartByte = 4, b = -75, PackNumber = 2, UnitT = UnitType.Temp });
            PGNs[19].parameters.Add(new() { Name = "Уставка температуры бака для выхода из ждущего.", BitLength = 8, StartByte = 5, b = -75, PackNumber = 2, UnitT = UnitType.Temp });
            PGNs[19].parameters.Add(new() { Name = "Уставка температуры бака для выхода из ждущего при разборе воды.", BitLength = 8, StartByte = 6, b = -75, PackNumber = 2, UnitT = UnitType.Temp });

            PGNs[20].parameters.Add(new() { Name = "Код неисправности", BitLength = 8, StartByte = 0 });
            PGNs[20].parameters.Add(new() { Name = "Количество морганий", BitLength = 8, StartByte = 1 });
            PGNs[20].parameters.Add(new() { Name = "Байт неисправностей 1", BitLength = 8, StartByte = 2 });
            PGNs[20].parameters.Add(new() { Name = "Байт неисправностей 2", BitLength = 8, StartByte = 3 });
            PGNs[20].parameters.Add(new() { Name = "Байт неисправностей 3", BitLength = 8, StartByte = 4 });
            PGNs[20].parameters.Add(new() { Name = "Байт неисправностей 4", BitLength = 8, StartByte = 5 });
            PGNs[20].parameters.Add(new() { Name = "Байт неисправностей 5", BitLength = 8, StartByte = 6 });
            PGNs[20].parameters.Add(new() { Name = "Байт неисправностей 6", BitLength = 8, StartByte = 7 });

            PGNs[21].parameters.Add(new() { Name = "Опорное напряжение процессора", BitLength = 8, StartByte = 0, UnitT = UnitType.Volt, a = 0.1 });
            PGNs[21].parameters.Add(new() { Name = "Температура процессора", BitLength = 8, StartByte = 1, UnitT = UnitType.Temp, b = -75 });
            PGNs[21].parameters.Add(new() { Name = "Температура бака", BitLength = 8, StartByte = 2, UnitT = UnitType.Temp, b = -75, Var = 106 });
            PGNs[21].parameters.Add(new() { Name = "Температура теплообменника", BitLength = 8, StartByte = 3, UnitT = UnitType.Temp, b = -75 });
            PGNs[21].parameters.Add(new() { Name = "Температура наружного воздуха", BitLength = 8, StartByte = 4, UnitT = UnitType.Temp, b = -75, Var = 107 });
            PGNs[21].parameters.Add(new() { Name = "Уровень жидкости в баке", BitLength = 8, StartByte = 6, Var = 105 });
            PGNs[21].parameters.Add(new() { Name = "Разбор воды", BitLength = 2, StartByte = 7, Meanings = defMeaningsYesNo, Var = 108 });

            PGNs[22].parameters.Add(new() { Name = "Зона 1", BitLength = 2, StartByte = 0, StartBit = 0, Meanings = defMeaningsOnOff, Var = 65 });
            PGNs[22].parameters.Add(new() { Name = "Зона 2", BitLength = 2, StartByte = 0, StartBit = 2, Meanings = defMeaningsOnOff, Var = 66 });
            PGNs[22].parameters.Add(new() { Name = "Зона 3", BitLength = 2, StartByte = 0, StartBit = 4, Meanings = defMeaningsOnOff, Var = 67 });
            PGNs[22].parameters.Add(new() { Name = "Зона 4", BitLength = 2, StartByte = 0, StartBit = 6, Meanings = defMeaningsOnOff, Var = 68 });
            PGNs[22].parameters.Add(new() { Name = "Зона 5", BitLength = 2, StartByte = 1, StartBit = 0, Meanings = defMeaningsOnOff, Var = 69 });
            PGNs[22].parameters.Add(new() { Name = "Температура зоны 1", BitLength = 8, StartByte = 2, UnitT = UnitType.Temp, b = -75, Var = 70 });
            PGNs[22].parameters.Add(new() { Name = "Температура зоны 2", BitLength = 8, StartByte = 3, UnitT = UnitType.Temp, b = -75, Var = 71 });
            PGNs[22].parameters.Add(new() { Name = "Температура зоны 3", BitLength = 8, StartByte = 4, UnitT = UnitType.Temp, b = -75, Var = 72 });
            PGNs[22].parameters.Add(new() { Name = "Температура зоны 4", BitLength = 8, StartByte = 5, UnitT = UnitType.Temp, b = -75, Var = 73 });
            PGNs[22].parameters.Add(new() { Name = "Температура зоны 5", BitLength = 8, StartByte = 6, UnitT = UnitType.Temp, b = -75, Var = 74 });
            PGNs[22].parameters.Add(new() { Name = "Подогреватель", BitLength = 2, StartByte = 7, StartBit = 0, Meanings = defMeaningsOnOff, Var = 63 });
            PGNs[22].parameters.Add(new() { Name = "ТЭН", BitLength = 2, StartByte = 7, StartBit = 2, Meanings = defMeaningsOnOff, Var = 64 });

            PGNs[23].parameters.Add(new() { Name = "Зад. ШИМ вентилятора зоны 1", BitLength = 8, StartByte = 0, UnitT = UnitType.Percent, Var = 100 });
            PGNs[23].parameters.Add(new() { Name = "Зад. ШИМ вентилятора зоны 2", BitLength = 8, StartByte = 1, UnitT = UnitType.Percent, Var = 101 });
            PGNs[23].parameters.Add(new() { Name = "Зад. ШИМ вентилятора зоны 3", BitLength = 8, StartByte = 2, UnitT = UnitType.Percent, Var = 102 });
            PGNs[23].parameters.Add(new() { Name = "Зад. ШИМ вентилятора зоны 4", BitLength = 8, StartByte = 3, UnitT = UnitType.Percent, Var = 103 });
            PGNs[23].parameters.Add(new() { Name = "Зад. ШИМ вентилятора зоны 5", BitLength = 8, StartByte = 4, UnitT = UnitType.Percent, Var = 104 });

            PGNs[24].parameters.Add(new() { Name = "Ступень скорости вентилятора зоны 1", BitLength = 4, StartByte = 0, });
            PGNs[24].parameters.Add(new() { Name = "Ступень скорости вентилятора зоны 2", BitLength = 4, StartByte = 0, StartBit = 4 });
            PGNs[24].parameters.Add(new() { Name = "Ступень скорости вентилятора зоны 3", BitLength = 4, StartByte = 1, });
            PGNs[24].parameters.Add(new() { Name = "Ступень скорости вентилятора зоны 4", BitLength = 4, StartByte = 1, StartBit = 4 });
            PGNs[24].parameters.Add(new() { Name = "Ступень скорости вентилятора зоны 5", BitLength = 4, StartByte = 2, });
            PGNs[24].parameters.Add(new() { Name = "Тек. ШИМ вентилятора зоны 1", BitLength = 8, StartByte = 3, UnitT = UnitType.Percent, Var = 95 });
            PGNs[24].parameters.Add(new() { Name = "Тек. ШИМ вентилятора зоны 2", BitLength = 8, StartByte = 4, UnitT = UnitType.Percent, Var = 96 });
            PGNs[24].parameters.Add(new() { Name = "Тек. ШИМ вентилятора зоны 3", BitLength = 8, StartByte = 5, UnitT = UnitType.Percent, Var = 97 });
            PGNs[24].parameters.Add(new() { Name = "Тек. ШИМ вентилятора зоны 4", BitLength = 8, StartByte = 6, UnitT = UnitType.Percent, Var = 98 });
            PGNs[24].parameters.Add(new() { Name = "Тек. ШИМ вентилятора зоны 5", BitLength = 8, StartByte = 7, UnitT = UnitType.Percent, Var = 99 });

            PGNs[25].parameters.Add(new() { Name = "Дневная уставка зоны 1", BitLength = 8, StartByte = 0, UnitT = UnitType.Temp, b = -75, Var = 75 });
            PGNs[25].parameters.Add(new() { Name = "Дневная уставка зоны 2", BitLength = 8, StartByte = 1, UnitT = UnitType.Temp, b = -75, Var = 76 });
            PGNs[25].parameters.Add(new() { Name = "Дневная уставка зоны 3", BitLength = 8, StartByte = 2, UnitT = UnitType.Temp, b = -75, Var = 77 });
            PGNs[25].parameters.Add(new() { Name = "Дневная уставка зоны 4", BitLength = 8, StartByte = 3, UnitT = UnitType.Temp, b = -75, Var = 78 });
            PGNs[25].parameters.Add(new() { Name = "Дневная уставка зоны 5", BitLength = 8, StartByte = 4, UnitT = UnitType.Temp, b = -75, Var = 79 });

            PGNs[26].parameters.Add(new() { Name = "Ночная уставка зоны 1", BitLength = 8, StartByte = 0, UnitT = UnitType.Temp, b = -75, Var = 80 });
            PGNs[26].parameters.Add(new() { Name = "Ночная уставка зоны 2", BitLength = 8, StartByte = 1, UnitT = UnitType.Temp, b = -75, Var = 81 });
            PGNs[26].parameters.Add(new() { Name = "Ночная уставка зоны 3", BitLength = 8, StartByte = 2, UnitT = UnitType.Temp, b = -75, Var = 82 });
            PGNs[26].parameters.Add(new() { Name = "Ночная уставка зоны 4", BitLength = 8, StartByte = 3, UnitT = UnitType.Temp, b = -75, Var = 83 });
            PGNs[26].parameters.Add(new() { Name = "Ночная уставка зоны 5", BitLength = 8, StartByte = 4, UnitT = UnitType.Temp, b = -75, Var = 84 });


            PGNs[27].parameters.Add(new() { Name = "Ручная уставка ШИМ зоны 1", BitLength = 8, StartByte = 0, UnitT = UnitType.Percent, Var = 90 });
            PGNs[27].parameters.Add(new() { Name = "Ручная уставка ШИМ зоны 2", BitLength = 8, StartByte = 1, UnitT = UnitType.Percent, Var = 91 });
            PGNs[27].parameters.Add(new() { Name = "Ручная уставка ШИМ зоны 3", BitLength = 8, StartByte = 2, UnitT = UnitType.Percent, Var = 92 });
            PGNs[27].parameters.Add(new() { Name = "Ручная уставка ШИМ зоны 4", BitLength = 8, StartByte = 3, UnitT = UnitType.Percent, Var = 93 });
            PGNs[27].parameters.Add(new() { Name = "Ручная уставка ШИМ зоны 5", BitLength = 8, StartByte = 4, UnitT = UnitType.Percent, Var = 94 });

            PGNs[27].parameters.Add(new() { Name = "Зона 1 Ручной режим", BitLength = 2, StartByte = 5, StartBit = 0, Meanings = defMeaningsOnOff, Var = 85 });
            PGNs[27].parameters.Add(new() { Name = "Зона 2 Ручной режим", BitLength = 2, StartByte = 5, StartBit = 2, Meanings = defMeaningsOnOff, Var = 86 });
            PGNs[27].parameters.Add(new() { Name = "Зона 3 Ручной режим", BitLength = 2, StartByte = 5, StartBit = 4, Meanings = defMeaningsOnOff, Var = 87 });
            PGNs[27].parameters.Add(new() { Name = "Зона 4 Ручной режим", BitLength = 2, StartByte = 5, StartBit = 6, Meanings = defMeaningsOnOff, Var = 88 });
            PGNs[27].parameters.Add(new() { Name = "Зона 5 Ручной режим", BitLength = 2, StartByte = 6, StartBit = 0, Meanings = defMeaningsOnOff, Var = 89 });

            PGNs[28].parameters.Add(new() { Name = "Общее время на всех режимах", BitLength = 32, StartByte = 0, UnitT = UnitType.Second });
            PGNs[28].parameters.Add(new() { Name = "Общее время работы (кроме ожидания команды)", BitLength = 32, StartByte = 4, UnitT = UnitType.Second });

            PGNs[29].parameters.Add(new() { Name = "Атмосферное давление", BitLength = 8, StartByte = 1, UnitT = UnitType.Pressure, PackNumber = 1 });
            PGNs[29].parameters.Add(new() { Name = "Среднее максимальное значение давления", BitLength = 24, StartByte = 2, UnitT = UnitType.Pressure, a = 0.001, PackNumber = 1 });
            PGNs[29].parameters.Add(new() { Name = "Среднее минимальное значение давления", BitLength = 24, StartByte = 4, UnitT = UnitType.Pressure, a = 0.001, PackNumber = 1 });

            PGNs[29].parameters.Add(new() { Name = "Разница между max и min  значениями", BitLength = 16, StartByte = 1, a = 0.01, UnitT = UnitType.Pressure, PackNumber = 2 });
            PGNs[29].parameters.Add(new() { Name = "Флаг появления пламени по пульсации давления", BitLength = 2, StartByte = 3, Meanings = defMeaningsYesNo, PackNumber = 2 });
            PGNs[29].parameters.Add(new() { Name = "Атмосферное давление", BitLength = 24, StartByte = 4, UnitT = UnitType.Pressure, a = 0.001, PackNumber = 2, Var = 60 });

            PGNs[31].parameters.Add(new() { Name = "Время работы", BitLength = 32, StartByte = 0, UnitT = UnitType.Second, Var = 3 });
            PGNs[31].parameters.Add(new() { Name = "Время работы на режиме", BitLength = 32, StartByte = 4, UnitT = UnitType.Second, Var = 4 });

            PGNs[100].parameters.Add(new() { Name = "Начальный адрес", BitLength = 24, StartByte = 1, PackNumber = 2, GetMeaning = r => $"{GetString("t_starting_address")}: 0X{(r + 0x8000000):X}" });
            PGNs[100].parameters.Add(new() { Name = "Длина данных", BitLength = 32, StartByte = 4, PackNumber = 2 });
            PGNs[100].parameters.Add(new() { Name = "Длина данных", BitLength = 24, StartByte = 1, PackNumber = 4 });
            PGNs[100].parameters.Add(new() { Name = "CRC", BitLength = 32, StartByte = 4, PackNumber = 4, GetMeaning = r => $"CRC: 0X{(r):X}" });
            PGNs[100].parameters.Add(new() { Name = "Адрес фрагмента", BitLength = 32, StartByte = 2, PackNumber = 5, GetMeaning = r => $"{GetString("t_fragment_address")}: 0X{r:X}" });

            PGNs[101].parameters.Add(new() { Name = "Первое слово", BitLength = 32, StartByte = 0, GetMeaning = r => $"1st: 0X{(r):X}" });
            PGNs[101].parameters.Add(new() { Name = "Второе слово", BitLength = 32, StartByte = 4, GetMeaning = r => $"2nd: 0X{(r):X}" });

            #endregion
        }

    }
}
