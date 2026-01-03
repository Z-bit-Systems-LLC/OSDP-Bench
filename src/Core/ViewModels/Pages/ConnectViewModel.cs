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
    private readonly PacketTraceEntryBuilder _traceEntryBuilder = new();
    private PacketTraceEntry? _lastPacketEntry;
    private byte[]? _lastConfiguredSecurityKey;
    private bool _securityKeyConfigured;
    private bool _isDisposed;
    private Timer? _usbStatusTimer;
    private readonly TaskCompletionSource<bool> _initializationComplete = new();

    /// <summary>
    /// Gets a task that completes when the initial serial port scan is finished.
    /// </summary>
    public Task InitializationComplete => _initializationComplete.Task;

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

        // Initialize connection types with localized strings
        UpdateConnectionTypes();

        // Subscribe to culture changes to update localized strings
        Resources.Resources.PropertyChanged += OnResourcesPropertyChanged;

        // Perform an initial port scan
        Task.Run(async () => await InitializeSerialPorts());
    }

    private void OnResourcesPropertyChanged(object? sender, System.ComponentModel.PropertyChangedEventArgs e)
    {
        // When culture changes, update the connection types with new localized strings
        UpdateConnectionTypes();
    }

    private void UpdateConnectionTypes()
    {
        int previousSelectedIndex = SelectedConnectionTypeIndex;

        ConnectionTypes.Clear();
        ConnectionTypes.Add(Resources.Resources.GetString("ConnectionType_Discover"));
        ConnectionTypes.Add(Resources.Resources.GetString("ConnectionType_Manual"));
        ConnectionTypes.Add(Resources.Resources.GetString("ConnectionType_PassiveMonitor"));

        // Restore selection after update
        SelectedConnectionTypeIndex = previousSelectedIndex;
    }

    private void OnDeviceManagementServiceOnTraceEntryReceived(object? sender, TraceEntry traceEntry)
    {
        UsingSecureChannel = _deviceManagementService.IsUsingSecureChannel;
        UsesDefaultSecurityKey = _deviceManagementService.UsesDefaultSecurityKey;

        // Configure security key on first trace entry or when key changes
        // This must happen before processing any packets so MessageSpy can track secure channel state
        EnsureSecurityKeyConfigured();

        // Update activity indicators based on a raw trace entry direction (works for encrypted packets too)
        UpdateActivityIndicators(traceEntry.Direction);

        PacketTraceEntry? packetTraceEntry = BuildPacketTraceEntry(traceEntry);
        if (packetTraceEntry == null) return;

        _lastPacketEntry = packetTraceEntry;
    }

    private void EnsureSecurityKeyConfigured()
    {
        var currentKey = _deviceManagementService.SecurityKey;
        if (!_securityKeyConfigured || !SecurityKeysEqual(_lastConfiguredSecurityKey, currentKey))
        {
            _traceEntryBuilder.WithSecurityKey(currentKey);
            _lastConfiguredSecurityKey = currentKey;
            _securityKeyConfigured = true;
        }
    }

    private static bool SecurityKeysEqual(byte[]? key1, byte[]? key2)
    {
        if (key1 == null && key2 == null) return true;
        if (key1 == null || key2 == null) return false;
        if (key1.Length != key2.Length) return false;
        return key1.AsSpan().SequenceEqual(key2);
    }

    private PacketTraceEntry? BuildPacketTraceEntry(TraceEntry traceEntry)
    {
        try
        {
            return _traceEntryBuilder.FromTraceEntry(traceEntry, _lastPacketEntry).Build();
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
            StatusText = Resources.Resources.GetString("Status_Connected");
            NakText = string.Empty;
            StatusLevel = StatusLevel.Connected;

            // Sync communication settings from service to keep UI in sync
            ConnectedAddress = _deviceManagementService.Address;
            ConnectedBaudRate = (int)_deviceManagementService.BaudRate;
            UseSecureChannel = _deviceManagementService.IsUsingSecureChannel;
            UseDefaultKey = _deviceManagementService.UsesDefaultSecurityKey;
        }
        else if (connectionStatus == ConnectionStatus.PassiveMonitoring)
        {
            StatusText = Resources.Resources.GetString("Status_PassiveMonitoring");
            NakText = string.Empty;
            StatusLevel = StatusLevel.PassiveMonitoring;
            ConnectedBaudRate = (int)_deviceManagementService.BaudRate;
        }
        else if (StatusLevel == StatusLevel.Discovered)
        {
            StatusText = Resources.Resources.GetString("Status_AttemptingToConnect");
            StatusLevel = StatusLevel.Connecting;
        }
        else if (connectionStatus == ConnectionStatus.InvalidSecurityKey)
        {
            StatusText = Resources.Resources.GetString("Status_InvalidSecurityKey");
            StatusLevel = StatusLevel.Error;
        }
        else
        {
            StatusText = Resources.Resources.GetString("Status_Disconnected");
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

    /// <summary>
    /// Called when UseDefaultKey property changes. In passive mode, prevents unchecking
    /// unless a custom key is provided.
    /// </summary>
    partial void OnUseDefaultKeyChanged(bool value)
    {
        // In passive mode, don't allow unchecking default key without a custom key
        if (!value && IsPassiveMode && string.IsNullOrWhiteSpace(SecurityKey))
        {
            UseDefaultKey = true;
        }
    }

    /// <summary>
    /// Called when SecurityKey property changes. In passive mode, if a custom key is cleared,
    /// automatically re-enable the default key.
    /// </summary>
    partial void OnSecurityKeyChanged(string value)
    {
        // In passive mode, if custom key is cleared, re-enable default key
        if (IsPassiveMode && string.IsNullOrWhiteSpace(value) && !UseDefaultKey)
        {
            UseDefaultKey = true;
        }
    }
    
    [ObservableProperty] private DateTime _lastTxActiveTime;

    [ObservableProperty] private DateTime _lastRxActiveTime;

    [ObservableProperty] private bool _usingSecureChannel;

    [ObservableProperty] private bool _usesDefaultSecurityKey;

    [ObservableProperty] private string _usbStatusText = string.Empty;

    /// <summary>
    /// Gets the available connection types (localized).
    /// </summary>
    [ObservableProperty] private ObservableCollection<string> _connectionTypes = [];

    /// <summary>
    /// Gets or sets the selected connection type index (0 = Discovery, 1 = Manual, 2 = Passive Monitor).
    /// </summary>
    [ObservableProperty] private int _selectedConnectionTypeIndex;

    /// <summary>
    /// Gets a value indicating whether Passive Monitor mode is selected.
    /// </summary>
    public bool IsPassiveMode => SelectedConnectionTypeIndex == 2;

    /// <summary>
    /// Gets a value indicating whether the Connect button should be visible.
    /// </summary>
    public bool ConnectVisible => CalculateConnectVisibility();

    /// <summary>
    /// Gets a value indicating whether the Disconnect button should be visible.
    /// </summary>
    public bool DisconnectVisible => CalculateDisconnectVisibility();

    /// <summary>
    /// Gets a value indicating whether the Start Discovery button should be visible.
    /// </summary>
    public bool StartDiscoveryVisible => CalculateStartDiscoveryVisibility();

    /// <summary>
    /// Gets a value indicating whether the Cancel Discovery button should be visible.
    /// </summary>
    public bool CancelDiscoveryVisible => CalculateCancelDiscoveryVisibility();

    /// <summary>
    /// Gets a value indicating whether the Start Passive Monitoring button should be visible.
    /// </summary>
    public bool StartPassiveMonitoringVisible => CalculateStartPassiveMonitoringVisibility();

    /// <summary>
    /// Gets a value indicating whether the Stop Passive Monitoring button should be visible.
    /// </summary>
    public bool StopPassiveMonitoringVisible => CalculateStopPassiveMonitoringVisibility();

    /// <summary>
    /// Gets a value indicating whether the ConnectionTypeComboBox should be enabled.
    /// </summary>
    public bool IsConnectionTypeEnabled => CalculateConnectionTypeEnabled();

    // Partial methods to notify when properties change
    partial void OnStatusLevelChanged(StatusLevel value)
    {
        _ = value; // Intentionally unused - only triggering dependent property notification
        NotifyButtonVisibilityChanged();
    }

    partial void OnSelectedConnectionTypeIndexChanged(int value)
    {
        _ = value; // Intentionally unused - only triggering dependent property notification
        NotifyButtonVisibilityChanged();
    }

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
            
            _initializationComplete.SetResult(true);
        }
        catch (Exception ex)
        {
            Console.WriteLine(Resources.Resources.GetString("Error_InitializingSerialPorts").Replace("{0}", ex.Message));
            StatusLevel = StatusLevel.NotReady;
            _initializationComplete.SetException(ex);
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
                StatusText = Resources.Resources.GetString("Status_AttemptingToDiscover");
                break;
                
            case DiscoveryStatus.LookingForDeviceOnConnection:
                StatusText = Resources.Resources.GetString("Status_AttemptingToDiscoverAtBaudRate").Replace("{0}", current.Connection.BaudRate.ToString());
                break;
                
            case DiscoveryStatus.ConnectionWithDeviceFound:
                StatusText = Resources.Resources.GetString("Status_FoundDeviceAtBaudRate").Replace("{0}", current.Connection.BaudRate.ToString());
                break;
                
            case DiscoveryStatus.LookingForDeviceAtAddress:
                StatusText = Resources.Resources.GetString("Status_AttemptingToDetermineDevice").Replace("{0}", current.Connection.BaudRate.ToString()).Replace("{1}", current.Address.ToString());
                break;
                
            case DiscoveryStatus.DeviceIdentified:
                StatusText = Resources.Resources.GetString("Status_AttemptingToIdentifyDevice").Replace("{0}", current.Connection.BaudRate.ToString()).Replace("{1}", current.Address.ToString());
                break;
                
            case DiscoveryStatus.CapabilitiesDiscovered:
                StatusText = Resources.Resources.GetString("Status_AttemptingToGetCapabilities").Replace("{0}", current.Connection.BaudRate.ToString()).Replace("{1}", current.Address.ToString());
                break;
                
            case DiscoveryStatus.Succeeded:
                HandleSuccessfulDiscovery(current);
                break;
                
            case DiscoveryStatus.DeviceNotFound:
                StatusText = Resources.Resources.GetString("Status_FailedToConnect");
                StatusLevel = StatusLevel.Disconnected;
                break;

            case DiscoveryStatus.Error:
                StatusText = Resources.Resources.GetString("Status_ErrorWhileDiscovering");
                StatusLevel = StatusLevel.Disconnected;
                break;

            case DiscoveryStatus.Cancelled:
                StatusLevel = StatusLevel.Disconnected;
                StatusText = Resources.Resources.GetString("Status_CancelledDiscovery");
                break;
                
            default:
                throw new ArgumentOutOfRangeException();
        }
    }

    private void HandleSuccessfulDiscovery(DiscoveryResult result)
    {
        StatusText = Resources.Resources.GetString("Status_SuccessfullyDiscovered").Replace("{0}", result.Connection.BaudRate.ToString()).Replace("{1}", result.Address.ToString());
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
        StatusText = Resources.Resources.GetString("Status_AttemptingToConnectManually");

        byte[]? securityKey = await GetSecurityKey();
        if (securityKey == null && !UseDefaultKey) return;

        await EstablishConnection(serialPortName, securityKey);
    }

    private async Task<byte[]?> GetSecurityKey()
    {
        // If secure channel is not being used, no key validation is needed
        if (!UseSecureChannel) return null;

        if (UseDefaultKey) return null;

        try
        {
            return HexConverter.FromHexString(SecurityKey, 32);
        }
        catch (Exception exception)
        {
            await _dialogService.ShowMessageDialog(
                Resources.Resources.GetString("Dialog_Connect_Title"),
                Resources.Resources.GetString("Dialog_InvalidSecurityKeyMessage").Replace("{0}", exception.Message),
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
        StatusText = Resources.Resources.GetString("Status_Disconnected");
        StatusLevel = StatusLevel.Disconnected;
        NakText = string.Empty;
        _lastPacketEntry = null;
        LastTxActiveTime = DateTime.MinValue;
        LastRxActiveTime = DateTime.MinValue;
    }

    [RelayCommand]
    private async Task StartPassiveMonitoring()
    {
        if (!ValidateSerialPort()) return;

        string serialPortName = SelectedSerialPort?.Name ?? string.Empty;
        StatusLevel = StatusLevel.PassiveMonitoring;
        StatusText = Resources.Resources.GetString("Status_PassiveMonitoring");

        // For passive monitoring, always decrypt:
        // - UseDefaultKey checked → decrypt with default key
        // - UseDefaultKey unchecked → must have custom key (enforced by OnUseDefaultKeyChanged)
        byte[]? securityKey = null;

        if (!UseDefaultKey)
        {
            // Use custom key for decryption
            securityKey = await GetSecurityKeyForPassiveMonitoring();
            if (securityKey == null) return; // Invalid key format
        }

        var connection = _serialPortConnectionService.GetConnection(serialPortName, SelectedBaudRate);
        await _deviceManagementService.StartPassiveMonitoring(connection, true, UseDefaultKey, securityKey);

        ConnectedBaudRate = SelectedBaudRate;
    }

    private async Task<byte[]?> GetSecurityKeyForPassiveMonitoring()
    {
        try
        {
            return HexConverter.FromHexString(SecurityKey, 32);
        }
        catch (Exception exception)
        {
            await _dialogService.ShowMessageDialog(
                Resources.Resources.GetString("Dialog_Connect_Title"),
                Resources.Resources.GetString("Dialog_InvalidSecurityKeyMessage").Replace("{0}", exception.Message),
                MessageIcon.Error);
            StatusLevel = StatusLevel.Ready;
            return null;
        }
    }

    [RelayCommand]
    private async Task StopPassiveMonitoring()
    {
        await _deviceManagementService.StopPassiveMonitoring();
        StatusText = Resources.Resources.GetString("Status_Disconnected");
        StatusLevel = StatusLevel.Disconnected;
        NakText = string.Empty;
        _lastPacketEntry = null;
        LastTxActiveTime = DateTime.MinValue;
        LastRxActiveTime = DateTime.MinValue;
    }

    private async void OnUsbDeviceChanged(object? sender, UsbDeviceChangedEventArgs eventArgs)
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
            
            if (AvailableSerialPorts.Count > 0)
            {
                // Try to reselect the previously selected port if it still exists
                var previousPort = AvailableSerialPorts.FirstOrDefault(p => p.Name == currentlySelectedPort);
                SelectedSerialPort = previousPort ??
                                     // Select the first available port
                                     AvailableSerialPorts.First();
                
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

            switch (eventArgs.ChangeType)
            {
                // Show notification based on the change type
                case UsbDeviceChangeType.Connected:
                    UsbStatusText = Resources.Resources.GetString("USB_DeviceConnected");
                    break;
                case UsbDeviceChangeType.Disconnected:
                {
                    UsbStatusText = Resources.Resources.GetString("USB_DeviceDisconnected");
                
                    // If we were connected and the device was removed, update status
                    if (StatusLevel == StatusLevel.Connected && !eventArgs.AvailablePorts.Contains(_deviceManagementService.PortName ?? ""))
                    {
                        await _deviceManagementService.Shutdown();
                        StatusLevel = StatusLevel.Disconnected;
                        StatusText = Resources.Resources.GetString("Status_DeviceDisconnectedUSBRemoved");
                    }

                    break;
                }
            }
            
            // Clear USB status after 3 seconds
            _usbStatusTimer?.Dispose();
            _usbStatusTimer = new Timer(_ => UsbStatusText = string.Empty, null, TimeSpan.FromSeconds(3), Timeout.InfiniteTimeSpan);
        }
        catch (Exception ex)
        {
            Console.WriteLine(Resources.Resources.GetString("Error_HandlingUSBDeviceChange").Replace("{0}", ex.Message));
        }
    }
    
    /// <inheritdoc />
    public void Dispose()
    {
        Dispose(true);
        GC.SuppressFinalize(this);
    }
    
    #region Button Visibility Calculation Methods

    private bool CalculateConnectVisibility()
    {
        // Show the Connect button when Manual mode is selected and not connected/connecting
        // Bug #3 Fix: Include Connecting and ConnectingManually in the connected check
        bool isConnected = StatusLevel == StatusLevel.Connected ||
                          StatusLevel == StatusLevel.Error ||
                          StatusLevel == StatusLevel.Connecting ||
                          StatusLevel == StatusLevel.ConnectingManually;

        return SelectedConnectionTypeIndex == 1 && !isConnected;
    }

    private bool CalculateDisconnectVisibility()
    {
        // Show the Disconnect button when connected or connecting
        // Bug #2 Fix: Don't show Disconnect for cancelled discovery (Error state when not actually connected)
        // Bug #3 Fix: Include Connecting and ConnectingManually states
        // Include Discovered state so user can cancel before auto-connection
        // Include Error state for invalid security key errors where user needs to disconnect
        bool isConnectedOrConnecting = StatusLevel == StatusLevel.Connected ||
                                       StatusLevel == StatusLevel.Connecting ||
                                       StatusLevel == StatusLevel.ConnectingManually ||
                                       StatusLevel == StatusLevel.Discovered ||
                                       StatusLevel == StatusLevel.Error;

        return isConnectedOrConnecting;
    }

    private bool CalculateStartDiscoveryVisibility()
    {
        // Show Start Discovery button when in Discovery mode and not discovering, discovered, connected, or connecting
        // Bug #4 Fix: Include Connecting state check
        bool isConnectedOrConnecting = StatusLevel == StatusLevel.Connected ||
                                       StatusLevel == StatusLevel.Error ||
                                       StatusLevel == StatusLevel.Connecting ||
                                       StatusLevel == StatusLevel.ConnectingManually;

        return SelectedConnectionTypeIndex == 0 &&
               StatusLevel is not StatusLevel.Discovering and not StatusLevel.Discovered &&
               !isConnectedOrConnecting;
    }

    private bool CalculateCancelDiscoveryVisibility()
    {
        // Show Cancel button only when actively discovering
        return SelectedConnectionTypeIndex == 0 && StatusLevel == StatusLevel.Discovering;
    }

    private bool CalculateConnectionTypeEnabled()
    {
        // Bug #1 Fix: Disable ConnectionTypeComboBox during active operations
        return StatusLevel != StatusLevel.Discovering &&
               StatusLevel != StatusLevel.Discovered &&
               StatusLevel != StatusLevel.Connecting &&
               StatusLevel != StatusLevel.ConnectingManually &&
               StatusLevel != StatusLevel.Connected &&
               StatusLevel != StatusLevel.PassiveMonitoring;
    }

    private bool CalculateStartPassiveMonitoringVisibility()
    {
        // Show Start Passive Monitoring when in Passive mode and not actively monitoring
        return SelectedConnectionTypeIndex == 2 && StatusLevel != StatusLevel.PassiveMonitoring;
    }

    private bool CalculateStopPassiveMonitoringVisibility()
    {
        // Show Stop Passive Monitoring when actively monitoring
        return StatusLevel == StatusLevel.PassiveMonitoring;
    }

    /// <summary>
    /// Notifies UI that button visibility properties may have changed.
    /// Should be called whenever StatusLevel or SelectedConnectionTypeIndex changes.
    /// </summary>
    private void NotifyButtonVisibilityChanged()
    {
        OnPropertyChanged(nameof(ConnectVisible));
        OnPropertyChanged(nameof(DisconnectVisible));
        OnPropertyChanged(nameof(StartDiscoveryVisible));
        OnPropertyChanged(nameof(CancelDiscoveryVisible));
        OnPropertyChanged(nameof(StartPassiveMonitoringVisible));
        OnPropertyChanged(nameof(StopPassiveMonitoringVisible));
        OnPropertyChanged(nameof(IsConnectionTypeEnabled));
        OnPropertyChanged(nameof(IsPassiveMode));
    }

    #endregion

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
            Resources.Resources.PropertyChanged -= OnResourcesPropertyChanged;

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
    ConnectingManually,
    PassiveMonitoring
#pragma warning restore CS1591 // Missing XML comment for publicly visible type or member
}