using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
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
        int[] Bitrates = new int[] { 20, 50, 125, 250, 500, 800, 1000 };
        public void GotCanMessage(object sender, EventArgs args)
        {
            CanMessage msg = (args as GotMessageEventArgs).receivedMessage;

            CanMessage targetMessage = messages.FirstOrDefault<CanMessage>(m => m.ID == msg.ID);
            if (targetMessage != null)
            {
                Dispatcher.Invoke(new Action(() => targetMessage.Update(msg)));
            }
            else
                Dispatcher.Invoke(new Action(() => messages.Add(msg)));

        }
        public CanAdapter canAdapter = new CanAdapter();
        public BindingList<CanMessage> messages = new BindingList<CanMessage>();


        public MainWindow()
        {
            InitializeComponent();
        }

        private void Window_Loaded(object sender, RoutedEventArgs e)
        {
            
            canAdapter.GotNewMessage += new EventHandler(GotCanMessage);
            CanMessageList.ItemsSource = messages;
            AC2P.ParseParamsname();
            CanBitrateField.ItemsSource = Bitrates;
        }


        private void Button_Click(object sender, RoutedEventArgs e)
        {
            PortNameList.Items.Clear();
            foreach (var p in SerialPort.GetPortNames())
                PortNameList.Items.Add(p);
            if (PortNameList.Items.Count > 0)
                PortNameList.SelectedIndex = 0;

        }

        private void Button_Click_1(object sender, RoutedEventArgs e)
        {
            if (canAdapter.PortOpened)
            {
                PortOpenButton.Content = "Open";
                canAdapter.PortClose();
            }
            else
            {
                PortOpenButton.Content = "Close";
                canAdapter.PortOpen();
            }
        }

        private void PortNameList_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            canAdapter.PortName = (sender as ComboBox).SelectedItem.ToString();
        }

        private void CanMessageList_AutoGeneratingColumn(object sender, DataGridAutoGeneratingColumnEventArgs e)
        {

        }

        private void Button_Click_2(object sender, RoutedEventArgs e)
        {
            canAdapter.StartNormal();
        }

        private void Button_Click_3(object sender, RoutedEventArgs e)
        {
            canAdapter.StartListen();
        }

        private void Button_Click_4(object sender, RoutedEventArgs e)
        {
            canAdapter.Stop();
        }
    }
}
