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
using CAN_Tool.Libs;
using System.Timers;
using System.Windows.Controls.Primitives;
using System.Windows.Controls;
using System.Reflection;
using System.Runtime.CompilerServices;
using System.Windows.Threading;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;

namespace CAN_Tool.ViewModels
{

    public partial class CanPageViewModel : ObservableObject
    {
        private MainWindowViewModel vm;
        public MainWindowViewModel VM => vm;

        public UpdatableList<CanMessage> MessageList { get; } = new();

        public CanMessage ConstructedMessage { set; get; } = new();

        private System.Timers.Timer autoSendTimer;

        private int autoSendCounter = 0;

        [ObservableProperty]
        private bool autoSend;


        [ObservableProperty]
        private int sendInterval = 100;

        [ObservableProperty]
        private UInt64 ulng = 0;

        public CanPageViewModel(MainWindowViewModel vm)
        {
            this.vm = vm;

            autoSendTimer = new(20);
            autoSendTimer.Elapsed += SpamTimerTick;
            autoSendTimer.Start();

        }


        public void ProcessMessage(CanMessage m) => MessageList.TryToAdd(m);

        [RelayCommand]
        private void SaveCanLog()
        {
            var path = Environment.GetFolderPath(Environment.SpecialFolder.Desktop) + "\\CAN_Log.txt";
            var log = "";
            foreach (var m in MessageList)
                log += m.ToString() + '|' + '\n';
            File.WriteAllText(path, log);
        }

        [RelayCommand]
        private void SendCanMessage() => vm.CanAdapter.Transmit(ConstructedMessage);

        private void SpamTimerTick(object sender, EventArgs e)
        {
            if (autoSendCounter > SendInterval/20)
            {
                autoSendCounter = 0;
                if (AutoSend)
                    vm.CanAdapter.Transmit(ConstructedMessage);
            }
            else
                autoSendCounter += 20;

            foreach (var m in MessageList)
                m.FreshCheck();
        }
    }
}
