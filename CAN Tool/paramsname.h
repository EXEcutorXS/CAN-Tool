/* Define to prevent recursive inclusion -------------------------------------*/
#ifndef __PARAMS_NAME_H
#define __PARAMS_NAME_H

/*----------------------------------------------------------------------------------------------------------------------------------
                                   Номера (имена) параметров работы (конфигурация)
----------------------------------------------------------------------------------------------------------------------------------*/
//                                        РУССКИЙ                                                                                                     ENGLISH
#define PAR_FAN_PWM                      0//ШИМ нагнетателя воздуха, Гц                                                                               @ FAN_PWM, Hz
#define PAR_FP_PULSE_MS                  1//Длительность импульса ТН, мс                                                                              @ Pulse width, ms
#define PAR_GP_NOMINAL_VOLTAGE           2//Номинальное напряжение свечи, В                                                                           @ Glow plug rated voltage, V
#define PAR_FLAME_APPEARANCE_DELTA       3//прирост температуры пламени для определения его появления (стадия P)                                      @ FLAME_APPEARANCE_DELTA
#define PAR_FLAME_APPEARANCE_TIMEOUT     4//максимальное время ожидания появления пламени при розжиге (стадия P), c                                   @ FLAME_APPEARANCE_TIMEOUT
#define PAR_FLAME_SENSOR_CALIBRATION     5//Калибровочное значение ИП, значения АЦП                                                                   @ FLAME SENSOR CALIBRATION Value
#define PAR_FRAME_SENSOR_CALIBRATION     6//Калибровочное значение датчика корпуса, значения АЦП                                                      @ FRAME SENSOR CALIBRATION Value
#define PAR_U_SUPPLY_NOMINAL             7//номинальное напряжение питания изделия (12/24В)                                                           @ Rated supply voltage (12/24 V)
#define PAR_U_SUPPLY_MIN                 8//минимальное значение напряжения питания, В*100                                                            @ Minimum supply voltage, V*100
#define PAR_U_SUPPLY_MAX                 9//максимальное значение напряжения питания, В*100                                                           @ Maximum supply voltage, V*100
#define PAR_DELTA_T_STOP                10//температура уставки <= температуры по датчику на X градусов (3)                                           @ DELTA_T_STOP
#define PAR_DELTA_T_RUN                 11//температура уставки >= температуры по датчику на Y градусов (3)                                           @ DELTA_T_RUN
#define PAR_ID1                         12///серийный номер. первые 4 байта                                                                            @ Device ID1
#define PAR_ID2                         13///серийный номер. вторые 4 байта                                                                            @ Device ID2
#define PAR_ID3                         14///серийный номер. третьи 4 байта                                                                            @ Device ID3
#define PAR_T_SETPOINT_ORIGINAL         15//уставка температуры жидкостного подогревателя (обычный режим работы)                                      @ T_SETPOINT ORIGINAL Mode
#define PAR_T_SETPOINT_WARMUP           16//уставка температуры жидкостного подогревателя (режим работы догреватель)                                  @ T_SETPOINT WARMUP Mode
#define PAR_T_SETPOINT_ECONOMY          17//уставка температуры жидкостного подогревателя (экономичный режим работы)                                  @ T_SETPOINT ECONOMY Mode
#define PAR_CAN_ADDRESS                 18//Адрес устройства в сети CAN                                                                               @ Device's CAN address
#define PAR_BRUSHLESS_FAN_CALIBRATION   19//Калибровочное значение безколлекторного двигателя                                                         @ Brushless motor's calibration value
#define PAR_SIGNALING_MODE              20//Режим запуска по каналу сигнализации: 0-откл., 1-имп., 2-защелка, 3-имп. и защелка                        @ Signaling_mode [0, 1, 2, 3]
#define PAR_ZONE1_ENABLE                21//Зона1 подключена (для систем отопления)                                                                   @ Zone1 enable
#define PAR_ZONE2_ENABLE                22//Зона2 подключена (для систем отопления)                                                                   @ Zone2 enable
#define PAR_ZONE3_ENABLE                23//Зона3 подключена (для систем отопления)                                                                   @ Zone3 enable
#define PAR_ZONE4_ENABLE                24//Зона4 подключена (для систем отопления)                                                                   @ Zone4 enable
#define PAR_ZONE5_ENABLE                25//Зона5 подключена (для систем отопления)                                                                   @ Zone5 enable
#define PAR_MAX_T_FLAME                 26//максимальная темепература выхлопных газов                                                                 @ Maximum flame temperature
#define PAR_FUEL_PERFORMANCE            27//производительность топливного насоса, мл/100 качков                                                       @ Fuel pump performance

#define PAR_HCU_ELEMENT_SETPOINT        28//уставка температуры ТЭНа для перехода в ждущий                                                            @ HCU_ELEMENT_SETPOINT
#define PAR_HCU_ELEMENT_ON              29//уставка температуры ТЭНа для выхода из ждущего                                                            @ HCU_ELEMENT_ON
#define PAR_HCU_ELEMENT_ON_DW           30//уставка температуры ТЭНа для выхода из ждущего при разборе воды                                           @ HCU_ELEMENT_ON_DW

#define PAR_HEATER_LIQUID_SETPOINT      31//уставка температуры жидкости подогревателя для перехода в ждущий                                          @ HEATER_LIQUID_SETPOINT
#define PAR_HEATER_LIQUID_IGNITION      32//уставка температуры жидкости подогревателя для выхода из ждущего                                          @ HEATER_LIQUID_IGNITION
#define PAR_HEATER_LIQUID_IGNITION_DW   33//уставка температуры жидкости подогревателя для выхода из ждущего при разборе воды при разборе воды        @ HEATER_LIQUID_IGNITION_DW

#define PAR_HCU_TANK_SETPOINT           34//уставка температуры бака для перехода в ждущий                                                            @ HCU_TANK_SETPOINT
#define PAR_HCU_TANK_IGNITION           35//уставка температуры бака для выхода из ждущего                                                            @ HCU_TANK_IGNITION
#define PAR_HCU_TANK_IGNITION_DW        36//уставка температуры бака для выхода из ждущего при разборе воды при разборе воды                          @ HCU_TANK_IGNITION_DW

#define PAR_DWATER_PRIORITY             37//приоритет бытовой воды в системе отопления                                                                @ Domestic water priority
#define PAR_DWATER_PAUSE                38//Пауза для начала разбора воды                                                                             @ Domestic water pause

                                        //=======дельта температуры переходов гистерезиса (вентиляторов отопления, мощностей подогревателей
