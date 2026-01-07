using System.Text;
using OSDP.Net.Tracing;
using OSDPBench.Core.Models;

namespace OSDPBench.Core.Services.Export;

/// <summary>
/// Exports packet trace data to a human-readable text (.txt) format.
/// Uses the OSDP.Net OSDPPacketTextFormatter for consistent formatting.
/// </summary>
public class ParsedPacketExporter : IPacketExporter
{
    private readonly OSDPPacketTextFormatter _formatter = new();

    /// <inheritdoc />
    public string FileExtension => ".txt";

    /// <inheritdoc />
    public string DisplayName => "Parsed Text";

    /// <inheritdoc />
    public Task<byte[]> ExportAsync(IEnumerable<PacketTraceEntry> packets)
    {
        var builder = new StringBuilder();
        builder.AppendLine("OSDP Packet Trace Export");
        builder.AppendLine($"Exported: {DateTime.Now:yyyy-MM-dd HH:mm:ss}");
        builder.AppendLine("Source: OSDP-Bench");
        builder.AppendLine(new string('=', 80));
        builder.AppendLine();

        foreach (var packet in packets.OrderBy(p => p.Timestamp))
        {
            var formattedPacket = _formatter.FormatPacket(packet.Packet, packet.Timestamp, packet.Interval);
            builder.Append(formattedPacket);
        }

        return Task.FromResult(Encoding.UTF8.GetBytes(builder.ToString()));
    }
}
