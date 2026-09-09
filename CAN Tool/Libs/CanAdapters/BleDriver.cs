using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using Windows.Devices.Bluetooth;
using Windows.Devices.Bluetooth.Advertisement;
using Windows.Devices.Bluetooth.GenericAttributeProfile;
using Windows.Storage.Streams;

namespace CAN_Tool.Libs.CanAdapters
{
    // Talks to the PU-28 bootloader's "BLE-CAN bridge" mode: a BlueNRG GATT service
    // that carries 20-byte application packets (see PU28-BOOT-CAN's User/Bluetooth/bluetooth.h
    // and User/Main/main.c::handlerBluetooth). Only the CAN TX/RX packet types are used
    // here; flash read/write/erase and status telemetry are not exposed through this
    // driver, matching what a plain CAN adapter is expected to do.
    public class BleDriver : ICanAdapterDriver, IScannableCanAdapterDriver
    {
        private static readonly Guid ServiceUuid = Guid.Parse("D973F2E0-B19E-11E2-9E96-0800200C9A66");
        private static readonly Guid TxCharUuid = Guid.Parse("D973F2E1-B19E-11E2-9E96-0800200C9A66");
        private static readonly Guid RxCharUuid = Guid.Parse("D973F2E2-B19E-11E2-9E96-0800200C9A66");

        private const int PacketLength = 20;

        // Type codes 1-19 are reserved by the app's own BLE protocol (bluetooth.h in both
        // PU28-Timberline and PU28-BOOT-CAN - the bootloader now speaks the app's protocol
        // for TYPE_MEMORY/TYPE_FRAG_*/TYPE_REBOOT too, not just this bridge), so the CAN
        // bridge - bootloader-only, no app counterpart - lives past that range.
        private const byte MsgCanTx = 20;
        private const byte MsgCanRx = 21;

        // Advertised by Make_Connection() in PU28-BOOT-CAN's User/Ble/sample_service.c.
        private const string DeviceNamePrefix = "Autoterm PU";

        // Populated by StartScan(); lets Open() skip a second discovery pass for a
        // device the UI already found when the user hit "refresh ports".
        private readonly Dictionary<string, ulong> _knownDevices = new();

        private BluetoothLEAdvertisementWatcher _scanWatcher;
        private Action<string> _onDeviceFound;

        private BluetoothLEDevice _device;
        private GattSession _session;
        private GattCharacteristic _txChar;
        private GattCharacteristic _rxChar;

        public event EventHandler<GotCanMessageEventArgs> MessageReceived;

        public void StartScan(Action<string> deviceFound)
        {
            StopScan();
            _onDeviceFound = deviceFound;
            _scanWatcher = new BluetoothLEAdvertisementWatcher { ScanningMode = BluetoothLEScanningMode.Active };
            _scanWatcher.Received += OnScanReceived;
            _scanWatcher.Start();
        }

        public void StopScan()
        {
            if (_scanWatcher == null) return;
            _scanWatcher.Received -= OnScanReceived;
            try { _scanWatcher.Stop(); } catch { }
            _scanWatcher = null;
        }

        private void OnScanReceived(BluetoothLEAdvertisementWatcher sender, BluetoothLEAdvertisementReceivedEventArgs args)
        {
            var name = args.Advertisement.LocalName;
            if (string.IsNullOrWhiteSpace(name)) return;
            if (!name.StartsWith(DeviceNamePrefix, StringComparison.Ordinal)) return;

            name = name.TrimEnd();
            var isNew = !_knownDevices.ContainsKey(name);
            _knownDevices[name] = args.BluetoothAddress;
            if (isNew) _onDeviceFound?.Invoke(name);
        }

        // The bootloader's BLE bridge has no distinct wire modes (no local echo, no
        // bus-off listen-only state) - all three just mean "connect to this device".
        public void OpenNormal(string portName) => Open(portName);
        public void OpenSelfReception(string portName) => Open(portName);
        public void OpenListenOnly(string portName) => Open(portName);

        private void Open(string portName)
        {
            if (string.IsNullOrWhiteSpace(portName))
                throw new InvalidOperationException("Select a BLE device first (Refresh ports, then pick it from the list).");

            var address = _knownDevices.TryGetValue(portName, out var known)
                ? known
                : ResolveAddressByScanning(portName, TimeSpan.FromSeconds(5));

            // WinRT's BLE APIs are Task-based, but ICanAdapterDriver is a synchronous
            // contract shared with the USB drivers - block here the same way SerialPort.Open()
            // or a PCAN init call would, rather than reshaping every driver's interface for one.
            ConnectAsync(address).GetAwaiter().GetResult();
        }

