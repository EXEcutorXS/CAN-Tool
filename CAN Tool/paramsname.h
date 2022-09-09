/* Define to prevent recursive inclusion -------------------------------------*/
#ifndef __PARAMS_NAME_H
#define __PARAMS_NAME_H

/*----------------------------------------------------------------------------------------------------------------------------------
                                   ������ (�����) ���������� ������ (������������)
----------------------------------------------------------------------------------------------------------------------------------*/
//                                        �������                                                                                                     ENGLISH
#define PAR_FAN_PWM                      0//��� ����������� �������, ��                                                                               @ FAN_PWM, Hz
#define PAR_FP_PULSE_MS                  1//������������ �������� ��, ��                                                                              @ Pulse width, ms
#define PAR_GP_NOMINAL_VOLTAGE           2//����������� ���������� �����, �                                                                           @ Glow plug rated voltage, V
#define PAR_FLAME_APPEARANCE_DELTA       3//������� ����������� ������� ��� ����������� ��� ��������� (������ P)                                      @ FLAME_APPEARANCE_DELTA
#define PAR_FLAME_APPEARANCE_TIMEOUT     4//������������ ����� �������� ��������� ������� ��� ������� (������ P), c                                   @ FLAME_APPEARANCE_TIMEOUT
#define PAR_FLAME_SENSOR_CALIBRATION     5//������������� �������� ��, �������� ���                                                                   @ FLAME SENSOR CALIBRATION Value
#define PAR_FRAME_SENSOR_CALIBRATION     6//������������� �������� ������� �������, �������� ���                                                      @ FRAME SENSOR CALIBRATION Value
#define PAR_U_SUPPLY_NOMINAL             7//����������� ���������� ������� ������� (12/24�)                                                           @ Rated supply voltage (12/24 V)
#define PAR_U_SUPPLY_MIN                 8//����������� �������� ���������� �������, �*100                                                            @ Minimum supply voltage, V*100
#define PAR_U_SUPPLY_MAX                 9//������������ �������� ���������� �������, �*100                                                           @ Maximum supply voltage, V*100
#define PAR_DELTA_T_STOP                10//����������� ������� <= ����������� �� ������� �� X �������� (3)                                           @ DELTA_T_STOP
#define PAR_DELTA_T_RUN                 11//����������� ������� >= ����������� �� ������� �� Y �������� (3)                                           @ DELTA_T_RUN
#define PAR_ID1                         12///�������� �����. ������ 4 �����                                                                            @ Device ID1
#define PAR_ID2                         13///�������� �����. ������ 4 �����                                                                            @ Device ID2
#define PAR_ID3                         14///�������� �����. ������ 4 �����                                                                            @ Device ID3
#define PAR_T_SETPOINT_ORIGINAL         15//������� ����������� ����������� ������������� (������� ����� ������)                                      @ T_SETPOINT ORIGINAL Mode
#define PAR_T_SETPOINT_WARMUP           16//������� ����������� ����������� ������������� (����� ������ �����������)                                  @ T_SETPOINT WARMUP Mode
#define PAR_T_SETPOINT_ECONOMY          17//������� ����������� ����������� ������������� (����������� ����� ������)                                  @ T_SETPOINT ECONOMY Mode
#define PAR_CAN_ADDRESS                 18//����� ���������� � ���� CAN                                                                               @ Device's CAN address
#define PAR_BRUSHLESS_FAN_CALIBRATION   19//������������� �������� ���������������� ���������                                                         @ Brushless motor's calibration value
#define PAR_SIGNALING_MODE              20//����� ������� �� ������ ������������: 0-����., 1-���., 2-�������, 3-���. � �������                        @ Signaling_mode [0, 1, 2, 3]
#define PAR_ZONE1_ENABLE                21//����1 ���������� (��� ������ ���������)                                                                   @ Zone1 enable
#define PAR_ZONE2_ENABLE                22//����2 ���������� (��� ������ ���������)                                                                   @ Zone2 enable
#define PAR_ZONE3_ENABLE                23//����3 ���������� (��� ������ ���������)                                                                   @ Zone3 enable
#define PAR_ZONE4_ENABLE                24//����4 ���������� (��� ������ ���������)                                                                   @ Zone4 enable
#define PAR_ZONE5_ENABLE                25//����5 ���������� (��� ������ ���������)                                                                   @ Zone5 enable
#define PAR_MAX_T_FLAME                 26//������������ ������������ ��������� �����                                                                 @ Maximum flame temperature
#define PAR_FUEL_PERFORMANCE            27//������������������ ���������� ������, ��/100 ������                                                       @ Fuel pump performance

#define PAR_HCU_ELEMENT_SETPOINT        28//������� ����������� ���� ��� �������� � ������                                                            @ HCU_ELEMENT_SETPOINT
#define PAR_HCU_ELEMENT_ON              29//������� ����������� ���� ��� ������ �� �������                                                            @ HCU_ELEMENT_ON
#define PAR_HCU_ELEMENT_ON_DW           30//������� ����������� ���� ��� ������ �� ������� ��� ������� ����                                           @ HCU_ELEMENT_ON_DW

#define PAR_HEATER_LIQUID_SETPOINT      31//������� ����������� �������� ������������� ��� �������� � ������                                          @ HEATER_LIQUID_SETPOINT
#define PAR_HEATER_LIQUID_IGNITION      32//������� ����������� �������� ������������� ��� ������ �� �������                                          @ HEATER_LIQUID_IGNITION
#define PAR_HEATER_LIQUID_IGNITION_DW   33//������� ����������� �������� ������������� ��� ������ �� ������� ��� ������� ���� ��� ������� ����        @ HEATER_LIQUID_IGNITION_DW

#define PAR_HCU_TANK_SETPOINT           34//������� ����������� ���� ��� �������� � ������                                                            @ HCU_TANK_SETPOINT
#define PAR_HCU_TANK_IGNITION           35//������� ����������� ���� ��� ������ �� �������                                                            @ HCU_TANK_IGNITION
#define PAR_HCU_TANK_IGNITION_DW        36//������� ����������� ���� ��� ������ �� ������� ��� ������� ���� ��� ������� ����                          @ HCU_TANK_IGNITION_DW

#define PAR_DWATER_PRIORITY             37//��������� ������� ���� � ������� ���������                                                                @ Domestic water priority
#define PAR_DWATER_PAUSE                38//����� ��� ������ ������� ����                                                                             @ Domestic water pause

                                        //=======������ ����������� ��������� ����������� (������������ ���������, ��������� ��������������
