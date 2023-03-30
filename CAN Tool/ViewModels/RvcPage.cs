using OmniProtocol;
using CAN_Tool.Infrastructure.Commands;
using CAN_Tool.ViewModels.Base;
using Microsoft.Win32;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Threading;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Input;
using System.ComponentModel;
using RVC;
using Can_Adapter;
using CAN_Tool.Libs;

namespace CAN_Tool.ViewModels
{

    internal class RvcPage : ViewModel
    {
        private MainWindowViewModel vm;
        public MainWindowViewModel VM => vm;

        public TimberlineHandler TimberlineHandler { get; } = new();

        public UpdatableList<RvcMessage> MessageList { get; } = new();

        private SynchronizationContext UIContext;

        public RvcPage(MainWindowViewModel vm)
        {

            this.vm = vm;
            vm.CanAdapter.GotNewMessage += GotNewMessage;

            UIContext = SynchronizationContext.Current;

            CanMessage m = new();
            m.DLC = 8;
            m.RTR = false;
            m.IDE = true;


            RvcMessage msg = new(m) { Priority = 6, SourceAdress = 101 };
            msg.Dgn = 0x1FFF7;
            msg.Data = new byte[8] { 0x01, 0x00, 0x60, 0x2D, 0x00, 0x25, 0x01, 0x3d };
            MessageList.TryToAdd(msg);
            msg = new(m) { Priority = 6, SourceAdress = 88 };
            msg.Dgn = 0x1FE97;
            msg.Data = new byte[8] { 0x01, 0xF0, 0xFF, 0xFF, 0xFF, 0xFF, 0xFF, 0xFF };
            MessageList.TryToAdd(msg);
            msg = new(m) { Priority = 3, SourceAdress = 88 };
            msg.Dgn = 0x1FEF6;
            msg.Data = new byte[8] { 0x03, 0x01, 0x55, 0x15, 0xFF, 0xFF, 0xFF, 0xFF };
            MessageList.TryToAdd(msg);

            SaveRvcLogCommand = new LambdaCommand(OnSaveRvcLogCommandExecuted, x => true);
            
        }

        private RvcMessage selectedMessage;
        public RvcMessage SelectedMessage
        {
            get => selectedMessage;
            set => Set(ref selectedMessage, value);
        }

        public void GotNewMessage(object sender, EventArgs e)
        {
            CanMessage m = (e as GotMessageEventArgs).receivedMessage;
            if (m.RvcCompatible)
                UIContext.Send(x => MessageList.TryToAdd(new RvcMessage(m)), null);

        }

        public ICommand SaveRvcLogCommand { set; get; }

        private void OnSaveRvcLogCommandExecuted(object parameter)
        {
            string path = Environment.GetFolderPath(Environment.SpecialFolder.Desktop) + "\\RVC_Log.txt";
            string log="";
            foreach (var m in MessageList)
                log += m.ToString()+'\n';
            File.WriteAllText(path, log);
        }
    }
}
