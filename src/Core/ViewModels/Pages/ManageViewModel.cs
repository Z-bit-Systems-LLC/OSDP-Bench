using System.Collections.ObjectModel;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using OSDP.Net.Connections;
using OSDPBench.Core.Actions;
using OSDPBench.Core.Models;
using OSDPBench.Core.Services;

namespace OSDPBench.Core.ViewModels.Pages
{
    public partial class ManageViewModel : ObservableObject
    {
        // ReSharper disable once NotAccessedField.Local
        private readonly IDialogService _dialogService;
        private readonly IDeviceManagementService _deviceManagementService;

        /// <summary>
        /// View model for the manage page.
        /// </summary>
        public ManageViewModel(IDialogService dialogService, IDeviceManagementService deviceManagementService)
        {
            _dialogService = dialogService ??
                             throw new ArgumentNullException(nameof(dialogService));
            _deviceManagementService = deviceManagementService ??
                                       throw new ArgumentNullException(nameof(deviceManagementService));

            UpdateFields();
            StatusLevel = _deviceManagementService.IsConnected ? StatusLevel.Connected : StatusLevel.Disconnected;

            _deviceManagementService.ConnectionStatusChange += DeviceManagementServiceOnConnectionStatusChange;
            _deviceManagementService.CardReadReceived += DeviceManagementServiceOnCardReadReceived;
            _deviceManagementService.DeviceLookupsChanged += DeviceManagementServiceOnDeviceLookupsChanged;
        }

        [RelayCommand]
        private async Task ExecuteDeviceAction()
        {
            object? result = null;
            if (SelectedDeviceAction != null)
            {
                try
                {
                    result = await _deviceManagementService.ExecuteDeviceAction(SelectedDeviceAction, DeviceActionParameter);
                }
                catch (Exception exception)
                {
                    await _dialogService.ShowMessageDialog("Performing Action", $"Issue with performing action. {exception.Message}", MessageIcon.Warning);
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
                        await _dialogService.ShowMessageDialog("Update Communications", $"Communication parameters didn't change.", MessageIcon.Warning);
                        return;
                    }
                    
                    await _dialogService.ShowMessageDialog("Update Communications", "Successfully update communications, reconnecting with new settings.", MessageIcon.Information);
                    
                    await _deviceManagementService.Shutdown();

                    await Task.Delay(TimeSpan.FromSeconds(1));

                    await _deviceManagementService.Connect(
                        new SerialPortOsdpConnection(_deviceManagementService.PortName,
                            (int)connectionParameters.BaudRate), connectionParameters.Address);
                }
            }
        }

        [ObservableProperty] private IReadOnlyList<int> _availableBaudRates = [9600, 19200, 38400, 57600, 115200, 230400];

        [ObservableProperty] private string _lastCardNumberRead;

        private void DeviceManagementServiceOnDeviceLookupsChanged(object? sender, EventArgs eventArgs)
        {
            UpdateFields();
        }

        private void DeviceManagementServiceOnCardReadReceived(object? sender, string cardNumber)
        {
            LastCardNumberRead = cardNumber;
        }

        private void UpdateFields()
        {
            IdentityLookup = _deviceManagementService.IdentityLookup;
            ConnectedPortName = _deviceManagementService.PortName;
            ConnectedAddress = _deviceManagementService.Address;
            ConnectedBaudRate = _deviceManagementService.BaudRate;
        }

        private void DeviceManagementServiceOnConnectionStatusChange(object? sender, bool isOnline)
        {
            StatusLevel = isOnline ? StatusLevel.Connected : StatusLevel.Disconnected;
        }

        [ObservableProperty] private string? _connectedPortName;

        [ObservableProperty] private byte _connectedAddress;

        [ObservableProperty] private uint _connectedBaudRate;

        [ObservableProperty] private IdentityLookup? _identityLookup;

        [ObservableProperty] private StatusLevel _statusLevel = StatusLevel.Disconnected;

        [ObservableProperty] private ObservableCollection<IDeviceAction> _availableDeviceActions = [new SetCommunicationAction(), new MonitorCardReads()];

        [ObservableProperty] private IDeviceAction? _selectedDeviceAction;

        [ObservableProperty] private object? _deviceActionParameter;
    }
}
