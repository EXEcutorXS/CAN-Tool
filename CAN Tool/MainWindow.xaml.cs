using OmniProtocol;
using CAN_Tool.ViewModels;
using MaterialDesignThemes.Wpf;
using ScottPlot.MarkerShapes;
using ScottPlot.Renderable;
using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Text.Json;
using System.Threading;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Controls.Primitives;
using static CAN_Tool.Libs.Helper;
using CAN_Tool.CustomControls;
using RVC;
using System.ComponentModel;
using CommunityToolkit.Mvvm.ComponentModel;

namespace CAN_Tool
{
    /// <summary>
    /// Interaction logic for MainWindow.xaml
    /// </summary>


    public partial class Settings:ObservableObject
    {
        public Settings()
        {
            Random random = new Random((int)DateTime.Now.Ticks);
            Colors = new Color[140];
            ShowFlag = new bool[140];
            LineWidthes = new int[140];
            LineStyles = new ScottPlot.LineStyle[140];
            MarkShapes = new ScottPlot.MarkerShape[140];
            for (int i = 0; i < 140; i++)
            {
                Colors[i] = Color.FromRgb((byte)random.Next(255), (byte)random.Next(255), (byte)random.Next(255));
                LineWidthes[i] = 1;
                MarkShapes[i] = ScottPlot.MarkerShape.none;
                LineStyles[i] = ScottPlot.LineStyle.Solid;
            }
        }
        public bool IsDark { get; set; }
        public bool ExpertModeOn { set; get; }
        public int ThemeNumber { get; set; }
        public int LangaugeNumber { get; set; }

        public Color[] Colors { get; set; }

        public bool[] ShowFlag { set; get; }
        public int[] LineWidthes { set; get; }
        public ScottPlot.LineStyle[] LineStyles { set; get; }
        public ScottPlot.MarkerShape[] MarkShapes { set; get; }

        public bool UseImperial { set; get; }

        

    }


    public partial class MainWindow : Window
    {
        MainWindowViewModel vm;

        SynchronizationContext UIcontext = SynchronizationContext.Current;

        private void SaveSettings()
        {
            if (vm.OmniInstance.SelectedConnectedDevice != null)

                foreach (var b in vm.OmniInstance.SelectedConnectedDevice.Status)
                {
                    App.Settings.Colors[b.Id] = (b.ChartBrush as SolidColorBrush).Color;
                    App.Settings.ShowFlag[b.Id] = (b.Display);
                    App.Settings.LineStyles[b.Id] = (b.LineStyle);
                    App.Settings.LineWidthes[b.Id] = (b.LineWidth);
                    App.Settings.MarkShapes[b.Id] = (b.MarkShape);
                }
            string serialized = JsonSerializer.Serialize(App.Settings);
            StreamWriter sw = new("settings.json", false);
            sw.Write(serialized);
            sw.Flush();
            sw.Dispose();

        }

        private void TryToLoadSettings()
        {
            try
            {
                
               using (FileStream fs = new FileStream("settings.json", FileMode.OpenOrCreate))
                {
                    App.Settings = JsonSerializer.Deserialize<Settings>(fs);


                }
            }
            catch
            {
                SaveSettings();
            }
        }
        public MainWindow()
        {
            vm = new();
            DataContext = vm;
            InitializeComponent();

            App.LanguageChanged += LanguageChanged;

            CultureInfo currLang = App.Language;

            //Заполняем меню смены языка:
            menuLanguage.Items.Clear();

            foreach (var lang in App.Languages)
            {

                ComboBoxItem menuLang = new();
                menuLang.Content = lang.Name;
                menuLang.Tag = lang;
                menuLang.Selected += ChangeLanguageClick;
                menuLanguage.Items.Add(menuLang);
            }

            vm.myChart = Chart;

            Chart.Plot.AddAxis(Edge.Right, 2, color: System.Drawing.Color.LightGreen);


            vm.CanAdapter.GotNewMessage += MessageHandler;

            vm.RefreshPortListCommand.Execute(null);

            TryToLoadSettings();

            menuLanguage.SelectedIndex = App.Settings.LangaugeNumber;
            menuColor.SelectedIndex = App.Settings.ThemeNumber;
            DarkModeCheckBox.IsChecked = App.Settings.IsDark;
            ExpertMode.IsChecked = App.Settings.ExpertModeOn;
            ImperialUnits.IsChecked = App.Settings.UseImperial;
        }