#define PAR_TU0                         39//������ �������� ����� 0                                                                                   @ TU0
#define PAR_TU1                         40//������ �������� ����� 1                                                                                   @ TU1
#define PAR_TU2                         41//������ �������� ����� 2                                                                                   @ TU2
#define PAR_TU3                         42//������ �������� ����� 3                                                                                   @ TU3
#define PAR_TU4                         43//������ �������� ����� 4                                                                                   @ TU4
#define PAR_TU5                         44//������ �������� ����� 5                                                                                   @ TU5
#define PAR_TU6                         45//������ �������� ����� 6                                                                                   @ TU6
#define PAR_TU7                         46//������ �������� ����� 7                                                                                   @ TU7
#define PAR_TU8                         47//������ �������� ����� 8                                                                                   @ TU8
#define PAR_TU9                         48//������ �������� ����� 9                                                                                   @ TU9

#define PAR_TD0                         49//������ �������� ���� 0                                                                                    @ TD0
#define PAR_TD1                         50//������ �������� ���� 1                                                                                    @ TD1
#define PAR_TD2                         51//������ �������� ���� 2                                                                                    @ TD2
#define PAR_TD3                         52//������ �������� ���� 3                                                                                    @ TD3
#define PAR_TD4                         53//������ �������� ���� 4                                                                                    @ TD4
#define PAR_TD5                         54//������ �������� ���� 5                                                                                    @ TD5
#define PAR_TD6                         55//������ �������� ���� 6                                                                                    @ TD6
#define PAR_TD7                         56//������ �������� ���� 7                                                                                    @ TD7
#define PAR_TD8                         57//������ �������� ���� 8                                                                                    @ TD8
#define PAR_TD9                         58//������ �������� ���� 9                                                                                    @ TD9

#define PAR_ZONE1_PUMP_NUM              59//����� ����� ��� ���� 1                                                                                    @ Zone1 water pump number
#define PAR_ZONE2_PUMP_NUM              60//����� ����� ��� ���� 2                                                                                    @ Zone2 water pump number
#define PAR_ZONE3_PUMP_NUM              61//����� ����� ��� ���� 3                                                                                    @ Zone3 water pump number
#define PAR_ZONE4_PUMP_NUM              62//����� ����� ��� ���� 4                                                                                    @ Zone4 water pump number
#define PAR_ZONE5_PUMP_NUM              63//����� ����� ��� ���� 5                                                                                    @ Zone5 water pump number

#define PAR_FAN1_PWM_LIM                64//����������� ��� ����������� ���� 1, %                                                                     @ Fan1_PWM_limitation, %
#define PAR_FAN2_PWM_LIM                65//����������� ��� ����������� ���� 2, %                                                                     @ Fan2_PWM_limitation, %
#define PAR_FAN3_PWM_LIM                66//����������� ��� ����������� ���� 3, %                                                                     @ Fan3_PWM_limitation, %
#define PAR_FAN4_PWM_LIM                67//����������� ��� ����������� ���� 4, %                                                                     @ Fan4_PWM_limitation, %
#define PAR_FAN5_PWM_LIM                68//����������� ��� ����������� ���� 5, %                                                                     @ Fan5_PWM_limitation, %

#define PAR_FANS_PWM_CORRECT_TIME       69//������������� ������������� ��� ������������, ��                                                          @ Fans PWM correction time, ms
#define PAR_TIME_FORMAT                 70//������ ������� (12/24 ��� AM/PM)                                                                          @ Time format (12/24 or AM/PM)
#define PAR_T_UNITS                     71//������� ��������� ����������� (C/F)                                                                       @ Temperature units (C/F)
#define PAR_DAYTIME_FROM                72//������ �������� ������� �����, ���                                                                        @ DAYTIME_FROM
#define PAR_DAYTIME_TO                  73//��������� �������� ������� �����, ���                                                                     @ DAYTIME_TO
#define PAR_BRIGHTNESS_DAY              74//������� ������� ����, %                                                                                   @ BRIGHTNESS_DAY
#define PAR_BRIGHTNESS_NIGHT            75//������� ������� �����, %                                                                                  @ BRIGHTNESS_NIGHT

#define PAR_T_RELAY_SETPOINT            76//����������� ��������� ����� ������ ����������                                                             @ T_RELAY_SETPOINT
#define PAR_WAIT_MODE_PUMP              77//������ ����� � ������ ������: 0 - �� ��������, ����� - ��������                                           @ WAIT_MODE_PUMP
#define PAR_ENGINE_PUMP                 78//������ ����� ��� ���������� ���������: 1 - ��������, ����� - �� ��������                                  @ ENGINE_PUMP
#define PAR_WARMUP_MODE                 79//����� �����������: 0 - ��������, 1 - ��������������, 2 - ������                                           @ WARMUP_MODE
#define PAR_RUN_ON_LATCHED              80//����������� ��� ���, ���� �������� � ����������� ��������� ����� ���������                                @ RUN_ON_LATCHED

#define PAR_DT_IGNITION_ORIGIN          81//������� �����: ������� ���������� ����� �������� � ������� �������                                        @ DT_IGNITION_ORIGIN
#define PAR_DT_IGNITION_ECONOMY         82//����������� �����: ������� ���������� ����� �������� � ������� �������                                    @ DT_IGNITION_ECONOMY
#define PAR_DT_IGNITION_WARMUP          83//�����������: ������� ���������� ����� �������� � ������� �������                                          @ DT_IGNITION_WARMUP

#define PAR_POWER_LEVEL_MIN             84//����������� ������� ��������                                                                              @ POWER_LEVEL_MIN
#define PAR_POWER_LEVEL_MAX             85//������������ ������� ��������                                                                             @ POWER_LEVEL_MAX
#define PAR_PERCENT_ENGINE              86//�������� �� ���������� ������� ��� ����������� ����������� ���������                                      @ PERCENT_ENGINE

