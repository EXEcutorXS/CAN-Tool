﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Navigation;
using System.Windows.Shapes;
using System.ComponentModel;
using System.IO.Ports;
using Can_Adapter;
using AdversCan;
using CAN_Tool.ViewModels;
using System.Globalization;
using ScottPlot;
using ScottPlot.Renderable;
using System.Threading;
using System.Windows.Data;
using CAN_Tool.ViewModels.Converters;
using System.Windows.Media.Animation;
using System.Text.RegularExpressions;
using System.Text.Json;
using System.IO;


namespace CAN_Tool
{
    /// <summary>
    /// Interaction logic for MainWindow.xaml
    /// </summary>


    public class Settings
    {
        public bool isDark { get; set; }
        public int themeNumber { get; set; }
        public int langaugeNumber { get; set; }
    }


    public partial class MainWindow : Window
    {
        private Settings settings = new();

        MainWindowViewModel vm;

        SynchronizationContext UIcontext = SynchronizationContext.Current;

        public List<Brush> Brushes;
        private void SaveSettings()
        {
            string serialized = JsonSerializer.Serialize(settings);
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
                    settings = JsonSerializer.Deserialize<Settings>(fs);
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

            vm.CanAdapter.GotNewMessage += MessageHandler;

            vm.RefreshPortListCommand.Execute(null);

            TryToLoadSettings();

            menuLanguage.SelectedIndex = settings.langaugeNumber;
            menuColor.SelectedIndex = settings.themeNumber;
            DarkModeCheckBox.IsChecked = settings.isDark;
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
            ulong res =  id << 48;
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
            if (vm.ManualControlPage.TurnOnWaterPumpCommand.CanExecute(null))
                vm.ManualControlPage.TurnOnWaterPumpCommand.Execute(null);

        }

        private void CheckBox_Unchecked(object sender, RoutedEventArgs e)
        {
            if (vm.ManualControlPage.TurnOffWaterPumpCommand.CanExecute(null))
                vm.ManualControlPage.TurnOffWaterPumpCommand.Execute(null);
        }

        private void ProgressBar_MouseWheel(object sender, MouseWheelEventArgs e)
        {
            if (e.Delta > 0)
                if (vm.ManualControlPage.IncreaceManualAirBlowerCommand.CanExecute(null))
                    vm.ManualControlPage.IncreaceManualAirBlowerCommand.Execute(null);
            if (e.Delta < 0)
                if (vm.ManualControlPage.DecreaseManualAirBlowerCommand.CanExecute(null))
                    vm.ManualControlPage.DecreaseManualAirBlowerCommand.Execute(null);
        }

        private void ProgressBar_MouseWheel_1(object sender, MouseWheelEventArgs e)
        {
            if (e.Delta > 0)
                if (vm.ManualControlPage.IncreaseManualFuelPumpCommand.CanExecute(null))
                    vm.ManualControlPage.IncreaseManualFuelPumpCommand.Execute(null);
            if (e.Delta < 0)
                if (vm.ManualControlPage.DecreaseFuelPumpCommand.CanExecute(null))
                    vm.ManualControlPage.DecreaseFuelPumpCommand.Execute(null);
        }

        private void ProgressBar_MouseWheel_2(object sender, MouseWheelEventArgs e)
        {
            if (e.Delta > 0)
                if (vm.ManualControlPage.IncreaseGlowPlugCommand.CanExecute(null))
                    vm.ManualControlPage.IncreaseGlowPlugCommand.Execute(null);
            if (e.Delta < 0)
                if (vm.ManualControlPage.DecreaseGlowPlugCommand.CanExecute(null))
                    vm.ManualControlPage.DecreaseGlowPlugCommand.Execute(null);
        }

        private void Disappear(object sender, EventArgs e)
        {
            AirArrow.Visibility = Visibility.Hidden;
            PlugArrow.Visibility = Visibility.Hidden;
            PumpArrow.Visibility = Visibility.Hidden;
        }

        private void DarkMode_Checked(object sender, RoutedEventArgs e)
        {
            settings.isDark = (bool)(sender as CheckBox).IsChecked;
            var resources = Application.Current.Resources.MergedDictionaries;

            var existingResourceDictionary = Application.Current.Resources.MergedDictionaries
                                            .FirstOrDefault(rd => rd.Source.ToString() == "pack://application:,,,/MaterialDesignThemes.Wpf;component/Themes/MaterialDesignTheme.Dark.xaml" ||
                                            rd.Source.ToString() == "pack://application:,,,/MaterialDesignThemes.Wpf;component/Themes/MaterialDesignTheme.Light.xaml");


            var source = settings.isDark ? "pack://application:,,,/MaterialDesignThemes.Wpf;component/Themes/MaterialDesignTheme.Dark.xaml" : "pack://application:,,,/MaterialDesignThemes.Wpf;component/Themes/MaterialDesignTheme.Light.xaml";
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
            settings.themeNumber = menuColor.SelectedIndex;
        }

        private void Button_Click(object sender, RoutedEventArgs e)
        {
            SaveSettings();
            App.Current.Shutdown();
        }

        private void menuLanguage_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            settings.langaugeNumber = menuLanguage.SelectedIndex;
        }

        private void CanBitrateField_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            try
            {
                vm?.CanAdapter.SetBitrate(CanBitrateField.SelectedIndex);
            }
            catch(Exception ex)
            {
                vm.Error = ex.Message;
            }
        }
    }


}
