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
using System.Windows.Controls.Primitives;
using System.Windows.Controls;
using System.Reflection;
using System.Runtime.CompilerServices;

namespace CAN_Tool.ViewModels
{
    internal class DataBox : TextBox, INotifyPropertyChanged
    {

        public event PropertyChangedEventHandler PropertyChanged;

        protected void OnPropertyChanged([CallerMemberName] string PropertyName = null)
        {
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(PropertyName));
        }

        protected bool Set<T>(ref T field, T value, [CallerMemberName] string PropertyName = null)
        {
            if (!Equals(field, value))
            {
                field = value;
                var attr = GetType().GetProperty(PropertyName)?.GetCustomAttribute(typeof(AffectsToAttribute));
                if (attr != null)
                {
                    AffectsToAttribute at = (AffectsToAttribute)attr;
                    foreach (var p in at.Props)
                        OnPropertyChanged(p);
                }

                //2 Вариант
                //foreach (var property in GetType().GetProperties()) OnPropertyChanged(property.Name);

                OnPropertyChanged(PropertyName);
                CommandManager.InvalidateRequerySuggested();  //   Фикc необновления статуса кнопок

                return true;
            }
            else
                return false;
        }


        private byte[] data;
        public byte[] Data { set => Set(ref data, value); get => data; }

        public string DataAsText => $"{Data[0]:02X} {Data[1]:02X} {Data[2]:02X} {Data[3]:02X} {Data[4]:02X} {Data[5]:02X} {Data[6]:02X} {Data[7]:02X} ";

        public DataBox()
        {
            Text = "ff ff ff ff ff ff ff ff";
            TextChanged += customTextChanged;
        }

        
        public void customTextChanged(object sender, EventArgs args)
        {
            SelectionStart
        }
    }

    internal class CanPageViewModel : ViewModel
    {
        private MainWindowViewModel vm;
        public MainWindowViewModel VM => vm;

        public UpdatableList<CanMessage> MessageList { get; } = new();

        public CanMessage ConstructedMessage { set; get; } = new();

        public bool AutoSend { set; get; } = false;

        private Timer autoSendTimer;

        public int SendInterval { set; get; } = 100;

        private UInt64 ulng = 0;

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