#define PAR_T_SETPOINT_HEATING          87//������� ����������� ����������� ������������� (���������)                                                 @ T_SETPOINT_HEATING
#define PAR_DT_IGNITION_HEATING         88//����� ���������: ������� ���������� ����� �������� � ������� �������                                      @ DT_IGNITION_HEATING
#define PAR_LANGUAGE                    89//����: 0 - ����������, 1 - �������                                                                         @ Language
#define PAR_SEND_SMS_ON_CALL            90//���������� ��� ��� ������: 0 - �� ����������, 1 - ����������                                              @ Send SMS on call
#define PAR_INTERNET_SETTINGS           91//��������� ���������                                                                                       @ Internet settings
#define PAR_ON_SIGNALING_OPERATE        92//��������� ������ ��� ������������ ������������: 0 - �� ���������, 1 - �� �����������, 2 - �� ���������    @ ON_SIGNALING_OPERATE
#define PAR_SAVED_RUNNING_TIME          93//����������� �������� ������� ������ ��� ������������ ������������, ������                                 @ SAVED_RUNNING_TIME
#define PAR_SAVED_MODE                  94//����������� �������� ������ ������ ��� ������������ ������������: 0 - �������, 1 - �����������, 3 - ���������, 4 - ������������ ������� @ SAVED_MODE
#define PAR_FAN_MANUAL_MODE             95//��������� ������� ������ ����������: 0 - ���������� ������������ ��� ���������� �������� �����������; 1 - ���������� �� ��������������� @ FAN_MANUAL_MODE

#define PAR_HIGH_VOLTAGE_FAULT_TIME     96//����� ������������� ������������� ��� ���������� ����������, *200��                                       @ Time of occurrence of a fault at high voltage, *200ms
#define PAR_LOW_VOLTAGE_FAULT_TIME      97//����� ������������� ������������� ��� ���������� ����������, *200��                                       @ Time of occurrence of a fault at low voltage, *200ms

#define PAR_PCB_TEMP_OR_ENGINE          98//����� � ������������ ������� ����. ��� ������� �����. ���������: 0-���������; 1-������ �����������        @ PCB_TEMP_OR_ENGINE

#define PAR_FANS_DT_START               99//������� ����������� �� ������� ��� ������� ����������� ��� �������� ��������                              @ Delta T when zone's fans start (for heating systems)
#define PAR_PUMP_ON_ELEMENT            100//�������� ����� ������� ��� ������ ����: 1 - ��������, 0 - ���������                                       @ Water pump of the heater: 1 - ON, 0 - OFF

#define PAR_BOILER_TEMP                101//����������� �������                           @ BOILER_TEMP
#define PAR_BOILER_TIME_KEEPWARM       102//����� ����������� ����������� � �������       @ BOILER_TIME_KEEPWARM
#define PAR_BOILER_HYST_TEMP           103//������. ���������� �����������                @ BOILER_HYST_TEMP
#define PAR_BOILER_HOTMODE             104//������ HOTMODE                                @ BOILER_HOTMODE
#define PAR_MODE_BOILER                105//��� �������: 1 - �������������, 0 - ��������� @ System type: 1 - accumulative, 0 - running
#define PAR_TYPE_AIRHEATER             106//��� ���������: 1-����������, 0-����������     @ AirHeaterType: 1 - convectors, 0 - fans
#define PAR_BOILER_TEMP_UPDATE_TIME    107//����� ������ ����� ������� ��� ������� ������� ����������� � ������ Keep warm, 10-120 ������.  @ Boiler temp update time 10-120 sec
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

#define PAR_CHECKING_ZONE_FANS         130//�������� �������������� ������������ ���: 0 - �� ���������, 1 - ���������        @ Checking zone fans for faults
#define PAR_USE_PRESSURE_MEASUREMENT   131//������������ ���������� �������� ��� ������������� ��: 0 - �� ������������, 1 - ������������ @ Use pressure measurement for the fuel pump correction
#define PAR_SET_DELAY_MAX_VOLTAGE   	 132//���������� �������� ������� �� ���������� ���������� @ Set delay time to determine overvoltage error
#define PAR_SET_DELAY_MIN_VOLTAGE   	 133//���������� �������� ������� �� ���������� ���������� @ Set delay time to determine undervoltage error

                                      //134
                                      //...
                                      //500

#define PAR_DISPLAY_ZONE1_DAY_T        501//������� ������� ����������� ���� 1, �         @ DISPLAY_ZONE1_DAY_T
#define PAR_DISPLAY_ZONE2_DAY_T        502//������� ������� ����������� ���� 2, �         @ DISPLAY_ZONE2_DAY_T
#define PAR_DISPLAY_ZONE3_DAY_T        503//������� ������� ����������� ���� 3, �         @ DISPLAY_ZONE3_DAY_T
#define PAR_DISPLAY_ZONE4_DAY_T        504//������� ������� ����������� ���� 4, �         @ DISPLAY_ZONE4_DAY_T
#define PAR_DISPLAY_ZONE5_DAY_T        505//������� ������� ����������� ���� 5, �         @ DISPLAY_ZONE5_DAY_T

#define PAR_DISPLAY_ZONE1_NIGHT_T      506//������� ������ ����������� ���� 1, �          @ DISPLAY_ZONE1_NIGHT_T
#define PAR_DISPLAY_ZONE2_NIGHT_T      507//������� ������ ����������� ���� 2, �          @ DISPLAY_ZONE2_NIGHT_T
#define PAR_DISPLAY_ZONE3_NIGHT_T      508//������� ������ ����������� ���� 3, �          @ DISPLAY_ZONE3_NIGHT_T
#define PAR_DISPLAY_ZONE4_NIGHT_T      509//������� ������ ����������� ���� 4, �          @ DISPLAY_ZONE4_NIGHT_T
#define PAR_DISPLAY_ZONE5_NIGHT_T      510//������� ������ ����������� ���� 5, �          @ DISPLAY_ZONE5_NIGHT_T
//---------------- NEW PARAMS BEW !!!!! -----------------------
#define PAR_DISPLAY_ZONE1_FAN_MANUAL_VALUE      520//�������� ���������� � ������ ������ ���� 1, %          @ DISPLAY_ZONE1_FAN_MANUAL_VALUE
#define PAR_DISPLAY_ZONE2_FAN_MANUAL_VALUE      521//�������� ���������� � ������ ������ ���� 2, %          @ DISPLAY_ZONE2_FAN_MANUAL_VALUE
#define PAR_DISPLAY_ZONE3_FAN_MANUAL_VALUE      522//�������� ���������� � ������ ������ ���� 3, %          @ DISPLAY_ZONE3_FAN_MANUAL_VALUE
#define PAR_DISPLAY_ZONE4_FAN_MANUAL_VALUE      523//�������� ���������� � ������ ������ ���� 4, %          @ DISPLAY_ZONE4_FAN_MANUAL_VALUE
#define PAR_DISPLAY_ZONE5_FAN_MANUAL_VALUE      524//�������� ���������� � ������ ������ ���� 5, %          @ DISPLAY_ZONE5_FAN_MANUAL_VALUE

