using OSDP.Net.Tracing;

namespace OSDPBench.Core.Models;

/// <summary>
/// A builder class used to construct instances of <see cref="PacketTraceEntry"/>.
/// </summary>
public class PacketTraceEntryBuilder
{
    private MessageSpy _messageSpy;
    private TraceEntry _traceEntry;
    private PacketTraceEntry? _lastTraceEntry;
    private DateTime _timestamp;

    /// <summary>
    /// Initializes a new instance of the <see cref="PacketTraceEntryBuilder"/> class
    /// with a default MessageSpy (no secure channel decryption).
    /// </summary>
    public PacketTraceEntryBuilder() : this(new MessageSpy())
    {
    }

    /// <summary>
    /// Initializes a new instance of the <see cref="PacketTraceEntryBuilder"/> class.
    /// </summary>
    /// <param name="messageSpy">The MessageSpy instance to use for parsing packets.
    /// Should be reused across packets to maintain secure channel state.</param>
    public PacketTraceEntryBuilder(MessageSpy messageSpy)
    {
        _messageSpy = messageSpy;
    }

    /// <summary>
    /// Configures the security key for secure channel decryption.
    /// Creates a new MessageSpy with the specified key, resetting secure channel state.
    /// </summary>
    /// <param name="securityKey">The security key for decryption, or null for no encryption.</param>
    /// <returns>The current builder instance for method chaining.</returns>
    /// <remarks>
    /// Call this method BEFORE any packets are processed to ensure the MessageSpy
    /// can track the secure channel negotiation from the beginning.
    /// </remarks>
    public PacketTraceEntryBuilder WithSecurityKey(byte[]? securityKey)
    {
        _messageSpy = securityKey != null ? new MessageSpy(securityKey) : new MessageSpy();
        return this;
    }

    /// <summary>
    /// Initializes the <see cref="PacketTraceEntryBuilder"/> instance with the specified trace entry and previous trace entry
    /// while also recording the current timestamp.
    /// </summary>
    /// <param name="traceEntry">The current trace entry that provides details for creating a new trace packet.</param>
    /// <param name="lastTraceEntry">The previous packet trace entry used for calculating the interval; can be null.</param>
    /// <returns>The current <see cref="PacketTraceEntryBuilder"/> instance, updated with the provided trace entry details.</returns>
    public PacketTraceEntryBuilder FromTraceEntry(TraceEntry traceEntry, PacketTraceEntry? lastTraceEntry)
    {
        _traceEntry = traceEntry;
        _lastTraceEntry = lastTraceEntry;
        _timestamp = DateTime.UtcNow;

        return this;
    }

    /// <summary>
    /// Creates and returns a new instance of the <see cref="PacketTraceEntry"/> class.
    /// </summary>
    /// <returns>A new instance of the <see cref="PacketTraceEntry"/> class, or null if parsing failed.</returns>
    /// <remarks>
    /// Uses <see cref="MessageSpy"/> to parse the packet data, which supports secure channel decryption
    /// when a security key was provided to the MessageSpy constructor.
    /// </remarks>
    public PacketTraceEntry? Build()
    {
        if (!_messageSpy.TryParsePacket(_traceEntry.Data, out var packet) || packet == null)
        {
            return null;
        }

        return PacketTraceEntry.Create(_traceEntry.Direction, _timestamp,
            _lastTraceEntry != null ? _timestamp - _lastTraceEntry.Timestamp : TimeSpan.Zero,
            packet);
    }
}
