using System;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Extensions.Logging;
using MvvmCross;
using MvvmCross.Base;
using MvvmCross.Commands;
using MvvmCross.Navigation;
using MvvmCross.ViewModels;
using OSDP.Net.PanelCommands.DeviceDiscover;
using OSDPBench.Core.Interactions;
using OSDPBench.Core.Models;
using OSDPBench.Core.Platforms;
using OSDPBench.Core.Services;

namespace OSDPBench.Core.ViewModels
{
    public class RootViewModel : MvxNavigationViewModel
    {
        private readonly IMvxNavigationService _navigationService;
        private readonly IDeviceManagementService _deviceManagementService;
        private ISerialPortConnection _serialPortConnection;
        private CancellationTokenSource _cancellationTokenSource;

        public RootViewModel(ILoggerFactory logProvider, IMvxNavigationService navigationService, IDeviceManagementService deviceManagementService,
            ISerialPortConnection serialPort) : base (logProvider, navigationService)
        {
            _navigationService = navigationService;
            _deviceManagementService = deviceManagementService ??
                                       throw new ArgumentNullException(nameof(deviceManagementService));

            _deviceManagementService.ConnectionStatusChange += DeviceManagementServiceOnConnectionStatusChange;
            _deviceManagementService.NakReplyReceived += DeviceManagementServiceOnNakReplyReceived;

            _serialPortConnection = serialPort ?? throw new ArgumentNullException(nameof(serialPort));
        }

        private void DeviceManagementServiceOnConnectionStatusChange(object sender, bool isConnected)
        {
            var dispatcher = Mvx.IoCProvider.Resolve<IMvxMainThreadAsyncDispatcher>();
            dispatcher.ExecuteOnMainThreadAsync(() =>
            {
                if (isConnected)
                {
                    StatusText = "Connected";
                    NakText = string.Empty;
                }
                else if (IsDiscovered)
                {
                    StatusText = "Attempting to connect";
                    StatusLevel = StatusLevel.Error;
                }
            });
        }

        private void DeviceManagementServiceOnNakReplyReceived(object sender, string errorMessage)
        {
            var dispatcher = Mvx.IoCProvider.Resolve<IMvxMainThreadAsyncDispatcher>();
            dispatcher.ExecuteOnMainThreadAsync(() => { NakText = errorMessage; });
        }

        public MvxObservableCollection<AvailableSerialPort> AvailableSerialPorts { get; } = new();

        public MvxObservableCollection<uint> AvailableBaudRates { get; } = new() {9600, 14400, 19200, 38400, 57600, 115200, 230400};

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

        private bool _useConfigurationAddress;

        public bool UseConfigurationAddress
        {
            get => _useConfigurationAddress;
            set => SetProperty(ref _useConfigurationAddress, value);
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
            set
            {
                SetProperty(ref _statusText, value);
                StatusLevel = IsDiscovering ? StatusLevel.Processing : StatusLevel.None;
            }
        }

        private string _nakText;