#define PAR_TU0                         39//Дельта перехода вверх 0                                                                                   @ TU0
#define PAR_TU1                         40//Дельта перехода вверх 1                                                                                   @ TU1
#define PAR_TU2                         41//Дельта перехода вверх 2                                                                                   @ TU2
#define PAR_TU3                         42//Дельта перехода вверх 3                                                                                   @ TU3
#define PAR_TU4                         43//Дельта перехода вверх 4                                                                                   @ TU4
#define PAR_TU5                         44//Дельта перехода вверх 5                                                                                   @ TU5
#define PAR_TU6                         45//Дельта перехода вверх 6                                                                                   @ TU6
#define PAR_TU7                         46//Дельта перехода вверх 7                                                                                   @ TU7
#define PAR_TU8                         47//Дельта перехода вверх 8                                                                                   @ TU8
#define PAR_TU9                         48//Дельта перехода вверх 9                                                                                   @ TU9

#define PAR_TD0                         49//Дельта перехода вниз 0                                                                                    @ TD0
#define PAR_TD1                         50//Дельта перехода вниз 1                                                                                    @ TD1
#define PAR_TD2                         51//Дельта перехода вниз 2                                                                                    @ TD2
#define PAR_TD3                         52//Дельта перехода вниз 3                                                                                    @ TD3
#define PAR_TD4                         53//Дельта перехода вниз 4                                                                                    @ TD4
#define PAR_TD5                         54//Дельта перехода вниз 5                                                                                    @ TD5
#define PAR_TD6                         55//Дельта перехода вниз 6                                                                                    @ TD6
#define PAR_TD7                         56//Дельта перехода вниз 7                                                                                    @ TD7
#define PAR_TD8                         57//Дельта перехода вниз 8                                                                                    @ TD8
#define PAR_TD9                         58//Дельта перехода вниз 9                                                                                    @ TD9

#define PAR_ZONE1_PUMP_NUM              59//Номер помпы для зоны 1                                                                                    @ Zone1 water pump number
#define PAR_ZONE2_PUMP_NUM              60//Номер помпы для зоны 2                                                                                    @ Zone2 water pump number
#define PAR_ZONE3_PUMP_NUM              61//Номер помпы для зоны 3                                                                                    @ Zone3 water pump number
#define PAR_ZONE4_PUMP_NUM              62//Номер помпы для зоны 4                                                                                    @ Zone4 water pump number
#define PAR_ZONE5_PUMP_NUM              63//Номер помпы для зоны 5                                                                                    @ Zone5 water pump number

#define PAR_FAN1_PWM_LIM                64//Ограничение ШИМ вентилятора зоны 1, %                                                                     @ Fan1_PWM_limitation, %
#define PAR_FAN2_PWM_LIM                65//Ограничение ШИМ вентилятора зоны 2, %                                                                     @ Fan2_PWM_limitation, %
#define PAR_FAN3_PWM_LIM                66//Ограничение ШИМ вентилятора зоны 3, %                                                                     @ Fan3_PWM_limitation, %
#define PAR_FAN4_PWM_LIM                67//Ограничение ШИМ вентилятора зоны 4, %                                                                     @ Fan4_PWM_limitation, %
#define PAR_FAN5_PWM_LIM                68//Ограничение ШИМ вентилятора зоны 5, %                                                                     @ Fan5_PWM_limitation, %

#define PAR_FANS_PWM_CORRECT_TIME       69//Периодичность корректировки ШИМ вентиляторов, мс                                                          @ Fans PWM correction time, ms
#define PAR_TIME_FORMAT                 70//Формат времени (12/24 или AM/PM)                                                                          @ Time format (12/24 or AM/PM)
#define PAR_T_UNITS                     71//Единицы измерения температуры (C/F)                                                                       @ Temperature units (C/F)
#define PAR_DAYTIME_FROM                72//Начало дневного времени суток, мин                                                                        @ DAYTIME_FROM
#define PAR_DAYTIME_TO                  73//Окончание дневного времени суток, мин                                                                     @ DAYTIME_TO
#define PAR_BRIGHTNESS_DAY              74//Яркость дисплея днем, %                                                                                   @ BRIGHTNESS_DAY
#define PAR_BRIGHTNESS_NIGHT            75//Яркость дисплея ночью, %                                                                                  @ BRIGHTNESS_NIGHT

#define PAR_T_RELAY_SETPOINT            76//Температура включения печки салона автомобиля                                                             @ T_RELAY_SETPOINT
#define PAR_WAIT_MODE_PUMP              77//Работа помпы в ждущем режиме: 0 - не работает, иначе - работает                                           @ WAIT_MODE_PUMP
#define PAR_ENGINE_PUMP                 78//Работа помпы при заведенном двигателе: 1 - работает, иначе - не работает                                  @ ENGINE_PUMP
#define PAR_WARMUP_MODE                 79//Режим догревателя: 0 - отключен, 1 - автоматический, 2 - ручной                                           @ WARMUP_MODE
#define PAR_RUN_ON_LATCHED              80//запускаться или нет, если сигналка в сработанном состоянии после остановки                                @ RUN_ON_LATCHED

#define PAR_DT_IGNITION_ORIGIN          81//обычный режим: разница температур между уставкой и началом розжига                                        @ DT_IGNITION_ORIGIN
#define PAR_DT_IGNITION_ECONOMY         82//экономичный режим: разница температур между уставкой и началом розжига                                    @ DT_IGNITION_ECONOMY
#define PAR_DT_IGNITION_WARMUP          83//догреватель: разница температур между уставкой и началом розжига                                          @ DT_IGNITION_WARMUP

#define PAR_POWER_LEVEL_MIN             84//минимальная ступень мощности                                                                              @ POWER_LEVEL_MIN
#define PAR_POWER_LEVEL_MAX             85//максимальная ступень мощности                                                                             @ POWER_LEVEL_MAX
#define PAR_PERCENT_ENGINE              86//проценты от напряжения питания для определения заведенного двигателя                                      @ PERCENT_ENGINE

#define PAR_T_SETPOINT_HEATING          87//уставка температуры жидкостного подогревателя (отопление)                                                 @ T_SETPOINT_HEATING
#define PAR_DT_IGNITION_HEATING         88//режим отопления: разница температур между уставкой и началом розжига                                      @ DT_IGNITION_HEATING
#define PAR_LANGUAGE                    89//Язык: 0 - английский, 1 - русский                                                                         @ Language
#define PAR_SEND_SMS_ON_CALL            90//Отправлять смс при звонке: 0 - не отправлять, 1 - отправлять                                              @ Send SMS on call
#define PAR_INTERNET_SETTINGS           91//настройки интернета                                                                                       @ Internet settings
#define PAR_ON_SIGNALING_OPERATE        92//параметры работы при срабатывании сигнализации: 0 - по умолчанию, 1 - по сохраненным, 2 - по последним    @ ON_SIGNALING_OPERATE
#define PAR_SAVED_RUNNING_TIME          93//сохраненное значение времени работы при срабатывании сигнализации, минуты                                 @ SAVED_RUNNING_TIME
#define PAR_SAVED_MODE                  94//сохраненное значение режима работы при срабатывании сигнализации: 0 - обычный, 1 - экономичный, 3 - отопление, 4 - отопительные системы @ SAVED_MODE
#define PAR_FAN_MANUAL_MODE             95//настройка ручного режима вентиляции: 0 - выключение вентиляторов при достижении заданной температуры; 1 - вентилятор не останавливается @ FAN_MANUAL_MODE