#define PAR_DISPLAY_ZONE1_FAN_MANUAL            525//����� ������ ����������: 1-������; 0 - ����.           @ DISPLAY_ZONE1_FAN_AUTO
#define PAR_DISPLAY_ZONE2_FAN_MANUAL            526//����� ������ ����������: 1-������; 0 - ����.           @ DISPLAY_ZONE2_FAN_AUTO
#define PAR_DISPLAY_ZONE3_FAN_MANUAL            527//����� ������ ����������: 1-������; 0 - ����.           @ DISPLAY_ZONE3_FAN_AUTO
#define PAR_DISPLAY_ZONE4_FAN_MANUAL            528//����� ������ ����������: 1-������; 0 - ����.           @ DISPLAY_ZONE4_FAN_AUTO
#define PAR_DISPLAY_ZONE5_FAN_MANUAL            529//����� ������ ����������: 1-������; 0 - ����.           @ DISPLAY_ZONE5_FAN_AUTO

#define PAR_DISPLAY_SYSTEM_LIMIT_WORKTIME       530//������������ ����� ������ �������, �����               @ DISPLAY_SYSTEM_LIMIT_WORKTIME
#define PAR_DISPLAY_TIMEOUT                     531//������� ��������� �������, ������                      @ DISPLAY_TIMEOUT
#define PAR_DISPLAY_SYSTEM_STORAGE_MODE         532//����� �������� ������� �� �����������, ���/����        @ DISPLAY_SYSTEM_STORAGE_MODE

#define PAR_HCU_ELEMENT_SETPOINT_STORAGE        540//������� ����������� ���� ��� �������� ���� � ������ � ������ StorageMode  @ HCU_ELEMENT_SETPOINT
#define PAR_HCU_ELEMENT_IGNITION_STORAGE        541//������� ����������� ���� ��� ������ ���� �� ������� � ������ StorageMode  @ HCU_ELEMENT_IGNITION
#define PAR_HCU_TANK_SETPOINT_STORAGE           542//������� ����������� �������� ���� ��� �������� ����� � ������ � ������ StorageMode    @ HEATER_LIQUID_SETPOINT
#define PAR_HCU_TANK_IGNITION_STORAGE           543//������� ����������� �������� ���� ��� ������ ����� �� ������� � ������ StorageMode    @ HEATER_LIQUID_IGNITION

#define PAR_RVC_INSTANCE                        600// ����� ���������� �� ���� CAN RVC                     @ RVC_DEVICE_INSTANCE

/*----------------------------------------------------------------------------------------------------------------------------------
                                   ������ (�����) ����� ���������� ������� �����
----------------------------------------------------------------------------------------------------------------------------------*/
#define BB_RUN_CNT                       1//������� ��������
#define BB_RUN_VENT_CNT                  2//������� �������� �� ����������
#define BB_RUN_OK_CNT                    3//������� �������� ����������� ��� ��������������
#define BB_RUN_ERR_CNT                   4//������� �������� ����������� � ���������������
#define BB_WORK_FLAG                     5//���� ������ ������������� ��� �������� ������ �������
#define BB_PWR_BREAK                     6//������� ������� ������� �� ����� ������
#define BB_TOTAL_TIME                    7//����� ����� ������
#define BB_POWER_0_TIME                  8//����� ������ �� 0 ��������
#define BB_POWER_1_TIME                  9//����� ������ �� 1 ��������
#define BB_POWER_2_TIME                 10//����� ������ �� 2 ��������
#define BB_POWER_3_TIME                 11//����� ������ �� 3 ��������
#define BB_POWER_4_TIME                 12//����� ������ �� 4 ��������
#define BB_POWER_5_TIME                 13//����� ������ �� 5 ��������
#define BB_POWER_6_TIME                 14//����� ������ �� 6 ��������
#define BB_POWER_7_TIME                 15//����� ������ �� 7 ��������
#define BB_POWER_8_TIME                 16//����� ������ �� 8 ��������
#define BB_POWER_9_TIME                 17//����� ������ �� 9 ��������
#define BB_VENT_TIME                    18//����� ������ �� ����������
#define BB_WAIT_MODE_TIME               19//����� ������ �� ������ ������
#define BB_RUN_FIRST_CNT                20//���������� �������� � ������ �������
#define BB_RUN_SECOND_CNT               21//���������� �������� �� ������ �������
#define BB_NO_IGNITION_CNT              22//���������� ���������� (������� ������� ���������)

#define BB_POWER_LOW_TIME               23//����� ������ �� ����� ��������
#define BB_POWER_MIDDLE_TIME            24//����� ������ �� ������� ��������
#define BB_POWER_HIGH_TIME              25//����� ������ �� ������������ ��������

#define BB_ELEMENT_TIME                 26//����� ������ ����, �
#define BB_POWER_10_TIME                27//����� ������ �� 10 ��������
#define BB_RUN_PUMP_CNT                 28//������� �������� �����
/*----------------------------------------------------------------------------------------------------------------------------------
                                   ������ (�����) ����������, ����������, ...
----------------------------------------------------------------------------------------------------------------------------------*/
//                                   ������ ������ ����� �����: %.0f. ���� �� ������� ��������� ������� �����, ��� ������ ��� ����� ��������� �� �������� ����������
//                                   � ������� ��������� ������ V (��� v) ������������� ��������, ���������� � ������
//������ ������:                                     ;                                              ; ������� ���������; ������ ������; ��� � �������; ����
#define VAR_STAGE                        1//uint8_t  ;������                                        ; V                ; %.0f           ; ������       ;
#define VAR_MODE                         2//uint8_t  ;����� ������                                  ; V                ; %.0f           ; �����        ;
#define VAR_RUNNING_TIME                 3//uint32_t ;����� ����� ������, c                         ; V                ; %.0f           ; ��.������    ;
#define VAR_MODE_TIME                    4//uint32_t ;����� ������ �� ������, c                     ; V                ; %.0f           ; ��.���.      ;

