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
using CAN_Adapter;
using CAN_Tool.Libs;


namespace CAN_Tool.ViewModels
{

    internal class RvcPageViewModel : ViewModel
    {
        private MainWindowViewModel vm;
        public MainWindowViewModel VM => vm;

        public TimberlineHandler TimberlineHandler { get; } = new();

        public UpdatableList<RvcMessage> MessageList { get; } = new();

        public Dictionary<int,DGN> DgnList => RVC.RVC.DGNs;

        public bool SpamState { set; get; }

        public System.Timers.Timer RefreshTimer;

        
        public int SpamInterval { set; get; } = 100;
        
        public bool RandomDgn { set; get; } = false;

        Task spamTask;

        public RvcPageViewModel(MainWindowViewModel vm)
        {
            this.vm = vm;

            SaveRvcLogCommand = new LambdaCommand(OnSaveRvcLogCommandExecuted, x => true);
            SendRvcMessageCommand = new LambdaCommand(OnSendRvcMessageCommandExecuted, x => true);

            RefreshTimer = new(250);
            RefreshTimer.Elapsed += RefreshTimerTick;
            RefreshTimer.Start();

            spamTask =  Task.Run(SpamFunction);

        }

        private void SpamFunction()
        {
            while (true)
            {
                Thread.Sleep(SpamInterval);

                if (SpamEnabled)
                {
                    if (RandomDgn)
                    {
                        CanMessage msg = new RvcMessage() { Dgn = new Random((int)DateTime.Now.Ticks).Next(0, 0x1FFFF) }.GetCanMessage();
                        vm.CanAdapter.Transmit(msg);
                    }
                    else
                        vm.CanAdapter.Transmit(ConstructedMessage.GetCanMessage());
                }

            }
        }

        private bool spamEnabled;
        public bool SpamEnabled
        {
            get => spamEnabled;
            set => Set(ref spamEnabled, value);
        }

        private RvcMessage selectedMessage;
        public RvcMessage SelectedMessage
        {
            get => selectedMessage;
            set => Set(ref selectedMessage, value);
        }

        public void ProcessMessage(CanMessage m)
        {
            if (m.RvcCompatible)
                MessageList.TryToAdd(new RvcMessage(m));
        }

        public ICommand SaveRvcLogCommand { set; get; }

        private void OnSaveRvcLogCommandExecuted(object parameter)
        {
            string path = Environment.GetFolderPath(Environment.SpecialFolder.Desktop) + "\\RVC_Log.txt";
            string log = "";
            foreach (var m in MessageList)
                log += m.ToString() + '|' + m.PrintParameters() + '\n';
            File.WriteAllText(path, log);
        }

        private void RefreshTimerTick(object sender, EventArgs e)
        {
            foreach (var m in MessageList)
                m.FreshCheck();
            if (spamTask!=null && spamTask.Status != TaskStatus.Running && spamTask.Status != TaskStatus.WaitingToRun && spamTask.Status != TaskStatus.Created)
                spamTask = Task.Run(SpamFunction);
        }

        public ICommand SendRvcMessageCommand { set; get; }

        private void OnSendRvcMessageCommandExecuted(object parameter)
        {
            CanMessage msg = ConstructedMessage.GetCanMessage();
            vm.CanAdapter.Transmit(msg);
        }

        public RvcMessage ConstructedMessage { set; get; } = new() { Dgn = 0x1FFFF, SourceAdress = 80, Priority = 6,Data = new byte[]{0xff, 0xff,0xff, 0xff, 0xff, 0xff, 0xff, 0xff } };

    }
}
