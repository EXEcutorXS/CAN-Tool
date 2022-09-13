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
using RVC;
using CAN_Tool.ViewModels;
using System.Globalization;
using ScottPlot;

namespace CAN_Tool
{
    /// <summary>
    /// Interaction logic for MainWindow.xaml
    /// </summary>

    public partial class MainWindow : Window
    {



        public MainWindow()
        {
            InitializeComponent();

            App.LanguageChanged += LanguageChanged;

            CultureInfo currLang = App.Language;

            //Заполняем меню смены языка:
            menuLanguage.Items.Clear();

            foreach (var lang in App.Languages)
            {

                ComboBoxItem menuLang = new();
                menuLang.Content = lang.DisplayName;
                menuLang.Tag = lang;
                menuLang.Selected += ChangeLanguageClick;
                menuLanguage.Items.Add(menuLang);
            }
            menuLanguage.SelectedIndex = 0;


            var plt = Chart.Plot;

            plt.Palette = ScottPlot.Palette.OneHalfDark;

            for (int i = 0; i < plt.Palette.Count(); i++)
            {
                double[] xs = DataGen.Consecutive(100);
                double[] ys = DataGen.Sin(100, phase: -i * .5 / plt.Palette.Count());
                plt.AddScatterLines(xs, ys, lineWidth: 3);
            }

            plt.Title($"{plt.Palette}");
            plt.AxisAuto(0, 0.1);
            plt.Style(ScottPlot.Style.Gray1);
            var bnColor = System.Drawing.ColorTranslator.FromHtml("#2e3440");
            plt.Style(figureBackground: bnColor, dataBackground: bnColor);

            plt.SaveFig("palette_OneHalfDark.png");
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
            ulong res = firstByte<<56|secondByte<<48;
            AC2PParameter[] pars = cmd.Parameters.Where(p=>p.AnswerOnly==false).ToArray();
            for (int i = 0; i < pars.Length; i++)
            {
                AC2PParameter p = pars[i];
                ulong rawValue;
                rawValue = (ulong)((mainWindowViewModel.CommandParametersArray[i] - p.b) / p.a);
                int shift = 0;
                shift = (7 - p.StartByte) * 8;
                shift -= ((p.BitLength + 7) / 8) * 8 - 8;
                shift += p.StartBit;
                rawValue<<=shift;
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

            int counter = 0;
            foreach (AC2PParameter p in cmd.Parameters.Where(p=>p.AnswerOnly==false))
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
    }

}
