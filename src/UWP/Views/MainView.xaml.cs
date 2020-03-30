using System;
using Windows.Devices.Enumeration;
using Windows.Devices.SerialCommunication;
using Windows.UI.Xaml;
using MvvmCross.Platforms.Uap.Views;
using OSDPBench.Core.ViewModels;

// The Blank Page item template is documented at https://go.microsoft.com/fwlink/?LinkId=234238

namespace OSDPBenchUWP.Views
{
    /// <summary>
    /// The main page
    /// </summary>
    public sealed partial class MainView : MvxWindowsPage
    {
        private MainViewModel MainViewModel => ViewModel as MainViewModel;

        public MainView()
        {
            InitializeComponent();
        }

        private async void MainView_OnLoaded(object sender, RoutedEventArgs e)
        {
            foreach (var item in await DeviceInformation.FindAllAsync(SerialDevice.GetDeviceSelector()))
            {
                var serialDevice = await SerialDevice.FromIdAsync(item.Id);
                MainViewModel.AvailableSerialPorts.Add(serialDevice.PortName);
                serialDevice.Dispose();
            }
        }
    }
}
