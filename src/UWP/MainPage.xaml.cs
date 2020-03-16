
// The Blank Page item template is documented at https://go.microsoft.com/fwlink/?LinkId=402352&clcid=0x409

using System;
using System.Collections.Generic;
using System.Linq;
using Windows.Devices.Enumeration;
using Windows.Devices.SerialCommunication;
using Windows.UI.Xaml;
using OSDP.Net;
using OSDP.Net.Connections;

namespace OSDPBenchUWP
{
    /// <summary>
    /// An empty page that can be used on its own or navigated to within a Frame.
    /// </summary>
    public sealed partial class MainPage
    {
        private ControlPanel _panel = new ControlPanel();
        private Guid _connectionId;

        public MainPage()
        {
            InitializeComponent();
        }

        private async void ButtonBase_OnClick(object sender, RoutedEventArgs e)
        {
            var portNamesList = new List<string>();
            foreach (var item in await DeviceInformation.FindAllAsync(SerialDevice.GetDeviceSelector()))
            {
                var serialDevice = await SerialDevice.FromIdAsync(item.Id);
                portNamesList.Add(serialDevice.PortName);
                serialDevice.Dispose();
            }

            if (portNamesList.Any())
            {
                _connectionId = _panel.StartConnection(new SerialPortOsdpConnection(portNamesList.First(), 9600));
                _panel.AddDevice(_connectionId, 1, true, true);
            }
        }
    }
}
