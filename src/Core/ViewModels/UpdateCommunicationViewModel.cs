using System;
using System.Threading.Tasks;
using MvvmCross.Commands;
using MvvmCross.Navigation;
using MvvmCross.ViewModels;
using OSDPBench.Core.Interactions;
using OSDPBench.Core.Models;
using OSDPBench.Core.Services;

namespace OSDPBench.Core.ViewModels
{
    public class UpdateCommunicationViewModel : MvxViewModel<CommunicationParameters, CommunicationParameters>
    {
        private readonly IMvxNavigationService _navigationService;
        private readonly IDeviceManagementService _deviceManagementService;

        public MvxObservableCollection<uint> AvailableBaudRates { get; } = new MvxObservableCollection<uint>
            {9600, 14400, 19200, 38400, 57600, 115200, 230400};

        private uint _selectedBaudRate;
        public uint SelectedBaudRate
        {
            get => _selectedBaudRate;
            set => SetProperty(ref _selectedBaudRate, value);
        }

        private int _address;
        public int Address
        {
            get => _address;
            set => SetProperty(ref _address, value);
        }

        private bool _isBusy;
        public bool IsBusy
        {
            get => _isBusy;
            set => SetProperty(ref _isBusy, value);
        }

        public UpdateCommunicationViewModel(IMvxNavigationService navigationService, IDeviceManagementService deviceManagementService)
        {
            _navigationService = navigationService ?? throw new ArgumentNullException(nameof(navigationService));
            _deviceManagementService = deviceManagementService ?? throw new ArgumentNullException(nameof(deviceManagementService));
        }

        public override void Prepare(CommunicationParameters communicationParameters)
        {
            SelectedBaudRate = communicationParameters.BaudRate;
            Address = communicationParameters.Address;
        }

        private MvxCommand _setCommunicationsCommand;

        public System.Windows.Input.ICommand SetCommunicationsCommand
        {
            get
            {
                return _setCommunicationsCommand = _setCommunicationsCommand ??
                                                     new MvxCommand(async () =>
                                                     {
                                                         IsBusy = true;
                                                         try
                                                         {
                                                             await DoSetCommunicationsCommand();
                                                         }
                                                         catch
                                                         {
                                                             _alertInteraction.Raise(
                                                                 new Alert(
                                                                     "Error while attempting to update communication settings."));
                                                         }
                                                         IsBusy = false;
                                                     });
            }
        }

        private async Task DoSetCommunicationsCommand()
        {
            var result =
                await _deviceManagementService.SetCommunicationCommand(
                    new CommunicationParameters(SelectedBaudRate, Address));

            await _navigationService.Close(this, result);
        }

        private readonly MvxInteraction<Alert> _alertInteraction =
            new MvxInteraction<Alert>();

        public IMvxInteraction<Alert> AlertInteraction => _alertInteraction;
    }
}
