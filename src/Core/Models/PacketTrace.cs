using System.ComponentModel.Design;
using System.Text.RegularExpressions;
using OSDP.Net.Messages;
using OSDP.Net.Model;
using OSDP.Net.Model.CommandData;
using OSDP.Net.Tracing;

namespace OSDPBench.Core.Models;

public class PacketTraceEntry
{
    public TraceDirection Direction { get; }
    
    public DateTime Timestamp { get; }
    
    public DateTime LocalTimestamp => Timestamp.ToLocalTime();
    
    public TimeSpan Interval { get; }
    
    public string Type {
        get
        {
            if (Packet.CommandType != null)
            {
                return ToSpacedString(Packet.CommandType) ?? "Unknown";
            }
            else if (Packet.ReplyType != null)
            {
                return ToSpacedString(Packet.ReplyType) ?? "Unknown";
            }
            else
            {
                return "Unknown";
            } }
    }

    public string Details => Packet.ParsePayloadData()?.ToString() ?? "Empty";
    
    private static string ToSpacedString(Enum enumValue)
    {
        // Use Regex to insert spaces before each capital letter, ignoring the first capital.
        var result = Regex.Replace(enumValue.ToString(), "(\\B[A-Z])", " $1");
        return result;
    }
    
    public Packet Packet { get; }

    // Private constructor
    private PacketTraceEntry(TraceDirection direction, DateTime timestamp, TimeSpan interval, Packet packet)
    {
        Direction = direction;
        Timestamp = timestamp;
        Interval = interval;
        Packet = packet;
    }

    // Factory method
    internal static PacketTraceEntry Create(TraceDirection direction, DateTime timestamp, TimeSpan interval, Packet packet)
    {
        return new PacketTraceEntry(direction, timestamp, interval, packet);
    }
}

public class PacketTraceEntryBuilder
{
    private TraceEntry _traceEntry;
    private PacketTraceEntry? _lastTraceEntry;
    private DateTime _timestamp;

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