#define PAR_HIGH_VOLTAGE_FAULT_TIME     96//Время возникновения неисправности при повышенном напряжении, *200мс                                       @ Time of occurrence of a fault at high voltage, *200ms
#define PAR_LOW_VOLTAGE_FAULT_TIME      97//Время возникновения неисправности при пониженном напряжении, *200мс                                       @ Time of occurrence of a fault at low voltage, *200ms

#define PAR_PCB_TEMP_OR_ENGINE          98//Плата с подключением датчика темп. или сигнала завед. двигателя: 0-двигатель; 1-датчик температуры        @ PCB_TEMP_OR_ENGINE

#define PAR_FANS_DT_START               99//падение температуры от уставки при котором вентиляторы зон начинают работать                              @ Delta T when zone's fans start (for heating systems)
#define PAR_PUMP_ON_ELEMENT            100//включать помпу изделия при работе ТЭНа: 1 - включать, 0 - выключать                                       @ Water pump of the heater: 1 - ON, 0 - OFF

#define PAR_BOILER_TEMP                101//температура бойлера                           @ BOILER_TEMP
#define PAR_BOILER_TIME_KEEPWARM       102//время поддержания температуры в бойлере       @ BOILER_TIME_KEEPWARM
#define PAR_BOILER_HYST_TEMP           103//бойлер. гистерезис температуры                @ BOILER_HYST_TEMP
#define PAR_BOILER_HOTMODE             104//бойлер HOTMODE                                @ BOILER_HOTMODE
#define PAR_MODE_BOILER                105//вид системы: 1 - накопительная, 0 - проточная @ System type: 1 - accumulative, 0 - running
#define PAR_TYPE_AIRHEATER             106//вид отопления: 1-конвекторы, 0-воздушники     @ AirHeaterType: 1 - convectors, 0 - fans
#define PAR_BOILER_TEMP_UPDATE_TIME    107//время работы помпы бойлера для нагрева датчика температуры в режиме Keep warm, 10-120 секунд.  @ Boiler temp update time 10-120 sec
#define PAR_TIMER1_TIME                110//TIMER1_TIME                                   @ TIMER1_TIME
#define PAR_TIMER1_WEEK                111//TIMER1_WEEK                                   @ TIMER1_WEEK
#define PAR_TIMER1_STATE               112//TIMER1_STATE                                  @ TIMER1_STATE
#define PAR_TIMER2_TIME                113//TIMER2_TIME                                   @ TIMER2_TIME
#define PAR_TIMER2_WEEK                114//TIMER2_WEEK                                   @ TIMER2_WEEK
#define PAR_TIMER2_STATE               115//TIMER2_STATE                                  @ TIMER2_STATE
#define PAR_TIMER3_TIME                116//TIMER3_TIME                                   @ TIMER3_TIME
#define PAR_TIMER3_WEEK                117//TIMER3_WEEK                                   @ TIMER3_WEEK
#define PAR_TIMER3_STATE               118//TIMER3_STATE                                  @ TIMER3_STATE
#define PAR_FAN1_MANUAL                120//FAN1_MANUAL                                   @ FAN1_MANUAL
#define PAR_FAN2_MANUAL                121//FAN2_MANUAL                                   @ FAN2_MANUAL
#define PAR_FAN3_MANUAL                122//FAN3_MANUAL                                   @ FAN3_MANUAL
#define PAR_FAN4_MANUAL                123//FAN4_MANUAL                                   @ FAN4_MANUAL
#define PAR_FAN5_MANUAL                124//FAN5_MANUAL                                   @ FAN5_MANUAL
//      PAR_FAN6_MANUAL                125//FAN6_MANUAL                                   @ FAN6_MANUAL
//      PAR_FAN7_MANUAL                126//FAN7_MANUAL                                   @ FAN7_MANUAL
//      PAR_FAN8_MANUAL                127//FAN8_MANUAL                                   @ FAN8_MANUAL
//      PAR_FAN9_MANUAL                128//FAN9_MANUAL                                   @ FAN9_MANUAL
//      PAR_FAN10_MANUAL               129//FAN9_MANUAL                                   @ FAN10_MANUAL

#define PAR_CHECKING_ZONE_FANS         130//Проверка неисправностей вентиляторов зон: 0 - не проверять, 1 - проверять        @ Checking zone fans for faults
#define PAR_USE_PRESSURE_MEASUREMENT   131//Использовать измеренное давление для корректировки ТН: 0 - не использовать, 1 - использовать @ Use pressure measurement for the fuel pump correction
#define PAR_SET_DELAY_MAX_VOLTAGE   	 132//Установить значение реакции на пониженное напряжение @ Set delay time to determine overvoltage error
#define PAR_SET_DELAY_MIN_VOLTAGE   	 133//Установить значение реакции на повышенное напряжение @ Set delay time to determine undervoltage error

                                      //134
                                      //...
                                      //500

#define PAR_DISPLAY_ZONE1_DAY_T        501//Уставка дневной температуры зоны 1, С         @ DISPLAY_ZONE1_DAY_T
#define PAR_DISPLAY_ZONE2_DAY_T        502//Уставка дневной температуры зоны 2, С         @ DISPLAY_ZONE2_DAY_T
#define PAR_DISPLAY_ZONE3_DAY_T        503//Уставка дневной температуры зоны 3, С         @ DISPLAY_ZONE3_DAY_T
#define PAR_DISPLAY_ZONE4_DAY_T        504//Уставка дневной температуры зоны 4, С         @ DISPLAY_ZONE4_DAY_T
#define PAR_DISPLAY_ZONE5_DAY_T        505//Уставка дневной температуры зоны 5, С         @ DISPLAY_ZONE5_DAY_T

#define PAR_DISPLAY_ZONE1_NIGHT_T      506//Уставка ночной температуры зоны 1, С          @ DISPLAY_ZONE1_NIGHT_T
#define PAR_DISPLAY_ZONE2_NIGHT_T      507//Уставка ночной температуры зоны 2, С          @ DISPLAY_ZONE2_NIGHT_T
#define PAR_DISPLAY_ZONE3_NIGHT_T      508//Уставка ночной температуры зоны 3, С          @ DISPLAY_ZONE3_NIGHT_T
#define PAR_DISPLAY_ZONE4_NIGHT_T      509//Уставка ночной температуры зоны 4, С          @ DISPLAY_ZONE4_NIGHT_T
#define PAR_DISPLAY_ZONE5_NIGHT_T      510//Уставка ночной температуры зоны 5, С          @ DISPLAY_ZONE5_NIGHT_T
//---------------- NEW PARAMS BEW !!!!! -----------------------
#define PAR_DISPLAY_ZONE1_FAN_MANUAL_VALUE      520//Мощность воздушника в ручном режиме зоны 1, %          @ DISPLAY_ZONE1_FAN_MANUAL_VALUE
#define PAR_DISPLAY_ZONE2_FAN_MANUAL_VALUE      521//Мощность воздушника в ручном режиме зоны 2, %          @ DISPLAY_ZONE2_FAN_MANUAL_VALUE
#define PAR_DISPLAY_ZONE3_FAN_MANUAL_VALUE      522//Мощность воздушника в ручном режиме зоны 3, %          @ DISPLAY_ZONE3_FAN_MANUAL_VALUE
#define PAR_DISPLAY_ZONE4_FAN_MANUAL_VALUE      523//Мощность воздушника в ручном режиме зоны 4, %          @ DISPLAY_ZONE4_FAN_MANUAL_VALUE
#define PAR_DISPLAY_ZONE5_FAN_MANUAL_VALUE      524//Мощность воздушника в ручном режиме зоны 5, %          @ DISPLAY_ZONE5_FAN_MANUAL_VALUE

