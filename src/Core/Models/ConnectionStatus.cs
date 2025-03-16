namespace OSDPBench.Core.Models;

/// <summary>
/// Represents the status of a connection within the system.
/// </summary>
public enum ConnectionStatus
{
    /// <summary>
    /// Indicates that the connection is not currently established.
    /// </summary>
    Disconnected,

    /// <summary>
    /// Represents a state where the connection has been successfully established.
    /// </summary>
    Connected,

    /// <summary>
    /// Indicates that the provided security key is invalid, preventing the connection from being established.
    /// </summary>
    InvalidSecurityKey
}