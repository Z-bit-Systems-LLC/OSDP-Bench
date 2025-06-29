using OSDPBench.Core.Models;
using OSDP.Net.Tracing;

namespace OSDPBench.Core.Services;

/// <summary>
/// Represents a service for tracking device state.
/// This service is designed to reduce direct coupling between ViewModels and the DeviceManagementService.
/// </summary>
public interface IDeviceStateService
{
    /// <summary>
    /// Gets a value indicating whether a device is connected.
    /// </summary>
    bool IsConnected { get; }
    
    /// <summary>
    /// Gets or sets the name of the port being used.
    /// </summary>
    string? PortName { get; }
    
    /// <summary>
    /// Gets the device address.
    /// </summary>
    byte Address { get; }
    
    /// <summary>
    /// Gets the baud rate being used.
    /// </summary>
    uint BaudRate { get; }
    
    /// <summary>
    /// Gets the identity lookup information.
    /// </summary>
    IdentityLookup? IdentityLookup { get; }
    
    /// <summary>
    /// Gets the capabilities lookup information.
    /// </summary>
    CapabilitiesLookup? CapabilitiesLookup { get; }
    
    /// <summary>
    /// Gets a value indicating whether a secure channel is being used.
    /// </summary>
    bool IsUsingSecureChannel { get; }
    
    /// <summary>
    /// Occurs when the connection status changes.
    /// </summary>
    event EventHandler<ConnectionStatus> ConnectionStatusChange;
    
    /// <summary>
    /// Occurs when a keypad read is received.
    /// </summary>
    event EventHandler<string> KeypadReadReceived;
    
    /// <summary>
    /// Occurs when a card read is received.
    /// </summary>
    event EventHandler<string> CardReadReceived;
    
    /// <summary>
    /// Occurs when a trace entry is received.
    /// </summary>
    event EventHandler<TraceEntry> TraceEntryReceived;
    
    /// <summary>
    /// Occurs when a NAK reply is received.
    /// </summary>
    event EventHandler<string> NakReplyReceived;
    
    /// <summary>
    /// Occurs when device lookups change.
    /// </summary>
    event EventHandler DeviceLookupsChanged;
}