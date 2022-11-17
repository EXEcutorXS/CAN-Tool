using AdversCan;
using Can_Adapter;
using CAN_Tool.ViewModels;
using MaterialDesignThemes.Wpf;
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


namespace CAN_Tool
{
    /// <summary>
    /// Interaction logic for MainWindow.xaml
    /// </summary>


    public class Settings
    {
        public Settings()
        {
            Random random = new Random((int)DateTime.Now.Ticks);
            Colors = new Color[100];
            ShowFlag = new bool[100];
            for (int i = 0; i < 100; i++)
                Colors[i] = Color.FromRgb((byte)random.Next(255), (byte)random.Next(255), (byte)random.Next(255));
        }
        public bool isDark { get; set; }
        public bool ExpertModeOn { set; get; }
        public int themeNumber { get; set; }
        public int langaugeNumber { get; set; }

        public Color[] Colors  { get; set; }

        public bool[] ShowFlag { set; get; }
}


    public partial class MainWindow : Window
    {
        MainWindowViewModel vm;

        SynchronizationContext UIcontext = SynchronizationContext.Current;

        private void SaveSettings()
        {
            foreach (var b in vm.SelectedConnectedDevice.Status)
            {
                App.Settings.Colors[b.Id] = (b.ChartBrush as SolidColorBrush).Color;
                App.Settings.ShowFlag[b.Id] = (b.Display);
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
            InitializeComponent();


            vm = (MainWindowViewModel)DataContext;

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

            menuLanguage.SelectedIndex = App.Settings.langaugeNumber;
            menuColor.SelectedIndex = App.Settings.themeNumber;
            DarkModeCheckBox.IsChecked = App.Settings.isDark;
            ExpertMode.IsChecked = App.Settings.ExpertModeOn;
        }

        private void MessageHandler(object sender, EventArgs args)
        {
            
            UIcontext.Send(x =>
            {
                if (LogExpander.IsExpanded)
                {
                    CanMessage msg = (args as GotMessageEventArgs).receivedMessage;
                    AC2PMessage m = new AC2PMessage(msg);
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
            AC2PCommand cmd = ((KeyValuePair<int, AC2PCommand>)CommandSelector.SelectedItem).Value;
            ulong id = (ulong)cmd.Id;
            ulong res = id << 48;
            AC2PParameter[] pars = cmd.Parameters.Where(p => p.AnswerOnly == false).ToArray();
            for (int i = 0; i < pars.Length; i++)
            {
                AC2PParameter p = pars[i];
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
            AC2PCommand cmd = ((KeyValuePair<int, AC2PCommand>)comboBox.SelectedItem).Value;
            CommandParameterPanel.Children.Clear();
            mainWindowViewModel.CommandParametersArray = new double[cmd.Parameters.Count];
            vm.CustomMessage.PGN = 1;
            vm.CustomMessage.Data[1] = (byte)cmd.Id;
            if (vm.SelectedConnectedDevice != null)
                vm.CustomMessage.ReceiverId = vm.SelectedConnectedDevice.ID;

            int counter = 0;
            foreach (AC2PParameter p in cmd.Parameters.Where(p => p.AnswerOnly == false))
            {
                StackPanel panel = new();
                panel.Orientation = System.Windows.Controls.Orientation.Horizontal;
                Label label = new();
                label.Content = p.Name;
                label.Name = $"label_{counter}";
                label.Margin = new Thickness(10);
                label.VerticalAlignment = System.Windows.VerticalAlignment.Center;
                panel.Children.Add(label);
                if (p.Meanings != null && p.Meanings.Count > 0)
                {
                    ComboBox cb = new();
                    cb.ItemsSource = p.Meanings;
                    cb.DisplayMemberPath = "Value";
                    cb.SelectionChanged += UpdateCommand;
                    cb.Name = $"field_{counter}";
                    cb.SelectedIndex = (int)p.DefaultValue;
                    cb.Margin = new Thickness(10);
                    cb.VerticalAlignment = System.Windows.VerticalAlignment.Center;
                    panel.Children.Add(cb);
                }
                else
                {
                    TextBox tb = new();
                    tb.Name = $"field_{counter}";
                    tb.Text = p.DefaultValue.ToString();
                    tb.TextChanged += UpdateCommand;
                    tb.Margin = new Thickness(10);
                    tb.VerticalAlignment = System.Windows.VerticalAlignment.Center;
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
                vm.SelectedMessage = (AC2PMessage)(sender as DataGrid).SelectedItems[(sender as DataGrid).SelectedItems.Count - 1]; //Мегакостыль фиксящий неизменение свойства SelectedItem DataGrid
            }
            catch { }
        }

        private void ColorPick(object sender, MouseButtonEventArgs e)
        {

        }

        private void CheckBox_Checked(object sender, RoutedEventArgs e)
        {
            if (vm.TurnOnWaterPumpCommand.CanExecute(null))
                vm.TurnOnWaterPumpCommand.Execute(null);

        }

        private void CheckBox_Unchecked(object sender, RoutedEventArgs e)
        {
            if (vm.TurnOffWaterPumpCommand.CanExecute(null))
                vm.TurnOffWaterPumpCommand.Execute(null);
        }

        private void ProgressBar_MouseWheel(object sender, MouseWheelEventArgs e)
        {
            if (e.Delta > 0)
                if (vm.IncreaceManualAirBlowerCommand.CanExecute(null))
                    vm.IncreaceManualAirBlowerCommand.Execute(null);
            if (e.Delta < 0)
                if (vm.DecreaseManualAirBlowerCommand.CanExecute(null))
                    vm.DecreaseManualAirBlowerCommand.Execute(null);
        }

        private void ProgressBar_MouseWheel_1(object sender, MouseWheelEventArgs e)
        {
            if (e.Delta > 0)
                if (vm.IncreaseManualFuelPumpCommand.CanExecute(null))
                    vm.IncreaseManualFuelPumpCommand.Execute(null);
            if (e.Delta < 0)
                if (vm.DecreaseFuelPumpCommand.CanExecute(null))
                    vm.DecreaseFuelPumpCommand.Execute(null);
        }

        private void ProgressBar_MouseWheel_2(object sender, MouseWheelEventArgs e)
        {
            if (e.Delta > 0)
                if (vm.IncreaseGlowPlugCommand.CanExecute(null))
                    vm.IncreaseGlowPlugCommand.Execute(null);
            if (e.Delta < 0)
                if (vm.DecreaseGlowPlugCommand.CanExecute(null))
                    vm.DecreaseGlowPlugCommand.Execute(null);
        }

        private void Disappear(object sender, EventArgs e)
        {
            AirArrow.Visibility = Visibility.Hidden;
            PlugArrow.Visibility = Visibility.Hidden;
            PumpArrow.Visibility = Visibility.Hidden;
        }

        private void DarkMode_Checked(object sender, RoutedEventArgs e)
        {
            App.Settings.isDark = (bool)(sender as CheckBox).IsChecked;
            var resources = Application.Current.Resources.MergedDictionaries;

            var existingResourceDictionary = Application.Current.Resources.MergedDictionaries
                                            .FirstOrDefault(rd => rd.Source.ToString() == "pack://application:,,,/MaterialDesignThemes.Wpf;component/Themes/MaterialDesignTheme.Dark.xaml" ||
                                            rd.Source.ToString() == "pack://application:,,,/MaterialDesignThemes.Wpf;component/Themes/MaterialDesignTheme.Light.xaml");


            var source = App.Settings.isDark ? "pack://application:,,,/MaterialDesignThemes.Wpf;component/Themes/MaterialDesignTheme.Dark.xaml" : "pack://application:,,,/MaterialDesignThemes.Wpf;component/Themes/MaterialDesignTheme.Light.xaml";
            var newResourceDictionary = new ResourceDictionary() { Source = new Uri(source) };

            Application.Current.Resources.MergedDictionaries.Remove(existingResourceDictionary);
            Application.Current.Resources.MergedDictionaries.Add(newResourceDictionary);

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
            App.Settings.themeNumber = menuColor.SelectedIndex;
        }

        private void Button_Click(object sender, RoutedEventArgs e)
        {
            SaveSettings();
            App.Current.Shutdown();
        }

        private void menuLanguage_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            App.Settings.langaugeNumber = menuLanguage.SelectedIndex;
        }

        private void CanBitrateField_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            try
            {
                vm?.CanAdapter.SetBitrate(CanBitrateField.SelectedIndex);
            }
            catch (Exception ex)
            {
                vm.Error = ex.Message;
            }
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
    }


}