#define VAR_U_SUPPLY                     5//uint16_t ;���������� �������, �*100                     ; V/100            ; %.2f           ; U, �         ;
#define VAR_T_FLAME                      6//int16_t  ;����������� ��                                ; V                ; %.0f           ; �����        ;
#define VAR_T_FRAME                      7//int16_t  ;����������� ����                              ; V                ; %.0f           ; �����        ;
#define VAR_T_PANEL                      8//int8_t   ;����������� ������� � ��                      ; V                ; %.0f           ; �������      ;
#define VAR_T_EXT                        9//int16_t  ;����������� ��������� �������                 ; V                ; %.0f           ; ������       ;
#define VAR_T_INLET                     10//int16_t  ;����������� �� �����                          ; V                ; %.0f           ; ������       ;
#define VAR_T_SETPOINT                  11//int8_t   ;����������� �������                           ; V                ; %.0f           ; ��������     ;
#define VAR_PWR_DEFINED                 12//uint8_t  ;�������� ��������                             ; V                ; %.0f           ; � ���        ;
#define VAR_PWR_REALIZED                13//uint8_t  ;������� ��������                              ; V                ; %.0f           ; � ���        ;
#define VAR_WORK_BY                     14//uint8_t  ;�� ������ ������� �������                     ; V                ; %.0f           ; ������ ��    ;
#define VAR_FAN_REV_DEFINED             15//uint8_t  ;�������� ������� ��                           ; V                ; %.0f           ; ��.���       ;
#define VAR_FAN_REV_MEASURED            16//uint8_t  ;���������� ������� ��                         ; V                ; %.0f           ; ��. ���      ;
#define VAR_FP_FREQ_DEFINED             17//uint16_t ;�������� ������� ��, ��*100                   ; V/100            ; %.2f           ; �� ���       ;
#define VAR_FP_FREQ_REALIZED            18//uint16_t ;������������� ������� ��, ��*100              ; V/100            ; %.2f           ; �� ���       ;
#define VAR_GP_PERIOD                   19//uint32_t ;������ �����                                  ; V                ; %.0f           ; ��. ���      ;
#define VAR_GP_PULSE                    20//uint32_t ;������� �����                                 ; V                ; %.0f           ; ��. ���      ;
#define VAR_GP_PERCENT                  21//uint8_t  ;�������� �������� �������� �����              ; V                ; %.0f           ; ��. %        ;
#define VAR_FLAME_CALIBRATION           22//uint16_t ;������������� �������� ��������� ��           ; V                ; %.0f           ; �.�. ��      ;
#define VAR_FRAME_CALIBRATION           23//uint16_t ;������������� �������� ��������� �������      ; V                ; %.0f           ; �.�. ����    ;

#define VAR_FAULT_CODE                  24//uint8_t  ;��� �������������                             ; V                ; %.0f           ; ������       ;
#define VAR_FAULT_BLINK_CODE            25//uint8_t  ;��� �������������. ���������� �������� (�������); V              ; %.0f           ; ������.���   ;
#define VAR_FAULT_BYTE0                 26//uint8_t  ;���� ��������������1                          ; V                ; %.0f           ; �.������0    ;
#define VAR_FAULT_BYTE1                 27//uint8_t  ;���� ��������������2                          ; V                ; %.0f           ; �.������1    ;
#define VAR_FAULT_BYTE2                 28//uint8_t  ;���� ��������������3                          ; V                ; %.0f           ; �.������2    ;
#define VAR_FAULT_BYTE3                 29//uint8_t  ;���� ��������������4                          ; V                ; %.0f           ; �.������3    ;
#define VAR_FAULT_BYTE4                 30//uint8_t  ;���� ��������������5                          ; V                ; %.0f           ; �.������4    ;
#define VAR_FAULT_BYTE5                 31//uint8_t  ;���� ��������������6                          ; V                ; %.0f           ; �.������5    ;
#define VAR_FAULT_BYTE6                 32//uint8_t  ;���� ��������������7                          ; V                ; %.0f           ; �.������6    ;
#define VAR_FAULT_BYTE7                 33//uint8_t  ;���� ��������������8                          ; V                ; %.0f           ; �.������7    ;

#define VAR_IGNITION_ATTEMPT            34//uint8_t  ;������� �������                               ; V                ; %.0f           ; �����������  ;
#define VAR_WAIT_MODE_ALLOWED           35//uint8_t  ;�������� ������ �����                         ; V                ; %.0f           ; ����.���     ;
#define VAR_FLAME_MINIMUM               36//int16_t  ;����������� �������� ��                       ; V                ; %.0f           ; �� min       ;
#define VAR_FLAME_LIMIT                 37//int16_t  ;������� ����� �������                         ; V                ; %.0f           ; �� lim       ;
#define VAR_FLAME_BREAK_NUM             38//uint8_t  ;���������� ������ ������� �� ����� ������     ; V                ; %.0f           ; ���.��.��    ;
#define VAR_OVERHEAT_TBORDER            39//int16_t  ;������� ���������                             ; V                ; %.0f           ; ��.������    ;
#define VAR_T_LIQUID                    40//int16_t  ;����������� ��������                          ; V                ; %.0f           ; ��           ;
#define VAR_T_OVERHEAT                  41//int16_t  ;����������� ���������                         ; V                ; %.0f           ; ����         ;
#define VAR_DT_FLAME                    42//int16_t  ;��������� ����������� �������                 ; V                ; %.0f           ; DT ��        ;
#define VAR_DT_LIQUID                   43//int16_t  ;��������� ����������� ��������                ; V                ; %.0f           ; DT ��        ;
#define VAR_DT_OVERHEAT                 44//int16_t  ;��������� ����������� ���������               ; V                ; %.0f           ; DT ����      ;
#define VAR_RELAY_STATE                 45//uint8_t  ;��������� ���� (���/����)                     ; V                ; %.0f           ; ����         ;
#define VAR_PUMP_STATE                  46//uint8_t  ;��������� ����� (���/����)                    ; V                ; %.0f           ; �����        ;
#define VAR_SIGNALING_STATE             47//uint8_t  ;��������� ������������ (������ ����/���)      ; V                ; %.0f           ; ������������ ;
#define VAR_ENGINE_STATE                48//uint8_t  ;��������� ��������� (�������/����������)      ; V                ; %.0f           ; ���������    ;

#define VAR_ADC_CH0                     49//uint16_t ;��� ����� 0                                   ; V                ; %.0f           ; ��� 0        ;
#define VAR_ADC_CH1                     50//uint16_t ;��� ����� 1                                   ; V                ; %.0f           ; ��� 1        ;
#define VAR_ADC_CH2                     51//uint16_t ;��� ����� 2                                   ; V                ; %.0f           ; ��� 2        ;
#define VAR_ADC_CH3                     52//uint16_t ;��� ����� 3                                   ; V                ; %.0f           ; ��� 3        ;
#define VAR_ADC_CH4                     53//uint16_t ;��� ����� 4                                   ; V                ; %.0f           ; ��� 4        ;
#define VAR_ADC_CH5                     54//uint16_t ;��� ����� 5                                   ; V                ; %.0f           ; ��� 5        ;
#define VAR_ADC_CH6                     55//uint16_t ;��� ����� 6                                   ; V                ; %.0f           ; ��� 6        ;
#define VAR_ADC_CH7                     56//uint16_t ;��� ����� 7                                   ; V                ; %.0f           ; ��� 7        ;
#define VAR_ADC_CH8                     57//uint16_t ;��� ����� 8                                   ; V                ; %.0f           ; ��� 8        ;
#define VAR_ADC_CH9                     58//uint16_t ;��� ����� 9                                   ; V                ; %.0f           ; ��� 9        ;

