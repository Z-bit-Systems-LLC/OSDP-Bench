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
            
            IdentityLookup = _deviceManagementService.IdentityLookup;
            ConnectedAddress = _deviceManagementService.Address;
            ConnectedBaudRate = (int)_deviceManagementService.BaudRate;

            _deviceManagementService.ConnectionStatusChange += DeviceManagementServiceOnConnectionStatusChange;
            _deviceManagementService.DeviceLookupsChanged += DeviceManagementServiceOnDeviceLookupsChanged;
        }

        private void DeviceManagementServiceOnDeviceLookupsChanged(object? sender, EventArgs e)
        {
            StatusText = "Device Ready";
        }

        private void DeviceManagementServiceOnConnectionStatusChange(object? sender, bool isOnline)
        {
            StatusText = isOnline ? "Connected" : "Disconnected";
        }
        
        [ObservableProperty] private string _statusText = string.Empty;
        
        [ObservableProperty] private byte _connectedAddress;

        [ObservableProperty] private int _connectedBaudRate;

        [ObservableProperty] private IdentityLookup? _identityLookup;

        [ObservableProperty] private ObservableCollection<IDeviceAction> _availableActions = [];
    }
}
