using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using CAN_Tool.ViewModels.Base;
using System.ComponentModel;
using System.Windows.Input;
using System.Windows;
using CAN_Tool.Infrastructure.Commands;
using System.IO.Ports;
using Can_Adapter;
using AdversCan;
using System.Windows.Threading;
using System.Threading;

namespace CAN_Tool.ViewModels
{
    internal class MainWindowViewModel : ViewModel
    {

        int[] _Bitrates => new int[] { 20, 50, 125, 250, 500, 800, 1000 };

        public int[] Bitrates => _Bitrates;

        CanAdapter _canAdapter;
        public CanAdapter canAdapter { get => _canAdapter; }

        AC2P _AC2PInstance;
        public AC2P AC2PInstance => _AC2PInstance;

        ConnectedDevice _connectedDevice;
        public ConnectedDevice SelectedConnectedDevice
        {
            set =>
                Set(ref _connectedDevice, value);
            get => _connectedDevice;
        }

        public Dictionary<CommandId, AC2PCommand> Commands => AC2P.commands;

        public AC2PMessage prepearedCommandMessage { get; set; }

        public AC2PMessage CustomMessage { get; set; } = new AC2PMessage();

        public string CustomMessageDataString { get; set; }

        public string CustomMessagePgn { set; get; }


        #region SelectedPortIndex;
        private int _SelectedPortIndex = -1;
        public int SelectedPortIndex
        {
            get => _SelectedPortIndex;
            set => Set(ref _SelectedPortIndex, value);
        }
        #endregion

        #region PortList
        private BindingList<string> _PortList = new BindingList<string>();
        public BindingList<string> PortList
        {
            get => _PortList;
            set => Set(ref _PortList, value);
        }
        #endregion

        #region ApplicationCloseCommand
        public ICommand ApplicationCloseCommand;
        private void OnApplicationCloseCommandExecuted(object parameter) => Application.Current.Shutdown();
        private bool CanApplicationCloseCommandExecute(object parameter) => true;

        #endregion

        #region RefreshPortsCommand
        public ICommand RefreshPortListCommand { get; }
        private void OnRefreshPortsCommandExecuted(object Parameter)
        {
            PortList.Clear();
            foreach (var port in SerialPort.GetPortNames())
                PortList.Add(port);
        }

        #endregion

        #region OpenPortCommand
        public ICommand OpenPortCommand { get; }
        private void OnOpenPortCommandExecuted(object parameter)
        {
            canAdapter.PortName = _PortList[SelectedPortIndex];
            canAdapter.PortOpen();
        }
        private bool CanOpenPortCommandExecute(object parameter) => (_SelectedPortIndex >= 0 && _PortList[_SelectedPortIndex].StartsWith("COM") && !canAdapter.PortOpened);
        #endregion

        #region ClosePortCommand
        public ICommand ClosePortCommand { get; }
        private void OnClosePortCommandExecuted(object parameter)
        {
            canAdapter.PortClose();
        }
        private bool CanClosePortCommandExecute(object parameter) => (canAdapter.PortOpened);
        #endregion

        #region CancelOperationCommand
        public ICommand CancelOperationCommand { get; }
        private void OnCancelOperationCommandExecuted(object parameter)
        {
            AC2PInstance.CurrentTask.onCancel();
        }
        private bool CanCancelOperationCommandExecute(object parameter) => (AC2PInstance.CurrentTask.Occupied);
        #endregion


        #region ReadConfigCommand
        public ICommand ReadConfigCommand { get; }
        private void OnReadConfigCommandExecuted(object parameter)
        {
            AC2PInstance.ReadAllParameters(_connectedDevice.ID);
        }
        private bool CanReadConfigCommandExecute(object parameter) =>
            (canAdapter.PortOpened && SelectedConnectedDevice != null && !AC2PInstance.CurrentTask.Occupied);
        #endregion

        #region SaveConfigCommand
        public ICommand SaveConfigCommand { get; }
        private void OnSaveConfigCommandExecuted(object parameter)
        {
            AC2PInstance.SaveParameters(_connectedDevice.ID);
        }
        private bool CanSaveConfigCommandExecute(object parameter) =>
            (canAdapter.PortOpened && SelectedConnectedDevice != null && !AC2PInstance.CurrentTask.Occupied && SelectedConnectedDevice.readedParameters.Count>0);
        #endregion

        #region ResetConfigCommand
        public ICommand ResetConfigCommand { get; }
        private void OnResetConfigCommandExecuted(object parameter)
        {
            AC2PInstance.ResetParameters(_connectedDevice.ID);
        }
        private bool CanResetConfigCommandExecute(object parameter) =>
            (canAdapter.PortOpened && SelectedConnectedDevice != null && !AC2PInstance.CurrentTask.Occupied);
        #endregion

        #region ReadBlackBoxDataCommand
        public ICommand ReadBlackBoxDataCommand { get; }
        private void OnReadBlackBoxDataCommandExecuted(object parameter)
        {
            AC2PInstance.ReadBlackBoxData(_connectedDevice.ID);
        }
        private bool CanReadBlackBoxDataExecute(object parameter) =>
            (canAdapter.PortOpened && SelectedConnectedDevice != null && !AC2PInstance.CurrentTask.Occupied);
        #endregion