#define VAR_T_CPU                       59//int16_t  ;����������� ����������                        ; V                ; %.0f           ; �cpu         ;
#define VAR_PRESSURE                    60//uint8_t  ;����������� ��������, ���                     ; V                ; %.0f           ; ����.,���    ;
#define VAR_T_TANK                      61//int16_t  ;����������� ���� � �������� ���������         ; V                ; %.0f           ; �����        ;

#define VAR_GLOWPLUG_STATE              62//uint8_t  ;��������� ����� (���/����)                     ; V                ; %.0f           ; ����         ;

#define VAR_START_BB_ERR_RECORD 0xFFFFFFFA//uint32_t ;������ ������ � ������������� � ��
//#define VAR_                            //uint_t


/*----------------------------------------------------------------------------------------------------------------------------------
                                   ������ � ������ ������
----------------------------------------------------------------------------------------------------------------------------------*/
#define STAGE_Z                          0                   //������ Z  @ Stage Z
#define STAGE_P                          1                   //������ P  @ Stage P
#define STAGE_H                          2                   //������ H  @ Stage H
#define STAGE_W                          3                   //������ W  @ Stage W
#define STAGE_F                          4                   //������ F  @ Stage F
#define STAGE_T                          5                   //������ T  @ Stage T
#define STAGE_M                          6                   //������ M  @ Stage M

#define Z_MODE_1_WAIT_COMMAND            1                   //�������� �������  @ Waiting for the command
#define Z_MODE_3_BLOWING_ON_POWER        3                   //�������� ��� ������ ������� (��������� �������/������� ������� �� �����������) @ Blowing on power on
#define Z_MODE_5_SAVE_DATA               5                   //���������� ������ � ����� ����� ���������� ������  @ Saving data

#define P_MODE_0_START_DIAGNOSTIC        0                   //��������� �����������                              @ Initial diagnostics
#define P_MODE_1_FAN_STARTING            1                   //����������� ����������� �������                    @ Fan starting
#define P_MODE_2_WAITING_T_DECREASE      2                   //�������� �������� ����������� (�������� � ���������� ��� ������� � ��������� @ Waiting for the temperature decrease
#define P_MODE_3_BLOWING_ON_START        3                   //�������� ������� ������� �� ����������� ��� �����  @ Blowing on start
#define P_MODE_4_WAITING                 4                   //������                                             @ Waiting

#define H_MODE_0_GLOW_PLUG_HEATING       0                   //�������� �����                                     @ Glow plug heating
#define H_MODE_1_IGNITION1               1                   //������ ������� �������                             @ Ignition 1
#define H_MODE_2_BLOWING_BETWEEN_IGNITIONS 2                 //�������� ����� ���������                           @ Blowing
#define H_MODE_3_IGNITION2               3                   //������ ������� �������                             @ Ignition 2
#define H_MODE_4_HEATING                 4                   //������� ������ ��������                            @ Heating
#define H_MODE_5_FLAME_BREAK_BLOWING     5                   //�������� ����� ����� ������� �� ��������           @ Blowing on flame break
#define H_MODE_6_BLOWING_BEFORE_WAIT     6                   //�������� ����� ������                              @ Blowing before waiting
#define H_MODE_7_PREPARING               7                   //���������� � �������                               @ Preparing for ignition

#define W_MODE_0_POWER0_FROM_TOP         0                   //������ �� 0 ������� ��������, �������� ������      @ Power level 0
#define W_MODE_1_POWER1_FROM_TOP         1                   //������ �� 1 ������� ��������, �������� ������      @ Power level 1
#define W_MODE_2_POWER2_FROM_TOP         2                   //������ �� 2 ������� ��������, �������� ������      @ Power level 2
#define W_MODE_3_POWER3_FROM_TOP         3                   //������ �� 3 ������� ��������, �������� ������      @ Power level 3
#define W_MODE_4_POWER4_FROM_TOP         4                   //������ �� 4 ������� ��������, �������� ������      @ Power level 4
#define W_MODE_5_POWER5_FROM_TOP         5                   //������ �� 5 ������� ��������, �������� ������      @ Power level 5
#define W_MODE_6_POWER6_FROM_TOP         6                   //������ �� 6 ������� ��������, �������� ������      @ Power level 6
#define W_MODE_7_POWER7_FROM_TOP         7                   //������ �� 7 ������� ��������, �������� ������      @ Power level 7
#define W_MODE_8_POWER8_FROM_TOP         8                   //������ �� 8 ������� ��������, �������� ������      @ Power level 8
#define W_MODE_9_POWER9_FROM_TOP         9                   //������ �� 9 ������� ��������, �������� ������      @ Power level 9
#define W_MODE_10_POWER10_FROM_TOP      10                   //������ �� 10 ������� ��������, �������� ������     @ Power level 10
#define W_MODE_11_POWER1_FROM_BOTTOM    11                   //������ �� 1 ������� ��������, �������� �����       @ Power level 1
#define W_MODE_12_POWER2_FROM_BOTTOM    12                   //������ �� 2 ������� ��������, �������� �����       @ Power level 2
#define W_MODE_13_POWER3_FROM_BOTTOM    13                   //������ �� 3 ������� ��������, �������� �����       @ Power level 3
#define W_MODE_14_POWER4_FROM_BOTTOM    14                   //������ �� 4 ������� ��������, �������� �����       @ Power level 4
#define W_MODE_15_POWER5_FROM_BOTTOM    15                   //������ �� 5 ������� ��������, �������� �����       @ Power level 5
#define W_MODE_16_POWER6_FROM_BOTTOM    16                   //������ �� 6 ������� ��������, �������� �����       @ Power level 6
#define W_MODE_17_POWER7_FROM_BOTTOM    17                   //������ �� 7 ������� ��������, �������� �����       @ Power level 7
#define W_MODE_18_POWER8_FROM_BOTTOM    18                   //������ �� 8 ������� ��������, �������� �����       @ Power level 8
#define W_MODE_19_POWER9_FROM_BOTTOM    19                   //������ �� 9 ������� ��������, �������� �����       @ Power level 9
#define W_MODE_20_POWER10_FROM_BOTTOM   20                   //������ �� 10 ������� ��������, �������� �����      @ Power level 10
#define W_MODE_21_BLOWING_VT            21                   //�������� ����� �����������                         @ Blowing
#define W_MODE_22_BLOWING_FLAME_BREAK   22                   //�������� ��� ����� �������                         @ Blowing on flaeme break
#define W_MODE_23_OVERHEAT              23                   //�������� � ���������� ��� ���������                @ Blowing on overheat
//#define W_MODE_24_PUMP_ONLY           24                   //������ �����                                       @ Water pump only
#define W_MODE_25_LOW                   25                   //�����                                              @ Low power
#define W_MODE_26_MIDDLE                26                   //�������                                            @ Middle power
#define W_MODE_27_HIGH                  27                   //�������                                            @ High power
#define W_MODE_28_BLOWING               28                   //�������� ����� ������                              @ Blowing before waiting
#define W_MODE_29_WAIT                  29                   //������                                             @ Waiting
#define W_MODE_30_PUMP_ONLY             30                   //������ �����                                       @ Water pump only
#define W_MODE_31_BOOST                 31                   //���������� ������                                  @ Boost mode
#define W_MODE_32_IGNITION_BY_PRESSURE_AFTER_FLAME_BREAK 32  //������ �� ������� �������� ����� ����� ������� �� ����� ������ @ Ignition by pressure after flame break
#define W_MODE_34_CALIBRATION_BEFORE_VENTILATION 34          //��������������� ����� �����������                  @ Calibration
#define W_MODE_35_VENTILATION           35                   //����������                                         @ Ventilation

