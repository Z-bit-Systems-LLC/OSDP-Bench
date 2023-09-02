using System;
using System.Threading.Tasks;
using Microsoft.Extensions.Logging;
using MvvmCross.Commands;
using MvvmCross.Navigation;
using MvvmCross.ViewModels;
using OSDPBench.Core.Interactions;
using OSDPBench.Core.Models;
using OSDPBench.Core.Platforms;
using OSDPBench.Core.Services;

namespace OSDPBench.Core.ViewModels
{
    public class UpdateCommunicationViewModel : MvxNavigationViewModel<CommunicationParameters>
    {
        private readonly IMvxNavigationService _navigationService;
        private readonly IDeviceManagementService _deviceManagementService;
        private readonly ISerialPortConnection _serialPortConnection;

        private string _portName;

        public UpdateCommunicationViewModel(ILoggerFactory logProvider, IMvxNavigationService navigationService, IDeviceManagementService deviceManagementService,
            ISerialPortConnection serialPortConnection) : base(logProvider, navigationService)
        {
            _navigationService = navigationService;
            _deviceManagementService = deviceManagementService ?? throw new ArgumentNullException(nameof(deviceManagementService));
            _serialPortConnection = serialPortConnection ?? throw new ArgumentNullException(nameof(serialPortConnection));
        }

        public MvxObservableCollection<uint> AvailableBaudRates { get; } = new() {9600, 19200, 38400, 57600, 115200, 230400};

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

        public override void Prepare(CommunicationParameters communicationParameters)
        {
            _portName = communicationParameters.PortName;
            SelectedBaudRate = communicationParameters.BaudRate;
            Address = communicationParameters.Address;
        }

        private MvxAsyncCommand _setCommunicationsCommand;

        public System.Windows.Input.ICommand SetCommunicationsCommand
        {
            get
            {
                return _setCommunicationsCommand ??= new MvxAsyncCommand(async () =>
                {
                    IsBusy = true;
                    try
                    {
                        await DoSetCommunicationsCommand();
                    }
                    catch (Exception exception)
                    {
                        _alertInteraction.Raise(
                            new Alert(
                                $"Error while attempting to update communication settings. {exception.Message}"));
                    }
                    IsBusy = false;

                    await _navigationService.Close(this);
                });
            }
        }

        private async Task DoSetCommunicationsCommand()
        {
            var results = await _deviceManagementService.SetCommunicationCommand(
                    new CommunicationParameters(_portName, SelectedBaudRate, (byte)Address));
            
            await _deviceManagementService.Shutdown();

            _deviceManagementService.Connect(_serialPortConnection.GetConnection(_portName, (int)results.BaudRate),
                results.Address);
        }

        private MvxAsyncCommand _cancelCommand;

        public System.Windows.Input.ICommand CancelCommand
        {
            get
            {
                return _cancelCommand ??= new MvxAsyncCommand(async () =>
                {
                    await _navigationService.Close(this);
                });
            }
        }

        private readonly MvxInteraction<Alert> _alertInteraction = new();

        public IMvxInteraction<Alert> AlertInteraction => _alertInteraction;
    }
}
