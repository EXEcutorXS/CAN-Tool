using CAN_Tool;
using CAN_Tool.Infrastructure.Commands;
using CAN_Tool.Libs;
using CAN_Tool.ViewModels;
using CAN_Tool.ViewModels.Base;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Linq;
using System.Threading;
using System.Windows;
using System.Windows.Input;
using static CAN_Tool.Libs.Helper;

namespace OmniProtocol
{
    public class DeviceViewModel : ViewModel
    {

        public DeviceViewModel(DeviceId newId)
        {
            LogInit();
            Id = newId;
            StartHeaterCommand = new LambdaCommand(x => ExecuteCommand(1, 0xff, 0xff), NotInManual);
            StopHeaterCommand = new LambdaCommand(x => ExecuteCommand(3), NotInManual);
            StartPumpCommand = new LambdaCommand(x => ExecuteCommand(4, 0, 0), NotInManual);
            StartVentCommand = new LambdaCommand(x => ExecuteCommand(10), NotInManual);
            ClearErrorsCommand = new LambdaCommand(x => ExecuteCommand(5), NotInManual);
            CalibrateTermocouplesCommand = new LambdaCommand(x => ExecuteCommand(20), NotInManual);
            if (Omni.Devices.TryGetValue(Id.Type, out var device))
                DeviceReference = device;
        }

        public void Transmit(CanMessage msg)
        {
            if (Application.Current.MainWindow != null)
                ((MainWindowViewModel)Application.Current.MainWindow.DataContext).CanAdapter.Transmit(msg);
        }

        public static void TransmitStatic(CanMessage msg)
        {
            if (Application.Current.MainWindow != null)
                ((MainWindowViewModel)Application.Current.MainWindow.DataContext).CanAdapter.Transmit(msg);
        }

        public bool WaitForFlag(ref bool flag, int delay)
        {
            var wd = 0;
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

        public bool NotInManual(object parameter) => !ManualMode;

        public void ExecuteCommand(int cmdNum, params byte[] data)
        {
            OmniMessage msg = new();
            msg.TransmitterId.Type = 126;
            msg.TransmitterId.Address = 6;
            msg.ReceiverId.Type = id.Type;
            msg.ReceiverId.Address = id.Address;
            msg.Pgn = 1;
            msg.Data = new byte[8];
            msg.Data[0] = (byte)(cmdNum >> 8);
            msg.Data[1] = (byte)(cmdNum & 0xFF);
            for (var i = 0; i < data.Length; i++)
                msg.Data[i + 2] = data[i];
            Transmit(msg.ToCanMessage());
        }

        public static void ExecuteCommandOnDevice(Tuple<DeviceId, int, byte[]> arg)
        {
            OmniMessage msg = new();
            msg.TransmitterId.Type = 126;
            msg.TransmitterId.Address = 6;
            msg.ReceiverId.Address = arg.Item1.Address;
            msg.ReceiverId.Type = arg.Item1.Type;
            msg.Pgn = 1;
            msg.Data = new byte[8];
            msg.Data[0] = (byte)(arg.Item2 >> 8);
            msg.Data[1] = (byte)(arg.Item2 & 0xFF);
            for (var i = 0; i < arg.Item3.Length; i++)
                msg.Data[i + 2] = arg.Item3[i];
            TransmitStatic(msg.ToCanMessage());
        }

        public ICommand StartHeaterCommand { get; }

        public ICommand StopHeaterCommand { get; }

        public ICommand StartPumpCommand { get; }

        public ICommand ClearErrorsCommand { get; }

        public ICommand StartVentCommand { get; }

        public ICommand CalibrateTermocouplesCommand { get; }

        public MainParameters Parameters { get; set; } = new();

        private DeviceId id;

        public DeviceId Id { get => id; set => Set(ref id, value); }

        private DateOnly prodDate;

        public DateOnly ProductionDate { get => prodDate; set => Set(ref prodDate, value); }

        private byte[] firmware = new byte[4];

        [AffectsTo(nameof(FirmwareAsText))]
        public byte[] Firmware { get => firmware; set => Set(ref firmware, value); }

        public string FirmwareAsText => firmware != null ? $"{firmware[0]}.{firmware[1]}.{firmware[2]}.{firmware[3]}" : GetString("t_no_firmware_data");

        private uint serial1 = 0;

        [AffectsTo(nameof(SerialAsString))]
        public uint Serial1 { get => serial1; set => Set(ref serial1, value); }

        private uint serial2 = 0;

        [AffectsTo(nameof(SerialAsString))]
        public uint Serial2 { get => serial2; set => Set(ref serial2, value); }

        private uint serial3 = 0;
        [AffectsTo(nameof(SerialAsString))]
        public uint Serial3 { get => serial3; set => Set(ref serial3, value); }

        public string SerialAsString => $"{serial1}.{serial2}.{serial3}";

        public UpdatableList<StatusVariable> Status { get; } = new();

        public UpdatableList<ReadedParameter> ReadParameters { get; } = new();

        public UpdatableList<ReadedBlackBoxValue> BbValues { get; } = new();

        public BindingList<BbError> BbErrors { get; } = new();

        public ObservableCollection<MainParameters> Log { get; } = new();

        public Timberline20OmniViewModel TimberlineParams { set; get; } = new();

        private bool manualMode;

        public bool ManualMode { get => manualMode; set => Set(ref manualMode, value); }

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

        public bool flagGetBbDone = false;

        public bool waitForBb = false;

        public uint fragmentAddress = 0;

        public int receivedFragmentLength = 0;

        public uint receivedFragmentCrc = 0;

        public string Name => ToString();

        public Device DeviceReference { get; }

        public string Img => $@"~\..\Images\{id.Type}.jpg";

        public override string ToString() { return DeviceReference != null ? $"{DeviceReference.Name}({id.Address})" : $"Device #<{Id.Type}>({id.Address})"; }

        public override bool Equals(object obj)
        {
            if (obj == null) return false;
            return obj is DeviceViewModel device && Id.Equals(device.Id);
        }

        public override int GetHashCode() { return Id.GetHashCode(); }

        private bool isLogWriting;

        public bool IsLogWriting { get => isLogWriting; private set => Set(ref isLogWriting, value); }

        public List<double[]> LogData = new();

        private int logCurrentPos;

        public int LogCurrentPos { get => logCurrentPos; private set => Set(ref logCurrentPos, value); }

        public void LogTick()
        {
            Log.Insert(0,((MainParameters)Parameters.Clone()));
            if (Log.Count > 120) Log.RemoveAt(120);
            if (!isLogWriting)
                return;

            if (LogCurrentPos < LogData[0].Length)
            {
                foreach (var sv in Status)
                    LogData[sv.Id][LogCurrentPos] = sv.Value;
                LogCurrentPos++;
            }
            else
                LogStop();
        }

        public void LogInit(int length = 14400)
        {
            LogCurrentPos = 0;
            LogData = new List<double[]>();
            for (var i = 0; i < 150; i++) //Переменных в paramsname.h пока намного меньше, но поставим пока 150
            {
                LogData.Add(new double[length]);
            }
        }

        public bool[] SupportedVariables { get; } = new bool[150];

        public void LogStart()
        {
            LogInit();
            IsLogWriting = true;
        }

        public void LogStop()
        {
            IsLogWriting = false;
        }
    }
}
