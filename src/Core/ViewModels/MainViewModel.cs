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
                _serialPort.SelectedSerialPort = SelectedSerialPort;
                _serialPort.SetBaudRate((int)_selectedBaudRate);
                _connectionId = _panel.StartConnection(_serialPort);

                StatusText = $"Attempting to connect at address {Address}";

                _panel.AddDevice(_connectionId, Address, true, false);

                int count = 0;
                while (!_isConnected && count++ < 5)
                {
                    Thread.Sleep(TimeSpan.FromSeconds(1));
                }

                if (_isConnected)
                {
                    StatusText = $"Connected device at address {Address}";
                    var report = await _panel.IdReport(_connectionId, Address);

                    StatusText = $"{report.SerialNumber}";
                }
                else
                {
                    StatusText = $"Failed to connect to device at address {Address}";
                }
            });
        }

        private readonly MvxInteraction<Alert> _alertInteraction =
            new MvxInteraction<Alert>();

        public IMvxInteraction<Alert> AlertInteraction => _alertInteraction;

        public override void ViewAppeared()
        {
            AvailableSerialPorts.AddRange(AsyncHelper.RunSync(() => _serialPort.FindAvailableSerialPorts()));

            if (AvailableSerialPorts.Any())
            {
                SelectedSerialPort = AvailableSerialPorts.First();
            }
            else
            {
                _alertInteraction.Raise(new Alert("No serial ports are available."));
            }

            SelectedBaudRate = 9600;

            base.ViewAppeared();
        }
    }
}
