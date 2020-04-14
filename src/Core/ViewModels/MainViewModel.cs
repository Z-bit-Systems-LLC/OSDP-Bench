using System.Linq;
using MvvmCross.ViewModels;
using OSDPBench.Core.Interactions;
using OSDPBench.Core.Models;
using OSDPBench.Core.Platforms;

namespace OSDPBench.Core.ViewModels
{
    public class MainViewModel : MvxViewModel
    {
        private readonly ISerialPort _serialPort;

        public MainViewModel(ISerialPort serialPort)
        {
            _serialPort = serialPort;
        }

        public MvxObservableCollection<SerialPort> AvailableSerialPorts { get; } =
            new MvxObservableCollection<SerialPort>();

        public MvxObservableCollection<uint> AvailableBaudRates { get; } = new MvxObservableCollection<uint>
            {9600, 14400, 19200, 38400, 57600, 115200};

        private SerialPort _selectedSerialPort;

        public SerialPort SelectedSerialPort
        {
            get => _selectedSerialPort;
            set => SetProperty(ref _selectedSerialPort, value);
        }

        private uint _selectedBaudRate;

        public uint SelectedBaudRate
        {
            get => _selectedBaudRate;
            set => SetProperty(ref _selectedBaudRate, value);
        }

        private readonly MvxInteraction<Alert> _alertInteraction =
            new MvxInteraction<Alert>();

        public IMvxInteraction<Alert> AlertInteraction => _alertInteraction;

        public override void ViewAppeared()
        {
            AvailableSerialPorts.AddRange(AsyncHelper.RunSync(() => _serialPort.FindAvailableSerialPorts()));

            if (AvailableSerialPorts.Any())
            {
                SelectedSerialPort = AvailableSerialPorts.First();
            }
            else
            {
                _alertInteraction.Raise(new Alert("No serial ports are available."));
            }

            SelectedBaudRate = 9600;

            base.ViewAppeared();
        }
    }
}