        private static ulong ResolveAddressByScanning(string name, TimeSpan timeout)
        {
            var tcs = new TaskCompletionSource<ulong>();
            var watcher = new BluetoothLEAdvertisementWatcher { ScanningMode = BluetoothLEScanningMode.Active };

            void Handler(BluetoothLEAdvertisementWatcher s, BluetoothLEAdvertisementReceivedEventArgs a)
            {
                var n = a.Advertisement.LocalName;
                if (!string.IsNullOrWhiteSpace(n) && n.TrimEnd() == name)
                    tcs.TrySetResult(a.BluetoothAddress);
            }

            watcher.Received += Handler;
            watcher.Start();
            try
            {
                var finished = Task.WhenAny(tcs.Task, Task.Delay(timeout)).GetAwaiter().GetResult();
                if (finished != tcs.Task)
                    throw new InvalidOperationException($"BLE device '{name}' not found (out of range or powered off).");
                return tcs.Task.Result;
            }
            finally
            {
                watcher.Received -= Handler;
                try { watcher.Stop(); } catch { }
            }
        }

        private async Task ConnectAsync(ulong address)
        {
            // Every await below is forced onto a thread-pool continuation via
            // AsTask().ConfigureAwait(false): Open() blocks the calling (UI) thread on this
            // method's result, and a WinRT await that defaults to capturing the UI
            // SynchronizationContext would try to resume on that same blocked thread -
            // an instant deadlock. Don't drop these while "cleaning up" this method.
            _device = await BluetoothLEDevice.FromBluetoothAddressAsync(address).AsTask().ConfigureAwait(false)
                ?? throw new InvalidOperationException("Could not open the BLE device (out of range or paired to another app).");
            _device.ConnectionStatusChanged += OnConnectionStatusChanged;

            // Windows treats a BLE connection nobody has asked it to "keep alive" as
            // background/idle and tends to negotiate a long, power-saving connection
            // interval for it - which, since every CAN frame is now a Write Request that
            // blocks for one full interval round trip (see Transmit() below), is exactly
            // what was making flashing crawl at ~150ms/message. Holding a GattSession open
            // with MaintainConnection=true is the standard way to tell Windows this
            // connection is actively used, so it negotiates (and keeps) a much shorter
            // interval instead of letting it drift upward between bursts of traffic.
            _session = await GattSession.FromDeviceIdAsync(_device.BluetoothDeviceId).AsTask().ConfigureAwait(false);
            var canMaintain = _session.CanMaintainConnection;
            if (canMaintain) _session.MaintainConnection = true;

            var servicesResult = await _device.GetGattServicesForUuidAsync(ServiceUuid, BluetoothCacheMode.Uncached).AsTask().ConfigureAwait(false);
            if (servicesResult.Status != GattCommunicationStatus.Success || servicesResult.Services.Count == 0)
                throw new InvalidOperationException($"CAN bridge service not found ({servicesResult.Status}).");
            var service = servicesResult.Services[0];

            var txResult = await service.GetCharacteristicsForUuidAsync(TxCharUuid, BluetoothCacheMode.Uncached).AsTask().ConfigureAwait(false);
            if (txResult.Status != GattCommunicationStatus.Success || txResult.Characteristics.Count == 0)
                throw new InvalidOperationException($"TX characteristic not found ({txResult.Status}).");
            _txChar = txResult.Characteristics[0];

            var rxResult = await service.GetCharacteristicsForUuidAsync(RxCharUuid, BluetoothCacheMode.Uncached).AsTask().ConfigureAwait(false);
            if (rxResult.Status != GattCommunicationStatus.Success || rxResult.Characteristics.Count == 0)
                throw new InvalidOperationException($"RX characteristic not found ({rxResult.Status}).");
            _rxChar = rxResult.Characteristics[0];

            var notifyStatus = await _txChar.WriteClientCharacteristicConfigurationDescriptorAsync(
                GattClientCharacteristicConfigurationDescriptorValue.Notify).AsTask().ConfigureAwait(false);
            if (notifyStatus != GattCommunicationStatus.Success)
                throw new InvalidOperationException($"Could not enable notifications ({notifyStatus}).");
            _txChar.ValueChanged += OnTxValueChanged;
        }

        private void OnConnectionStatusChanged(BluetoothLEDevice sender, object args)
        {
            // ICanAdapterDriver has no "unexpectedly disconnected" event; the app will
            // simply notice frames stop arriving, same as an unplugged USB adapter today.
            if (sender.ConnectionStatus == BluetoothConnectionStatus.Disconnected)
                System.Diagnostics.Debug.WriteLine("BleDriver: device went out of range or disconnected.");
        }

