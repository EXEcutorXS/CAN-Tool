using CAN_Tool.ViewModels;
using CommunityToolkit.Mvvm.ComponentModel;
using MaterialDesignThemes.Wpf;
using OmniProtocol;
using RVC;
using ScottPlot;
using ScottPlot.Palettes;
using ScottPlot.Renderable;
using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Text.Json;
using System.Threading;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Controls.Primitives;
using System.Windows.Input;
using System.Windows.Media;
using static CAN_Tool.Libs.Helper;

namespace CAN_Tool
{
    /// <summary>
    /// Interaction logic for MainWindow.xaml
    /// </summary>


    public partial class Settings : ObservableObject
    {
        public Settings()
        {
            Random random = new Random((int)DateTime.Now.Ticks);
            Colors = new Color[250];
            ShowFlag = new bool[250];
            LineWidthes = new int[250];
            LineStyles = new LineStyle[250];
            MarkShapes = new MarkerShape[250];
            for (int i = 0; i < 250; i++)
            {
                Colors[i] = Color.FromRgb((byte)random.Next(255), (byte)random.Next(255), (byte)random.Next(255));
                LineWidthes[i] = 1;
                MarkShapes[i] = MarkerShape.none;
                LineStyles[i] = LineStyle.Solid;
            }
            AdapterType = CanAdapter.AdapterType.VSCom;
        }
        public bool IsDark { get; set; }
        public int ThemeNumber { get; set; }
        public int LangaugeNumber { get; set; }

        public Color[] Colors { get; set; }

        public bool[] ShowFlag { set; get; }
        public int[] LineWidthes { set; get; }
        public LineStyle[] LineStyles { set; get; }
        public MarkerShape[] MarkShapes { set; get; }

        public bool UseImperial { set; get; }

        public CanAdapter.AdapterType AdapterType { get; set; }
        public int CanBitrateIndex { get; set; } = 5;



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
            App.Settings.AdapterType = vm.CanAdapter.Type;
            App.Settings.CanBitrateIndex = vm.SelectedCanBitrate;
            string serialized = JsonSerializer.Serialize(App.Settings);
            StreamWriter sw = new("settings.json", false);
            sw.Write(serialized);
            sw.Flush();
            sw.Dispose();

        }

        protected override  void OnClosing(System.ComponentModel.CancelEventArgs e)
        {
            base.OnClosing(e);

            try
            {
                if (vm.CanAdapter.PortOpened)
                    vm.CanAdapter.PortClose();
                SaveSettings();
            }
            catch (Exception ex)
            {
                // Обработка ошибок
                e.Cancel = true; // Отменяем закрытие при ошибке
                MessageBox.Show($"Ошибка при сохранении: {ex.Message}");
            }
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
            
            this.Title = $"CAN Tool (build {BuildInfo.BuildDate})";

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

            vm.RefreshPortListCommand.Execute(null);

            TryToLoadSettings();

            menuLanguage.SelectedIndex = App.Settings.LangaugeNumber;
            menuColor.SelectedIndex = App.Settings.ThemeNumber;
            DarkModeCheckBox.IsChecked = App.Settings.IsDark;
            ImperialUnits.IsChecked = App.Settings.UseImperial;
            vm.CanAdapter.Type = App.Settings.AdapterType;
            vm.SelectedCanBitrate = App.Settings.CanBitrateIndex;
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
            var src = e.OriginalSource as DependencyObject;
            while (src != null)
            {
                if (src is ScottPlot.WpfPlot) return;
                src = VisualTreeHelper.GetParent(src);
            }
            try { DragMove(); } catch { }
        }

