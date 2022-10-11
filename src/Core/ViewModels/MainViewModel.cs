using System;
using System.Linq;
using System.Threading.Tasks;
using MvvmCross;
using MvvmCross.Base;
using MvvmCross.Commands;
using MvvmCross.Navigation;
using MvvmCross.ViewModels;
using OSDPBench.Core.Interactions;
using OSDPBench.Core.Models;
using OSDPBench.Core.Platforms;
using OSDPBench.Core.Services;

namespace OSDPBench.Core.ViewModels
{
    public class MainViewModel : MvxViewModel
    {
        private readonly byte _configurationAddress = 127;

        private readonly IMvxNavigationService _navigationService;
        private readonly IDeviceManagementService _deviceManagementService;
        private readonly ISerialPortConnection _serialPort;

        public MainViewModel(IMvxNavigationService navigationService, IDeviceManagementService deviceManagementService,
            ISerialPortConnection serialPort)
        {
            _navigationService = navigationService ?? throw new ArgumentNullException(nameof(navigationService));
            _deviceManagementService = deviceManagementService ??
                                       throw new ArgumentNullException(nameof(deviceManagementService));

            _deviceManagementService.ConnectionStatusChange += DeviceManagementServiceOnConnectionStatusChange;
            _deviceManagementService.NakReplyReceived += DeviceManagementServiceOnNakReplyReceived;

            _serialPort = serialPort ?? throw new ArgumentNullException(nameof(serialPort));
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
                    StatusText = "Lost Connection";
                    StatusLevel = StatusLevel.Error;
                }
            });
        }

        private void DeviceManagementServiceOnNakReplyReceived(object sender, string errorMessage)
        {
            var dispatcher = Mvx.IoCProvider.Resolve<IMvxMainThreadAsyncDispatcher>();
            dispatcher.ExecuteOnMainThreadAsync(() => { NakText = errorMessage; });
        }

        public MvxObservableCollection<AvailableSerialPort> AvailableSerialPorts { get; } =
            new MvxObservableCollection<AvailableSerialPort>();

        public MvxObservableCollection<uint> AvailableBaudRates { get; } = new MvxObservableCollection<uint>
            {9600, 14400, 19200, 38400, 57600, 115200, 230400};

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

        private MvxCommand _goDiscoverDeviceCommand;

        public System.Windows.Input.ICommand DiscoverDeviceCommand
        {
            get
            {
                return _goDiscoverDeviceCommand = _goDiscoverDeviceCommand ??
                                                  new MvxCommand(async () =>
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

            _serialPort.SelectedSerialPort = SelectedSerialPort;

            IdentityLookup = new IdentityLookup(null);
            CapabilitiesLookup = new CapabilitiesLookup(null);

            StatusText = "Attempting to discover device";
            NakText = string.Empty;

            foreach (var baudRate in AvailableBaudRates)
            {
                StatusText = $"Attempting to discover device at {baudRate}";
                _serialPort.SetBaudRate((int) baudRate);
                IsDiscovered =
                    await _deviceManagementService.DiscoverDevice(_serialPort, (byte)(UseConfigurationAddress ? _configurationAddress : Address), RequireSecureChannel);

                if (!IsDiscovered) continue;

                _selectedBaudRate = baudRate;
                break;
            }

            if (IsDiscovered)
            {
                IdentityLookup = _deviceManagementService.IdentityLookup;
                CapabilitiesLookup = _deviceManagementService.CapabilitiesLookup;
            }
            else
            {
                _deviceManagementService.Shutdown();
                StatusText = "Failed to connect to device";
                StatusLevel = StatusLevel.Error;
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
                                                 new MvxCommand(async () =>
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

            _deviceManagementService.Shutdown();

            IdentityLookup = new IdentityLookup(null);
            CapabilitiesLookup = new CapabilitiesLookup(null);

            StatusText = string.Empty;
            NakText = string.Empty;

            AvailableSerialPorts.Clear();

            var foundAvailableSerialPorts = (await _serialPort.FindAvailableSerialPorts()).ToArray();

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

        private MvxCommand _updateCommunicationCommand;

        public System.Windows.Input.ICommand UpdateCommunicationCommand
        {
            get
            {
                return _updateCommunicationCommand = _updateCommunicationCommand ??
                                                     new MvxCommand(async () =>
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

            var result = await _navigationService
                .Navigate<UpdateCommunicationViewModel, CommunicationParameters, CommunicationParameters>(
                    new CommunicationParameters(SelectedBaudRate, Address));

            if (result == null) return;

            Address = result.Address;
            SelectedBaudRate = result.BaudRate;

            _deviceManagementService.Shutdown();
            await DoDiscoverDeviceCommand();
        }

        private MvxCommand _goResetDeviceCommand;

        /// <summary>
        /// Gets the reset device command.
        /// </summary>
        /// <value>The reset device command.</value>
        public System.Windows.Input.ICommand ResetDeviceCommand
        {
            get
            {
                return _goResetDeviceCommand = _goResetDeviceCommand ??
                                               new MvxCommand(() =>
                                               {
                                                   try
                                                   {
                                                       DoDiscoverResetCommand();
                                                   }
                                                   catch
                                                   {
                                                       _alertInteraction.Raise(
                                                           new Alert("Error while attempting to reset device."));
                                                   }
                                               });
            }
        }

        private void DoDiscoverResetCommand()
        {
            NakText = string.Empty;

            if (!IdentityLookup.CanSendResetCommand)
            {
                _alertInteraction.Raise(
                    new Alert(IdentityLookup.ResetInstructions));
                return;
            }

            _deviceManagementService.Shutdown();
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
                    await _deviceManagementService.ResetDevice(_serialPort);
                    _alertInteraction.Raise(new Alert("Successfully sent reset commands. Power cycle device again and then perform a discovery."));
                }
                catch (Exception exception)
                {
                    _alertInteraction.Raise(new Alert(exception.Message + " Perform a discovery to reconnect to the device."));
                }
            }));
        }

        private readonly MvxInteraction<Alert> _alertInteraction =
            new MvxInteraction<Alert>();

        /// <summary>
        /// Gets the alert interaction.
        /// </summary>
        /// <value>The alert interaction.</value>
        public IMvxInteraction<Alert> AlertInteraction => _alertInteraction;


        private readonly MvxInteraction<YesNoQuestion> _yesNoInteraction =
            new MvxInteraction<YesNoQuestion>();

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