        private void MessageHandler(object sender, EventArgs args)
        {

            UIcontext.Send(x =>
            {
                if (LogExpander.IsExpanded) { 
                    OmniMessage m = new OmniMessage((args as GotCanMessageEventArgs).receivedMessage);
                    LogField.AppendText(m.ToString());
                }
            }, null);
        }
        
        
        private void LanguageChanged(Object sender, EventArgs e)
        {
            CultureInfo currLang = App.Language;

            //Отмечаем нужный пункт смены языка как выбранный язык
            foreach (ComboBoxItem i in menuLanguage.Items)
            {
                i.IsSelected = i.Tag is CultureInfo ci && ci.Equals(currLang);
            }


        }
        
        private void ChangeLanguageClick(Object sender, EventArgs e)
        {
            ComboBoxItem ci = sender as ComboBoxItem;
            if (ci != null)
            {
                CultureInfo lang = ci.Tag as CultureInfo;
                if (lang != null)
                {
                    App.Language = lang;
                }
            }

        }
        
        private void Window_MouseLeftButtonDown(object sender, MouseButtonEventArgs e)
        {
            try
            {
                if (!GraphTab.IsSelected)     //Костыль, решает проблему неправильной интерпретации мыши графиком
                    DragMove();
            }
            catch { }
        }

        #region Command constructor
        // Заносим изменённое значение в массив и обновляем свойство Data для CustomMessage
        private void UpdateCommand(object sender, EventArgs e)
        {
            double value = 0;
            if (sender is ComboBox)
                value = ((KeyValuePair<int, string>)((sender as ComboBox).SelectedItem)).Key;
            if (sender is TextBox)
                try
                {
                    value = Convert.ToDouble((sender as TextBox).Text);
                }
                catch
                {
                    value = 0;
                }

            vm.CommandParametersArray[Convert.ToInt32((sender as Control).Name.Substring(6))] = value;
            OmniCommand cmd = ((KeyValuePair<int, OmniCommand>)CommandSelector.SelectedItem).Value;
            ulong id = (ulong)cmd.Id;
            ulong res = id << 48;
            OmniPgnParameter[] pars = cmd.Parameters.Where(p => p.AnswerOnly == false).ToArray();
            for (int i = 0; i < pars.Length; i++)
            {
                OmniPgnParameter p = pars[i];
                ulong rawValue;
                rawValue = (ulong)((vm.CommandParametersArray[i] - p.b) / p.a);
                int shift = 0;
                shift = (7 - p.StartByte) * 8;
                shift -= ((p.BitLength + 7) / 8) * 8 - 8;
                shift += p.StartBit;
                rawValue <<= shift;
                res |= rawValue;
            }
            byte[] data = new byte[8];
            for (int i = 0; i < 8; i++)
            {
                data[i] = (byte)((res >> (7 - i) * 8) & 0xFF);
            }
            vm.CustomMessage.Data = data;
        }

        //Рисуем новые элементы управления и инициализируем в модели массив значений
        private void ComboBox_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            MainWindowViewModel mainWindowViewModel = (MainWindowViewModel)DataContext;
            ComboBox comboBox = sender as ComboBox;
            OmniCommand cmd = ((KeyValuePair<int, OmniCommand>)comboBox.SelectedItem).Value;
            CommandParameterPanel.Children.Clear();
            mainWindowViewModel.CommandParametersArray = new double[cmd.Parameters.Count];
            vm.CustomMessage.Pgn = 1;
            vm.CustomMessage.Data[1] = (byte)cmd.Id;
            if (vm.OmniInstance.SelectedConnectedDevice != null)
            {
                vm.CustomMessage.ReceiverId.Address = vm.OmniInstance.SelectedConnectedDevice.Id.Address;
                vm.CustomMessage.ReceiverId.Type = vm.OmniInstance.SelectedConnectedDevice.Id.Type;
            }

