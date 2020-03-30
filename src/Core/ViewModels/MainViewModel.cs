using MvvmCross.ViewModels;

namespace OSDPBench.Core.ViewModels
{
    public class MainViewModel : MvxViewModel
    {
        private MvxObservableCollection<string> _availableSerialPorts = new MvxObservableCollection<string>();
        public MvxObservableCollection<string> AvailableSerialPorts
        {
            get => _availableSerialPorts;
            private set => SetProperty(ref _availableSerialPorts, value);
        }

        private string _serialPortName;
        public string SerialPortName
        {
            get => _serialPortName;
            set => SetProperty(ref _serialPortName, value);
        }
    }
}
