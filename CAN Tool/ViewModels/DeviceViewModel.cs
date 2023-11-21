using CAN_Adapter;
using CAN_Tool.ViewModels.Base;
using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Diagnostics;
using System.Diagnostics.CodeAnalysis;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Media;
using CAN_Tool.Libs;
using CAN_Tool;
using ScottPlot;
using static CAN_Tool.Libs.Helper;
using CAN_Tool.Infrastructure.Commands;
using CAN_Tool.ViewModels;
using System.Windows.Input;

namespace OmniProtocol
{
    public class DeviceViewModel : ViewModel
    {

        public void Transmit(CanMessage msg)
        {
            NeedToTransmit?.Invoke(this, new NeedToTransmitEventArgs() { msgToTransmit = msg });
        }

        public event EventHandler NeedToTransmit;

        public bool WaitForFlag(ref bool flag, int delay)
        {
            int wd = 0;
            while (!flag && wd < delay)
            {
                wd++;
                Thread.Sleep(1);
            }
            if (!flag)
            {
                flag = false;
                return false;
            }
            else
            {
                flag = false;
                return true;
            }
        }

        public bool NotInManual(object parameter) => ManualMode;

        public void ExecuteCommand(int cmdNum, params byte[] data)
        {
            OmniMessage msg = new();
            msg.TransmitterType = 126;
            msg.TransmitterAddress = 6;
            msg.ReceiverId = ID;
            msg.PGN = 1;
            msg.Data = new byte[8];
            msg.Data[0] = (byte)(cmdNum >> 8);
            msg.Data[1] = (byte)(cmdNum & 0xFF);
            for (int i = 0; i < data.Length; i++)
                msg.Data[i + 2] = data[i];

            NeedToTransmit.Invoke(this, new NeedToTransmitEventArgs() { msgToTransmit = msg.ToCanMessage() });
        }

        public ICommand StartHeaterCommand { get; }
        private void OnStartHeaterCommandExecuted(object parameter)
        {
            ExecuteCommand(1, 0xff, 0xff);
        }



        public ICommand StopHeaterCommand { get; }
        private void OnStopHeaterCommandExecuted(object parameter)
        {
            ExecuteCommand(3);
        }



        public ICommand StartPumpCommand { get; }
        private void OnStartPumpCommandExecuted(object parameter)
        {
            ExecuteCommand(4, 0, 0);
        }

        public ICommand ClearErrorsCommand { get; }
        private void OnClearErrorsCommandExecuted(object parameter)
        {
            ExecuteCommand(5);
        }


        public ICommand StartVentCommand { get; }
        private void OnStartVentCommandExecuted(object parameter)
        {
            ExecuteCommand(10);
        }


        public ICommand CalibrateTermocouplesCommand { get; }
        private void OnCalibrateTermocouplesCommandExecuted(object parameter)
        {
            ExecuteCommand(20);
        }

        public DeviceViewModel(DeviceId newId)
        {
            LogInit();
            ID = newId;
            StartHeaterCommand = new LambdaCommand(OnStartHeaterCommandExecuted, NotInManual);
            StopHeaterCommand = new LambdaCommand(OnStopHeaterCommandExecuted, NotInManual);
            StartPumpCommand = new LambdaCommand(OnStartPumpCommandExecuted, NotInManual);
            StartVentCommand = new LambdaCommand(OnStartVentCommandExecuted, NotInManual);
            ClearErrorsCommand = new LambdaCommand(OnClearErrorsCommandExecuted, null);
            CalibrateTermocouplesCommand = new LambdaCommand(OnCalibrateTermocouplesCommandExecuted, NotInManual);
            if (Omni.Devices.ContainsKey(ID.Type))
                deviceReference = Omni.Devices[ID.Type];
        }

        public MainParameters Parameters { get; set; } = new();

        private DeviceId id;

        public DeviceId ID
        {
            get { return id; }
            set { Set(ref id, value); }
        }

        private DateOnly prodDate;

        public DateOnly ProductionDate
        {
            get => prodDate;
            set => Set(ref prodDate, value);
        }

        private byte[] firmware = new byte[4];

        [AffectsTo(nameof(FirmwareAsText))]
        public byte[] Firmware
        {
            get { return firmware; }
            set { Set(ref firmware, value); }
        }

        public string FirmwareAsText
        {
            get
            {
                if (firmware != null)
                    return $"{firmware[0]}.{firmware[1]}.{firmware[2]}.{firmware[3]}";
                else
                    return GetString("t_no_firmware_data");
            }
        }