            int counter = 0;
            foreach (OmniPgnParameter p in cmd.Parameters.Where(p => p.AnswerOnly == false))
            {
                StackPanel panel = new();
                panel.Orientation = Orientation.Horizontal;
                Label label = new();
                label.Content = GetString(p.Name);
                label.Name = $"label_{counter}";
                label.Margin = new Thickness(10);
                label.VerticalAlignment = VerticalAlignment.Center;
                panel.Children.Add(label);
                if (p.Meanings != null && p.Meanings.Count > 0)
                {
                    ComboBox cb = new();
                    cb.ItemsSource = p.Meanings.Select(s=>new KeyValuePair<int, string> (s.Key, GetString(s.Value)));
                    cb.DisplayMemberPath = "Value";
                    cb.SelectionChanged += UpdateCommand;
                    cb.Name = $"field_{counter}";
                    cb.SelectedIndex = (int)p.DefaultValue;
                    cb.Margin = new Thickness(10);
                    cb.VerticalAlignment = VerticalAlignment.Center;
                    panel.Children.Add(cb);
                }
                else
                {
                    TextBox tb = new();
                    tb.Name = $"field_{counter}";
                    tb.Text = p.DefaultValue.ToString();
                    tb.TextChanged += UpdateCommand;
                    tb.Margin = new Thickness(10);
                    tb.VerticalAlignment = VerticalAlignment.Center;
                    tb.Style = (Style)App.Current.TryFindResource("MaterialDesignOutlinedTextBox");
                    panel.Children.Add(tb);
                }
                mainWindowViewModel.CommandParametersArray[counter++] = p.DefaultValue;
                CommandParameterPanel.Children.Add(panel);

            }
        }
        #endregion

