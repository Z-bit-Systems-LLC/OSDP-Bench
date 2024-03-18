using CommunityToolkit.Mvvm.ComponentModel;
using MvvmCore.Services;

namespace MvvmCore.ViewModels.Pages
{
    public class ManageViewModel : ObservableObject
    {
        private readonly IDialogService _dialogService;
        private readonly IDeviceManagementService _deviceManagementService;
        private readonly ISerialPortConnectionService _serialPortConnectionService;

        public ManageViewModel(IDialogService dialogService, IDeviceManagementService deviceManagementService,
            ISerialPortConnectionService serialPortConnectionService)
        {
            _dialogService = dialogService ??
                             throw new ArgumentNullException(nameof(dialogService));
            _deviceManagementService = deviceManagementService ??
                                       throw new ArgumentNullException(nameof(deviceManagementService));
            _serialPortConnectionService = serialPortConnectionService ??
                                           throw new ArgumentNullException(nameof(serialPortConnectionService));

            _deviceManagementService.ConnectionStatusChange += DeviceManagementServiceOnConnectionStatusChange;
        }

        private void DeviceManagementServiceOnConnectionStatusChange(object? sender, bool e)
        {

        }
    }
}
