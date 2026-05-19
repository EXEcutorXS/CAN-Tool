using CAN_Tool.ViewModels;
using OmniProtocol;
using ScottPlot.WPF;
using System.Collections.Generic;
using System.Linq;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using System.Windows.Media;
using static CAN_Tool.Libs.Helper;

namespace CAN_Tool.CustomControls
{
    public partial class OmniModeView : UserControl
    {
        private MainWindowViewModel Vm => (MainWindowViewModel)DataContext;

        public OmniModeView() => InitializeComponent();

        private bool _updatingSliders;

        private void OmniModeView_Loaded(object sender, RoutedEventArgs e)
        {
            Vm.myChart = Chart;
            Vm.CanAdapter.GotNewMessage += MessageHandler;
        }

        private void OmniModeView_Unloaded(object sender, RoutedEventArgs e)
        {
            if (Vm == null) return;
            Vm.CanAdapter.GotNewMessage -= MessageHandler;
            Vm.myChart = null;
        }

        private void ColorSlider_ValueChanged(object sender, RoutedPropertyChangedEventArgs<double> e)
        {
            if (_updatingSliders) return;
            var color = Color.FromRgb((byte)SliderR.Value, (byte)SliderG.Value, (byte)SliderB.Value);
            UpdateColorPreview(color);
            if (DataSet.SelectedItem != null && DataSet.SelectedItems.Count == 1)
                (DataSet.SelectedItem as StatusVariable).ChartBrush = new SolidColorBrush(color);
        }

        private void UpdateColorPreview(Color color)
        {
            ColorPreview.Background = new SolidColorBrush(color);
            LabelR.Text = color.R.ToString();
            LabelG.Text = color.G.ToString();
            LabelB.Text = color.B.ToString();
            HexLabel.Text = $"#{color.R:X2}{color.G:X2}{color.B:X2}";
        }

        private void SetPickerColor(Color color)
        {
            _updatingSliders = true;
            SliderR.Value = color.R;
            SliderG.Value = color.G;
            SliderB.Value = color.B;
            _updatingSliders = false;
            UpdateColorPreview(color);
        }

        private void MessageHandler(object sender, System.EventArgs args)
        {
            Dispatcher.Invoke(() =>
            {
                if (LogExpander.IsExpanded)
                {
                    OmniMessage m = new OmniMessage((args as GotCanMessageEventArgs).receivedMessage);
                    LogField.AppendText(m.ToString());
                }
            });
        }

        private void AC2PmessagesField_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            try { Vm.SelectedMessage = (OmniMessage)(sender as DataGrid).SelectedItems[(sender as DataGrid).SelectedItems.Count - 1]; }
            catch { }
        }

