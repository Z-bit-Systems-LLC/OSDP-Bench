using OSDP.Net.Connections;
using OSDP.Net.PanelCommands.DeviceDiscover;
using OSDP.Net.Tracing;
using OSDPBench.Core.Actions;
using OSDPBench.Core.Models;

namespace OSDPBench.Core.Services;

/// <summary>
/// Represents a service for managing devices.
/// </summary>
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

    /// <summary>
    /// Gets or sets the name of the port.
    /// </summary>
    /// <remarks>
    /// The PortName property represents the name of the port to which the device is connected.
    /// </remarks>
    string? PortName { get; set; }

    /// <summary>
    /// Gets the address of a device.
    /// </summary>
    byte Address { get; }

    /// <summary>
    /// Gets the baud rate used for communication with the device.
    /// </summary>
    uint BaudRate { get; }

    /// <summary>
    /// Indicates whether the communication channel with the device is secured using a secure channel protocol.
    /// </summary>
    bool IsUsingSecureChannel { get; }

    /// <summary>
    /// Gets a value indicating whether the device uses the default security key.
    /// </summary>
    /// <value>
    /// <c>true</c> if the device uses the default security key; otherwise, <c>false</c>.
    /// </value>
    bool UsesDefaultSecurityKey { get; }

    /// <summary>
    /// Gets the security key used for secure channel communication.
    /// </summary>
    /// <remarks>
    /// Returns the custom security key if one was provided, or null if using the default key or not using secure channel.
    /// Used by the tracing system to decrypt secure channel traffic.
    /// </remarks>
    byte[]? SecurityKey { get; }

    /// <summary>
    /// Gets the connection status of the device.
    /// </summary>
    /// <remarks>
    /// This property indicates whether a connection with the device is established.
    /// </remarks>
    public bool IsConnected { get; }

    /// <summary>
    /// Establishes a connection with a device.
    /// </summary>
    /// <param name="connection">The connection to use for communication.</param>
    /// <param name="address">The address of the device.</param>
    /// <param name="useSecureChannel">Connect device using secure channel</param>
    /// <param name="useDefaultSecurityKey">Use the default key to connect with a secure channel</param>
    /// <param name="securityKey">Security key if default is not used</param>
    Task Connect(IOsdpConnection connection, byte address, bool useSecureChannel = false,
        bool useDefaultSecurityKey = true, byte[]? securityKey = null);
    
    /// <summary>
    /// Reestablishes a connection with a device using the specified connection and address.
    /// </summary>
    /// <param name="osdpConnection">The connection instance to use for communication.</param>
    /// <param name="address">The address of the device to reconnect with.</param>
    /// <returns>A task representing the asynchronous reconnect operation.</returns>
    Task Reconnect(IOsdpConnection osdpConnection, byte address);

    /// <summary>
    /// Reestablishes a connection with a device after communication settings have been changed.
    /// This method does not wait for the device to go offline on the old connection settings
    /// since the device has already switched to new communication parameters.
    /// </summary>
    /// <param name="osdpConnection">The connection instance to use for communication.</param>
    /// <param name="address">The address of the device to reconnect with.</param>
    /// <returns>A task representing the asynchronous reconnect operation.</returns>
    Task ReconnectAfterCommunicationChange(IOsdpConnection osdpConnection, byte address);

    /// <summary>
    /// Discovers a device asynchronously over the provided connections.
    /// </summary>
    /// <param name="connections">The connections to use for device discovery.</param>
    /// <param name="progress">The progress of the discovery process.</param>
    /// <param name="cancellationToken">A cancellation token that can be used to cancel the discovery process.</param>
    /// <returns>Information regarding the result of the discovery process.</returns>
    Task<DiscoveryResult> DiscoverDevice(IEnumerable<IOsdpConnection> connections, DiscoveryProgress progress,
        CancellationToken cancellationToken);

    /// <summary>
    /// Executes a device action with the provided parameter.
    /// </summary>
    /// <param name="deviceAction">The device action to execute.</param>
    /// <param name="parameter">The parameter to pass to the device action.</param>
    /// <returns>A task that represents the asynchronous execution of the device action.</returns>
    Task<object> ExecuteDeviceAction(IDeviceAction deviceAction, object? parameter);

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
    event EventHandler<ConnectionStatus> ConnectionStatusChange;

    /// <summary>
    /// Occurs when the device lookups (IdentityLookup or CapabilitiesLookup) change.
    /// </summary>
    event EventHandler DeviceLookupsChanged;

    /// <summary>
    /// Event triggered when a NAK reply is received from the device.
    /// </summary>
    event EventHandler<string> NakReplyReceived;

    /// <summary>
    /// Event that is fired when a card read is received by the device.
    /// </summary>
    event EventHandler<string> CardReadReceived;

    /// <summary>
    /// Event triggered when a read from the keypad is received.
    /// </summary>
    event EventHandler<string> KeypadReadReceived;

    /// <summary>
    /// Event that is triggered when a trace entry is received.
    /// </summary>
    event EventHandler<TraceEntry>? TraceEntryReceived;
}