        private void DarkMode_Checked(object sender, RoutedEventArgs e)
        {
            App.Settings.IsDark = (bool)(sender as CheckBox).IsChecked;
            var resources = Application.Current.Resources.MergedDictionaries;

            var existingResourceDictionary = Application.Current.Resources.MergedDictionaries
                                            .FirstOrDefault(rd => rd.Source != null &&
                                            (rd.Source.ToString() == "pack://application:,,,/MaterialDesignThemes.Wpf;component/Themes/MaterialDesignTheme.Dark.xaml" ||
                                            rd.Source.ToString() == "pack://application:,,,/MaterialDesignThemes.Wpf;component/Themes/MaterialDesignTheme.Light.xaml"));


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
                                            .FirstOrDefault(rd => rd.Source != null && rd.Source.ToString().Contains("component/Themes/Recommended/Primary"));

            string source = "pack://application:,,,/MaterialDesignColors;component/Themes/Recommended/Primary/MaterialDesignColor.LightGreen.xaml";

            switch (menuColor.SelectedIndex)
            {
                case 0: source = "pack://application:,,,/MaterialDesignColors;component/Themes/Recommended/Primary/MaterialDesignColor.Indigo.xaml"; break;
                case 1: source = "pack://application:,,,/MaterialDesignColors;component/Themes/Recommended/Primary/MaterialDesignColor.Blue.xaml"; break;
                case 2: source = "pack://application:,,,/MaterialDesignColors;component/Themes/Recommended/Primary/MaterialDesignColor.LightBlue.xaml"; break;
                case 3: source = "pack://application:,,,/MaterialDesignColors;component/Themes/Recommended/Primary/MaterialDesignColor.Cyan.xaml"; break;
                case 4: source = "pack://application:,,,/MaterialDesignColors;component/Themes/Recommended/Primary/MaterialDesignColor.Teal.xaml"; break;
                case 5: source = "pack://application:,,,/MaterialDesignColors;component/Themes/Recommended/Primary/MaterialDesignColor.Green.xaml"; break;
                case 6: source = "pack://application:,,,/MaterialDesignColors;component/Themes/Recommended/Primary/MaterialDesignColor.LightGreen.xaml"; break;
                case 7: source = "pack://application:,,,/MaterialDesignColors;component/Themes/Recommended/Primary/MaterialDesignColor.Lime.xaml"; break;
                case 8: source = "pack://application:,,,/MaterialDesignColors;component/Themes/Recommended/Primary/MaterialDesignColor.Yellow.xaml"; break;
                case 9: source = "pack://application:,,,/MaterialDesignColors;component/Themes/Recommended/Primary/MaterialDesignColor.Amber.xaml"; break;
                case 10: source = "pack://application:,,,/MaterialDesignColors;component/Themes/Recommended/Primary/MaterialDesignColor.Orange.xaml"; break;
                case 11: source = "pack://application:,,,/MaterialDesignColors;component/Themes/Recommended/Primary/MaterialDesignColor.DeepOrange.xaml"; break;
                case 12: source = "pack://application:,,,/MaterialDesignColors;component/Themes/Recommended/Primary/MaterialDesignColor.Brown.xaml"; break;
                case 13: source = "pack://application:,,,/MaterialDesignColors;component/Themes/Recommended/Primary/MaterialDesignColor.Grey.xaml"; break;
                case 14: source = "pack://application:,,,/MaterialDesignColors;component/Themes/Recommended/Primary/MaterialDesignColor.BlueGrey.xaml"; break;
                case 15: source = "pack://application:,,,/MaterialDesignColors;component/Themes/Recommended/Primary/MaterialDesignColor.Red.xaml"; break;
                case 16: source = "pack://application:,,,/MaterialDesignColors;component/Themes/Recommended/Primary/MaterialDesignColor.Pink.xaml"; break;
                case 17: source = "pack://application:,,,/MaterialDesignColors;component/Themes/Recommended/Primary/MaterialDesignColor.Purple.xaml"; break;
                case 18
                :
                    source = "pack://application:,,,/MaterialDesignColors;component/Themes/Recommended/Primary/MaterialDesignColor.DeepPurple.xaml"; break;
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




    }


}
