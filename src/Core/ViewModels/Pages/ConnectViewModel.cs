using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using OSDP.Net.PanelCommands.DeviceDiscover;
using System.Collections.ObjectModel;
using OSDPBench.Core.Models;
using OSDPBench.Core.Services;

namespace OSDPBench.Core.ViewModels.Pages;

public partial class ConnectViewModel : ObservableObject
{
    private readonly IDialogService _dialogService;
    private readonly IDeviceManagementService _deviceManagementService;
    private ISerialPortConnectionService? _serialPortConnectionService;

    /// <summary>
    /// ViewModel for the Connect page.
    /// </summary>
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
        _deviceManagementService.NakReplyReceived += DeviceManagementServiceOnNakReplyReceived;
    }

    private void DeviceManagementServiceOnConnectionStatusChange(object? sender, ConnectionStatus connectionStatus)
    {
        if (connectionStatus == ConnectionStatus.Connected)
        {
            StatusText = "Connected";
            NakText = string.Empty;
            StatusLevel = StatusLevel.Connected;
        }
        else if (StatusLevel == StatusLevel.Discovered)
        {
            StatusText = "Attempting to connect";
            StatusLevel = StatusLevel.Connecting;
        }
        else if (connectionStatus == ConnectionStatus.InvalidSecurityKey)
        {
            StatusText = "Invalid security key";
            StatusLevel = StatusLevel.Error;
        }
        else
        {
            StatusText = "Disconnected";
            StatusLevel = StatusLevel.Disconnected;
        }
    }

    private void DeviceManagementServiceOnNakReplyReceived(object? sender, string nakMessage)
    {
        NakText = nakMessage;
    }

    /// <summary>
    /// Represents the status text of the connection.
    /// </summary>
    [ObservableProperty] private string _statusText = string.Empty;

    [ObservableProperty] private string _nakText = string.Empty;

    [ObservableProperty] private StatusLevel _statusLevel = StatusLevel.Ready;

    [ObservableProperty] private ObservableCollection<AvailableSerialPort> _availableSerialPorts = [];

    [ObservableProperty] private AvailableSerialPort? _selectedSerialPort;

    [ObservableProperty] private IReadOnlyList<int> _availableBaudRates = [9600, 19200, 38400, 57600, 115200, 230400];

    [ObservableProperty] private int _selectedBaudRate = 9600;

    [ObservableProperty] private byte _selectedAddress;

    [ObservableProperty] private byte _connectedAddress;

    [ObservableProperty] private int _connectedBaudRate;

    [ObservableProperty] private bool _useSecureChannel;

    [ObservableProperty] private bool _useDefaultKey = true;

    [ObservableProperty] private string _securityKey = string.Empty;

    [RelayCommand]
    private async Task ScanSerialPorts()
    {
        if (StatusLevel != StatusLevel.Ready && StatusLevel != StatusLevel.NotReady &&
            !await _dialogService.ShowConfirmationDialog("Rescan Serial Ports",
                "This will shutdown existing connection to the PD. Are you sure you want to continue?",
                MessageIcon.Warning)) return;

        StatusLevel = StatusLevel.NotReady;

        await _deviceManagementService.Shutdown();

        StatusText = string.Empty;
        NakText = string.Empty;

        AvailableSerialPorts.Clear();

        var serialPortConnectionService = _serialPortConnectionService;
        if (serialPortConnectionService == null) return;

        var foundAvailableSerialPorts = await serialPortConnectionService.FindAvailableSerialPorts();

        bool anyFound = false;
        foreach (var found in foundAvailableSerialPorts)
        {
            anyFound = true;
            AvailableSerialPorts.Add(found);
        }

        if (anyFound)
        {
            SelectedSerialPort = AvailableSerialPorts.First();
            StatusLevel = StatusLevel.Ready;
        }
        else
        {
            await _dialogService.ShowMessageDialog("Error",
                "No serial ports are available. Make sure that required drivers are installed.", MessageIcon.Error);
            StatusLevel = StatusLevel.NotReady;
        }
    }

    [RelayCommand(IncludeCancelCommand = true)]
    private async Task DiscoverDevice(CancellationToken token)
    {
        var serialPortConnectionService = _serialPortConnectionService;
        if (serialPortConnectionService == null) return;

        string serialPortName = SelectedSerialPort?.Name ?? string.Empty;
        if (string.IsNullOrWhiteSpace(serialPortName)) return;
        _deviceManagementService.PortName = serialPortName;

        StatusLevel = StatusLevel.Discovering;
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
                    StatusText =
                        $"Successfully discovered device {current.Connection.BaudRate} with address {current.Address}";
                    StatusLevel = StatusLevel.Discovered;
                    _serialPortConnectionService = current.Connection as ISerialPortConnectionService;
                    ConnectedAddress = current.Address;
                    ConnectedBaudRate = current.Connection.BaudRate;
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
                    StatusLevel = StatusLevel.Error;
                    StatusText = "Cancelled discovery";
                    break;
                default:
                    throw new ArgumentOutOfRangeException();
            }
        });

        var connections = serialPortConnectionService.GetConnectionsForDiscovery(serialPortName);

        try
        {
            await _deviceManagementService.DiscoverDevice(connections, progress, token);
        }
        catch
        {
            // ignored
        }

        if (StatusLevel == StatusLevel.Discovered)
        {

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
    }

    [RelayCommand]
    private async Task ConnectDevice()
    {
        var serialPortConnectionService = _serialPortConnectionService;
        if (serialPortConnectionService == null) return;

        string serialPortName = SelectedSerialPort?.Name ?? string.Empty;
        if (string.IsNullOrWhiteSpace(serialPortName)) return;
        _deviceManagementService.PortName = serialPortName;

        StatusLevel = StatusLevel.ConnectingManually;
        StatusText = "Attempting to connect manually";

        byte[]? securityKey = null;

        try
        {
            if (!UseDefaultKey)
            {
                securityKey = HexConverter.FromHexString(SecurityKey, 32);
            }
        }
        catch (Exception exception)
        {
            await _dialogService.ShowMessageDialog("Connect", $"Invalid security key entered. {exception.Message}",
                MessageIcon.Error);
            return;
        }

        await _deviceManagementService.Shutdown();
        await _deviceManagementService.Connect(
            serialPortConnectionService.GetConnection(serialPortName, SelectedBaudRate), SelectedAddress,
            UseSecureChannel, UseDefaultKey, securityKey);
        ConnectedAddress = SelectedAddress;
        ConnectedBaudRate = SelectedBaudRate;
    }
}

/// <summary>
/// Specifies the status level of a connection.
/// </summary>
public enum StatusLevel
{
#pragma warning disable CS1591 // Missing XML comment for publicly visible type or member
    None,
    Connected,
    Connecting,
    NotReady,
    Ready,
    Discovering,
    Discovered,
    Error,
    Disconnected,
    ConnectingManually
#pragma warning restore CS1591 // Missing XML comment for publicly visible type or member

}