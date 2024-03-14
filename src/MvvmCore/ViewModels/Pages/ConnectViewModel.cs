using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using MvvmCore.Models;
using MvvmCore.Services;
using OSDP.Net.PanelCommands.DeviceDiscover;
using System.Collections.ObjectModel;

namespace MvvmCore.ViewModels.Pages
{
    public partial class ConnectViewModel : ObservableObject
    {
        private readonly IDialogService _dialogService;
        private readonly IDeviceManagementService _deviceManagementService;
        private ISerialPortConnectionService _serialPortConnectionService;
        private CancellationTokenSource? _cancellationTokenSource;

        public ConnectViewModel(IDialogService dialogService, IDeviceManagementService deviceManagementService,
            ISerialPortConnectionService serialPortConnectionService)
        {
            _dialogService = dialogService ??
                             throw new ArgumentNullException(nameof(dialogService));
            _deviceManagementService = deviceManagementService ??
                                       throw new ArgumentNullException(nameof(deviceManagementService));
            _serialPortConnectionService = serialPortConnectionService ??
                                    throw new ArgumentNullException(nameof(serialPortConnectionService));

            _deviceManagementService.ConnectionStatusChange += DeviceManagementServiceOnConnectionStatusChange;
        }

        private void DeviceManagementServiceOnConnectionStatusChange(object? sender, bool isConnected)
        {
            IsConnected = isConnected;
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
        }

        [ObservableProperty] private bool _isConnected;

        [ObservableProperty] private bool _isDiscovered;

        [ObservableProperty] private bool _isDiscovering;

        [ObservableProperty] private bool _isReadyToDiscover;

        [ObservableProperty] private string _statusText = string.Empty;

        [ObservableProperty] private string _nakText = string.Empty;

        [ObservableProperty] private StatusLevel _statusLevel = StatusLevel.None;

        [ObservableProperty] private ObservableCollection<AvailableSerialPort> _availableSerialPorts = [];

        [ObservableProperty] private AvailableSerialPort? _selectedSerialPort;

        [ObservableProperty] private IReadOnlyList<uint> _availableBaudRates = [9600, 19200, 38400, 57600, 115200, 230400];

        [ObservableProperty] private uint _selectedBaudRate = 9600;

        [ObservableProperty] private byte _address;

        [RelayCommand]
        private async Task ScanSerialPorts()
        {
            IsDiscovered = false;
            IsDiscovering = true;

            await _deviceManagementService.Shutdown();

            //IdentityLookup = new IdentityLookup(null);
            //CapabilitiesLookup = new CapabilitiesLookup(null);

            StatusText = string.Empty;
            NakText = string.Empty;

            AvailableSerialPorts.Clear();

            var foundAvailableSerialPorts = await _serialPortConnectionService.FindAvailableSerialPorts();

            bool anyFound = false;
            foreach (var found in foundAvailableSerialPorts)
            {
                anyFound = true;
                AvailableSerialPorts.Add(found);
            }

            if (anyFound)
            {
                SelectedSerialPort = AvailableSerialPorts.First();
                IsReadyToDiscover = true;
            }
            else
            {
                await _dialogService.ShowMessageDialogAsync("Error", "No serial ports are available.  Make sure that required drivers are installed.");
                IsReadyToDiscover = false;
            }

            IsDiscovering = false;
        }

        [RelayCommand]
        private async Task DiscoverDevice()
        {
            IsReadyToDiscover = false;
            IsDiscovering = true;
            IsDiscovered = false;

            //IdentityLookup = new IdentityLookup(null);
            //CapabilitiesLookup = new CapabilitiesLookup(null);

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
                        StatusText =
                            $"Attempting to determine device at {current.Connection.BaudRate} with address {current.Address}";
                        break;
                    case DiscoveryStatus.DeviceIdentified:
                        StatusText =
                            $"Attempting to identify device at {current.Connection.BaudRate} with address {current.Address}";
                        break;
                    case DiscoveryStatus.CapabilitiesDiscovered:
                        StatusText =
                            $"Attempting to get capabilities of device at {current.Connection.BaudRate} with address {current.Address}";
                        break;
                    case DiscoveryStatus.Succeeded:
                        IsConnected = false;
                        IsDiscovered = true;
                        SelectedBaudRate = (uint)current.Connection.BaudRate;
                        Address = current.Address;
                        _serialPortConnectionService = current.Connection as ISerialPortConnectionService;
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

            var connections = _serialPortConnectionService.GetConnectionsForDiscovery(SelectedSerialPort.Name);
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
                //IdentityLookup = _deviceManagementService.IdentityLookup;
                //CapabilitiesLookup = _deviceManagementService.CapabilitiesLookup;

                /* if (CapabilitiesLookup?.SecureChannel ?? false)
                 {
                     SecureChannelStatusText = _deviceManagementService.UsesDefaultSecurityKey
                         ? "Default key is set"
                         : "*** A non-default key is set, a reset is required to perform actions. ***";
                 }
                 else
                 {
                     SecureChannelStatusText = string.Empty;
                 }*/
            }

            IsReadyToDiscover = true;
            IsDiscovering = false;
        }
    }

    public enum StatusLevel
    {
        None,
        Processing,
        Error
    }
}
