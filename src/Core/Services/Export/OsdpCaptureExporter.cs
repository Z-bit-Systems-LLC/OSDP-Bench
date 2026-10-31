using System.Text;
using System.Text.Json;
using OSDP.Net.Tracing;
using OSDPBench.Core.Models;

namespace OSDPBench.Core.Services.Export;

/// <summary>
/// Exports packet trace data to the OSDP Capture (.osdpcap) JSON format.
/// Each line is a JSON object compatible with OSDP trace analyzers.
/// </summary>
public class OsdpCaptureExporter : IPacketExporter
{
    private const string SourceIdentifier = "OSDP-Bench";

    /// <inheritdoc />
    public string FileExtension => ".osdpcap";

    /// <inheritdoc />
    public string DisplayName => "OSDP Capture";

    /// <inheritdoc />
    public Task<byte[]> ExportAsync(IEnumerable<PacketTraceEntry> packets)
    {
        var builder = new StringBuilder();

        foreach (var packet in packets.OrderBy(p => p.Timestamp))
        {
            var unixTime = packet.Timestamp.Subtract(new DateTime(1970, 1, 1));
            long timeSec = (long)Math.Floor(unixTime.TotalSeconds);
            long timeNano = (unixTime.Ticks - timeSec * TimeSpan.TicksPerSecond) * 100L;

            string ioValue = packet.Direction switch
            {
                TraceDirection.Input => "input",
                TraceDirection.Output => "output",
                TraceDirection.Trace => "trace",
                _ => "unknown"
            };

            builder.AppendLine(WriteEntry(
                timeSec.ToString("F0"),
                timeNano.ToString("000000000"),
                ioValue,
                BitConverter.ToString(packet.RawData)));
        }

        return Task.FromResult(Encoding.UTF8.GetBytes(builder.ToString()));
    }

    // Written field by field rather than serialized from an object, so it needs no reflection and still works in a
    // trimmed build, such as the web version's
    private static string WriteEntry(string timeSec, string timeNano, string io, string data)
    {
        using var stream = new MemoryStream();
        using (var writer = new Utf8JsonWriter(stream))
        {
            writer.WriteStartObject();
            writer.WriteString("timeSec", timeSec);
            writer.WriteString("timeNano", timeNano);
            writer.WriteString("io", io);
            writer.WriteString("data", data);
            writer.WriteString("osdpTraceVersion", "1");
            writer.WriteString("osdpSource", SourceIdentifier);
            writer.WriteEndObject();
        }

        return Encoding.UTF8.GetString(stream.ToArray());
    }
}
