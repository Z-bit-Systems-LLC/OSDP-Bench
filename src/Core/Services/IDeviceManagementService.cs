using OSDP.Net.Connections;
using OSDP.Net.PanelCommands.DeviceDiscover;
using OSDPBench.Core.Actions;
using OSDPBench.Core.Models;

namespace OSDPBench.Core.Services
{
    public interface IDeviceManagementService
    {
        /// <summary>
        /// Gets the IdentityLookup object that provides information about the device's identity.
        /// </summary>
        IdentityLookup? IdentityLookup { get; }

        /// <summary>
        /// Gets the capabilities of the device.
        /// </summary>
        CapabilitiesLookup? CapabilitiesLookup { get; }

        byte Address { get; }

        uint BaudRate { get; }

        bool UsesDefaultSecurityKey { get; }

        public bool IsConnected { get; }

        /// <summary>
        /// Establishes a connection with a device.
        /// </summary>
        /// <param name="connection">The connection to use for communication.</param>
        /// <param name="address">The address of the device.</param>
        Task Connect(IOsdpConnection connection, byte address);

        /// <summary>
        /// Discovers a device asynchronously over the provided connections.
        /// </summary>
        /// <param name="connections">The connections to use for device discovery.</param>
        /// <param name="progress">The progress of the discovery process.</param>
        /// <param name="cancellationToken">A cancellation token that can be used to cancel the discovery process.</param>
        /// <returns>Information regarding the result of the discovery process.</returns>
        Task<DiscoveryResult> DiscoverDevice(IEnumerable<IOsdpConnection> connections, DiscoveryProgress progress, CancellationToken cancellationToken);

        /// <summary>
        /// Asynchronously shuts down the device management service.
        /// </summary>
        /// <remarks>
        /// This method is responsible for shutting down the device management service. It sets the IdentityLookup and CapabilitiesLookup to null and calls the Shutdown method on the underlying panel.
        /// </remarks>
        Task Shutdown();

        /// <summary>
        /// Represents a variable that tracks the connection status change event.
        /// </summary>
        event EventHandler<bool> ConnectionStatusChange;

        event EventHandler DeviceLookupsChanged;

        /// <summary>
        /// Event triggered when a NAK reply is received from the device.
        /// </summary>
        event EventHandler<string> NakReplyReceived;

        /// <summary>
        /// Event that is fired when a card read is received by the device.
        /// </summary>
        event EventHandler<string> CardReadReceived;
    }
}
