using System;
using System.Threading.Tasks;
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

        /// <summary>
        /// Discovers the device.
        /// </summary>
        /// <param name="connection">The connection.</param>
        /// <param name="address">The address.</param>
        /// <param name="requireSecureChannel">if set to <c>true</c> [require secure channel].</param>
        /// <returns>Task&lt;System.Boolean&gt;.</returns>
        Task<bool> DiscoverDevice(ISerialPortConnection connection, byte address, bool requireSecureChannel);

        /// <summary>
        /// Sets the communication command.
        /// </summary>
        /// <param name="communicationParameters">The communication parameters.</param>
        /// <returns>Task&lt;CommunicationParameters&gt;.</returns>
        Task<CommunicationParameters> SetCommunicationCommand(CommunicationParameters communicationParameters);

        /// <summary>
        /// Shuts down this communications.
        /// </summary>
        void Shutdown();

        /// <summary>
        /// Occurs when connection status changes.
        /// </summary>
        event EventHandler<bool> ConnectionStatusChange;
    }
}
