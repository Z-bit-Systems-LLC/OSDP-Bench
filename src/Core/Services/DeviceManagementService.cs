﻿using System;
using System.Collections;
using System.Text;
using System.Threading.Tasks;
using OSDP.Net;
using OSDP.Net.Model.CommandData;
using OSDPBench.Core.Models;
using OSDPBench.Core.Platforms;

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
        public void Shutdown()
        {
            _panel.Shutdown();
        }

        /// <inheritdoc />
        public event EventHandler<bool> ConnectionStatusChange;
        protected virtual void OnConnectionStatusChange(bool e)
        {
            ConnectionStatusChange?.Invoke(this, e);
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