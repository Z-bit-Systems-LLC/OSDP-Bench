using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using OSDP.Net.PanelCommands.DeviceDiscover;
using System.Collections.ObjectModel;
using OSDP.Net.Tracing;
using OSDPBench.Core.Models;
using OSDPBench.Core.Services;

namespace OSDPBench.Core.ViewModels.Pages;

/// <summary>
/// ViewModel for the Connect page.
/// </summary>
public partial class ConnectViewModel : ObservableObject
{
    // Default baud rates available for connection
    private static readonly IReadOnlyList<int> DefaultBaudRates = [9600, 19200, 38400, 57600, 115200, 230400];
    
    private readonly IDialogService _dialogService;
    private readonly IDeviceManagementService _deviceManagementService;
    
    private ISerialPortConnectionService _serialPortConnectionService;
    private PacketTraceEntry? _lastPacketEntry;

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
        _deviceManagementService.TraceEntryReceived += OnDeviceManagementServiceOnTraceEntryReceived;
    }

    private void OnDeviceManagementServiceOnTraceEntryReceived(object? sender, TraceEntry traceEntry)
    {
        if (_deviceManagementService.IsUsingSecureChannel) return;

        PacketTraceEntry? packetTraceEntry = BuildPacketTraceEntry(traceEntry);
        if (packetTraceEntry == null) return;
        
        UpdateActivityIndicators(packetTraceEntry.Direction);
        
        _lastPacketEntry = packetTraceEntry;
    }

    private PacketTraceEntry? BuildPacketTraceEntry(TraceEntry traceEntry)
    {
        try
        {
            var builder = new PacketTraceEntryBuilder();
            return builder.FromTraceEntry(traceEntry, _lastPacketEntry).Build();
        }
        catch (Exception)
        {
            return null;
        }
    }
    
    private void UpdateActivityIndicators(TraceDirection direction)
    {
        switch (direction)
        {
            case TraceDirection.Output:
                LastTxActiveTime = DateTime.Now;
                break;
            case TraceDirection.Input or TraceDirection.Trace:
                LastRxActiveTime = DateTime.Now;
                break;
        }
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

    [ObservableProperty] private StatusLevel _statusLevel = StatusLevel.NotReady;

    [ObservableProperty] private ObservableCollection<AvailableSerialPort> _availableSerialPorts = [];

    [ObservableProperty] private AvailableSerialPort? _selectedSerialPort;

    [ObservableProperty] private IReadOnlyList<int> _availableBaudRates = DefaultBaudRates;

    [ObservableProperty] private int _selectedBaudRate = DefaultBaudRates[0]; // Default to first baud rate (9600)

    [ObservableProperty] private byte _selectedAddress;

    [ObservableProperty] private byte _connectedAddress;

    [ObservableProperty] private int _connectedBaudRate;

    [ObservableProperty] private bool _useSecureChannel;

    [ObservableProperty] private bool _useDefaultKey = true;

    [ObservableProperty] private string _securityKey = string.Empty;
    
    [ObservableProperty] private DateTime _lastTxActiveTime;
    
    [ObservableProperty] private DateTime _lastRxActiveTime;

    [RelayCommand]
    private async Task ScanSerialPorts()
    {
        // Check if the user wants to proceed when already connected
        if (!await ConfirmScanWhenConnected()) return;

        // Prepare for scanning
        await PrepareForSerialPortScan();

        // Perform the scan and populate the available ports
        bool portsFound = await FindAndPopulateSerialPorts();

        // Update UI based on scan results
        await UpdateUiAfterSerialPortScan(portsFound);
    }

    private async Task<bool> ConfirmScanWhenConnected()
    {
        if (StatusLevel != StatusLevel.Ready && StatusLevel != StatusLevel.NotReady)
        {
            return await _dialogService.ShowConfirmationDialog(
                "Rescan Serial Ports",
                "This will shutdown existing connection to the PD. Are you sure you want to continue?",
                MessageIcon.Warning);
        }
        
        return true;
    }

    private async Task PrepareForSerialPortScan()
    {
        StatusLevel = StatusLevel.NotReady;
        await _deviceManagementService.Shutdown();
        StatusText = string.Empty;
        NakText = string.Empty;
        AvailableSerialPorts.Clear();
    }

    private async Task<bool> FindAndPopulateSerialPorts()
    {
        var foundPorts = await _serialPortConnectionService.FindAvailableSerialPorts();
        bool anyFound = false;
        
        foreach (var port in foundPorts)
        {
            anyFound = true;
            AvailableSerialPorts.Add(port);
        }
        
        return anyFound;
    }

    private async Task UpdateUiAfterSerialPortScan(bool portsFound)
    {
        if (portsFound)
        {
            SelectedSerialPort = AvailableSerialPorts.First();
            StatusLevel = StatusLevel.Ready;
        }
        else
        {
            await _dialogService.ShowMessageDialog("Error",
                "No serial ports are available. Make sure that required drivers are installed.", 
                MessageIcon.Error);
            StatusLevel = StatusLevel.NotReady;
        }
    }

    [RelayCommand(IncludeCancelCommand = true)]
    private async Task DiscoverDevice(CancellationToken token)
    {
        if (!ValidateSerialPort()) return;
        
        StatusLevel = StatusLevel.Discovering;
        NakText = string.Empty;

        var progress = new DiscoveryProgress(UpdateDiscoveryStatus);
        var connections = _serialPortConnectionService.GetConnectionsForDiscovery(
            SelectedSerialPort?.Name ?? string.Empty);

        try
        {
            await _deviceManagementService.DiscoverDevice(connections, progress, token);
        }
        catch
        {
            // Exceptions are handled by the discovery progress
        }
    }

    private bool ValidateSerialPort()
    {
        string serialPortName = SelectedSerialPort?.Name ?? string.Empty;
        if (string.IsNullOrWhiteSpace(serialPortName)) return false;
        
        _deviceManagementService.PortName = serialPortName;
        return true;
    }

    private void UpdateDiscoveryStatus(DiscoveryResult current)
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
                HandleSuccessfulDiscovery(current);
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
    }

    private void HandleSuccessfulDiscovery(DiscoveryResult result)
    {
        StatusText = $"Successfully discovered device {result.Connection.BaudRate} with address {result.Address}";
        StatusLevel = StatusLevel.Discovered;
        
        if (result.Connection is ISerialPortConnectionService service)
        {
            _serialPortConnectionService = service;
        }
        
        ConnectedAddress = result.Address;
        ConnectedBaudRate = result.Connection.BaudRate;
    }

    [RelayCommand]
    private async Task ConnectDevice()
    {
        if (!ValidateSerialPort()) return;
        
        string serialPortName = SelectedSerialPort?.Name ?? string.Empty;
        StatusLevel = StatusLevel.ConnectingManually;
        StatusText = "Attempting to connect manually";

        byte[]? securityKey = await GetSecurityKey();
        if (securityKey == null && !UseDefaultKey) return;

        await EstablishConnection(serialPortName, securityKey);
    }

    private async Task<byte[]?> GetSecurityKey()
    {
        if (UseDefaultKey) return null;
        
        try
        {
            return HexConverter.FromHexString(SecurityKey, 32);
        }
        catch (Exception exception)
        {
            await _dialogService.ShowMessageDialog(
                "Connect", 
                $"Invalid security key entered. {exception.Message}",
                MessageIcon.Error);
            return null;
        }
    }

    private async Task EstablishConnection(string serialPortName, byte[]? securityKey)
    {
        await _deviceManagementService.Shutdown();
        
        await _deviceManagementService.Connect(
            _serialPortConnectionService.GetConnection(serialPortName, SelectedBaudRate), 
            SelectedAddress,
            UseSecureChannel, 
            UseDefaultKey, 
            securityKey);
            
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