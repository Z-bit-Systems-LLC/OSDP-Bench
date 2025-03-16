﻿using System.Collections.ObjectModel;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using OSDP.Net.Connections;
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

    /// <inheritdoc />
    public ManageViewModel(IDialogService dialogService, IDeviceManagementService deviceManagementService)
    {
        _dialogService = dialogService ??
                         throw new ArgumentNullException(nameof(dialogService));
        _deviceManagementService = deviceManagementService ??
                                   throw new ArgumentNullException(nameof(deviceManagementService));

        LastCardNumberRead = string.Empty;
        KeypadReadData = string.Empty;

        UpdateFields();
        StatusLevel = _deviceManagementService.IsConnected ? StatusLevel.Connected : StatusLevel.Disconnected;

        _deviceManagementService.ConnectionStatusChange += DeviceManagementServiceOnConnectionStatusChange;
        _deviceManagementService.CardReadReceived += DeviceManagementServiceOnCardReadReceived;
        _deviceManagementService.KeypadReadReceived += DeviceManagementServiceOnKeypadReadReceived;
        _deviceManagementService.DeviceLookupsChanged += DeviceManagementServiceOnDeviceLookupsChanged;
    }

    [RelayCommand]
    private async Task ExecuteDeviceAction()
    {
        object? result = null;
        if (SelectedDeviceAction != null)
        {
            if (SelectedDeviceAction is ResetCypressDeviceAction && IdentityLookup != null)
            {
                if (!IdentityLookup.CanSendResetCommand)
                {
                    await _dialogService.ShowMessageDialog("Reset Device", IdentityLookup.ResetInstructions,
                        MessageIcon.Information);
                    return;
                }

                await _deviceManagementService.Shutdown();
                if (!await _dialogService.ShowConfirmationDialog("Reset Device",
                        "Do you want to reset device, if so power cycle then click yes when the device boots up.",
                        MessageIcon.Warning))
                {
                    await _deviceManagementService.Connect(new SerialPortOsdpConnection(
                        _deviceManagementService.PortName,
                        (int)_deviceManagementService.BaudRate), _deviceManagementService.Address);
                    return;
                }

                try
                {
                    await _deviceManagementService.ExecuteDeviceAction(SelectedDeviceAction,
                        new SerialPortOsdpConnection(_deviceManagementService.PortName,
                            (int)_deviceManagementService.BaudRate));
                    await _dialogService.ShowMessageDialog("Reset Device",
                        "Successfully sent reset commands. Power cycle device again and then perform a discovery.",
                        MessageIcon.Information);
                }
                catch (Exception exception)
                {
                    await _dialogService.ShowMessageDialog("Reset Device",
                        exception.Message + " Perform a discovery to reconnect to the device.",
                        MessageIcon.Error);
                }

                return;
            }

            try
            {
                result = await _deviceManagementService.ExecuteDeviceAction(SelectedDeviceAction,
                    DeviceActionParameter);
            }
            catch (Exception exception)
            {
                await _dialogService.ShowMessageDialog("Performing Action",
                    $"Issue with performing action. {exception.Message}", MessageIcon.Warning);
                return;
            }
        }

        if (SelectedDeviceAction is SetCommunicationAction)
        {
            if (result is CommunicationParameters connectionParameters)
            {
                if (_deviceManagementService.BaudRate == connectionParameters.BaudRate &&
                    _deviceManagementService.Address == connectionParameters.Address)
                {
                    await _dialogService.ShowMessageDialog("Update Communications",
                        $"Communication parameters didn't change.", MessageIcon.Warning);
                    return;
                }

                await _dialogService.ShowMessageDialog("Update Communications",
                    "Successfully update communications, reconnecting with new settings.", MessageIcon.Information);

                await _deviceManagementService.Shutdown();

                await Task.Delay(TimeSpan.FromSeconds(1));

                await _deviceManagementService.Connect(
                    new SerialPortOsdpConnection(_deviceManagementService.PortName,
                        (int)connectionParameters.BaudRate), connectionParameters.Address);
            }
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
        new MonitorCardReads(), 
        new MonitorKeypadReads(),
        new ResetCypressDeviceAction(), 
        new SetCommunicationAction(),
        new SetReaderLedAction()
    ];
        
    [ObservableProperty] private ObservableCollection<CardReadEntry> _cardReadEntries = [];

    [ObservableProperty] private IDeviceAction? _selectedDeviceAction;

    [ObservableProperty] private object? _deviceActionParameter;
}