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
        int[] Bitrates = new int[] { 20, 50, 125, 250, 500, 800, 1000 };
        public void GotCanMessage(object sender, EventArgs args)
        {
            CanMessage msg = (args as GotMessageEventArgs).receivedMessage;

            CanMessage targetMessage = canMessages.FirstOrDefault<CanMessage>(m => m.IDE == msg.IDE && m.RTR == msg.RTR && m.ID == msg.ID);
            if (targetMessage != null)
            {
                Dispatcher.Invoke(new Action(() => targetMessage.Update(msg)));
            }
            else
                Dispatcher.Invoke(new Action(() => canMessages.Add(msg)));
            AC2Pmessage foundAC2PMessage = AC2Pmessages.FirstOrDefault(m => m.Similiar(new AC2Pmessage(msg)));
            if (foundAC2PMessage != null)
                Dispatcher.Invoke(new Action(() =>
                foundAC2PMessage.Update(new AC2Pmessage(msg))));
            else
                Dispatcher.Invoke(new Action(() =>
                {
                    AC2Pmessages.Add(new AC2Pmessage(msg));
                }));
            Dispatcher.Invoke(() => AC2P.ParseCanMessage(msg));

        }
        public CanAdapter canAdapter = new CanAdapter();
        public BindingList<CanMessage> canMessages = new BindingList<CanMessage>();
        public BindingList<AC2Pmessage> AC2Pmessages = new BindingList<AC2Pmessage>();


        public MainWindow()
        {
            InitializeComponent();
        }

        private void Window_Loaded(object sender, RoutedEventArgs e)
        {
            AC2P.canAdapter = canAdapter;
            AC2P.ParseParamsname();

            canAdapter.GotNewMessage += new EventHandler(GotCanMessage);
            AC2P.progressBarUpdated += new EventHandler(ProgressChanged);
            CanMessageList.ItemsSource = canMessages;
            AC2PMessageList.ItemsSource = AC2Pmessages;
            CanBitrateField.ItemsSource = Bitrates;
            CurrentDeviceSelectorField.ItemsSource = AC2P.connectedDevices;
            CanBitrateField.SelectedIndex = 3;

            foreach (var p in SerialPort.GetPortNames())
                PortNameList.Items.Add(p);
            if (PortNameList.Items.Count > 0)
                PortNameList.SelectedIndex = 0;

            CommandSelectorField.ItemsSource = AC2P.commands;
            
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

        private void ProgressChanged(object sender, EventArgs e)
        {
            ConfigProgressBar.Value = AC2P.Progress;
            ProgressText.Text = AC2P.Progress.ToString();
        }

        private void PortNameList_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            if ((sender as ComboBox).SelectedItem != null)
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

        private void AC2PMessageList_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            AC2PVerboseInfoField.Text = (AC2PMessageList.CurrentItem as AC2Pmessage)?.VerboseInfo().Replace(';', '\n');
        }

        private void CurrentDeviceSelectorField_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            ConfigParameterField.ItemsSource = (CurrentDeviceSelectorField.SelectedItem as ConnectedDevice)?.readedParameters;
        }

        private void readParametersButton_Click(object sender, RoutedEventArgs e)
        {
            if (CurrentDeviceSelectorField.SelectedItem == null) return;
            AC2P.connectedDevices.FirstOrDefault(d => d.ID.Equals((CurrentDeviceSelectorField.SelectedItem as ConnectedDevice).ID))?.readedParameters.Clear();
            AC2P.ReadAllParameters((CurrentDeviceSelectorField?.SelectedItem as ConnectedDevice).ID);
        }

        private void saveParametersButton_Click(object sender, RoutedEventArgs e)
        {
            AC2P.SaveParameters((CurrentDeviceSelectorField?.SelectedItem as ConnectedDevice).ID);
        }

        private void eraseParametersButton_Click(object sender, RoutedEventArgs e)
        {
            AC2P.connectedDevices.FirstOrDefault(d => d.ID.Equals((CurrentDeviceSelectorField.SelectedItem as ConnectedDevice).ID)).readedParameters.Clear();
            AC2P.EraseParameters((CurrentDeviceSelectorField?.SelectedItem as ConnectedDevice).ID);
        }

        private void StartButton_Click(object sender, RoutedEventArgs e)
        {
            
        }

        private void CanBitrateField_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            var selected = (sender as ComboBox).SelectedItem;
            if (selected == null) return;
            canAdapter.SetBitrate(int.Parse(selected.ToString()));
        }

        private void CommandSelectorField_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            
        }
    }

}