#define PAR_DISPLAY_ZONE1_FAN_MANUAL            525//Режим работы воздушника: 1-ручной; 0 - авто.           @ DISPLAY_ZONE1_FAN_AUTO
#define PAR_DISPLAY_ZONE2_FAN_MANUAL            526//Режим работы воздушника: 1-ручной; 0 - авто.           @ DISPLAY_ZONE2_FAN_AUTO
#define PAR_DISPLAY_ZONE3_FAN_MANUAL            527//Режим работы воздушника: 1-ручной; 0 - авто.           @ DISPLAY_ZONE3_FAN_AUTO
#define PAR_DISPLAY_ZONE4_FAN_MANUAL            528//Режим работы воздушника: 1-ручной; 0 - авто.           @ DISPLAY_ZONE4_FAN_AUTO
#define PAR_DISPLAY_ZONE5_FAN_MANUAL            529//Режим работы воздушника: 1-ручной; 0 - авто.           @ DISPLAY_ZONE5_FAN_AUTO

#define PAR_DISPLAY_SYSTEM_LIMIT_WORKTIME       530//Максимальное время работы системы, часов               @ DISPLAY_SYSTEM_LIMIT_WORKTIME
#define PAR_DISPLAY_TIMEOUT                     531//Таймаут подсветки дисплея, секунд                      @ DISPLAY_TIMEOUT
#define PAR_DISPLAY_SYSTEM_STORAGE_MODE         532//Режим хранения системы от промерзания, вкл/выкл        @ DISPLAY_SYSTEM_STORAGE_MODE

#define PAR_HCU_ELEMENT_SETPOINT_STORAGE        540//уставка температуры бака для перехода ТЭНа в ждущий в режиме StorageMode  @ HCU_ELEMENT_SETPOINT
#define PAR_HCU_ELEMENT_IGNITION_STORAGE        541//уставка температуры бака для выхода ТЭНа из ждущего в режиме StorageMode  @ HCU_ELEMENT_IGNITION
#define PAR_HCU_TANK_SETPOINT_STORAGE           542//уставка температуры жидкости бака для перехода котла в ждущий в режиме StorageMode    @ HEATER_LIQUID_SETPOINT
#define PAR_HCU_TANK_IGNITION_STORAGE           543//уставка температуры жидкости бака для выхода котла из ждущего в режиме StorageMode    @ HEATER_LIQUID_IGNITION

#define PAR_RVC_INSTANCE                        600// номер устройства на шине CAN RVC                     @ RVC_DEVICE_INSTANCE

/*----------------------------------------------------------------------------------------------------------------------------------
                                   Номера (имена) общих параметров черного ящика
----------------------------------------------------------------------------------------------------------------------------------*/
#define BB_RUN_CNT                       1//счетчик запусков
#define BB_RUN_VENT_CNT                  2//счетчик запусков на вентиляции
#define BB_RUN_OK_CNT                    3//счетчик запусков завершенных без неисправностей
#define BB_RUN_ERR_CNT                   4//счетчик запусков завершенных с неисправностями
#define BB_WORK_FLAG                     5//флаг работы подогревателя для фиксации сброса питания
#define BB_PWR_BREAK                     6//счетчик сбросов питания во время работы
#define BB_TOTAL_TIME                    7//общее время работы
#define BB_POWER_0_TIME                  8//время работы на 0 мощности
#define BB_POWER_1_TIME                  9//время работы на 1 мощности
#define BB_POWER_2_TIME                 10//время работы на 2 мощности
#define BB_POWER_3_TIME                 11//время работы на 3 мощности
#define BB_POWER_4_TIME                 12//время работы на 4 мощности
#define BB_POWER_5_TIME                 13//время работы на 5 мощности
#define BB_POWER_6_TIME                 14//время работы на 6 мощности
#define BB_POWER_7_TIME                 15//время работы на 7 мощности
#define BB_POWER_8_TIME                 16//время работы на 8 мощности
#define BB_POWER_9_TIME                 17//время работы на 9 мощности
#define BB_VENT_TIME                    18//время работы на вентиляции
#define BB_WAIT_MODE_TIME               19//время работы на ждущем режиме
#define BB_RUN_FIRST_CNT                20//количество запусков с первой попытки
#define BB_RUN_SECOND_CNT               21//количество запусков со второй попытки
#define BB_NO_IGNITION_CNT              22//количество незапусков (попытки розжига исчерпаны)

#define BB_POWER_LOW_TIME               23//время работы на малой мощности
#define BB_POWER_MIDDLE_TIME            24//время работы на средней мощности
#define BB_POWER_HIGH_TIME              25//время работы на максимальной мощности

#define BB_ELEMENT_TIME                 26//время работы ТЭНа, с
#define BB_POWER_10_TIME                27//время работы на 10 мощности
#define BB_RUN_PUMP_CNT                 28//счетчик запусков помпы
/*----------------------------------------------------------------------------------------------------------------------------------
                                   Номера (имена) параметров, переменных, ...
----------------------------------------------------------------------------------------------------------------------------------*/
//                                   Формат вывода целых чисел: %.0f. Если по формуле получится дробное число, при выводе оно будет округлено по правилам математики
//                                   В формуле пересчета вместо V (или v) подставляется значение, полученное в пакете
//Формат записи:                                     ;                                              ; Формула пересчета; Формат вывода; Имя в таблице; Цвет
#define VAR_STAGE                        1//uint8_t  ;Стадия                                        ; V                ; %.0f           ; Стадия       ;
#define VAR_MODE                         2//uint8_t  ;Режим работы                                  ; V                ; %.0f           ; Режим        ;
#define VAR_RUNNING_TIME                 3//uint32_t ;Общее время работы, c                         ; V                ; %.0f           ; Вр.Работы    ;
#define VAR_MODE_TIME                    4//uint32_t ;Время работы на режиме, c                     ; V                ; %.0f           ; Вр.Реж.      ;

