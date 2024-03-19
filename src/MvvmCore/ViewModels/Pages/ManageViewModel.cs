using CommunityToolkit.Mvvm.ComponentModel;
using MvvmCore.Models;
using MvvmCore.Services;

namespace MvvmCore.ViewModels.Pages
{
    public partial class ManageViewModel : ObservableObject
    {
        private readonly IDialogService _dialogService;
        private readonly IDeviceManagementService _deviceManagementService;

        public ManageViewModel(IDialogService dialogService, IDeviceManagementService deviceManagementService)
        {
            _dialogService = dialogService ??
                             throw new ArgumentNullException(nameof(dialogService));
            _deviceManagementService = deviceManagementService ??
                                       throw new ArgumentNullException(nameof(deviceManagementService));

            _deviceManagementService.ConnectionStatusChange += DeviceManagementServiceOnConnectionStatusChange;
            _deviceManagementService.DeviceLookupsChanged += DeviceManagementServiceOnDeviceLookupsChanged;
        }

        private void DeviceManagementServiceOnDeviceLookupsChanged(object? sender, EventArgs e)
        {
            IdentityLookup = _deviceManagementService.IdentityLookup;
        }

        private void DeviceManagementServiceOnConnectionStatusChange(object? sender, bool e)
        {
        }

        [ObservableProperty] private IdentityLookup? _identityLookup;
    }
}
