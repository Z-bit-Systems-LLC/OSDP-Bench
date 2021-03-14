using System;
using System.Collections;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using MvvmCross;
using MvvmCross.Base;
using MvvmCross.Commands;
using MvvmCross.ViewModels;
using OSDP.Net;
using OSDPBench.Core.Interactions;
using OSDPBench.Core.Models;
using OSDPBench.Core.Platforms;

namespace OSDPBench.Core.ViewModels
{
    public class MainViewModel : MvxViewModel
    {
        private readonly ISerialPortConnection _serialPort;
        private readonly ControlPanel _panel = new ControlPanel();

        private Guid _connectionId;
        private bool _isConnected;


        public MainViewModel(ISerialPortConnection serialPort)
        {
            _serialPort = serialPort;
            _panel.ConnectionStatusChanged += (sender, args) =>
            {
                _isConnected = args.IsConnected;

                var dispatcher = Mvx.IoCProvider.Resolve<IMvxMainThreadAsyncDispatcher>();
                dispatcher.ExecuteOnMainThreadAsync(() =>
                {
                    StatusText = _isConnected ? "Connected" : "Failed to connect";
                });
            };
            _panel.NakReplyReceived += (sender, args) =>
            {

            };
            _panel.RawCardDataReplyReceived += (sender, args) =>
            {
                var dispatcher = Mvx.IoCProvider.Resolve<IMvxMainThreadAsyncDispatcher>();
                dispatcher.ExecuteOnMainThreadAsync(() =>
                {
                    _alertInteraction.Raise(new Alert($"Card read -> {FormatData(args.RawCardData.Data)}"));
                });
            };
        }

        private static string FormatData(BitArray bitArray)
        {
            var builder = new StringBuilder();
            foreach (bool bit in bitArray)
            {
                builder.Append(bit ? "1" : "0");
            }

            return builder.ToString();
        }

        public MvxObservableCollection<AvailableSerialPort> AvailableSerialPorts { get; } =
            new MvxObservableCollection<AvailableSerialPort>();

        public MvxObservableCollection<uint> AvailableBaudRates { get; } = new MvxObservableCollection<uint>
            {9600, 14400, 19200, 38400, 57600, 115200};

        private AvailableSerialPort _selectedSerialPort;

        public AvailableSerialPort SelectedSerialPort
        {
            get => _selectedSerialPort;
            set => SetProperty(ref _selectedSerialPort, value);
        }

        private uint _selectedBaudRate;
        public uint SelectedBaudRate
        {
            get => _selectedBaudRate;
            set => SetProperty(ref _selectedBaudRate, value);
        }

        private bool _autoDetectBaudRate;
        public bool AutoDetectBaudRate
        {
            get => _autoDetectBaudRate;
            set => SetProperty(ref _autoDetectBaudRate, value);
        }

        private int _address;
        public int Address
        {
            get => _address;
            set => SetProperty(ref _address, value);
        }

        private bool _requireSecureChannel;
        public bool RequireSecureChannel
        {
            get => _requireSecureChannel;
            set => SetProperty(ref _requireSecureChannel, value);
        }

        private string _statusText;
        public string StatusText
        {
            get => _statusText;
            set => SetProperty(ref _statusText, value);
        }

        private IdentityLookup _identityLookup;
        public IdentityLookup IdentityLookup
        {
            get => _identityLookup;
            set => SetProperty(ref _identityLookup, value);
        }

        private CapabilitiesLookup _capabilitiesLookup;
        public CapabilitiesLookup CapabilitiesLookup
        {
            get => _capabilitiesLookup;
            set => SetProperty(ref _capabilitiesLookup, value);
        }


        private bool _isReadyToDiscover;
        public bool IsReadyToDiscover
        {
            get => _isReadyToDiscover;
            set => SetProperty(ref _isReadyToDiscover, value);
        }

        private bool _isDiscovering;
        public bool IsDiscovering
        {
            get => _isDiscovering;
            set => SetProperty(ref _isDiscovering, value);
        }

        private MvxCommand _goDiscoverDeviceCommand;

        public System.Windows.Input.ICommand DiscoverDeviceCommand
        {
            get
            {
                return _goDiscoverDeviceCommand = _goDiscoverDeviceCommand ??
                                                  new MvxCommand(async () => { await DoDiscoverDeviceCommand(); });
            }
        }

        private async Task DoDiscoverDeviceCommand()
        {
            IsReadyToDiscover = false;
            IsDiscovering = true;

            _serialPort.SelectedSerialPort = SelectedSerialPort;
            _serialPort.SetBaudRate((int) _selectedBaudRate);

            IdentityLookup = new IdentityLookup(null);
            CapabilitiesLookup = new CapabilitiesLookup(null);

            _panel.Shutdown();

            _connectionId = _panel.StartConnection(_serialPort);

            StatusText = "Attempting to connect";

            _panel.AddDevice(_connectionId, (byte) Address, false, false);

            bool successfulConnection = WaitForConnection();

            if (successfulConnection)
            {
                await GetIdentity();
                await GetCapabilities();

                _panel.AddDevice(_connectionId, (byte) Address, CapabilitiesLookup.CRC,
                    RequireSecureChannel && CapabilitiesLookup.SecureChannel);
            }

            IsReadyToDiscover = true;
            IsDiscovering = false;
        }

        private MvxCommand _scanSerialPortsCommand;

        public System.Windows.Input.ICommand ScanSerialPortsCommand
        {
            get
            {
                return _scanSerialPortsCommand = _scanSerialPortsCommand ??
                                                 new MvxCommand(async () => { await DoScanSerialPortsCommand(); });
            }
        }

        private async Task DoScanSerialPortsCommand()
        {
            IsDiscovering = true;

            _panel.Shutdown();
            IdentityLookup = new IdentityLookup(null);
            CapabilitiesLookup = new CapabilitiesLookup(null);
            StatusText = string.Empty;

            AvailableSerialPorts.Clear();

            var foundAvailableSerialPorts = (await _serialPort.FindAvailableSerialPorts()).ToArray();

            if (foundAvailableSerialPorts.Any())
            {
                // Ensure all ports are listed
                for (var index = 0; index < foundAvailableSerialPorts.Length; index++)
                {
                    AvailableSerialPorts.Add(foundAvailableSerialPorts[index]);
                }

                SelectedSerialPort = AvailableSerialPorts.First();
                IsReadyToDiscover = true;
            }
            else
            {
                _alertInteraction.Raise(new Alert("No serial ports are available."));
                IsReadyToDiscover = false;
            }

            IsDiscovering = false;
        }

        private async Task GetIdentity()
        {
            IdentityLookup = new IdentityLookup(await _panel.IdReport(_connectionId, (byte)Address));
        }

        private async Task GetCapabilities()
        {
            CapabilitiesLookup = new CapabilitiesLookup(await _panel.DeviceCapabilities(_connectionId, (byte) Address));
        }

        private bool WaitForConnection()
        {
            int count = 0;
            while (!_isConnected && count++ < 5)
            {
                Thread.Sleep(TimeSpan.FromSeconds(1));
            }

            return _isConnected;
        }

        private readonly MvxInteraction<Alert> _alertInteraction =
            new MvxInteraction<Alert>();

        public IMvxInteraction<Alert> AlertInteraction => _alertInteraction;

        public override async void ViewAppeared()
        {
            await DoScanSerialPortsCommand();

            SelectedBaudRate = 9600;

            base.ViewAppeared();
        }
    }
}
