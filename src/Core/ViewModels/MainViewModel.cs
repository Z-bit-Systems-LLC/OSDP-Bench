using System;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
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
            };
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

        private byte _address;
        public byte Address
        {
            get => _address;
            set => SetProperty(ref _address, value);
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

        private bool _isReadyToConnect;
        public bool IsReadyToConnect
        {
            get => _isReadyToConnect;
            set => SetProperty(ref _isReadyToConnect, value);
        }

        private MvxCommand _goConnectCommand;

        public System.Windows.Input.ICommand AttemptConnectCommand
        {
            get
            {
                _goConnectCommand = _goConnectCommand ??
                                    new MvxCommand(DoAttemptConnectCommand);
                return _goConnectCommand;
            }
        }

        private void DoAttemptConnectCommand()
        {
            Task.Run(async () =>
            {
                IsReadyToConnect = false;

                _serialPort.SelectedSerialPort = SelectedSerialPort;
                _serialPort.SetBaudRate((int)_selectedBaudRate);

                IdentityLookup = new IdentityLookup(null);
                _panel.Shutdown();
                _connectionId = _panel.StartConnection(_serialPort);

                StatusText = "Attempting to connect with plain text";

                _panel.AddDevice(_connectionId, Address, true, false);

                bool successfulConnection = WaitForConnection();

                if (successfulConnection)
                {
                    StatusText = "Connected";
                    await GetIdentity();
                    return;
                }
                
                StatusText = "Attempting to connect with secure channel";

                _panel.Shutdown();
                _connectionId = _panel.StartConnection(_serialPort);
                _panel.AddDevice(_connectionId, Address, true, true);

                successfulConnection = WaitForConnection();
                if (successfulConnection)
                {
                    StatusText = "Connected";
                    await GetIdentity();
                }
                else
                {
                    StatusText = "Failed to connect";
                    IsReadyToConnect = true;
                }
            });
        }

        private MvxCommand _scanSerialPortsCommand;
        public System.Windows.Input.ICommand ScanSerialPortsCommand
        {
            get
            {
                _scanSerialPortsCommand = _scanSerialPortsCommand ??
                                          new MvxCommand(DoScanSerialPortsCommand);
                return _scanSerialPortsCommand;
            }
        }

        private void DoScanSerialPortsCommand()
        {
            var foundAvailableSerialPorts = AsyncHelper.RunSync(() => _serialPort.FindAvailableSerialPorts()).ToArray();

            if (foundAvailableSerialPorts.Any())
            {
                AvailableSerialPorts.AddRange(foundAvailableSerialPorts);
                SelectedSerialPort = AvailableSerialPorts.First();
                IsReadyToConnect = true;
            }
            else
            {
                _alertInteraction.Raise(new Alert("No serial ports are available."));
                IsReadyToConnect = false;
            }
        }

        private async Task GetIdentity()
        {
            IdentityLookup = new IdentityLookup(await _panel.IdReport(_connectionId, Address));
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

        public override void ViewAppeared()
        {
            DoScanSerialPortsCommand();

            SelectedBaudRate = 9600;

            base.ViewAppeared();
        }
    }
}
