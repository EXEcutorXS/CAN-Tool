using OmniProtocol;
using System;
using System.ComponentModel;
using System.Data;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using System.Windows.Threading;

namespace CAN_Tool.CustomControls
{
    public partial class HeaterControl : UserControl
    {
        HeaterDeviceViewModel Vm => DataContext as HeaterDeviceViewModel;

        private DataTable _waterfallTable;
        private readonly DispatcherTimer _refreshTimer;

        public HeaterControl()
        {
            InitializeComponent();
            DataContextChanged += OnDataContextChanged;

            _refreshTimer = new DispatcherTimer { Interval = TimeSpan.FromSeconds(1) };
            _refreshTimer.Tick += (_, _) => RefreshWaterfallRows();
            _refreshTimer.Start();
        }

        private void OnDataContextChanged(object sender, DependencyPropertyChangedEventArgs e)
        {
            if (e.OldValue is HeaterDeviceViewModel oldVm)
                oldVm.Status.ListChanged -= OnStatusListChanged;

            if (e.NewValue is HeaterDeviceViewModel newVm)
            {
                newVm.Status.ListChanged += OnStatusListChanged;
                BuildWaterfallTable();
            }
        }

        private void OnStatusListChanged(object sender, ListChangedEventArgs e)
        {
            if (e.ListChangedType is ListChangedType.ItemAdded
                                  or ListChangedType.ItemDeleted
                                  or ListChangedType.Reset)
                BuildWaterfallTable();
        }

        private void BuildWaterfallTable()
        {
            WaterfallGrid.ItemsSource = null;
            WaterfallGrid.Columns.Clear();

            if (Vm?.Status == null || Vm.Status.Count == 0)
            {
                _waterfallTable = null;
                return;
            }

            _waterfallTable = new DataTable();

            // Safe internal column names (C0, C1, ...) avoid any ShortName special chars.
            // ShortName is used only as the visible header.
            foreach (var sv in Vm.Status)
            {
                string colId = $"C{sv.Id}";
                _waterfallTable.Columns.Add(colId, typeof(string));
                WaterfallGrid.Columns.Add(new DataGridTextColumn
                {
                    Header  = sv.ShortName,
                    Binding = new System.Windows.Data.Binding(colId),
                    Width   = DataGridLength.Auto,
                });
            }

            // Row 0 = newest (History[0]), last row = oldest (History[HistorySize-1])
            for (int t = 0; t < StatusVariable.HistorySize; t++)
            {
                var row = _waterfallTable.NewRow();
                int col = 0;
                foreach (var sv in Vm.Status)
                    row[col++] = sv.History[t];
                _waterfallTable.Rows.Add(row);
            }

            WaterfallGrid.ItemsSource = _waterfallTable.DefaultView;
        }

        private void RefreshWaterfallRows()
        {
            if (_waterfallTable == null || Vm?.Status == null) return;
            if (_waterfallTable.Columns.Count != Vm.Status.Count) { BuildWaterfallTable(); return; }

            // Row t = History[t]: row 0 = newest, last row = oldest
            for (int t = 0; t < StatusVariable.HistorySize; t++)
            {
                var row = _waterfallTable.Rows[t];
                int col = 0;
                foreach (var sv in Vm.Status)
                    row[col++] = sv.History[t];
            }
        }

