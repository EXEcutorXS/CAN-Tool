using System;
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

namespace CAN_Tool
{
    /// <summary>
    /// Interaction logic for MainWindow.xaml
    /// </summary>


    public partial class MainWindow : Window
    {

        MainWindowViewModel vm;

        SynchronizationContext UIcontext = SynchronizationContext.Current;

        public List<Brush> Brushes;
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
                menuLang.Content = lang.NativeName;
                menuLang.Tag = lang;
                menuLang.Selected += ChangeLanguageClick;
                menuLanguage.Items.Add(menuLang);
            }
            menuLanguage.SelectedIndex = 0;

            vm.myChart = Chart;

            Chart.Plot.AddAxis(Edge.Right, 2, color: System.Drawing.Color.LightGreen);

            vm.canAdapter.GotNewMessage += MessageHandler;
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
            MainWindowViewModel mainWindowViewModel = (MainWindowViewModel)DataContext;
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

            mainWindowViewModel.CommandParametersArray[Convert.ToInt32((sender as Control).Name.Substring(6))] = value;
            AC2PCommand cmd = ((KeyValuePair<CommandId, AC2PCommand>)CommandSelector.SelectedItem).Value;
            ulong firstByte = cmd.firstByte;
            ulong secondByte = cmd.secondByte;
            ulong res = firstByte << 56 | secondByte << 48;
            AC2PParameter[] pars = cmd.Parameters.Where(p => p.AnswerOnly == false).ToArray();
            for (int i = 0; i < pars.Length; i++)
            {
                AC2PParameter p = pars[i];
                ulong rawValue;
                rawValue = (ulong)((mainWindowViewModel.CommandParametersArray[i] - p.b) / p.a);
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
            mainWindowViewModel.CustomMessage.Data = data;
        }

        //Рисуем новые элементы управления и инициализируем в модели массив значений
        private void ComboBox_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            MainWindowViewModel mainWindowViewModel = (MainWindowViewModel)DataContext;
            ComboBox comboBox = sender as ComboBox;
            AC2PCommand cmd = ((KeyValuePair<CommandId, AC2PCommand>)comboBox.SelectedItem).Value;
            CommandParameterPanel.Children.Clear();
            mainWindowViewModel.CommandParametersArray = new double[cmd.Parameters.Count];
            vm.CustomMessage.PGN = 1;
            vm.CustomMessage.Command = cmd.Id;
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
                    panel.Children.Add(cb);
                }
                else
                {
                    TextBox tb = new();
                    tb.Name = $"field_{counter}";
                    tb.Text = p.DefaultValue.ToString();
                    tb.TextChanged += UpdateCommand;
                    tb.Margin = new Thickness(10);
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

    }


}