#define VAR_U_SUPPLY                     5//uint16_t ;Напряжение питания, В*100                     ; V/100            ; %.2f           ; U, В         ;
#define VAR_T_FLAME                      6//int16_t  ;Температура ИП                                ; V                ; %.0f           ; Тплам        ;
#define VAR_T_FRAME                      7//int16_t  ;Температура корп                              ; V                ; %.0f           ; Ткорп        ;
#define VAR_T_PANEL                      8//int8_t   ;Температура датчика в ПУ                      ; V                ; %.0f           ; Тпульта      ;
#define VAR_T_EXT                        9//int16_t  ;Температура кабинного датчика                 ; V                ; %.0f           ; Твнешн       ;
#define VAR_T_INLET                     10//int16_t  ;Температура на входе                          ; V                ; %.0f           ; Твхода       ;
#define VAR_T_SETPOINT                  11//int8_t   ;Температура уставки                           ; V                ; %.0f           ; Туставки     ;
#define VAR_PWR_DEFINED                 12//uint8_t  ;Заданная мощность                             ; V                ; %.0f           ; Р зад        ;
#define VAR_PWR_REALIZED                13//uint8_t  ;Текущая мощность                              ; V                ; %.0f           ; Р тек        ;
#define VAR_WORK_BY                     14//uint8_t  ;По какому датчику работал                     ; V                ; %.0f           ; Работа по    ;
#define VAR_FAN_REV_DEFINED             15//uint8_t  ;Заданные обороты НВ                           ; V                ; %.0f           ; Об.зад       ;
#define VAR_FAN_REV_MEASURED            16//uint8_t  ;Измеренные обороты НВ                         ; V                ; %.0f           ; Об. изм      ;
#define VAR_FP_FREQ_DEFINED             17//uint16_t ;Заданная частота ТН, Гц*100                   ; V/100            ; %.2f           ; ТН зад       ;
#define VAR_FP_FREQ_REALIZED            18//uint16_t ;Реализованная частота ТН, Гц*100              ; V/100            ; %.2f           ; ТН тек       ;
#define VAR_GP_PERIOD                   19//uint32_t ;Период свечи                                  ; V                ; %.0f           ; Св. пер      ;
#define VAR_GP_PULSE                    20//uint32_t ;Импульс свечи                                 ; V                ; %.0f           ; Св. имп      ;
#define VAR_GP_PERCENT                  21//uint8_t  ;Заданные проценты мощности свечи              ; V                ; %.0f           ; Св. %        ;
#define VAR_FLAME_CALIBRATION           22//uint16_t ;Калибровочное значение термопары ИП           ; V                ; %.0f           ; К.З. ИП      ;
#define VAR_FRAME_CALIBRATION           23//uint16_t ;Калибровочное значение термопары корпуса      ; V                ; %.0f           ; К.З. Корп    ;

#define VAR_FAULT_CODE                  24//uint8_t  ;Код неисправности                             ; V                ; %.0f           ; Неиспр       ;
#define VAR_FAULT_BLINK_CODE            25//uint8_t  ;Код неисправности. Количество морганий (миганий); V              ; %.0f           ; Неиспр.Миг   ;
#define VAR_FAULT_BYTE0                 26//uint8_t  ;Байт неисправностей1                          ; V                ; %.0f           ; Б.неиспр0    ;
#define VAR_FAULT_BYTE1                 27//uint8_t  ;Байт неисправностей2                          ; V                ; %.0f           ; Б.неиспр1    ;
#define VAR_FAULT_BYTE2                 28//uint8_t  ;Байт неисправностей3                          ; V                ; %.0f           ; Б.неиспр2    ;
#define VAR_FAULT_BYTE3                 29//uint8_t  ;Байт неисправностей4                          ; V                ; %.0f           ; Б.неиспр3    ;
#define VAR_FAULT_BYTE4                 30//uint8_t  ;Байт неисправностей5                          ; V                ; %.0f           ; Б.неиспр4    ;
#define VAR_FAULT_BYTE5                 31//uint8_t  ;Байт неисправностей6                          ; V                ; %.0f           ; Б.неиспр5    ;
#define VAR_FAULT_BYTE6                 32//uint8_t  ;Байт неисправностей7                          ; V                ; %.0f           ; Б.неиспр6    ;
#define VAR_FAULT_BYTE7                 33//uint8_t  ;Байт неисправностей8                          ; V                ; %.0f           ; Б.неиспр7    ;

#define VAR_IGNITION_ATTEMPT            34//uint8_t  ;Попытка розжига                               ; V                ; %.0f           ; ПопыткаРозж  ;
#define VAR_WAIT_MODE_ALLOWED           35//uint8_t  ;Разрешен ждущий режим                         ; V                ; %.0f           ; Ждущ.Реж     ;
#define VAR_FLAME_MINIMUM               36//int16_t  ;Минимальное значение ИП                       ; V                ; %.0f           ; ИП min       ;
#define VAR_FLAME_LIMIT                 37//int16_t  ;Граница срыва пламени                         ; V                ; %.0f           ; ИП lim       ;
#define VAR_FLAME_BREAK_NUM             38//uint8_t  ;Количество срывов пламени во время работы     ; V                ; %.0f           ; Кол.Ср.Пл    ;
#define VAR_OVERHEAT_TBORDER            39//int16_t  ;Граница перегрева                             ; V                ; %.0f           ; Гр.Перегр    ;
#define VAR_T_LIQUID                    40//int16_t  ;Температура жидкости                          ; V                ; %.0f           ; Тж           ;
#define VAR_T_OVERHEAT                  41//int16_t  ;Температура перегрева                         ; V                ; %.0f           ; Тпер         ;
#define VAR_DT_FLAME                    42//int16_t  ;Изменение температуры пламени                 ; V                ; %.0f           ; DT ИП        ;
#define VAR_DT_LIQUID                   43//int16_t  ;Изменение температуры жидкости                ; V                ; %.0f           ; DT Тж        ;
#define VAR_DT_OVERHEAT                 44//int16_t  ;Изменение температуры перегрева               ; V                ; %.0f           ; DT Тпер      ;
#define VAR_RELAY_STATE                 45//uint8_t  ;Состояние реле (ВКЛ/ВЫКЛ)                     ; V                ; %.0f           ; Реле         ;
#define VAR_PUMP_STATE                  46//uint8_t  ;Состояние помпы (ВКЛ/ВЫКЛ)                    ; V                ; %.0f           ; Помпа        ;
#define VAR_SIGNALING_STATE             47//uint8_t  ;Состояние сигнализации (сигнал есть/нет)      ; V                ; %.0f           ; Сигнализация ;
#define VAR_ENGINE_STATE                48//uint8_t  ;Состояние двигателя (заведен/остановлен)      ; V                ; %.0f           ; Двигатель    ;