        private void FuelPumpMouseWheel(object sender, MouseWheelEventArgs e)
        {
            if (Vm == null || !Vm.OverrideState.FuelPumpOverriden) return;
            int newFrequency = Vm.OverrideState.FuelPumpOverridenFrequencyX100;
            int k = Keyboard.IsKeyDown(Key.LeftShift) ? 100 : 10;
            newFrequency += Math.Sign(e.Delta) * k;
            if (newFrequency < 0) newFrequency = 0;
            if (newFrequency > 1000) newFrequency = 1000;
            byte overrideByte1 = 0;
            byte overrideByte2 = 0;
            byte overrideStatesByte = 0;
            overrideByte1 |= 3;
            overrideByte1 |= 3 << 2;
            overrideByte1 |= 3 << 4;
            overrideByte1 |= 3 << 6;
            overrideByte2 |= 3;
            overrideStatesByte |= 3;
            overrideStatesByte |= 3 << 2;
            byte[] data = { overrideByte1, overrideByte2, overrideStatesByte, (byte)Vm.OverrideState.BlowerOverridenRevs, (byte)Vm.OverrideState.GlowPlugOverridenPower, (byte)(newFrequency>>8), (byte)(newFrequency & 0xFF), 0xFF };
            OmniMessage msg = new() { Pgn = 47, ReceiverId = Vm.Id, Data = data };
            Vm.Transmit(msg.ToCanMessage());
        }

        private void GlowPlugMouseWheel(object sender, MouseWheelEventArgs e)
        {
            if (Vm == null || !Vm.OverrideState.GlowPlugOverriden) return;
            int newPower = Vm.OverrideState.GlowPlugOverridenPower;
            int k = Keyboard.IsKeyDown(Key.LeftShift) ? 10 : 1;
            newPower += Math.Sign(e.Delta) * k;
            if (newPower < 0) newPower = 0;
            if (newPower > 100) newPower = 100;
            byte overrideByte1 = 0;
            byte overrideByte2 = 0;
            byte overrideStatesByte = 0;
            overrideByte1 |= 3;
            overrideByte1 |= 3 << 2;
            overrideByte1 |= 3 << 4;
            overrideByte1 |= 3 << 6;
            overrideByte2 |= 3;
            overrideStatesByte |= 3;
            overrideStatesByte |= 3 << 2;
            byte[] data = { overrideByte1, overrideByte2, overrideStatesByte, (byte)Vm.OverrideState.BlowerOverridenRevs, (byte)newPower, (byte)(Vm.OverrideState.FuelPumpOverridenFrequencyX100 >> 8), (byte)(Vm.OverrideState.FuelPumpOverridenFrequencyX100), 0xFF };
            OmniMessage msg = new() { Pgn = 47, ReceiverId = Vm.Id, Data = data };
            Vm.Transmit(msg.ToCanMessage());
        }


        private void SetBlowerOverrideVal(int newRevs)
        {
            if (newRevs < 0) newRevs = 0;
            if (newRevs > 200) newRevs = 200;
            byte overrideByte1 = 0;
            byte overrideByte2 = 0;
            byte overrideStatesByte = 0;
            overrideByte1 |= 3;
            overrideByte1 |= 3 << 2;
            overrideByte1 |= 3 << 4;
            overrideByte1 |= 3 << 6;
            overrideByte2 |= 3;
            overrideStatesByte |= 3;
            overrideStatesByte |= 3 << 2;
            byte[] data = { overrideByte1, overrideByte2, overrideStatesByte, (byte)newRevs, (byte)Vm.OverrideState.GlowPlugOverridenPower, (byte)(Vm.OverrideState.FuelPumpOverridenFrequencyX100 >> 8), (byte)(Vm.OverrideState.FuelPumpOverridenFrequencyX100), 0xFF };
            OmniMessage msg = new() { Pgn = 47, ReceiverId = Vm.Id, Data = data };
            Vm.Transmit(msg.ToCanMessage());
        }

        private void BlowerMouseWheel(object sender, MouseWheelEventArgs e)
        {
            if (Vm == null || !Vm.OverrideState.BlowerOverriden) return;
            int newRevs = Vm.OverrideState.BlowerOverridenRevs;
            int k = Keyboard.IsKeyDown(Key.LeftShift) ? 10 : 1;
            newRevs += Math.Sign(e.Delta) * k;
            SetBlowerOverrideVal(newRevs);
        }

