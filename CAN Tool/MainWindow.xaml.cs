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
        }

        private void UpdateCommand(object sender, EventArgs e)
        {

        }

        private void ComboBox_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            ComboBox comboBox = sender as ComboBox;
            AC2PCommand cmd = ((KeyValuePair<CommandId, AC2PCommand>)comboBox.SelectedItem).Value;
            CommandParameterPanel.Children.Clear();
            foreach (AC2PParameter p in cmd.Parameters)
            {
                StackPanel panel = new();
                panel.Orientation = Orientation.Horizontal;
                Label label = new();
                label.Content = p.Name;
                label.Name = p.ParamsName + "label";
                label.Margin = new Thickness(10);
                panel.Children.Add(label);
                if (p.Meanings != null && p.Meanings.Count > 0)
                {
                    ComboBox cb = new();
                    cb.ItemsSource = p.Meanings;
                    cb.DisplayMemberPath = "Value";
                    cb.SelectionChanged += UpdateCommand;
                    cb.Name = p.ParamsName + "Field";
                    cb.SelectedIndex = (int)p.DefaultValue;
                    cb.Margin = new Thickness(10);
                    panel.Children.Add(cb);
                }
                else
                {
                    TextBox tb = new();
                    tb.Name = p.ParamsName + "Field";
                    tb.Text = p.DefaultValue.ToString();
                    tb.Margin = new Thickness(10);
                    panel.Children.Add(tb);
                }

                CommandParameterPanel.Children.Add(panel);

            }
        }
    }

}
