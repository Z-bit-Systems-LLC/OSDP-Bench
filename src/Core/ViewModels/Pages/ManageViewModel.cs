using System.Collections.ObjectModel;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using OSDP.Net.Tracing;
using OSDPBench.Core.Actions;
using OSDPBench.Core.Models;
using OSDPBench.Core.Services;

namespace OSDPBench.Core.ViewModels.Pages;

/// <summary>
/// Represents the view model for managing data related to a specific page in the application.
/// This class is responsible for handling the logic and interaction between the page and the services
/// used in the application, such as dialog services and device management services.
/// It initializes and subscribes to events that manage connection statuses, device lookups,
/// and external device input data such as card reads and keypad inputs.
/// </summary>
public partial class ManageViewModel : ObservableObject
{
    private readonly IDialogService _dialogService;
    private readonly IDeviceManagementService _deviceManagementService;
    private readonly ISerialPortConnectionService _serialPortConnectionService;
    private readonly PacketTraceEntryBuilder _traceEntryBuilder = new();

    private PacketTraceEntry? _lastPacketEntry;
    private byte[]? _lastConfiguredSecurityKey;
    private bool _securityKeyConfigured;

    /// <inheritdoc />
    public ManageViewModel(IDialogService dialogService, IDeviceManagementService deviceManagementService, ISerialPortConnectionService serialPortConnectionService)
    {
        _dialogService = dialogService ??
                         throw new ArgumentNullException(nameof(dialogService));
        _deviceManagementService = deviceManagementService ??
                                   throw new ArgumentNullException(nameof(deviceManagementService));
        _serialPortConnectionService = serialPortConnectionService ??
                                       throw new ArgumentNullException(nameof(serialPortConnectionService));

        LastCardNumberRead = string.Empty;
        KeypadReadData = string.Empty;

        UpdateFields();
        StatusLevel = _deviceManagementService.IsConnected ? StatusLevel.Connected : StatusLevel.Disconnected;

        _deviceManagementService.ConnectionStatusChange += DeviceManagementServiceOnConnectionStatusChange;
        _deviceManagementService.CardReadReceived += DeviceManagementServiceOnCardReadReceived;
        _deviceManagementService.KeypadReadReceived += DeviceManagementServiceOnKeypadReadReceived;
        _deviceManagementService.DeviceLookupsChanged += DeviceManagementServiceOnDeviceLookupsChanged;
        _deviceManagementService.TraceEntryReceived += OnDeviceManagementServiceOnTraceEntryReceived;
    }

    [RelayCommand]
    private async Task ExecuteDeviceAction()
    {
        if (SelectedDeviceAction == null) return;

        await ExceptionHelper.ExecuteSafelyAsync(_dialogService, Resources.Resources.GetString("Dialog_PerformingAction_Title"), async () =>
        {
            if (SelectedDeviceAction is ResetCypressDeviceAction)
            {
                await HandleResetCypressDeviceAction();
                return;
            }

            var result = await ExecuteSelectedDeviceAction();
            if (result != null && SelectedDeviceAction is SetCommunicationAction)
            {
                await ExceptionHelper.ExecuteSafelyAsync(
                    _dialogService,
                    Resources.Resources.GetString("Dialog_UpdateCommunications_Title"),
                    async () =>
                    {
                        await HandleSetCommunicationAction(result);
                        return true;
                    },
                    false);
            }
        });
    }

    private async Task<object?> ExecuteSelectedDeviceAction()
    {
        return await ExceptionHelper.ExecuteSafelyAsync(
            _dialogService,
            Resources.Resources.GetString("Dialog_PerformingAction_Title"), 
            async () => await _deviceManagementService.ExecuteDeviceAction(SelectedDeviceAction!, DeviceActionParameter),
            null);
    }

    private async Task HandleSetCommunicationAction(object result)
    {
        if (result is not CommunicationParameters connectionParameters) return;

        bool parametersChanged =
            _deviceManagementService.BaudRate != connectionParameters.BaudRate ||
            _deviceManagementService.Address != connectionParameters.Address;

        if (!parametersChanged)
        {
            await _dialogService.ShowMessageDialog(Resources.Resources.GetString("Dialog_UpdateCommunications_Title"),
                Resources.Resources.GetString("Dialog_UpdateCommunications_NoChange"), MessageIcon.Warning);
            return;
        }

        // Reconnect with new settings before showing success
        if (_deviceManagementService.PortName != null)
        {
            // Use ReconnectAfterCommunicationChange to avoid waiting for device to go offline on old connection
            // This method will wait for the connection to fully establish before returning
            await _deviceManagementService.ReconnectAfterCommunicationChange(
                _serialPortConnectionService.GetConnection(
                    _deviceManagementService.PortName,
                    (int)connectionParameters.BaudRate),
                connectionParameters.Address);
        }

        await _dialogService.ShowMessageDialog(Resources.Resources.GetString("Dialog_UpdateCommunications_Title"),
            Resources.Resources.GetString("Dialog_UpdateCommunications_Success"), MessageIcon.Information);
    }