        public void Close()
        {
            if (_txChar != null)
            {
                _txChar.ValueChanged -= OnTxValueChanged;
                try
                {
                    _txChar.WriteClientCharacteristicConfigurationDescriptorAsync(
                        GattClientCharacteristicConfigurationDescriptorValue.None).GetAwaiter().GetResult();
                }
                catch { }
                _txChar = null;
            }
            _rxChar = null;

            if (_session != null)
            {
                try { _session.MaintainConnection = false; } catch { }
                _session.Dispose();
                _session = null;
            }

            if (_device != null)
            {
                _device.ConnectionStatusChanged -= OnConnectionStatusChanged;
                _device.Dispose();
                _device = null;
            }
        }

        // The BLE bridge carries whatever bitrate the PU-28's CAN bus is wired for -
        // there's no MSG_SET_BITRATE in the packet protocol, so this is a no-op kept
        // only so BleDriver satisfies the same interface as the USB adapters.
        public void SetBitrate(int bitrate) { }

        public void Transmit(CanMessage message)
        {
            var rxChar = _rxChar ?? throw new InvalidOperationException("Not connected.");

            var p = new byte[PacketLength];
            p[0] = MsgCanTx;
            p[1] = 0;
            p[2] = (byte)((message.Ide ? 1 : 0) | (message.Rtr ? 2 : 0));
            var id = unchecked((uint)message.Id);
            p[3] = (byte)(id & 0xFF);
            p[4] = (byte)((id >> 8) & 0xFF);
            p[5] = (byte)((id >> 16) & 0xFF);
            p[6] = (byte)((id >> 24) & 0xFF);
            var dlc = (byte)Math.Min(message.Dlc, 8);
            p[7] = dlc;
            for (var i = 0; i < 8; i++) p[8 + i] = i < dlc ? message.Data[i] : (byte)0;
            p[19] = Crc8Of(p, 19); // TYPE_CAN_TX isn't in the firmware's CRC8_EXEMPT set - see main.c

            var writer = new DataWriter();
            writer.WriteBytes(p);

            // WriteWithoutResponse just enqueues the packet locally and returns - it has no
            // idea whether the peripheral actually got it, and packets sent faster than the
            // BLE link/the bootloader's ~1kHz relay loop can drain (see Bluetooth::handler()
            // in User/Bluetooth/bluetooth.cpp) get silently dropped rather than queued. A
            // fixed inter-send delay was tried first and didn't fix it reliably - flashing
            // still lost chunks unpredictably, because the real constraint isn't a fixed
            // interval, it's "did the peripheral confirm this specific packet". The RX
            // characteristic supports CHAR_PROP_WRITE (see Add_Sample_Service() in
            // User/Ble/sample_service.c), so WriteWithResponse turns every send into a real
            // ATT Write Request/Response round trip: this call now blocks until the
            // bootloader's BLE stack has actually acknowledged the packet, which is genuine
            // backpressure instead of a guessed sleep - slower per message, but each one is
            // now known-delivered before the next is sent, which is what flashing needs.
            var status = rxChar.WriteValueAsync(writer.DetachBuffer(), GattWriteOption.WriteWithResponse)
                .AsTask().GetAwaiter().GetResult();
            if (status != GattCommunicationStatus.Success)
                throw new InvalidOperationException($"Send failed ({status}).");
        }

        private void OnTxValueChanged(GattCharacteristic sender, GattValueChangedEventArgs args)
        {
            var buffer = args.CharacteristicValue;
            if (buffer.Length < PacketLength) return;

            var p = new byte[buffer.Length];
            DataReader.FromBuffer(buffer).ReadBytes(p);

            if (p[0] != MsgCanRx) return; // status telemetry and other packet types aren't CAN traffic

            var dlc = p[7];
            if (dlc > 8) dlc = 8;
            var data = new byte[8];
            Array.Copy(p, 8, data, 0, 8);

            var message = new CanMessage
            {
                Id = p[3] | (p[4] << 8) | (p[5] << 16) | (p[6] << 24),
                Ide = (p[2] & 1) != 0,
                Rtr = (p[2] & 2) != 0,
                Dlc = dlc,
                Data = data,
            };

            MessageReceived?.Invoke(this, new GotCanMessageEventArgs { receivedMessage = message });
        }

        // CRC-8 (poly 0x07, init 0x00, MSB-first) of bytes[0..len). Matches the firmware's
        // crc8Of() (main.c) and the phone app's (app.js) bit-for-bit - added 2026-09-08 as a
        // protocol-wide packet integrity gate; the bootloader drops anything that fails this
        // check (except TYPE_MEMORY_DATA/TYPE_FRAG_DATA/TYPE_FRAG_DATA_ACK, which use byte 19
        // for their own data - not applicable to TYPE_CAN_TX).
        private static byte Crc8Of(byte[] data, int len)
        {
            byte crc = 0;
            for (var i = 0; i < len; i++)
            {
                crc ^= data[i];
                for (var b = 0; b < 8; b++)
                    crc = (crc & 0x80) != 0 ? (byte)((crc << 1) ^ 0x07) : (byte)(crc << 1);
            }
            return crc;
        }
    }
}
