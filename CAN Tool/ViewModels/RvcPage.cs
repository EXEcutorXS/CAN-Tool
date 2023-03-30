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
            //UpdateFirmwareCommand = new LambdaCommand(OnUpdateFirmwareCommandExecuted, CanUpdateFirmwareCommandExecute);

        }

        public void GotNewMessage(object sender, EventArgs e)
        {
            CanMessage m = (e as GotMessageEventArgs).receivedMessage;
            if (m.RvcCompatible)
                UIContext.Send(x => MessageList.TryToAdd(new RvcMessage(m)),null);
            
        }
    }
}
