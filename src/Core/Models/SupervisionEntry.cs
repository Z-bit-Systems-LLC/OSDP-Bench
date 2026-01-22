namespace OSDPBench.Core.Models;

/// <summary>
/// Represents an entry for a supervision event, containing the event type, status, and timestamp.
/// </summary>
/// <param name="Timestamp">The timestamp when the event occurred.</param>
/// <param name="EventType">The type of supervision event.</param>
/// <param name="Status">The status description for the event.</param>
public record SupervisionEntry(DateTime Timestamp, SupervisionEventType EventType, string Status);

/// <summary>
/// Defines the types of supervision events that can be tracked.
/// </summary>
public enum SupervisionEventType
{
    /// <summary>
    /// Tamper status event from osdp_LSTATR.
    /// </summary>
    Tamper,

    /// <summary>
    /// Power status event from osdp_LSTATR.
    /// </summary>
    Power,

    /// <summary>
    /// Communication status from connection status changes.
    /// </summary>
    Communication
}
