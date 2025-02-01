using OSDP.Net.Tracing;

namespace OSDPBench.Core.Models;

/// <summary>
/// A builder class used to construct instances of <see cref="PacketTraceEntry"/>.
/// </summary>
public class PacketTraceEntryBuilder
{
    private TraceEntry _traceEntry;
    private PacketTraceEntry? _lastTraceEntry;
    private DateTime _timestamp;

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
    /// <returns>A new instance of the <see cref="PacketTraceEntry"/> class, fully constructed based on the specified trace entry details.</returns>
    public PacketTraceEntry Build()
    {
        return PacketTraceEntry.Create(_traceEntry.Direction, _timestamp,
            _lastTraceEntry != null ? _timestamp - _lastTraceEntry.Timestamp : TimeSpan.Zero,
            PacketDecoding.ParseMessage(_traceEntry.Data));
    }
}