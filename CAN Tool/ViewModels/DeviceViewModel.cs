using CAN_Tool;
using CAN_Tool.Infrastructure.Commands;
using CAN_Tool.Libs;
using CAN_Tool.ViewModels;
using CAN_Tool.ViewModels.Base;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
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
    public partial class DeviceViewModel : ObservableObject
    {
        public partial class OverrideStateClass : ObservableObject
        {
            [ObservableProperty] public bool blowerOverriden;
            [ObservableProperty] public bool fuelPumpOverriden;
            [ObservableProperty] public bool glowPlugOverriden;
            [ObservableProperty] public bool relayOverriden;
            [ObservableProperty] public bool pumpOverriden;

            [ObservableProperty] public int blowerOverridenRevs;
            [ObservableProperty] public int fuelPumpOverridenFrequencyX100;
            [ObservableProperty] public int glowPlugOverridenPower;
            [ObservableProperty] public bool relayOverridenState;
            [ObservableProperty] public bool pumpOverridenState;
        }

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

            if (DeviceReference?.DevType == DeviceType.Binar || DeviceReference?.DevType == DeviceType.Planar)
                SecondMessages = true;
        }

        [RelayCommand]
        public void IncPowerLevel(object parameter)
        {

            byte powerLevel = (byte)(Parameters.SetPowerLevel);
            if (powerLevel == 254) return;
            if (powerLevel > 8) powerLevel = 254;
            else powerLevel++;
            byte[] data = { 0, 19, powerLevel, 0xFF, 0xFF, 0xFF, 0xFF, 0xFF };
            OmniMessage msg = new() { Pgn = 1, ReceiverId = Id, Data = data };
            Transmit(msg.ToCanMessage());
        }

        [RelayCommand]
        public void DecPowerLevel(object parameter)
        {

            byte powerLevel = (byte)(Parameters.SetPowerLevel);
            if (powerLevel == 0) return;
            if (powerLevel == 254) powerLevel = 9;
            else powerLevel--;
            byte[] data = { 0, 19, powerLevel, 0xFF, 0xFF, 0xFF, 0xFF, 0xFF };
            OmniMessage msg = new() { Pgn = 1, ReceiverId = Id, Data = data };
            Transmit(msg.ToCanMessage());
        }

        [RelayCommand]
        public void updateOverrideStatus()
        {
            byte overrideByte1 = 0;
            byte overrideByte2 = 0;
            byte overrideStatesByte = 0;
            if (OverrideState.FuelPumpOverriden) overrideByte1 |= 1;
            if (OverrideState.RelayOverriden) overrideByte1 |= 1<<2;
            if (OverrideState.GlowPlugOverriden) overrideByte1 |= 1<<4;
            if (OverrideState.PumpOverriden) overrideByte1 |= 1<<6;
            if (OverrideState.BlowerOverriden) overrideByte2 |= 1;
            if (OverrideState.PumpOverridenState) overrideStatesByte |= 1;
            if (OverrideState.RelayOverridenState) overrideStatesByte |= 4;
            byte[] data = { overrideByte1, overrideByte2, overrideStatesByte, (byte)OverrideState.BlowerOverridenRevs, (byte)OverrideState.GlowPlugOverridenPower, (byte)(OverrideState.FuelPumpOverridenFrequencyX100 / 256), (byte)(OverrideState.FuelPumpOverridenFrequencyX100 / 256), 0xFF };
            OmniMessage msg = new() { Pgn = 47, ReceiverId = Id, Data = data };
            Transmit(msg.ToCanMessage());

        }
        [RelayCommand]
        public void updateOverrideFuelPumpFreq()
        {
            byte[] data = {0xFF, 0xFF, 0xFF, 0xFF, 0xFF, (byte)(OverrideState.FuelPumpOverridenFrequencyX100/256), (byte)(OverrideState.FuelPumpOverridenFrequencyX100 / 256) ,0xFF};
            OmniMessage msg = new() {Pgn=47,ReceiverId = Id, Data = data};
            Transmit(msg.ToCanMessage());
        }

        [RelayCommand]
        public void updateOverrideBlower()
        {
            byte[] data = { 0xFF, 0xFF, 0xFF, (byte)OverrideState.BlowerOverridenRevs, 0xFF, 0xFF, 0xFF, 0xFF };
            OmniMessage msg = new() { Pgn = 47, ReceiverId = Id, Data = data };
            Transmit(msg.ToCanMessage());
        }

        [RelayCommand]
        public void updateOverrideGlowPlug()
        {
            byte[] data = { 0xFF, 0xFF, 0xFF, 0xFF, (byte)OverrideState.GlowPlugOverridenPower, 0xFF, 0xFF, 0xFF};
            OmniMessage msg = new() { Pgn = 47, ReceiverId = Id, Data = data };
            Transmit(msg.ToCanMessage());
        }

        [RelayCommand]
        public void updateOverrideGlowPlugFlag()
        {
            byte flag = (byte)(OverrideState.GlowPlugOverriden ? 0b11011111 : 0b11001111);
            byte[] data = { flag, 0xFF, 0xFF, 0xFF, 0xFF, 0xFF, 0xFF, 0xFF };
            OmniMessage msg = new() { Pgn = 47, ReceiverId = Id, Data = data };
            Transmit(msg.ToCanMessage());
        }

        [RelayCommand]
        public void updateOverrideFuelPumpFlag()
        {
            byte flag = (byte)(OverrideState.FuelPumpOverriden ? 0b11011111 : 0b11001111);
            byte[] data = { flag, 0xFF, 0xFF, 0xFF, 0xFF, 0xFF, 0xFF, 0xFF };
            OmniMessage msg = new() { Pgn = 47, ReceiverId = Id, Data = data };
            Transmit(msg.ToCanMessage());
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
            msg.ReceiverId.Type = Id.Type;
            msg.ReceiverId.Address = Id.Address;
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

        public CommonParameters Parameters { get; set; } = new();

        [ObservableProperty] private DeviceId id;

        [ObservableProperty] private DateOnly productionDate;

        [ObservableProperty] private bool secondMessages = false;

        public BindingList<int> Serial { get; } = new() { 0, 0, 0 };
        public BindingList<int> Firmware { get; } = new() { 0, 0, 0 ,0};
        public BindingList<int> BootFirmware { get; } = new() { 0, 0, 0, 0 };

        public UpdatableList<StatusVariable> Status { get; } = new();

        public UpdatableList<ReadedParameter> ReadParameters { get; } = new();

        public UpdatableList<ReadedBlackBoxValue> BbValues { get; } = new();

        public BindingList<BbError> BbErrors { get; } = new();

        public ObservableCollection<CommonParameters> Log { get; } = new();

        public Timberline20OmniViewModel TimberlineParams { set; get; } = new();

        public ACInverterViewModel ACInverterParams { set; get; } = new();

        public GenericLoadTrippleViewModel GenericLoadTripple { set; get; } = new();

        [ObservableProperty] public bool manualMode;

        [ObservableProperty] OverrideStateClass overrideState = new();

        public bool flagEraseDone = false;

        public bool flagSetAdrDone = false;

        public bool flagProgramDone = false;

        public bool flagTransmissionCheck = false;

        public bool flagCrcGetDone = false;

        public bool flagDataGetDone = false;

        public int receivedDataLength = 0;

        public uint receiverDataCrc = 0;

        public bool flagGetParamDone = false;

        public bool flagGetBbDone = false;

        public bool waitForBb = false;

        public uint fragmentAddress = 0;

        public int receivedFragmentLength = 0;

        public uint receivedFragmentCrc = 0;

        public string Name => ToString();

        public DeviceTemplate DeviceReference { get; }

        public string Img => $@"~\..\Images\{Id.Type}.jpg";

        public override string ToString() { return DeviceReference != null ? $"{DeviceReference.Name}({Id.Address})" : $"Device #<{Id.Type}>({Id.Address})"; }

        public override bool Equals(object obj)
        {
            if (obj == null || obj.GetType()!=typeof(DeviceViewModel)) return false;
            return Id.Equals((obj as DeviceViewModel).Id);
        }

        public override int GetHashCode() { return Id.GetHashCode(); }

        [ObservableProperty] private bool isLogWriting = true;

        public List<double[]> LogData = new();

        public double[] PressureLog = new double[720000];

        [ObservableProperty] public int pressureLogPointer = 0;
        public bool PressureLogWriting = false;

        [ObservableProperty] private int logCurrentPos;
       

        public void LogTick()
        {
            Log.Insert(0,((CommonParameters)Parameters.Clone()));
            if (Log.Count > 120) Log.RemoveAt(120);
            if (!IsLogWriting)
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
