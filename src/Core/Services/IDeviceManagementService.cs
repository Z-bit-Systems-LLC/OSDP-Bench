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
        /// List of device action available.
        /// </summary>
        List<IDeviceAction> AvailableActions { get; }

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
        /// Sets the communication command.
        /// </summary>
        /// <param name="communicationParameters">The communication parameters.</param>
        /// <returns>Task&lt;CommunicationParameters&gt;.</returns>
        Task<CommunicationParameters> SetCommunicationCommand(CommunicationParameters communicationParameters);

        /// <summary>
        /// Resets the device.
        /// </summary>
        /// <param name="connectionService">The connectionService.</param>
        /// <returns>Task.</returns>
        Task ResetDevice(ISerialPortConnectionService connectionService);

        /// <summary>
        /// Asynchronously shuts down the device management service.
        /// </summary>
        /// <remarks>
        /// This method is responsible for shutting down the device management service. It sets the IdentityLookup and CapabilitiesLookup to null and calls the Shutdown method on the underlying panel.
        /// </remarks>
        Task Shutdown();

        /// <summary>
        /// Occurs when connectionService status changes.
        /// </summary>
        event EventHandler<bool> ConnectionStatusChange;

        event EventHandler DeviceLookupsChanged;

        /// <summary>
        /// Occurs when NAK reply received.
        /// </summary>
        event EventHandler<string> NakReplyReceived;

        /// <summary>
        /// 
        /// </summary>
        event EventHandler<string> CardReadReceived;
    }
}
