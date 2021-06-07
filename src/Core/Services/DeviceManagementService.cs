using System;
using System.Collections;
using System.Text;
using System.Threading.Tasks;
using OSDP.Net;
using OSDP.Net.Model.ReplyData;
using OSDPBench.Core.Models;
using OSDPBench.Core.Platforms;
using CommunicationConfiguration = OSDP.Net.Model.CommandData.CommunicationConfiguration;
using ManufacturerSpecific = OSDP.Net.Model.CommandData.ManufacturerSpecific;

namespace OSDPBench.Core.Services
{
    /// <summary>
    /// Class DeviceManagementService.
    /// Implements the <see cref="OSDPBench.Core.Services.IDeviceManagementService" />
    /// </summary>
    /// <seealso cref="OSDPBench.Core.Services.IDeviceManagementService" />
    public class DeviceManagementService : IDeviceManagementService
    {
        private readonly ControlPanel _panel;

        private Guid _connectionId;
        private bool _isConnected;
        private byte _address;
        private bool _requireSecureChannel;

        /// <summary>
        /// Initializes a new instance of the <see cref="DeviceManagementService"/> class.
        /// </summary>
        /// <param name="panel">The panel.</param>
        /// <exception cref="ArgumentNullException">panel</exception>
        public DeviceManagementService(ControlPanel panel)
        {
            _panel = panel ?? throw new ArgumentNullException(nameof(panel));

            _panel.ConnectionStatusChanged += (sender, args) =>
            {
                _isConnected = args.IsConnected;
                OnConnectionStatusChange(_isConnected);
            };

            _panel.NakReplyReceived += (sender, args) =>
            {
                OnNakReplyReceived(ToFormattedText(args.Nak.ErrorCode));
            };
        }

        /// <inheritdoc />
        public IdentityLookup IdentityLookup { get; private set; }

        /// <inheritdoc />
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

        /// <inheritdoc />
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

        /// <inheritdoc />
        public async Task ResetDevice(ISerialPortConnection connection)
        {
            Shutdown();

            _connectionId = _panel.StartConnection(connection, TimeSpan.Zero);

            _panel.AddDevice(_connectionId, _address, false, false);

            const int maximumAttempts = 15;
            const int requiredNumberOfAcks = 10;
            int totalAcks = 0;
            int totalAttempts = 0;
            while (totalAttempts++ < maximumAttempts && totalAcks < requiredNumberOfAcks)
            {
                try
                {
                    var result = await _panel.ManufacturerSpecificCommand(_connectionId, _address,
                        new ManufacturerSpecific(new byte[] { 0xCA, 0x44, 0x6C }, new byte[] { 0x05 }));

                    if (result.Ack)
                    {
                        totalAcks++;
                    }
                }
                catch
                {
                    await Task.Delay(TimeSpan.FromSeconds(1));
                }
            }

            if (totalAcks < requiredNumberOfAcks)
            {
                throw new Exception("Reset commands were not accepted.");
            }
        }

        private static string ToFormattedText(ErrorCode value)
        {
            var builder = new StringBuilder();

            foreach (var character in value.ToString())
            {
                if (char.IsUpper(character))
                {
                    builder.Append(" ");
                }

                builder.Append(character);
            }

            return builder.ToString().TrimStart();
        }

        /// <inheritdoc />
        public void Shutdown()
        {
            _panel.Shutdown();
        }

        /// <inheritdoc />
        public event EventHandler<bool> ConnectionStatusChange;
        protected virtual void OnConnectionStatusChange(bool isConnected)
        {
            ConnectionStatusChange?.Invoke(this, isConnected);
        }

        /// <inheritdoc />
        public event EventHandler<string> NakReplyReceived;
        protected virtual void OnNakReplyReceived(string errorMessage)
        {
            NakReplyReceived?.Invoke(this, errorMessage);
        }

        private async Task<bool> WaitForConnection()
        {
            int count = 0;
            while (!_isConnected && count++ < 10)
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