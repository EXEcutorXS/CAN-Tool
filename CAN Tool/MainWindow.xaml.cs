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

        private void Grid_Loaded(object sender, RoutedEventArgs e)
        {
            CommandSelectorField.ItemsSource = AC2P.commands;
            CommandSelectorField.DisplayMemberPath = "Value.Name";
            CommandSelectorField.SelectedValuePath = "Value";

            

            


        }
    }

}