#define W_MODE_40_VENTILATION0          40                   //���������� �� 0 ��������                           @ Ventilation level 0
#define W_MODE_41_VENTILATION1          41                   //���������� �� 1 ��������                           @ Ventilation level 1
#define W_MODE_42_VENTILATION2          42                   //���������� �� 2 ��������                           @ Ventilation level 2
#define W_MODE_43_VENTILATION3          43                   //���������� �� 3 ��������                           @ Ventilation level 3
#define W_MODE_44_VENTILATION4          44                   //���������� �� 4 ��������                           @ Ventilation level 4
#define W_MODE_45_VENTILATION5          45                   //���������� �� 5 ��������                           @ Ventilation level 5
#define W_MODE_46_VENTILATION6          46                   //���������� �� 6 ��������                           @ Ventilation level 6
#define W_MODE_47_VENTILATION7          47                   //���������� �� 7 ��������                           @ Ventilation level 7
#define W_MODE_48_VENTILATION8          48                   //���������� �� 8 ��������                           @ Ventilation level 8
#define W_MODE_49_VENTILATION9          49                   //���������� �� 9 ��������                           @ Ventilation level 9

#define W_MODE_50_POWER0                50                   //����� ������ �� 0 ������� ��������                 @ Power level 0
#define W_MODE_51_POWER1                51                   //����� ������ �� 1 ������� ��������                 @ Power level 1
#define W_MODE_52_POWER2                52                   //����� ������ �� 2 ������� ��������                 @ Power level 2
#define W_MODE_53_POWER3                53                   //����� ������ �� 3 ������� ��������                 @ Power level 3
#define W_MODE_54_POWER4                54                   //����� ������ �� 4 ������� ��������                 @ Power level 4
#define W_MODE_55_POWER5                55                   //����� ������ �� 5 ������� ��������                 @ Power level 5
#define W_MODE_56_POWER6                56                   //����� ������ �� 6 ������� ��������                 @ Power level 6
#define W_MODE_57_POWER7                57                   //����� ������ �� 7 ������� ��������                 @ Power level 7
#define W_MODE_58_POWER8                58                   //����� ������ �� 8 ������� ��������                 @ Power level 8
#define W_MODE_59_POWER9                59                   //����� ������ �� 9 ������� ��������                 @ Power level 9

#define F_MODE_0_BLOWING                 0                   //���������� ���������� ������                       @ Shutting down
#define F_MODE_1_BLOWING_OVERHEAT        1                   //�������� ��� ���������                             @ Shutting down
#define F_MODE_2_BLOWING_BR_SENSORS      2                   //�������� ��� ������ �������� �� ��� �������        @ Shutting down
#define F_MODE_3_BLOWING_FAULT           3                   //�������� ���  �������������� �����, ��, �� ��� ���������� ���������� ������ ������� @ Shutting down
#define F_MODE_4_BLOWING_NO_FP           4                   //���������� ������ ��� ��                           @ Shutting down
#define F_MODE_5_BLOWING_NO_GP           5                   //���������� ������ ��� ����� � ��                   @ Shutting down

#define T_MODE_0_PCB_TESTING             0                   //�������� �����                                     @ PCB testing
#define T_MODE_1_FAN_CALIBRATION         1                   //���������� ������������� ��                        @ Motor calibration

#define M_MODE_0_MANUAL                  0                   //������ ����� ������. ��������� ������ �������� �� ����������������� ������ @ Manual mode

//=============================================================������ ������ ��� ����������� �� ������ ����������
#define PANEL_MODE_STOPPED               0                   //����������
#define PANEL_MODE_IGNITION              1                   //������
#define PANEL_MODE_HIGH                  2                   //�������
#define PANEL_MODE_MEDIUM                3                   //�������
#define PANEL_MODE_LOW                   4                   //�����
#define PANEL_MODE_BLOW                  5                   //��������
#define PANEL_MODE_WAIT                  6                   //������
#define PANEL_MODE_NONAME                7                   //�� ����������
#define PANEL_MODE_VENTILATION           8                   //����������
#define PANEL_MODE_PUMP_ONLY             9                   //������ ������ �����

/*----------------------------------------------------------------------------------------------------------------------------------
                                   ������ (�����) ���������� ������ ��� �������� ������ �� ������ 
----------------------------------------------------------------------------------------------------------------------------------*/
#define MDM_TOTAL_TIME                   1//uint32_t ;��������, c                                   ; V                ; %.0f           ; ��.���.      ;
#define MDM_STAGE                        2//uint8_t  ;������                                        ; V                ; %.0f           ; ������       ;
#define MDM_MODE                         3//uint8_t  ;����� ������                                  ; V                ; %.0f           ; �����        ;
#define MDM_RUNNING_TIME                 4//uint32_t ;����� ����� ������, c                         ; V                ; %.0f           ; ��.������    ;
#define MDM_MODE_TIME                    5//uint32_t ;����� ������ �� ������, c                     ; V                ; %.0f           ; ��.���.      ;