        private uint serial1 = 0;

        [AffectsTo(nameof(SerialAsString))]
        public uint Serial1
        {
            get => serial1;
            set => Set(ref serial1, value);
        }

        private uint serial2 = 0;

        [AffectsTo(nameof(SerialAsString))]
        public uint Serial2
        {
            get => serial2;
            set => Set(ref serial2, value);
        }

        private uint serial3 = 0;

        [AffectsTo(nameof(SerialAsString))]
        public uint Serial3
        {
            get => serial3;
            set => Set(ref serial3, value);
        }

        public string SerialAsString => $"{serial1}.{serial2}.{serial3}";

        UpdatableList<StatusVariable> status = new();
        public UpdatableList<StatusVariable> Status => status;

        private readonly UpdatableList<ReadedParameter> readedParameters = new();
        public UpdatableList<ReadedParameter> ReadedParameters => readedParameters;

        private UpdatableList<ReadedBlackBoxValue> bbValues = new();
        public UpdatableList<ReadedBlackBoxValue> BBValues => bbValues;

        private UpdatableList<BBError> bbErrors = new();
        public UpdatableList<BBError> BBErrors => bbErrors;

        private readonly BindingList<MainParameters> log = new();
        public BindingList<MainParameters> Log => log;

        public Timberline20OmniViewModel Timber { set; get; } = new();

        private bool manualMode;

        public bool ManualMode
        {
            get { return manualMode; }
            set { Set(ref manualMode, value); }
        }

        public bool flagEraseDone = false;

        public bool flagSetAdrDone = false;

        public bool flagProgramDone = false;

        public bool flagTransmissionCheck = false;

        public bool flagCrcGetDone = false;

        public bool flagDataGetDone = false;

        public int receivedDataLength = 0;

        public uint receiverDataCrc = 0;

        public bool flagGetParamDone = false;

        public bool flagGetVersionDone = false;

        public bool flagGetBBDone = false;

        public bool waitForBB = false;

        public uint fragmentAdress = 0;

        public int receivedFragmentLength = 0;

        public uint receivedFragmentCrc = 0;

        public string Name => ToString();

        private Device deviceReference;

        public Device DeviceReference => deviceReference;

        public string Img => $"~\\..\\Images\\{id.Type}.jpg";

        public override string ToString()
        {
            if (deviceReference != null)
                return $"{deviceReference.Name}({id.Address})";
            else
                return $"Device #<{ID.Type}>({id.Address})";

        }

        public override bool Equals(object obj)
        {
            if (obj == null) return false;
            if (obj is not DeviceViewModel) return false;
            return ID.Equals((obj as DeviceViewModel).ID);
        }

        public override int GetHashCode()
        {
            return ID.GetHashCode();
        }

        private bool isLogWriting = false;

        public bool IsLogWriting
        {
            get { return isLogWriting; }
            private set { Set(ref isLogWriting, value); }
        }

        public List<double[]> LogData = new List<double[]>();

        private int logCurrentPos;

        public int LogCurrentPos
        {
            get => logCurrentPos;
            private set => Set(ref logCurrentPos, value);
        }


        public void LogTick()
        {
            Log.Insert(0, (MainParameters)Parameters.Clone());

            if (!isLogWriting)
                return;

            if (LogCurrentPos < LogData[0].Length)
            {
                foreach (StatusVariable sv in Status)
                    LogData[sv.Id][LogCurrentPos] = sv.Value;
                LogCurrentPos++;

            }
            else
            {
                LogStop();
                LogDataOverrun?.Invoke(this, null);
            }


        }

        public void LogInit(int length = 86400)
        {
            LogCurrentPos = 0;
            LogData = new List<double[]>();
            for (int i = 0; i < 150; i++) //Переменных в paramsname.h пока намного меньше, но поставим пока 140
            {
                LogData.Add(new double[length]);
            }
        }

        private bool[] supportedVariables = new bool[150];

        public bool[] SupportedVariables => supportedVariables;

        public int SupportedVariablesCount
        {
            get
            {
                int ret = 0;
                foreach (var s in supportedVariables)
                    if (s == true) ret++;
                return ret;
            }
        }
        public void LogStart()
        {
            LogInit();
            IsLogWriting = true;
        }
        public void LogClear()
        {
            LogInit();
        }

        public void LogStop()
        {
            IsLogWriting = false;
        }

        public void SaveReport()
        {

        }
        public event EventHandler LogDataOverrun;

    }
}