        private void DataSet_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            if (DataSet.SelectedItem != null)
                SetPickerColor(((DataSet.SelectedItem as StatusVariable).ChartBrush as SolidColorBrush).Color);
        }

        private void ManualAirMouseWheelEventHandler(object sender, MouseWheelEventArgs e)
        {
            int k = Keyboard.IsKeyDown(Key.LeftShift) ? 10 : 1;
            Vm.ManualPage.ChangeManualAirBlowerCommand.Execute((System.Math.Sign(e.Delta) * k).ToString());
        }

        private void ManualFuelMouseWheelEventHandler(object sender, MouseWheelEventArgs e)
        {
            int k = Keyboard.IsKeyDown(Key.LeftShift) ? 100 : 5;
            Vm.ManualPage.ChangeManualFuelPumpCommand.Execute((System.Math.Sign(e.Delta) * k).ToString());
        }

        private void ManualGlowPlugMouseWheelEventHandler(object sender, MouseWheelEventArgs e)
        {
            int k = Keyboard.IsKeyDown(Key.LeftShift) ? 10 : 1;
            Vm.ManualPage.ChangeGlowPlugCommand.Execute((System.Math.Sign(e.Delta) * k).ToString());
        }

        #region Command constructor
        private void UpdateCommand(object sender, System.EventArgs e)
        {
            double value = 0;
            if (sender is ComboBox cb)
                value = ((KeyValuePair<int, string>)cb.SelectedItem).Key;
            if (sender is TextBox tb)
                try { value = System.Convert.ToDouble(tb.Text); } catch { value = 0; }

            Vm.CommandParametersArray[System.Convert.ToInt32((sender as Control).Name.Substring(6))] = value;
            OmniCommand cmd = ((KeyValuePair<int, OmniCommand>)CommandSelector.SelectedItem).Value;
            ulong id = (ulong)cmd.Id;
            ulong res = id << 48;
            OmniPgnParameter[] pars = cmd.Parameters.Where(p => p.AnswerOnly == false).ToArray();
            for (int i = 0; i < pars.Length; i++)
            {
                OmniPgnParameter p = pars[i];
                ulong rawValue = (ulong)((Vm.CommandParametersArray[i] - p.b) / p.a);
                int shift = (7 - p.StartByte) * 8;
                shift -= ((p.BitLength + 7) / 8) * 8 - 8;
                shift += p.StartBit;
                rawValue <<= shift;
                res |= rawValue;
            }
            byte[] data = new byte[8];
            for (int i = 0; i < 8; i++)
                data[i] = (byte)((res >> (7 - i) * 8) & 0xFF);
            Vm.CustomMessage.Data = data;
        }

        private void ComboBox_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            ComboBox comboBox = sender as ComboBox;
            OmniCommand cmd = ((KeyValuePair<int, OmniCommand>)comboBox.SelectedItem).Value;
            CommandParameterPanel.Children.Clear();
            Vm.CommandParametersArray = new double[cmd.Parameters.Count];
            Vm.CustomMessage.Pgn = 1;
            Vm.CustomMessage.Data[1] = (byte)cmd.Id;
            if (Vm.OmniInstance.SelectedConnectedDevice != null)
            {
                Vm.CustomMessage.ReceiverId.Address = Vm.OmniInstance.SelectedConnectedDevice.Id.Address;
                Vm.CustomMessage.ReceiverId.Type = Vm.OmniInstance.SelectedConnectedDevice.Id.Type;
            }

            int counter = 0;
            foreach (OmniPgnParameter p in cmd.Parameters.Where(p => p.AnswerOnly == false))
            {
                StackPanel panel = new() { Orientation = Orientation.Horizontal };
                Label label = new() { Content = GetString(p.Name), Name = $"label_{counter}", Margin = new Thickness(10), VerticalAlignment = VerticalAlignment.Center };
                panel.Children.Add(label);
                if (p.Meanings != null && p.Meanings.Count > 0)
                {
                    ComboBox cb = new()
                    {
                        ItemsSource = p.Meanings.Select(s => new KeyValuePair<int, string>(s.Key, GetString(s.Value))),
                        DisplayMemberPath = "Value",
                        Name = $"field_{counter}",
                        SelectedIndex = (int)p.DefaultValue,
                        Margin = new Thickness(10),
                        VerticalAlignment = VerticalAlignment.Center
                    };
                    cb.SelectionChanged += UpdateCommand;
                    panel.Children.Add(cb);
                }
                else
                {
                    TextBox tb = new()
                    {
                        Name = $"field_{counter}",
                        Text = p.DefaultValue.ToString(),
                        Margin = new Thickness(10),
                        VerticalAlignment = VerticalAlignment.Center,
                        Style = (Style)App.Current.TryFindResource("MaterialDesignOutlinedTextBox")
                    };
                    tb.TextChanged += UpdateCommand;
                    panel.Children.Add(tb);
                }
                Vm.CommandParametersArray[counter++] = p.DefaultValue;
                CommandParameterPanel.Children.Add(panel);
            }
        }
        #endregion
    }
}
