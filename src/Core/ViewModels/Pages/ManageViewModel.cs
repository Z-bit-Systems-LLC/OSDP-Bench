using System.Collections.ObjectModel;
using CommunityToolkit.Mvvm.ComponentModel;
using OSDPBench.Core.Actions;
using OSDPBench.Core.Models;
using OSDPBench.Core.Services;

namespace OSDPBench.Core.ViewModels.Pages
{
    public partial class ManageViewModel : ObservableObject
    {
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

            _deviceManagementService.ConnectionStatusChange += DeviceManagementServiceOnConnectionStatusChange;
            _deviceManagementService.DeviceLookupsChanged += DeviceManagementServiceOnDeviceLookupsChanged;
        }

        private void DeviceManagementServiceOnDeviceLookupsChanged(object? sender, EventArgs e)
        {
            UpdateFields();
        }

        private void UpdateFields()
        {
            IdentityLookup = _deviceManagementService.IdentityLookup;
            ConnectedAddress = _deviceManagementService.Address;
            ConnectedBaudRate = _deviceManagementService.BaudRate;
        }

        private void DeviceManagementServiceOnConnectionStatusChange(object? sender, bool isOnline)
        {
            StatusLevel = isOnline ? StatusLevel.Connected : StatusLevel.Disconnected;
        }
        
        [ObservableProperty] private byte _connectedAddress;

        [ObservableProperty] private uint _connectedBaudRate;

        [ObservableProperty] private IdentityLookup? _identityLookup;

        [ObservableProperty] private StatusLevel _statusLevel = StatusLevel.Disconnected;

        [ObservableProperty] private ObservableCollection<IDeviceAction> _availableActions = [];
    }
}
