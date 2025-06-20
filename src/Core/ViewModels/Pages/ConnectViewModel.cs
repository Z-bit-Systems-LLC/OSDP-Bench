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
public partial class ConnectViewModel : ObservableObject, IDisposable
{
    // Default baud rates available for connection
    private static readonly IReadOnlyList<int> DefaultBaudRates = [9600, 19200, 38400, 57600, 115200, 230400];
    
    private readonly IDialogService _dialogService;
    private readonly IDeviceManagementService _deviceManagementService;
    private readonly IUsbDeviceMonitorService? _usbDeviceMonitorService;
    
    private ISerialPortConnectionService _serialPortConnectionService;
    private PacketTraceEntry? _lastPacketEntry;
    private bool _isDisposed;
    private Timer? _usbStatusTimer;

    /// <summary>
    /// ViewModel for the Connect page.
    /// </summary>
    public ConnectViewModel(IDialogService dialogService, IDeviceManagementService deviceManagementService,
        ISerialPortConnectionService serialPortConnectionService, IUsbDeviceMonitorService? usbDeviceMonitorService = null)
    {
        _dialogService = dialogService ??
                         throw new ArgumentNullException(nameof(dialogService));
        _deviceManagementService = deviceManagementService ??
                                   throw new ArgumentNullException(nameof(deviceManagementService));
        _serialPortConnectionService = serialPortConnectionService ??
                                       throw new ArgumentNullException(nameof(serialPortConnectionService));
        _usbDeviceMonitorService = usbDeviceMonitorService;
        
        _deviceManagementService.ConnectionStatusChange += DeviceManagementServiceOnConnectionStatusChange;
        _deviceManagementService.NakReplyReceived += DeviceManagementServiceOnNakReplyReceived;
        _deviceManagementService.TraceEntryReceived += OnDeviceManagementServiceOnTraceEntryReceived;
        
        // Start USB monitoring if available
        if (_usbDeviceMonitorService != null)
        {
            _usbDeviceMonitorService.UsbDeviceChanged += OnUsbDeviceChanged;
            _usbDeviceMonitorService.StartMonitoring();
        }
        
        // Perform initial port scan
        Task.Run(async () => await InitializeSerialPorts());
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
    
    [ObservableProperty] private string _usbStatusText = string.Empty;

    private async Task InitializeSerialPorts()
    {
        try
        {
            var foundPorts = await _serialPortConnectionService.FindAvailableSerialPorts();
            
            foreach (var port in foundPorts)
            {
                AvailableSerialPorts.Add(port);
            }
            
            if (AvailableSerialPorts.Count > 0)
            {
                SelectedSerialPort = AvailableSerialPorts.First();
                StatusLevel = StatusLevel.Ready;
            }
            else
            {
                StatusLevel = StatusLevel.NotReady;
            }
        }
        catch (Exception ex)
        {
            Console.WriteLine($"Error initializing serial ports: {ex.Message}");
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

    [RelayCommand]
    private async Task DisconnectDevice()
    {
        await _deviceManagementService.Shutdown();
        StatusText = "Disconnected";
        StatusLevel = StatusLevel.Disconnected;
        NakText = string.Empty;
        _lastPacketEntry = null;
        LastTxActiveTime = DateTime.MinValue;
        LastRxActiveTime = DateTime.MinValue;
    }
    
    private async void OnUsbDeviceChanged(object? sender, UsbDeviceChangedEventArgs e)
    {
        try
        {
            // Get current port selection
            var currentlySelectedPort = SelectedSerialPort?.Name;
            
            // Clear and repopulate the available ports
            AvailableSerialPorts.Clear();
            
            var availablePorts = await _serialPortConnectionService.FindAvailableSerialPorts();
            foreach (var port in availablePorts)
            {
                AvailableSerialPorts.Add(port);
            }
            
            // Handle port selection based on change type
            if (AvailableSerialPorts.Count > 0)
            {
                // Try to reselect the previously selected port if it still exists
                var previousPort = AvailableSerialPorts.FirstOrDefault(p => p.Name == currentlySelectedPort);
                if (previousPort != null)
                {
                    SelectedSerialPort = previousPort;
                }
                else
                {
                    // Select the first available port
                    SelectedSerialPort = AvailableSerialPorts.First();
                }
                
                if (StatusLevel == StatusLevel.NotReady)
                {
                    StatusLevel = StatusLevel.Ready;
                }
            }
            else
            {
                SelectedSerialPort = null;
                if (StatusLevel == StatusLevel.Ready)
                {
                    StatusLevel = StatusLevel.NotReady;
                }
            }
            
            // Show notification based on change type
            if (e.ChangeType == UsbDeviceChangeType.Connected)
            {
                UsbStatusText = "USB device connected";
            }
            else if (e.ChangeType == UsbDeviceChangeType.Disconnected)
            {
                UsbStatusText = "USB device disconnected";
                
                // If we were connected and the device was removed, update status
                if (StatusLevel == StatusLevel.Connected && !e.AvailablePorts.Contains(_deviceManagementService.PortName ?? ""))
                {
                    await _deviceManagementService.Shutdown();
                    StatusLevel = StatusLevel.Disconnected;
                    StatusText = "Device disconnected - USB removed";
                }
            }
            else
            {
                UsbStatusText = "USB ports changed";
            }
            
            // Clear USB status after 3 seconds
            _usbStatusTimer?.Dispose();
            _usbStatusTimer = new Timer(_ => UsbStatusText = string.Empty, null, TimeSpan.FromSeconds(3), Timeout.InfiniteTimeSpan);
        }
        catch (Exception ex)
        {
            Console.WriteLine($"Error handling USB device change: {ex.Message}");
        }
    }
    
    /// <inheritdoc />
    public void Dispose()
    {
        Dispose(true);
        GC.SuppressFinalize(this);
    }
    
    /// <summary>
    /// Releases unmanaged and - optionally - managed resources.
    /// </summary>
    protected virtual void Dispose(bool disposing)
    {
        if (_isDisposed) return;
        
        if (disposing)
        {
            // Unsubscribe from events
            _deviceManagementService.ConnectionStatusChange -= DeviceManagementServiceOnConnectionStatusChange;
            _deviceManagementService.NakReplyReceived -= DeviceManagementServiceOnNakReplyReceived;
            _deviceManagementService.TraceEntryReceived -= OnDeviceManagementServiceOnTraceEntryReceived;
            
            if (_usbDeviceMonitorService != null)
            {
                _usbDeviceMonitorService.UsbDeviceChanged -= OnUsbDeviceChanged;
                _usbDeviceMonitorService.StopMonitoring();
            }
            
            _usbStatusTimer?.Dispose();
        }
        
        _isDisposed = true;
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