    private async Task HandleResetCypressDeviceAction()
    {
        if (IdentityLookup == null) return;

        if (!IdentityLookup.CanSendResetCommand)
        {
            await _dialogService.ShowMessageDialog(
                Resources.Resources.GetString("Dialog_ResetDevice_Title"), 
                IdentityLookup.ResetInstructions,
                MessageIcon.Information);
            return;
        }

        await _deviceManagementService.Shutdown();
        
        bool userConfirmed = await _dialogService.ShowConfirmationDialog(
            Resources.Resources.GetString("Dialog_ResetDevice_Title"),
            Resources.Resources.GetString("Dialog_ResetDevice_Confirmation"),
            MessageIcon.Warning);
            
        if (!userConfirmed)
        {
            if (_deviceManagementService.PortName != null)
            {    
                await _deviceManagementService.Reconnect(_serialPortConnectionService.GetConnection(
                        _deviceManagementService.PortName,
                        (int)_deviceManagementService.BaudRate),
                    _deviceManagementService.Address);
            }
            return;
        }

        bool success = await ExceptionHelper.ExecuteSafelyAsync(_dialogService, Resources.Resources.GetString("Dialog_ResetDevice_Title"), async () =>
        {
            if (_deviceManagementService.PortName != null)
            {
                await _deviceManagementService.ExecuteDeviceAction(
                    SelectedDeviceAction!,
                    _serialPortConnectionService.GetConnection(
                        _deviceManagementService.PortName,
                        (int)_deviceManagementService.BaudRate));
            }
        });
        
        if (success)
        {
            await _dialogService.ShowMessageDialog(
                Resources.Resources.GetString("Dialog_ResetDevice_Title"),
                Resources.Resources.GetString("Dialog_ResetDevice_Success"),
                MessageIcon.Information);
        }
        else
        {
            await _dialogService.ShowMessageDialog(
                Resources.Resources.GetString("Dialog_ResetDevice_Title"),
                Resources.Resources.GetString("Dialog_ResetDevice_Failed"),
                MessageIcon.Error);
        }
    }

    [ObservableProperty] private IReadOnlyList<int> _availableBaudRates =
        [9600, 19200, 38400, 57600, 115200, 230400];

    [ObservableProperty] private string _lastCardNumberRead;
        
    [ObservableProperty] private string _keypadReadData;

    private void DeviceManagementServiceOnDeviceLookupsChanged(object? sender, EventArgs eventArgs)
    {
        UpdateFields();
    }
        
    private void DeviceManagementServiceOnCardReadReceived(object? sender, string cardNumber)
    {
        LastCardNumberRead = cardNumber;
        CardReadEntries.Insert(0, new CardReadEntry 
        { 
            Timestamp = DateTime.Now,
            CardNumber = cardNumber 
        });
            
        while (CardReadEntries.Count > 5)
        {
            CardReadEntries.RemoveAt(CardReadEntries.Count - 1);
        }
    }
        
    private void DeviceManagementServiceOnKeypadReadReceived(object? sender, string keypadReadData)
    {
        KeypadReadData += keypadReadData;
    }
    
    private void OnDeviceManagementServiceOnTraceEntryReceived(object? sender, TraceEntry traceEntry)
    {
        // Configure security key on first trace entry or when key changes
        // This must happen before processing any packets so MessageSpy can track secure channel state
        EnsureSecurityKeyConfigured();

        // Update activity indicators based on raw trace entry direction (works for encrypted packets too)
        switch (traceEntry.Direction)
        {
            // Flash the appropriate LED based on a direction
            case TraceDirection.Output:
                LastTxActiveTime = DateTime.Now;
                break;
            case TraceDirection.Input or TraceDirection.Trace:
                LastRxActiveTime = DateTime.Now;
                break;
        }

        PacketTraceEntry? packetTraceEntry;
        try
        {
            packetTraceEntry = _traceEntryBuilder.FromTraceEntry(traceEntry, _lastPacketEntry).Build();
        }
        catch (Exception)
        {
            return;
        }

        // Build() can return null if packet parsing failed
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

    private void UpdateFields()
    {
        IdentityLookup = _deviceManagementService.IdentityLookup;
        ConnectedPortName = _deviceManagementService.PortName;
        ConnectedAddress = _deviceManagementService.Address;
        ConnectedBaudRate = _deviceManagementService.BaudRate;
    }

    private void DeviceManagementServiceOnConnectionStatusChange(object? sender, ConnectionStatus connectionStatus)
    {
        switch (connectionStatus)
        {
            case ConnectionStatus.Disconnected:
                StatusLevel = StatusLevel.Disconnected;
                break;
            case ConnectionStatus.Connected:
                StatusLevel = StatusLevel.Connected;
                break;
            case ConnectionStatus.InvalidSecurityKey:
                StatusLevel = StatusLevel.Error;
                break;
            default:
                StatusLevel = StatusLevel.Disconnected;
                break;
        }
    }

    [ObservableProperty] private string? _connectedPortName;

    [ObservableProperty] private byte _connectedAddress;

    [ObservableProperty] private uint _connectedBaudRate;

    [ObservableProperty] private IdentityLookup? _identityLookup;

    [ObservableProperty] private StatusLevel _statusLevel = StatusLevel.Disconnected;

    [ObservableProperty] private ObservableCollection<IDeviceAction> _availableDeviceActions =
    [
        new ControlBuzzerAction(),
        new FileTransferAction(),
        new MonitoringAction(MonitoringType.CardReads),
        new MonitoringAction(MonitoringType.KeypadReads),
        new ResetCypressDeviceAction(), 
        new SetCommunicationAction(),
        new SetReaderLedAction()
    ];
        
    [ObservableProperty] private ObservableCollection<CardReadEntry> _cardReadEntries = [];

    [ObservableProperty] private IDeviceAction? _selectedDeviceAction;

    [ObservableProperty] private object? _deviceActionParameter;
    
    [ObservableProperty] private DateTime _lastTxActiveTime;
    
    [ObservableProperty] private DateTime _lastRxActiveTime;
}