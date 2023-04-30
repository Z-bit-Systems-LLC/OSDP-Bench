using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using OSDP.Net.Connections;
using OSDP.Net.PanelCommands.DeviceDiscover;
using OSDPBench.Core.Models;
using OSDPBench.Core.Platforms;

namespace OSDPBench.Core.Services
{
    public interface IDeviceManagementService
    {
        /// <summary>
        /// Gets the identity data.
        /// </summary>
        IdentityLookup IdentityLookup { get; }

        /// <summary>
        /// Gets the capabilities data.
        /// </summary>
        CapabilitiesLookup CapabilitiesLookup { get; }

        byte Address { get; }

        uint BaudRate { get; }

        bool UsesDefaultSecurityKey { get; }

        void Connect(IOsdpConnection connection, byte address);

        /// <summary>
        ///   <para>
        /// Discovers the device.
        /// </para>
        /// </summary>
        /// <param name="connections"></param>
        /// <param name="progress"></param>
        /// <param name="cancellationToken"></param>
        Task<DiscoveryResult> DiscoverDevice(IEnumerable<IOsdpConnection> connections, DiscoveryProgress progress, CancellationToken cancellationToken);

        /// <summary>
        /// Sets the communication command.
        /// </summary>
        /// <param name="communicationParameters">The communication parameters.</param>
        /// <returns>Task&lt;CommunicationParameters&gt;.</returns>
        Task<CommunicationParameters> SetCommunicationCommand(CommunicationParameters communicationParameters);

        /// <summary>
        /// Resets the device.
        /// </summary>
        /// <param name="connection">The connection.</param>
        /// <returns>Task.</returns>
        Task ResetDevice(ISerialPortConnection connection);

        /// <summary>
        /// Shuts down this communications.
        /// </summary>
        Task Shutdown();

        /// <summary>
        /// Occurs when connection status changes.
        /// </summary>
        event EventHandler<bool> ConnectionStatusChange;

        /// <summary>
        /// Occurs when NAK reply received.
        /// </summary>
        event EventHandler<string> NakReplyReceived;
    }
}
