using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;

namespace OmniProtocol
{
    public partial class HeaterDeviceViewModel : DeviceViewModel
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

        public HeaterDeviceViewModel(DeviceId id) : base(id)
        {
            SecondMessages = (DeviceReference?.DevType == DeviceType_t.Binar ||
                              DeviceReference?.DevType == DeviceType_t.Planar);

            StartHeaterCommand = new RelayCommand(() => ExecuteCommand(1, 0xff, 0xff));
            StopHeaterCommand  = new RelayCommand(() => ExecuteCommand(3));
            StartPumpCommand   = new RelayCommand(() => ExecuteCommand(4, 0, 0));
            StartVentCommand   = new RelayCommand(() => ExecuteCommand(10));
            ClearErrorsCommand = new RelayCommand(() => ExecuteCommand(5));
            CalibrateTermocouplesCommand = new RelayCommand(() => ExecuteCommand(20));
        }

        // ── Состояние ──────────────────────────────────────────────────

        [ObservableProperty] private OverrideStateClass overrideState = new();

        // ── Команды управления нагревателем ────────────────────────────
        public RelayCommand StartHeaterCommand { get; }
        public RelayCommand StopHeaterCommand { get; }
        public RelayCommand StartPumpCommand { get; }
        public RelayCommand ClearErrorsCommand { get; }
        public RelayCommand StartVentCommand { get; }
        public RelayCommand CalibrateTermocouplesCommand { get; }

        [RelayCommand]
        public void IncPowerLevel(object parameter)
        {
            byte powerLevel = (byte)Parameters.SetPowerLevel;
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
            byte powerLevel = (byte)Parameters.SetPowerLevel;
            if (powerLevel == 0) return;
            if (powerLevel == 254) powerLevel = 9;
            else powerLevel--;
            byte[] data = { 0, 19, powerLevel, 0xFF, 0xFF, 0xFF, 0xFF, 0xFF };
            OmniMessage msg = new() { Pgn = 1, ReceiverId = Id, Data = data };
            Transmit(msg.ToCanMessage());
        }

        [RelayCommand]
        public void UpdateOverrideStatus()
        {
            byte overrideByte1 = 0, overrideByte2 = 0, overrideStatesByte = 0;
            if (OverrideState.FuelPumpOverriden) overrideByte1 |= 1;
            if (OverrideState.RelayOverriden)    overrideByte1 |= 1 << 2;
            if (OverrideState.GlowPlugOverriden) overrideByte1 |= 1 << 4;
            if (OverrideState.PumpOverriden)     overrideByte1 |= 1 << 6;
            if (OverrideState.BlowerOverriden)   overrideByte2 |= 1;
            if (OverrideState.PumpOverridenState)  overrideStatesByte |= 1;
            if (OverrideState.RelayOverridenState) overrideStatesByte |= 4;
            byte[] data = {
                overrideByte1, overrideByte2, overrideStatesByte,
                (byte)OverrideState.BlowerOverridenRevs,
                (byte)OverrideState.GlowPlugOverridenPower,
                (byte)(OverrideState.FuelPumpOverridenFrequencyX100 / 256),
                (byte)(OverrideState.FuelPumpOverridenFrequencyX100 / 256),
                0xFF
            };
            Transmit(new OmniMessage { Pgn = 47, ReceiverId = Id, Data = data }.ToCanMessage());
        }

        [RelayCommand]
        public void UpdateOverrideFuelPumpFreq()
        {
            byte[] data = { 0xFF, 0xFF, 0xFF, 0xFF, 0xFF,
                (byte)(OverrideState.FuelPumpOverridenFrequencyX100 / 256),
                (byte)(OverrideState.FuelPumpOverridenFrequencyX100 / 256), 0xFF };
            Transmit(new OmniMessage { Pgn = 47, ReceiverId = Id, Data = data }.ToCanMessage());
        }

        [RelayCommand]
        public void UpdateOverrideBlower()
        {
            byte[] data = { 0xFF, 0xFF, 0xFF, (byte)OverrideState.BlowerOverridenRevs, 0xFF, 0xFF, 0xFF, 0xFF };
            Transmit(new OmniMessage { Pgn = 47, ReceiverId = Id, Data = data }.ToCanMessage());
        }

        [RelayCommand]
        public void UpdateOverrideGlowPlug()
        {
            byte[] data = { 0xFF, 0xFF, 0xFF, 0xFF, (byte)OverrideState.GlowPlugOverridenPower, 0xFF, 0xFF, 0xFF };
            Transmit(new OmniMessage { Pgn = 47, ReceiverId = Id, Data = data }.ToCanMessage());
        }

        [RelayCommand]
        public void UpdateOverrideGlowPlugFlag()
        {
            byte flag = (byte)(OverrideState.GlowPlugOverriden ? 0b11011111 : 0b11001111);
            byte[] data = { flag, 0xFF, 0xFF, 0xFF, 0xFF, 0xFF, 0xFF, 0xFF };
            Transmit(new OmniMessage { Pgn = 47, ReceiverId = Id, Data = data }.ToCanMessage());
        }

        [RelayCommand]
        public void UpdateOverrideFuelPumpFlag()
        {
            byte flag = (byte)(OverrideState.FuelPumpOverriden ? 0b11011111 : 0b11001111);
            byte[] data = { flag, 0xFF, 0xFF, 0xFF, 0xFF, 0xFF, 0xFF, 0xFF };
            Transmit(new OmniMessage { Pgn = 47, ReceiverId = Id, Data = data }.ToCanMessage());
        }
    }
}