        #region ReadBlackBoxErrorsCommand
        public ICommand ReadBlackBoxErrorsCommand { get; }
        private void OnReadBlackBoxErrorsCommandExecuted(object parameter)
        {
            Task.Run(()=>AC2PInstance.ReadErrorsBlackBox(_connectedDevice.ID));
        }
        private bool CanReadBlackBoxErrorsExecute(object parameter) =>
            (canAdapter.PortOpened && SelectedConnectedDevice != null && !AC2PInstance.CurrentTask.Occupied);
        #endregion

        #region EraseBlackBoxErrorsCommand
        public ICommand EraseBlackBoxErrorsCommand { get; }
        private void OnEraseBlackBoxErrorsCommandExecuted(object parameter)
        {
            Task.Run(() => AC2PInstance.EraseErrorsBlackBox(_connectedDevice.ID));
        }
        private bool CanEraseBlackBoxErrorsExecute(object parameter) =>
            (canAdapter.PortOpened && SelectedConnectedDevice != null && !AC2PInstance.CurrentTask.Occupied);
        #endregion

        #region EraseBlackBoxDataCommand
        public ICommand EraseBlackBoxDataCommand { get; }
        private void OnEraseBlackBoxDataCommandExecuted(object parameter)
        {
            Task.Run(() => AC2PInstance.EraseErrorsBlackBox(_connectedDevice.ID));
        }
        private bool CanEraseBlackBoxDataExecute(object parameter) =>
            (canAdapter.PortOpened && SelectedConnectedDevice != null && !AC2PInstance.CurrentTask.Occupied);
        #endregion

        #region SendCustomMessageCommand
        public ICommand SendCustomMessageCommand { get; }
        private void OnSendCustomMessageCommandExecuted(object parameter)
        {
            byte[] data = new byte[8];
            if (CustomMessageDataString.Length != 16) return;
            for (int i = 0; i < 8; i++)
                data[i] = Convert.ToByte(CustomMessageDataString.Substring(i * 2, 2), 16);
            CustomMessage.Data = data;
            CustomMessage.TransmitterId = new DeviceId(126, 6);
            CustomMessage.ReceiverId = SelectedConnectedDevice.ID;
            CustomMessage.PGN = Convert.ToInt32(CustomMessagePgn);
            AC2PInstance.SendMessage(CustomMessage);
        }
        private bool CanSendCustomMessageCommandExecute(object parameter)
        {
            if (CustomMessageDataString.Length != 16) return false;
            try
            {
                Convert.ToInt64(CustomMessageDataString, 16);
                if (Convert.ToInt32(CustomMessagePgn)>511) return false;
            }
            catch
            {
                return false;
            }
            if (!canAdapter.PortOpened || SelectedConnectedDevice == null) return false;
            return true;
        }

        #endregion

        public MainWindowViewModel()
        {
            AC2P.ParseParamsname();
            _canAdapter = new CanAdapter();
            _AC2PInstance = new AC2P(canAdapter);

            ApplicationCloseCommand = new LambdaCommand(OnApplicationCloseCommandExecuted, CanApplicationCloseCommandExecute);
            OpenPortCommand = new LambdaCommand(OnOpenPortCommandExecuted, CanOpenPortCommandExecute);
            ClosePortCommand = new LambdaCommand(OnClosePortCommandExecuted, CanClosePortCommandExecute);
            RefreshPortListCommand = new LambdaCommand(OnRefreshPortsCommandExecuted);
            ReadConfigCommand = new LambdaCommand(OnReadConfigCommandExecuted, CanReadConfigCommandExecute);
            ReadBlackBoxDataCommand = new LambdaCommand(OnReadBlackBoxDataCommandExecuted, CanReadBlackBoxDataExecute);
            ReadBlackBoxErrorsCommand = new LambdaCommand(OnReadBlackBoxErrorsCommandExecuted, CanReadBlackBoxErrorsExecute);
            EraseBlackBoxErrorsCommand = new LambdaCommand(OnEraseBlackBoxErrorsCommandExecuted, CanEraseBlackBoxErrorsExecute);
            EraseBlackBoxDataCommand = new LambdaCommand(OnEraseBlackBoxDataCommandExecuted, CanEraseBlackBoxDataExecute);
            SendCustomMessageCommand = new LambdaCommand(OnSendCustomMessageCommandExecuted, CanSendCustomMessageCommandExecute);
            CancelOperationCommand = new LambdaCommand(OnCancelOperationCommandExecuted, CanCancelOperationCommandExecute);
            SaveConfigCommand = new LambdaCommand(OnSaveConfigCommandExecuted, CanSaveConfigCommandExecute);
            ResetConfigCommand = new LambdaCommand(OnResetConfigCommandExecuted, CanResetConfigCommandExecute);
            
        }

    }
}