        public string NakText
        {
            get => _nakText;
            set => SetProperty(ref _nakText, value);
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

        private bool _isDiscovered;

        public bool IsDiscovered
        {
            get => _isDiscovered;
            set => SetProperty(ref _isDiscovered, value);
        }
        
        private StatusLevel _statusLevel;

        public StatusLevel StatusLevel
        {
            get => _statusLevel;
            set => SetProperty(ref _statusLevel, value);
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

        private MvxAsyncCommand _goDiscoverDeviceCommand;

        public System.Windows.Input.ICommand DiscoverDeviceCommand
        {
            get
            {
                return _goDiscoverDeviceCommand ??= new MvxAsyncCommand(async () =>
                {
                    try
                    {
                        await DoDiscoverDeviceCommand();
                    }
                    catch
                    {
                        _alertInteraction.Raise(
                            new Alert("Error while attempting to discover device."));
                    }
                });
            }
        }

        private async Task DoDiscoverDeviceCommand()
        {
            IsReadyToDiscover = false;
            IsDiscovering = true;
            IsDiscovered = false;

            IdentityLookup = new IdentityLookup(null);
            CapabilitiesLookup = new CapabilitiesLookup(null);

            NakText = string.Empty;

            var progress = new DiscoveryProgress(current =>
            {
                switch (current.Status)
                {
                    case DiscoveryStatus.Started:
                        StatusText = "Attempting to discover device";
                        break;
                    case DiscoveryStatus.LookingForDeviceOnConnection:
                        StatusText = $"Attempting to discover device at {current.Connection.BaudRate}";
                        break;
                    case DiscoveryStatus.ConnectionWithDeviceFound:
                        StatusText = $"Found device at {current.Connection.BaudRate}";
                        break;
                    case DiscoveryStatus.LookingForDeviceAtAddress:
                        StatusText = $"Attempting to determine device at {current.Connection.BaudRate} with address {current.Address}";
                        break;
                    case DiscoveryStatus.DeviceIdentified:
                        StatusText = $"Attempting to identify device at {current.Connection.BaudRate} with address {current.Address}";
                        break;
                    case DiscoveryStatus.CapabilitiesDiscovered:
                        StatusText = $"Attempting to get capabilities of device at {current.Connection.BaudRate} with address {current.Address}";
                        break;
                    case DiscoveryStatus.Succeeded:
                        IsDiscovered = true;
                        Address = current.Address;
                        _serialPortConnection = current.Connection as ISerialPortConnection;
                        _selectedBaudRate = (uint)current.Connection.BaudRate;
                        break;
                    case DiscoveryStatus.DeviceNotFound:
                        StatusText = "Failed to connect to device";
                        StatusLevel = StatusLevel.Error;
                        break;
                    case DiscoveryStatus.Error:
                        StatusText = "Error while discovering device";
                        StatusLevel = StatusLevel.Error;
                        break;
                    case DiscoveryStatus.Cancelled:
                        StatusText = "Cancelled discovery";
                        break;
                    default:
                        throw new ArgumentOutOfRangeException();
                }
            });

            await _deviceManagementService.Shutdown();

            var connections = _serialPortConnection.EnumBaudRates(SelectedSerialPort.Name);
            _cancellationTokenSource = new CancellationTokenSource();

            try
            {
                await _deviceManagementService.DiscoverDevice(connections, progress, _cancellationTokenSource.Token);
            }
            catch
            {
                // ignored
            }

            if (IsDiscovered)
            {
                IdentityLookup = _deviceManagementService.IdentityLookup;
                CapabilitiesLookup = _deviceManagementService.CapabilitiesLookup;
            }

            IsReadyToDiscover = true;
            IsDiscovering = false;
        }

        private MvxCommand _goCancelDiscoverDeviceCommand;

        public System.Windows.Input.ICommand CancelDiscoverDeviceCommand
        {
            get
            {
                return _goCancelDiscoverDeviceCommand = new MvxCommand( () =>
                {
                    _cancellationTokenSource?.Cancel();
                });
            }
        }


        private MvxAsyncCommand _scanSerialPortsCommand;

        public System.Windows.Input.ICommand ScanSerialPortsCommand
        {
            get
            {
                return _scanSerialPortsCommand ??= new MvxAsyncCommand(async () =>
                {
                    try
                    {
                        await DoScanSerialPortsCommand();
                    }
                    catch
                    {
                        _alertInteraction.Raise(
                            new Alert(
                                "Error while attempting to scan for serial ports."));
                    }
                });
            }
        }

        private async Task DoScanSerialPortsCommand()
        {
            IsDiscovered = false;
            IsDiscovering = true;

            await _deviceManagementService.Shutdown();

            IdentityLookup = new IdentityLookup(null);
            CapabilitiesLookup = new CapabilitiesLookup(null);

            StatusText = string.Empty;
            NakText = string.Empty;

            AvailableSerialPorts.Clear();

            var foundAvailableSerialPorts = (await _serialPortConnection.FindAvailableSerialPorts()).ToArray();

            if (foundAvailableSerialPorts.Any())
            {
                // Ensure all ports are listed
                foreach (var found in foundAvailableSerialPorts)
                {
                    AvailableSerialPorts.Add(found);
                }

                SelectedSerialPort = AvailableSerialPorts.First();
                IsReadyToDiscover = true;
            }
            else
            {
                _alertInteraction.Raise(new Alert("No serial ports are available.  Make sure that required drivers are installed."));
                IsReadyToDiscover = false;
            }

            IsDiscovering = false;
        }

        private MvxAsyncCommand _updateCommunicationCommand;

        public System.Windows.Input.ICommand UpdateCommunicationCommand
        {
            get
            {
                return _updateCommunicationCommand ??= new MvxAsyncCommand(async () =>
                {
                    try
                    {
                        await DoUpdateCommunicationCommand();
                    }
                    catch
                    {
                        _alertInteraction.Raise(
                            new Alert(
                                "Error while attempting to update communication settings."));
                    }
                });
            }
        }

        private async Task DoUpdateCommunicationCommand()
        {
            NakText = string.Empty;

            bool success = await _navigationService
                .Navigate<UpdateCommunicationViewModel, CommunicationParameters>(
                    new CommunicationParameters(SelectedBaudRate, Address));

            if (!success) return;

            await _deviceManagementService.Shutdown();
            await DoDiscoverDeviceCommand();
        }

        private MvxAsyncCommand _goResetDeviceCommand;

        /// <summary>
        /// Gets the reset device command.
        /// </summary>
        /// <value>The reset device command.</value>
        public System.Windows.Input.ICommand ResetDeviceCommand
        {
            get
            {
                return _goResetDeviceCommand ??= new MvxAsyncCommand(async () =>
                {
                    try
                    {
                        await DoDiscoverResetCommand();
                    }
                    catch
                    {
                        _alertInteraction.Raise(
                            new Alert("Error while attempting to reset device."));
                    }
                });
            }
        }

        private async Task DoDiscoverResetCommand()
        {
            NakText = string.Empty;

            if (!IdentityLookup.CanSendResetCommand)
            {
                _alertInteraction.Raise(
                    new Alert(IdentityLookup.ResetInstructions));
                return;
            }

            await _deviceManagementService.Shutdown();
            IsDiscovered = false;
            StatusText = string.Empty;

            _yesNoInteraction.Raise(new YesNoQuestion("Do you want to reset device, if so power cycle then click yes when the device boots up.", async result =>
            {
                if (!result)
                {
                    _alertInteraction.Raise(new Alert("Perform a discovery to reconnect to the device."));
                    return;
                }
                
                try
                {
                    await _deviceManagementService.ResetDevice(_serialPortConnection);
                    _alertInteraction.Raise(new Alert("Successfully sent reset commands. Power cycle device again and then perform a discovery."));
                }
                catch (Exception exception)
                {
                    _alertInteraction.Raise(new Alert(exception.Message + " Perform a discovery to reconnect to the device."));
                }
            }));
        }

        private readonly MvxInteraction<Alert> _alertInteraction = new();

        /// <summary>
        /// Gets the alert interaction.
        /// </summary>
        /// <value>The alert interaction.</value>
        public IMvxInteraction<Alert> AlertInteraction => _alertInteraction;


        private readonly MvxInteraction<YesNoQuestion> _yesNoInteraction = new();

        /// <summary>
        /// Gets the yes no interaction.
        /// </summary>
        /// <value>The yes no interaction.</value>
        public IMvxInteraction<YesNoQuestion> YesNoInteraction => _yesNoInteraction;

        public override async void Prepare()
        {
            base.Prepare();

            await DoScanSerialPortsCommand();

            SelectedBaudRate = 9600;
        }
    }

    public enum StatusLevel
    {
        None,
        Processing,
        Error
    }
}
