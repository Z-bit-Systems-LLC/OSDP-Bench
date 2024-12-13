namespace OSDPBench.Core.Models;

/// <summary>
/// Represents an entry for a card read event, containing the card number and the timestamp of when the card was read.
/// Instances of this class are typically used to log or display details of card read events.
/// </summary>
public record CardReadEntry
{
    // ReSharper disable once UnusedAutoPropertyAccessor.Global
    /// <summary>
    /// Gets the timestamp indicating the exact date and time when the card read event occurred.
    /// This property is automatically initialized with the current system time at the moment of an entry's creation
    /// and serves as a reference for when the card was scanned.
    /// </summary>
    public DateTime Timestamp { get; init; }
    
    // ReSharper disable once UnusedAutoPropertyAccessor.Global
    /// <summary>
    /// Gets the card number associated with a specific card read event.
    /// This property contains the unique identification number read from the card,
    /// which is usually utilized to authenticate or identify the cardholder during the event.
    /// </summary>
    public required string CardNumber { get; init; }
}