#define VAR_ADC_CH0                     49//uint16_t ;АЦП канал 0                                   ; V                ; %.0f           ; АЦП 0        ;
#define VAR_ADC_CH1                     50//uint16_t ;АЦП канал 1                                   ; V                ; %.0f           ; АЦП 1        ;
#define VAR_ADC_CH2                     51//uint16_t ;АЦП канал 2                                   ; V                ; %.0f           ; АЦП 2        ;
#define VAR_ADC_CH3                     52//uint16_t ;АЦП канал 3                                   ; V                ; %.0f           ; АЦП 3        ;
#define VAR_ADC_CH4                     53//uint16_t ;АЦП канал 4                                   ; V                ; %.0f           ; АЦП 4        ;
#define VAR_ADC_CH5                     54//uint16_t ;АЦП канал 5                                   ; V                ; %.0f           ; АЦП 5        ;
#define VAR_ADC_CH6                     55//uint16_t ;АЦП канал 6                                   ; V                ; %.0f           ; АЦП 6        ;
#define VAR_ADC_CH7                     56//uint16_t ;АЦП канал 7                                   ; V                ; %.0f           ; АЦП 7        ;
#define VAR_ADC_CH8                     57//uint16_t ;АЦП канал 8                                   ; V                ; %.0f           ; АЦП 8        ;
#define VAR_ADC_CH9                     58//uint16_t ;АЦП канал 9                                   ; V                ; %.0f           ; АЦП 9        ;

#define VAR_T_CPU                       59//int16_t  ;Температура процессора                        ; V                ; %.0f           ; Тcpu         ;
#define VAR_PRESSURE                    60//uint8_t  ;Атмосферное давление, кПа                     ; V                ; %.0f           ; Давл.,кПа    ;
#define VAR_T_TANK                      61//int16_t  ;Температура бака в системах отопления         ; V                ; %.0f           ; Тбака        ;

#define VAR_GLOWPLUG_STATE              62//uint8_t  ;Состояние свечи (ВКЛ/ВЫКЛ)                     ; V                ; %.0f           ; Реле         ;

#define VAR_START_BB_ERR_RECORD 0xFFFFFFFA//uint32_t ;Начало записи о неисправности в ЧЯ
//#define VAR_                            //uint_t


/*----------------------------------------------------------------------------------------------------------------------------------
                                   Стадии и режимы работы
----------------------------------------------------------------------------------------------------------------------------------*/
#define STAGE_Z                          0                   //Стадия Z  @ Stage Z
#define STAGE_P                          1                   //Стадии P  @ Stage P
#define STAGE_H                          2                   //Стадии H  @ Stage H
#define STAGE_W                          3                   //Стадии W  @ Stage W
#define STAGE_F                          4                   //Стадии F  @ Stage F
#define STAGE_T                          5                   //Стадии T  @ Stage T
#define STAGE_M                          6                   //Стадии M  @ Stage M

#define Z_MODE_1_WAIT_COMMAND            1                   //Ожидание команды  @ Waiting for the command
#define Z_MODE_3_BLOWING_ON_POWER        3                   //Проверка при подаче питания (индиктора пламени/датчика корпуса на неостылость) @ Blowing on power on
#define Z_MODE_5_SAVE_DATA               5                   //Сохранение данных о пуске после завершения работы  @ Saving data

#define P_MODE_0_START_DIAGNOSTIC        0                   //Стартовая диагностика                              @ Initial diagnostics
#define P_MODE_1_FAN_STARTING            1                   //Страгивание нагнетателя воздуха                    @ Fan starting
#define P_MODE_2_WAITING_T_DECREASE      2                   //Ожидание снижения температуры (жидкости у жидкостных или воздуха у воздушных @ Waiting for the temperature decrease
#define P_MODE_3_BLOWING_ON_START        3                   //Проверка датчика корпуса на неостылость при пуске  @ Blowing on start
#define P_MODE_4_WAITING                 4                   //Ждущий                                             @ Waiting

#define H_MODE_0_GLOW_PLUG_HEATING       0                   //Разогрев свечи                                     @ Glow plug heating
#define H_MODE_1_IGNITION1               1                   //Первая попытка розжига                             @ Ignition 1
#define H_MODE_2_BLOWING_BETWEEN_IGNITIONS 2                 //Продувка между розжигами                           @ Blowing
#define H_MODE_3_IGNITION2               3                   //Вторая попытка розжига                             @ Ignition 2
#define H_MODE_4_HEATING                 4                   //Прогрев камеры сгорания                            @ Heating
#define H_MODE_5_FLAME_BREAK_BLOWING     5                   //Продувка после срыва пламени на прогреве           @ Blowing on flame break
#define H_MODE_6_BLOWING_BEFORE_WAIT     6                   //Продувка перед ждущим                              @ Blowing before waiting
#define H_MODE_7_PREPARING               7                   //Подготовка к розжигу                               @ Preparing for ignition

