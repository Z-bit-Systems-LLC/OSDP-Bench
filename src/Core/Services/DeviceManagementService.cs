using System;
using System.Collections;
using System.Text;
using System.Threading.Tasks;
using OSDP.Net;
using OSDP.Net.Model.CommandData;
using OSDPBench.Core.Models;
using OSDPBench.Core.Platforms;

namespace OSDPBench.Core.Services
{
    public class DeviceManagementService : IDeviceManagementService
    {
        private readonly ControlPanel _panel;

        private Guid _connectionId;
        private bool _isConnected;
        private byte _address;
        private bool _requireSecureChannel;

        public DeviceManagementService(ControlPanel panel)
        {
            _panel = panel ?? throw new ArgumentNullException(nameof(panel));

            _panel.ConnectionStatusChanged += (sender, args) =>
            {
                _isConnected = args.IsConnected;
                OnConnectionStatusChange(_isConnected);
            };
        }

        public IdentityLookup IdentityLookup { get; private set; }

        public CapabilitiesLookup CapabilitiesLookup { get; private set; }


        public async Task<bool> DiscoverDevice(ISerialPortConnection connection, byte address, bool requireSecureChannel)
        {
            _address = address;
            _requireSecureChannel = requireSecureChannel;

            _panel.Shutdown();

            _connectionId = _panel.StartConnection(connection);

            _panel.AddDevice(_connectionId, _address, false, false);

            bool successfulConnection = await WaitForConnection();

            if (!successfulConnection)
            {
                return false;
            }
            
            await GetIdentity();
            await GetCapabilities();

            _panel.AddDevice(_connectionId, _address, CapabilitiesLookup.CRC,
                _requireSecureChannel && CapabilitiesLookup.SecureChannel);

            return await WaitForConnection();
        }

        public async Task<CommunicationParameters> SetCommunicationCommand(
            CommunicationParameters communicationParameters)
        {
            try
            {
                var result = await _panel.CommunicationConfiguration(_connectionId, _address,
                    new CommunicationConfiguration((byte)communicationParameters.Address,
                        (int)communicationParameters.BaudRate));

                return new CommunicationParameters((uint)result.BaudRate, result.Address);
            }
            catch (TimeoutException)
            {
                return communicationParameters;
            }

        }

        public void Shutdown()
        {
            _panel.Shutdown();
        }

        public event EventHandler<bool> ConnectionStatusChange;
        protected virtual void OnConnectionStatusChange(bool e)
        {
            ConnectionStatusChange?.Invoke(this, e);
        }

        private async Task<bool> WaitForConnection()
        {
            int count = 0;
            while (!_isConnected && count++ < 1)
            {
                await Task.Delay(TimeSpan.FromMilliseconds(100));
            }

            return _isConnected;
        }

        private async Task GetIdentity()
        {
            IdentityLookup = new IdentityLookup(await _panel.IdReport(_connectionId, _address));
        }

        private async Task GetCapabilities()
        {
            CapabilitiesLookup = new CapabilitiesLookup(await _panel.DeviceCapabilities(_connectionId, _address));
        }

        private static string FormatData(BitArray bitArray)
        {
            var builder = new StringBuilder();
            foreach (bool bit in bitArray)
            {
                builder.Append(bit ? "1" : "0");
            }

            return builder.ToString();
        }
    }
}