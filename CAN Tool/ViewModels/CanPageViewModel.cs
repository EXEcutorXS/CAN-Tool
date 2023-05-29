using OmniProtocol;
using CAN_Tool.Infrastructure.Commands;
using CAN_Tool.ViewModels.Base;
using Microsoft.Win32;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Windows;
using System.Windows.Input;
using System.ComponentModel;
using RVC;
using CAN_Adapter;
using CAN_Tool.Libs;
using System.Timers;

namespace CAN_Tool.ViewModels
{

    internal class CanPageViewModel : ViewModel
    {
        private MainWindowViewModel vm;
        public MainWindowViewModel VM => vm;

        public UpdatableList<CanMessage> MessageList { get; } = new();

        public CanMessage ConstructedMessage { set; get; } = new();

        public bool AutoSend { set; get; } = false;

        private Timer autoSendTimer;

        public int SendInterval { set; get; } = 100;

        private UInt64 ulng=0;
        
        public UInt64 Ulng { set => Set(ref ulng, value); get => ulng; }


        public CanPageViewModel(MainWindowViewModel vm)
        {
            this.vm = vm;

            autoSendTimer = new(200);
            autoSendTimer.Elapsed += SpamTimerTick;
            autoSendTimer.Start();
            SaveCanLogCommand = new LambdaCommand(OnSaveCanLogCommandExecuted, x => true);
            SendCanMessageCommand = new LambdaCommand(OnSendCanMessageCommandExecuted, x => true);
        }

        private int autoSendCounter = 0;

        public void ProcessMessage(CanMessage m)
        {
            MessageList.TryToAdd(m);
        }

        public ICommand SaveCanLogCommand { set; get; }

        private void OnSaveCanLogCommandExecuted(object parameter)
        {
            string path = Environment.GetFolderPath(Environment.SpecialFolder.Desktop) + "\\CAN_Log.txt";
            string log = "";
            foreach (var m in MessageList)
                log += m.ToString() + '|' + '\n';
            File.WriteAllText(path, log);
        }

        private void SpamTimerTick(object sender, EventArgs e)
        {
            if (autoSendCounter > SendInterval)
            {
                autoSendCounter = 0;

                vm.CanAdapter.Transmit(ConstructedMessage);
            }

            foreach (var m in MessageList)
                m.FreshCheck();

        }

        public ICommand SendCanMessageCommand { set; get; }

        private void OnSendCanMessageCommandExecuted(object parameter)
        {
            vm.CanAdapter.Transmit(ConstructedMessage);
        }


    }
}