#define W_MODE_0_POWER0_FROM_TOP         0                   //Работа на 0 ступени мощности, движемся сверху      @ Power level 0
#define W_MODE_1_POWER1_FROM_TOP         1                   //Работа на 1 ступени мощности, движемся сверху      @ Power level 1
#define W_MODE_2_POWER2_FROM_TOP         2                   //Работа на 2 ступени мощности, движемся сверху      @ Power level 2
#define W_MODE_3_POWER3_FROM_TOP         3                   //Работа на 3 ступени мощности, движемся сверху      @ Power level 3
#define W_MODE_4_POWER4_FROM_TOP         4                   //Работа на 4 ступени мощности, движемся сверху      @ Power level 4
#define W_MODE_5_POWER5_FROM_TOP         5                   //Работа на 5 ступени мощности, движемся сверху      @ Power level 5
#define W_MODE_6_POWER6_FROM_TOP         6                   //Работа на 6 ступени мощности, движемся сверху      @ Power level 6
#define W_MODE_7_POWER7_FROM_TOP         7                   //Работа на 7 ступени мощности, движемся сверху      @ Power level 7
#define W_MODE_8_POWER8_FROM_TOP         8                   //Работа на 8 ступени мощности, движемся сверху      @ Power level 8
#define W_MODE_9_POWER9_FROM_TOP         9                   //Работа на 9 ступени мощности, движемся сверху      @ Power level 9
#define W_MODE_10_POWER10_FROM_TOP      10                   //Работа на 10 ступени мощности, движемся сверху     @ Power level 10
#define W_MODE_11_POWER1_FROM_BOTTOM    11                   //Работа на 1 ступени мощности, движемся снизу       @ Power level 1
#define W_MODE_12_POWER2_FROM_BOTTOM    12                   //Работа на 2 ступени мощности, движемся снизу       @ Power level 2
#define W_MODE_13_POWER3_FROM_BOTTOM    13                   //Работа на 3 ступени мощности, движемся снизу       @ Power level 3
#define W_MODE_14_POWER4_FROM_BOTTOM    14                   //Работа на 4 ступени мощности, движемся снизу       @ Power level 4
#define W_MODE_15_POWER5_FROM_BOTTOM    15                   //Работа на 5 ступени мощности, движемся снизу       @ Power level 5
#define W_MODE_16_POWER6_FROM_BOTTOM    16                   //Работа на 6 ступени мощности, движемся снизу       @ Power level 6
#define W_MODE_17_POWER7_FROM_BOTTOM    17                   //Работа на 7 ступени мощности, движемся снизу       @ Power level 7
#define W_MODE_18_POWER8_FROM_BOTTOM    18                   //Работа на 8 ступени мощности, движемся снизу       @ Power level 8
#define W_MODE_19_POWER9_FROM_BOTTOM    19                   //Работа на 9 ступени мощности, движемся снизу       @ Power level 9
#define W_MODE_20_POWER10_FROM_BOTTOM   20                   //Работа на 10 ступени мощности, движемся снизу      @ Power level 10
#define W_MODE_21_BLOWING_VT            21                   //Продувка перед вентиляцией                         @ Blowing
#define W_MODE_22_BLOWING_FLAME_BREAK   22                   //Продувка при срыве пламени                         @ Blowing on flaeme break
#define W_MODE_23_OVERHEAT              23                   //Продувка и вентиляция при перегреве                @ Blowing on overheat
//#define W_MODE_24_PUMP_ONLY           24                   //Только помпа                                       @ Water pump only
#define W_MODE_25_LOW                   25                   //Малый                                              @ Low power
#define W_MODE_26_MIDDLE                26                   //Средний                                            @ Middle power
#define W_MODE_27_HIGH                  27                   //Сильный                                            @ High power
#define W_MODE_28_BLOWING               28                   //Продувка перед ждущим                              @ Blowing before waiting
#define W_MODE_29_WAIT                  29                   //Ждущий                                             @ Waiting
#define W_MODE_30_PUMP_ONLY             30                   //Только помпа                                       @ Water pump only
#define W_MODE_31_BOOST                 31                   //Ускоренный нагрев                                  @ Boost mode
#define W_MODE_32_IGNITION_BY_PRESSURE_AFTER_FLAME_BREAK 32  //Розжиг по датчику давления после срыва пламени во время работы @ Ignition by pressure after flame break
#define W_MODE_34_CALIBRATION_BEFORE_VENTILATION 34          //Автокаллибровка перед вентиляцией                  @ Calibration
#define W_MODE_35_VENTILATION           35                   //Вентиляция                                         @ Ventilation

#define W_MODE_40_VENTILATION0          40                   //Вентиляция на 0 мощности                           @ Ventilation level 0
#define W_MODE_41_VENTILATION1          41                   //Вентиляция на 1 мощности                           @ Ventilation level 1
#define W_MODE_42_VENTILATION2          42                   //Вентиляция на 2 мощности                           @ Ventilation level 2
#define W_MODE_43_VENTILATION3          43                   //Вентиляция на 3 мощности                           @ Ventilation level 3
#define W_MODE_44_VENTILATION4          44                   //Вентиляция на 4 мощности                           @ Ventilation level 4
#define W_MODE_45_VENTILATION5          45                   //Вентиляция на 5 мощности                           @ Ventilation level 5
#define W_MODE_46_VENTILATION6          46                   //Вентиляция на 6 мощности                           @ Ventilation level 6
#define W_MODE_47_VENTILATION7          47                   //Вентиляция на 7 мощности                           @ Ventilation level 7
#define W_MODE_48_VENTILATION8          48                   //Вентиляция на 8 мощности                           @ Ventilation level 8
#define W_MODE_49_VENTILATION9          49                   //Вентиляция на 9 мощности                           @ Ventilation level 9

#define W_MODE_50_POWER0                50                   //Режим работы на 0 ступени мощности                 @ Power level 0
#define W_MODE_51_POWER1                51                   //Режим работы на 1 ступени мощности                 @ Power level 1
#define W_MODE_52_POWER2                52                   //Режим работы на 2 ступени мощности                 @ Power level 2
#define W_MODE_53_POWER3                53                   //Режим работы на 3 ступени мощности                 @ Power level 3
#define W_MODE_54_POWER4                54                   //Режим работы на 4 ступени мощности                 @ Power level 4
#define W_MODE_55_POWER5                55                   //Режим работы на 5 ступени мощности                 @ Power level 5
#define W_MODE_56_POWER6                56                   //Режим работы на 6 ступени мощности                 @ Power level 6
#define W_MODE_57_POWER7                57                   //Режим работы на 7 ступени мощности                 @ Power level 7
#define W_MODE_58_POWER8                58                   //Режим работы на 8 ступени мощности                 @ Power level 8
#define W_MODE_59_POWER9                59                   //Режим работы на 9 ступени мощности                 @ Power level 9

#define F_MODE_0_BLOWING                 0                   //Нормальное завершения работы                       @ Shutting down
#define F_MODE_1_BLOWING_OVERHEAT        1                   //Продувка при перегреве                             @ Shutting down
#define F_MODE_2_BLOWING_BR_SENSORS      2                   //Продувка при обрыве датчиков ИП или корпуса        @ Shutting down
#define F_MODE_3_BLOWING_FAULT           3                   //Продувка при  неисправностях помпы, ТН, НВ или превышении количества срывов пламени @ Shutting down
#define F_MODE_4_BLOWING_NO_FP           4                   //Завершение работы без ТН                           @ Shutting down
#define F_MODE_5_BLOWING_NO_GP           5                   //Завершение работы без свечи и ТН                   @ Shutting down

#define T_MODE_0_PCB_TESTING             0                   //Проверка платы                                     @ PCB testing
#define T_MODE_1_FAN_CALIBRATION         1                   //Калибровка коллекторного НВ                        @ Motor calibration

#define M_MODE_0_MANUAL                  0                   //Ручной режим работы. Параметры работы задаются по последовательному каналу @ Manual mode

//=============================================================режимы работы для отображения на пульте управления
#define PANEL_MODE_STOPPED               0                   //остановлен
#define PANEL_MODE_IGNITION              1                   //розжиг
#define PANEL_MODE_HIGH                  2                   //сильный
#define PANEL_MODE_MEDIUM                3                   //средний
#define PANEL_MODE_LOW                   4                   //малый
#define PANEL_MODE_BLOW                  5                   //продувка
#define PANEL_MODE_WAIT                  6                   //ждущий
#define PANEL_MODE_NONAME                7                   //не отображать
#define PANEL_MODE_VENTILATION           8                   //вентиляция
#define PANEL_MODE_PUMP_ONLY             9                   //работа только помпы