        private void AC2PmessagesField_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            MainWindowViewModel vm = (MainWindowViewModel)DataContext;
            try
            {
                vm.SelectedMessage = (OmniMessage)(sender as DataGrid).SelectedItems[(sender as DataGrid).SelectedItems.Count - 1]; //Мегакостыль фиксящий неизменение свойства SelectedItem DataGrid
            }
            catch { }
        }

        private void ColorPick(object sender, MouseButtonEventArgs e)
        {

        }

        private void CheckBox_Checked(object sender, RoutedEventArgs e)
        {
            if (vm.ManualPage.TurnOnWaterPumpCommand.CanExecute(null))
                vm.ManualPage.TurnOnWaterPumpCommand.Execute(null);

        }

        private void CheckBox_Unchecked(object sender, RoutedEventArgs e)
        {
            if (vm.ManualPage.TurnOffWaterPumpCommand.CanExecute(null))
                vm.ManualPage.TurnOffWaterPumpCommand.Execute(null);
        }

        private void ManualAirMouseWheelEventHandler(object sender, MouseWheelEventArgs e)
        {
            int delta = 1;
            if (Keyboard.GetKeyStates(Key.LeftShift) == KeyStates.Down)
                delta = 5;
            if (e.Delta > 0)
                if (vm.ManualPage.IncreaceManualAirBlowerCommand.CanExecute(null))
                    vm.ManualPage.IncreaceManualAirBlowerCommand.Execute(delta);
            if (e.Delta < 0)
                if (vm.ManualPage.DecreaseManualAirBlowerCommand.CanExecute(null))
                    vm.ManualPage.DecreaseManualAirBlowerCommand.Execute(delta * -1);
        }

        private void ProgressBar_MouseWheel_1(object sender, MouseWheelEventArgs e)
        {
            if (e.Delta > 0)
                if (vm.ManualPage.IncreaseManualFuelPumpCommand.CanExecute(null))
                    vm.ManualPage.IncreaseManualFuelPumpCommand.Execute(null);
            if (e.Delta < 0)
                if (vm.ManualPage.DecreaseFuelPumpCommand.CanExecute(null))
                    vm.ManualPage.DecreaseFuelPumpCommand.Execute(null);
        }

        private void ProgressBar_MouseWheel_2(object sender, MouseWheelEventArgs e)
        {
            if (e.Delta > 0)
                if (vm.ManualPage.IncreaseGlowPlugCommand.CanExecute(null))
                    vm.ManualPage.IncreaseGlowPlugCommand.Execute(null);
            if (e.Delta < 0)
                if (vm.ManualPage.DecreaseGlowPlugCommand.CanExecute(null))
                    vm.ManualPage.DecreaseGlowPlugCommand.Execute(null);
        }

        private void Disappear(object sender, EventArgs e)
        {
            AirArrow.Visibility = Visibility.Hidden;
            PlugArrow.Visibility = Visibility.Hidden;
            PumpArrow.Visibility = Visibility.Hidden;
        }

        private void DarkMode_Checked(object sender, RoutedEventArgs e)
        {
            App.Settings.IsDark = (bool)(sender as CheckBox).IsChecked;
            var resources = Application.Current.Resources.MergedDictionaries;

            var existingResourceDictionary = Application.Current.Resources.MergedDictionaries
                                            .FirstOrDefault(rd => rd.Source.ToString() == "pack://application:,,,/MaterialDesignThemes.Wpf;component/Themes/MaterialDesignTheme.Dark.xaml" ||
                                            rd.Source.ToString() == "pack://application:,,,/MaterialDesignThemes.Wpf;component/Themes/MaterialDesignTheme.Light.xaml");


            var source = App.Settings.IsDark ? "pack://application:,,,/MaterialDesignThemes.Wpf;component/Themes/MaterialDesignTheme.Dark.xaml" : "pack://application:,,,/MaterialDesignThemes.Wpf;component/Themes/MaterialDesignTheme.Light.xaml";
            var newResourceDictionary = new ResourceDictionary() { Source = new Uri(source) };

            Application.Current.Resources.MergedDictionaries.Remove(existingResourceDictionary);
            Application.Current.Resources.MergedDictionaries.Add(newResourceDictionary);

        }

        private void ImperialUnits_Checked(object sender, RoutedEventArgs e)
        {
            App.Settings.UseImperial = (bool)ImperialUnits.IsChecked;
        }


        private void menuColor_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            var resources = Application.Current.Resources.MergedDictionaries;

            var existingResourceDictionary = Application.Current.Resources.MergedDictionaries
                                            .FirstOrDefault(rd => rd.Source.ToString().Contains("component/Themes/Recommended/Primary"));

            string source = "pack://application:,,,/MaterialDesignColors;component/Themes/Recommended/Primary/MaterialDesignColor.LightGreen.xaml";

            switch (menuColor.SelectedIndex)
            {
                case 0: source = "pack://application:,,,/MaterialDesignColors;component/Themes/Recommended/Primary/MaterialDesignColor.Red.xaml"; break;
                case 1: source = "pack://application:,,,/MaterialDesignColors;component/Themes/Recommended/Primary/MaterialDesignColor.Pink.xaml"; break;
                case 2: source = "pack://application:,,,/MaterialDesignColors;component/Themes/Recommended/Primary/MaterialDesignColor.Purple.xaml"; break;
                case 3: source = "pack://application:,,,/MaterialDesignColors;component/Themes/Recommended/Primary/MaterialDesignColor.DeepPurple.xaml"; break;
                case 4: source = "pack://application:,,,/MaterialDesignColors;component/Themes/Recommended/Primary/MaterialDesignColor.Indigo.xaml"; break;
                case 5: source = "pack://application:,,,/MaterialDesignColors;component/Themes/Recommended/Primary/MaterialDesignColor.Blue.xaml"; break;
                case 6: source = "pack://application:,,,/MaterialDesignColors;component/Themes/Recommended/Primary/MaterialDesignColor.LightBlue.xaml"; break;
                case 7: source = "pack://application:,,,/MaterialDesignColors;component/Themes/Recommended/Primary/MaterialDesignColor.Cyan.xaml"; break;
                case 8: source = "pack://application:,,,/MaterialDesignColors;component/Themes/Recommended/Primary/MaterialDesignColor.Teal.xaml"; break;
                case 9: source = "pack://application:,,,/MaterialDesignColors;component/Themes/Recommended/Primary/MaterialDesignColor.Green.xaml"; break;
                case 10: source = "pack://application:,,,/MaterialDesignColors;component/Themes/Recommended/Primary/MaterialDesignColor.LightGreen.xaml"; break;
                case 11: source = "pack://application:,,,/MaterialDesignColors;component/Themes/Recommended/Primary/MaterialDesignColor.Lime.xaml"; break;
                case 12: source = "pack://application:,,,/MaterialDesignColors;component/Themes/Recommended/Primary/MaterialDesignColor.Yellow.xaml"; break;
                case 13: source = "pack://application:,,,/MaterialDesignColors;component/Themes/Recommended/Primary/MaterialDesignColor.Amber.xaml"; break;
                case 14: source = "pack://application:,,,/MaterialDesignColors;component/Themes/Recommended/Primary/MaterialDesignColor.Orange.xaml"; break;
                case 15: source = "pack://application:,,,/MaterialDesignColors;component/Themes/Recommended/Primary/MaterialDesignColor.DeepOrange.xaml"; break;
                case 16: source = "pack://application:,,,/MaterialDesignColors;component/Themes/Recommended/Primary/MaterialDesignColor.Brown.xaml"; break;
                case 17: source = "pack://application:,,,/MaterialDesignColors;component/Themes/Recommended/Primary/MaterialDesignColor.Grey.xaml"; break;
                case 18: source = "pack://application:,,,/MaterialDesignColors;component/Themes/Recommended/Primary/MaterialDesignColor.BlueGrey.xaml"; break;
                default: break;
            }

            var newResourceDictionary = new ResourceDictionary() { Source = new Uri(source) };

            Application.Current.Resources.MergedDictionaries.Remove(existingResourceDictionary);
            Application.Current.Resources.MergedDictionaries.Add(newResourceDictionary);
            App.Settings.ThemeNumber = menuColor.SelectedIndex;
        }

        private void ExitButtonClick(object sender, RoutedEventArgs e)
        {
            SaveSettings();
            App.Current.Shutdown();
        }

        private void menuLanguage_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            App.Settings.LangaugeNumber = menuLanguage.SelectedIndex;
        }

        private void CanBitrateField_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            try
            {
                vm?.CanAdapter.SetBitrate(CanBitrateField.SelectedIndex);
            }
            catch { }
        }

        private void ExpertMode_CheckedChanged(object sender, RoutedEventArgs e)
        {
            App.Settings.ExpertModeOn = (bool)ExpertMode.IsChecked;
        }

        private void ColorPicker_MouseLeftButtonUp(object sender, MouseButtonEventArgs e)
        {
            if (DataSet.SelectedItem != null && DataSet.SelectedItems.Count == 1)
                (DataSet.SelectedItem as StatusVariable).ChartBrush = new SolidColorBrush((sender as ColorPicker).Color);
        }

        private void ColorPicker_StylusUp(object sender, StylusEventArgs e)
        {
            if (DataSet.SelectedItem != null && DataSet.SelectedItems.Count == 1)
                (DataSet.SelectedItem as StatusVariable).ChartBrush = new SolidColorBrush((sender as ColorPicker).Color);
        }

        private void DataSet_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            if (DataSet.SelectedItem != null)
                ColorPicker.Color = ((DataSet.SelectedItem as StatusVariable).ChartBrush as SolidColorBrush).Color;
        }

        private void ScrollBar_ValueChanged(object sender, RoutedPropertyChangedEventArgs<double> e)
        {

            //vm.CanAdapter.Transmit()
        }




        private void Button_Click(object sender, RoutedEventArgs e)
        {
            CanMessage m = new();
            Random r = new(DateTime.Now.Millisecond);
            m.Ide = (r.Next(0, 255)%2)==0;
            m.Rtr = (r.Next(0, 255) % 2) == 0;
            if (m.Ide)
                m.Id = r.Next(0, 0x1FFFFFFF);
            else
                m.Id = r.Next(0, 0x7FF);
            m.Dlc = (byte)r.Next(1, 9);
            for (int i = 0; i < m.Dlc; i++)
                m.Data[i] = (byte)r.Next(0, 256);

            vm.CanPage.MessageList.TryToAdd(m);
        }

        private void CanListDoubleClick(object sender, MouseButtonEventArgs e)
        {
            if (CanMessageList.SelectedItem!=null)
                vm.CanPage.ConstructedMessage.Update(CanMessageList.SelectedItem as CanMessage);
        }

        private void RVCMessageList_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            MainWindowViewModel vm = (MainWindowViewModel)DataContext;
            try
            {
                vm.RvcPage.SelectedMessage = (RvcMessage)(sender as DataGrid).SelectedItems[(sender as DataGrid).SelectedItems.Count - 1]; //Мегакостыль фиксящий неизменение свойства SelectedItem DataGrid
            }
            catch { }

        }

        private void SetTimeButtonPressed(object sender, RoutedEventArgs e)
        {
            vm.RvcPage.Timberline15.SetTime(DateTime.Now);
        }

        private void ToggleHeaterButtonPressed(object sender, RoutedEventArgs e)
        {
            vm.RvcPage.Timberline15.ToggleHeater();
        }

        private void ToggleElementButtonPressed(object sender, RoutedEventArgs e)
        {
            vm.RvcPage.Timberline15.ToggleElement();
        }

        private void ToggleWaterButtonPressed(object sender, RoutedEventArgs e)
        {
            vm.RvcPage.Timberline15.ToggleWater();
        }

        private void ToggleZoneButtonPressed(object sender, RoutedEventArgs e)
        {
            vm.RvcPage.Timberline15.ToggleZone();
        }

        private void TogglePumpButtonPressed(object sender, RoutedEventArgs e)
        {
            vm.RvcPage.Timberline15.TogglePump();
        }

        private void ToggleFanManualModeButtonPressed(object sender, RoutedEventArgs e)
        {
            vm.RvcPage.Timberline15.ToggleFanManualMode();
        }

        private void ToggleScheduleModeButtonPressed(object sender, RoutedEventArgs e)
        {
            vm.RvcPage.Timberline15.ToggleScheduleMode();
        }

        private void DaySetPointValueChanged(object sender, RoutedEventArgs e)
        {
            vm?.RvcPage.Timberline15.SetDaySetpoint((int)(sender as ScrollBar).Value);
        }

        private void NightSetPointValueChanged(object sender, RoutedEventArgs e)
        {
            vm?.RvcPage.Timberline15.SetNightSetpoint((int)(sender as ScrollBar).Value);
        }

        private void ManualFanSpeedValueChanged(object sender, RoutedEventArgs e)
        {
            vm?.RvcPage.Timberline15.SetFanManualSpeed((byte)(sender as ScrollBar).Value);
        }

        private void SystemDurationValueChanged(object sender, RoutedPropertyChangedEventArgs<double> e)
        {
            vm?.RvcPage.Timberline15.SetSystemDuration((int)(sender as ScrollBar).Value);
        }

        private void WaterDurationValueChanged(object sender, RoutedPropertyChangedEventArgs<double> e)
        {
            vm?.RvcPage.Timberline15.SetWaterDuration((int)(sender as ScrollBar).Value);
        }

        private void NightTimeChanged(object sender, RoutedPropertyChangedEventArgs<DateTime?> e)
        {
            vm?.RvcPage.Timberline15.SetNightStart((sender as TimePicker).SelectedTime.Value.Hour, (sender as TimePicker).SelectedTime.Value.Minute);
        }

        
            private void DayStartChanged(object sender, RoutedPropertyChangedEventArgs<DateTime?> e)
        {
            vm?.RvcPage.Timberline15.SetDayStart((sender as TimePicker).SelectedTime.Value.Hour, (sender as TimePicker).SelectedTime.Value.Minute);
        }

        private void ClearErrorsButtonPressed(object sender, RoutedEventArgs e)
        {
            vm?.RvcPage.Timberline15.ClearErrors();
        }


    }


}