        private void BlowerOverrideClick(object sender, RoutedEventArgs e)
        {
            byte overrideByte1 = 0;
            byte overrideByte2 = 0;
            byte overrideStatesByte = 0;
            overrideByte1 |= 3;
            overrideByte1 |= 3 << 2;
            overrideByte1 |= 3 << 4;
            overrideByte1 |= 3 << 6;
            if (!Vm.OverrideState.BlowerOverriden) overrideByte2 |= 1;
            overrideStatesByte |= 3;
            overrideStatesByte |= 3 << 2;
            byte[] data = { overrideByte1, overrideByte2, overrideStatesByte, (byte)Vm.OverrideState.BlowerOverridenRevs, (byte)Vm.OverrideState.GlowPlugOverridenPower, (byte)(Vm.OverrideState.FuelPumpOverridenFrequencyX100>>8), (byte)(Vm.OverrideState.FuelPumpOverridenFrequencyX100), 0xFF };
            OmniMessage msg = new() { Pgn = 47, ReceiverId = Vm.Id, Data = data };
            Vm.Transmit(msg.ToCanMessage());
        }

        private void FuelPumpClick(object sender, RoutedEventArgs e)
        {
            byte overrideByte1 = 0;
            byte overrideByte2 = 0;
            byte overrideStatesByte = 0;
            if (!Vm.OverrideState.FuelPumpOverriden) overrideByte1 |= 1;
            overrideByte1 |= 3 << 2;
            overrideByte1 |= 3 << 4;
            overrideByte1 |= 3 << 6;
            overrideByte2 |= 3;
            overrideStatesByte |= 3;
            overrideStatesByte |= 3 << 2;
            byte[] data = { overrideByte1, overrideByte2, overrideStatesByte, (byte)Vm.OverrideState.BlowerOverridenRevs, (byte)Vm.OverrideState.GlowPlugOverridenPower, (byte)(Vm.OverrideState.FuelPumpOverridenFrequencyX100 >> 8), (byte)(Vm.OverrideState.FuelPumpOverridenFrequencyX100), 0xFF };
            OmniMessage msg = new() { Pgn = 47, ReceiverId = Vm.Id, Data = data };
            Vm.Transmit(msg.ToCanMessage());
        }

        private void GlowPlugClick(object sender, RoutedEventArgs e)
        {

            byte overrideByte1 = 0;
            byte overrideByte2 = 0;
            byte overrideStatesByte = 0;
            overrideByte1 |= 3;
            overrideByte1 |= 3 << 2;
            if (!Vm.OverrideState.GlowPlugOverriden) overrideByte1 |= 1 << 4;
            overrideByte1 |= 3 << 6;
            overrideByte2 |= 3;
            overrideStatesByte |= 3;
            overrideStatesByte |= 3 << 2;
            byte[] data = { overrideByte1, overrideByte2, overrideStatesByte, (byte)Vm.OverrideState.BlowerOverridenRevs, (byte)Vm.OverrideState.GlowPlugOverridenPower, (byte)(Vm.OverrideState.FuelPumpOverridenFrequencyX100 >> 8), (byte)(Vm.OverrideState.FuelPumpOverridenFrequencyX100), 0xFF };
            OmniMessage msg = new() { Pgn = 47, ReceiverId = Vm.Id, Data = data };
            Vm.Transmit(msg.ToCanMessage());

        }

        private void ReduceOverridenRevsButtonClick(object sender, RoutedEventArgs e)
        {
            SetBlowerOverrideVal(Keyboard.IsKeyDown(Key.LeftShift)? Vm.OverrideState.BlowerOverridenRevs-10: Vm.OverrideState.BlowerOverridenRevs-1);
        }

        private void IncreaseOverridenRevsButtonClick(object sender, RoutedEventArgs e)
        {
            SetBlowerOverrideVal(Keyboard.IsKeyDown(Key.LeftShift) ? Vm.OverrideState.BlowerOverridenRevs + 10 : Vm.OverrideState.BlowerOverridenRevs + 1);
        }
    }
}