/*----------------------------------------------------------------------------------------------------------------------------------
                                   Номера (имена) параметров работы для передачи данных на сервер 
----------------------------------------------------------------------------------------------------------------------------------*/
#define MDM_TOTAL_TIME                   1//uint32_t ;Моточасы, c                                   ; V                ; %.0f           ; Вр.Реж.      ;
#define MDM_STAGE                        2//uint8_t  ;Стадия                                        ; V                ; %.0f           ; Стадия       ;
#define MDM_MODE                         3//uint8_t  ;Режим работы                                  ; V                ; %.0f           ; Режим        ;
#define MDM_RUNNING_TIME                 4//uint32_t ;Общее время работы, c                         ; V                ; %.0f           ; Вр.Работы    ;
#define MDM_MODE_TIME                    5//uint32_t ;Время работы на режиме, c                     ; V                ; %.0f           ; Вр.Реж.      ;

#define MDM_U_SUPPLY                     6//uint16_t ;Напряжение питания, В*100                     ; V/100            ; %.2f           ; U, В         ;
#define MDM_T_FLAME                      7//int16_t  ;Температура ИП                                ; V                ; %.0f           ; Тплам        ;
#define MDM_T_FRAME                      8//int16_t  ;Температура корп                              ; V                ; %.0f           ; Ткорп        ;
#define MDM_T_PANEL                      9//int8_t   ;Температура датчика в ПУ                      ; V                ; %.0f           ; Тпульта      ;
#define MDM_T_EXT                       10//int16_t  ;Температура кабинного датчика                 ; V                ; %.0f           ; Твнешн       ;
#define MDM_T_INLET                     11//int16_t  ;Температура на входе                          ; V                ; %.0f           ; Твхода       ;
#define MDM_T_SETPOINT                  12//int8_t   ;Температура уставки                           ; V                ; %.0f           ; Туставки     ;
#define MDM_PWR_DEFINED                 13//uint8_t  ;Заданная мощность                             ; V                ; %.0f           ; Р зад        ;
#define MDM_PWR_REALIZED                14//uint8_t  ;Текущая мощность                              ; V                ; %.0f           ; Р тек        ;
#define MDM_WORK_BY                     15//uint8_t  ;По какому датчику работал                     ; V                ; %.0f           ; Работа по    ;
#define MDM_FAN_REV_DEFINED             16//uint8_t  ;Заданные обороты НВ                           ; V                ; %.0f           ; Об.зад       ;
#define MDM_FAN_REV_MEASURED            17//uint8_t  ;Измеренные обороты НВ                         ; V                ; %.0f           ; Об. изм      ;
#define MDM_FP_FREQ_DEFINED             18//uint16_t ;Заданная частота ТН, Гц*100                   ; V/100            ; %.2f           ; ТН зад       ;
#define MDM_FP_FREQ_REALIZED            19//uint16_t ;Реализованная частота ТН, Гц*100              ; V/100            ; %.2f           ; ТН тек       ;
#define MDM_FAULT_CODE                  20//uint8_t  ;Код неисправности                             ; V                ; %.0f           ; Неиспр       ;
#define MDM_FLAME_LIMIT                 21//int16_t  ;Граница срыва пламени                         ; V                ; %.0f           ; ИП lim       ;
#define MDM_FLAME_BREAK_NUM             22//uint8_t  ;Количество срывов пламени во время работы     ; V                ; %.0f           ; Кол.Ср.Пл    ;
#define MDM_OVERHEAT_TBORDER            23//int16_t  ;Граница перегрева                             ; V                ; %.0f           ; Гр.Перегр    ;
#define MDM_T_LIQUID                    24//int16_t  ;Температура жидкости                          ; V                ; %.0f           ; Тж           ;
#define MDM_T_OVERHEAT                  25//int16_t  ;Температура перегрева                         ; V                ; %.0f           ; Тпер         ;
#define MDM_RELAY_STATE                 26//uint8_t  ;Состояние реле (ВКЛ/ВЫКЛ)                     ; V                ; %.0f           ; Реле         ;
#define MDM_PUMP_STATE                  27//uint8_t  ;Состояние помпы (ВКЛ/ВЫКЛ)                    ; V                ; %.0f           ; Помпа        ;
#define MDM_SIGNALING_STATE             28//uint8_t  ;Состояние сигнализации (сигнал есть/нет)      ; V                ; %.0f           ; Сигнализация ;
#define MDM_ENGINE_STATE                29//uint8_t  ;Состояние двигателя (заведен/остановлен)      ; V                ; %.0f           ; Двигатель    ;
#define MDM_MODE_LIQUID_HEATER          30//uint8_t  ;Режим работы жидкостного подогревателя        ; V                ; %.0f           ; Режим подогр.;
#define MDM_PRESSURE                    31//uint8_t  ;Атмосферное давление, кПа                     ; V                ; %.0f           ; Давл.,кПа    ;
#define MDM_RUN_CNT                     32//uint16_t ;счетчик запусков
#define MDM_RUN_VENT_CNT                33//uint16_t ;счетчик запусков на вентиляции
#define MDM_RUN_OK_CNT                  34//uint16_t ;счетчик запусков завершенных без неисправностей
#define MDM_RUN_ERR_CNT                 35//uint16_t ;счетчик запусков завершенных с неисправностями
#define MDM_POWER_0_TIME                36//uint32_t ;время работы на 0 мощности
#define MDM_POWER_1_TIME                37//uint32   ;время работы на 1 мощности
#define MDM_POWER_2_TIME                38//uint32   ;время работы на 2 мощности
#define MDM_POWER_3_TIME                39//uint32   ;время работы на 3 мощности
#define MDM_POWER_4_TIME                40//uint32   ;время работы на 4 мощности
#define MDM_POWER_5_TIME                41//uint32   ;время работы на 5 мощности
#define MDM_POWER_6_TIME                42//uint32   ;время работы на 6 мощности
#define MDM_POWER_7_TIME                43//uint32   ;время работы на 7 мощности
#define MDM_POWER_8_TIME                44//время работы на 8 мощности
#define MDM_POWER_9_TIME                45//время работы на 9 мощности
#define MDM_RUN_FIRST_CNT               46//количество запусков с первой попытки
#define MDM_RUN_SECOND_CNT              47//количество запусков со второй попытки
#define MDM_NO_IGNITION_CNT             48//количество незапусков (попытки розжига исчерпаны)
#define MDM_SPARK_STATE                 49//искра
#define MDM_VALVE_STATE                 50//клапан
#define MDM_HE_STATE                    51//ТЭН
#define MDM_ILLUMINATION                52//освещённость ИП
#define MDM_STAGE_TIME                  53//Время работы на стадии
#define MDM_CURRENT                     54//ток НВ
#define MDM_FLAME_ON                    55//пламя есть или нет

#define MDM_SIGNAL_ON_PROBE							56//сигнал с зонда
#define MDM_T_PLATE											57//температура платы
#define MDM_V_HIGH_PRESSURE							58//состояние клапана высокого давления
#define MDM_V_CUTOFF										59//состояние отсечного клапана
#define MDM_V_SMALL											60//состояния малого клапана
#define MDM_V_STRONG										61//состояние сильного клапана
#define MDM_PROBE_FLAME_EXIST						62//флаг наличия пламени

#endif /* __PARAMS_NAME_H */