#define MDM_U_SUPPLY                     6//uint16_t ;���������� �������, �*100                     ; V/100            ; %.2f           ; U, �         ;
#define MDM_T_FLAME                      7//int16_t  ;����������� ��                                ; V                ; %.0f           ; �����        ;
#define MDM_T_FRAME                      8//int16_t  ;����������� ����                              ; V                ; %.0f           ; �����        ;
#define MDM_T_PANEL                      9//int8_t   ;����������� ������� � ��                      ; V                ; %.0f           ; �������      ;
#define MDM_T_EXT                       10//int16_t  ;����������� ��������� �������                 ; V                ; %.0f           ; ������       ;
#define MDM_T_INLET                     11//int16_t  ;����������� �� �����                          ; V                ; %.0f           ; ������       ;
#define MDM_T_SETPOINT                  12//int8_t   ;����������� �������                           ; V                ; %.0f           ; ��������     ;
#define MDM_PWR_DEFINED                 13//uint8_t  ;�������� ��������                             ; V                ; %.0f           ; � ���        ;
#define MDM_PWR_REALIZED                14//uint8_t  ;������� ��������                              ; V                ; %.0f           ; � ���        ;
#define MDM_WORK_BY                     15//uint8_t  ;�� ������ ������� �������                     ; V                ; %.0f           ; ������ ��    ;
#define MDM_FAN_REV_DEFINED             16//uint8_t  ;�������� ������� ��                           ; V                ; %.0f           ; ��.���       ;
#define MDM_FAN_REV_MEASURED            17//uint8_t  ;���������� ������� ��                         ; V                ; %.0f           ; ��. ���      ;
#define MDM_FP_FREQ_DEFINED             18//uint16_t ;�������� ������� ��, ��*100                   ; V/100            ; %.2f           ; �� ���       ;
#define MDM_FP_FREQ_REALIZED            19//uint16_t ;������������� ������� ��, ��*100              ; V/100            ; %.2f           ; �� ���       ;
#define MDM_FAULT_CODE                  20//uint8_t  ;��� �������������                             ; V                ; %.0f           ; ������       ;
#define MDM_FLAME_LIMIT                 21//int16_t  ;������� ����� �������                         ; V                ; %.0f           ; �� lim       ;
#define MDM_FLAME_BREAK_NUM             22//uint8_t  ;���������� ������ ������� �� ����� ������     ; V                ; %.0f           ; ���.��.��    ;
#define MDM_OVERHEAT_TBORDER            23//int16_t  ;������� ���������                             ; V                ; %.0f           ; ��.������    ;
#define MDM_T_LIQUID                    24//int16_t  ;����������� ��������                          ; V                ; %.0f           ; ��           ;
#define MDM_T_OVERHEAT                  25//int16_t  ;����������� ���������                         ; V                ; %.0f           ; ����         ;
#define MDM_RELAY_STATE                 26//uint8_t  ;��������� ���� (���/����)                     ; V                ; %.0f           ; ����         ;
#define MDM_PUMP_STATE                  27//uint8_t  ;��������� ����� (���/����)                    ; V                ; %.0f           ; �����        ;
#define MDM_SIGNALING_STATE             28//uint8_t  ;��������� ������������ (������ ����/���)      ; V                ; %.0f           ; ������������ ;
#define MDM_ENGINE_STATE                29//uint8_t  ;��������� ��������� (�������/����������)      ; V                ; %.0f           ; ���������    ;
#define MDM_MODE_LIQUID_HEATER          30//uint8_t  ;����� ������ ����������� �������������        ; V                ; %.0f           ; ����� ������.;
#define MDM_PRESSURE                    31//uint8_t  ;����������� ��������, ���                     ; V                ; %.0f           ; ����.,���    ;
#define MDM_RUN_CNT                     32//uint16_t ;������� ��������
#define MDM_RUN_VENT_CNT                33//uint16_t ;������� �������� �� ����������
#define MDM_RUN_OK_CNT                  34//uint16_t ;������� �������� ����������� ��� ��������������
#define MDM_RUN_ERR_CNT                 35//uint16_t ;������� �������� ����������� � ���������������
#define MDM_POWER_0_TIME                36//uint32_t ;����� ������ �� 0 ��������
#define MDM_POWER_1_TIME                37//uint32   ;����� ������ �� 1 ��������
#define MDM_POWER_2_TIME                38//uint32   ;����� ������ �� 2 ��������
#define MDM_POWER_3_TIME                39//uint32   ;����� ������ �� 3 ��������
#define MDM_POWER_4_TIME                40//uint32   ;����� ������ �� 4 ��������
#define MDM_POWER_5_TIME                41//uint32   ;����� ������ �� 5 ��������
#define MDM_POWER_6_TIME                42//uint32   ;����� ������ �� 6 ��������
#define MDM_POWER_7_TIME                43//uint32   ;����� ������ �� 7 ��������
#define MDM_POWER_8_TIME                44//����� ������ �� 8 ��������
#define MDM_POWER_9_TIME                45//����� ������ �� 9 ��������
#define MDM_RUN_FIRST_CNT               46//���������� �������� � ������ �������
#define MDM_RUN_SECOND_CNT              47//���������� �������� �� ������ �������
#define MDM_NO_IGNITION_CNT             48//���������� ���������� (������� ������� ���������)
#define MDM_SPARK_STATE                 49//�����
#define MDM_VALVE_STATE                 50//������
#define MDM_HE_STATE                    51//���
#define MDM_ILLUMINATION                52//������������ ��
#define MDM_STAGE_TIME                  53//����� ������ �� ������
#define MDM_CURRENT                     54//��� ��
#define MDM_FLAME_ON                    55//����� ���� ��� ���

#define MDM_SIGNAL_ON_PROBE							56//������ � �����
#define MDM_T_PLATE											57//����������� �����
#define MDM_V_HIGH_PRESSURE							58//��������� ������� �������� ��������
#define MDM_V_CUTOFF										59//��������� ��������� �������
#define MDM_V_SMALL											60//��������� ������ �������
#define MDM_V_STRONG										61//��������� �������� �������
#define MDM_PROBE_FLAME_EXIST						62//���� ������� �������

#endif /* __PARAMS_NAME_H */
