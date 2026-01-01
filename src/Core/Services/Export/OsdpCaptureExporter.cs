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

            var entry = new
            {
                timeSec = timeSec.ToString("F0"),
                timeNano = timeNano.ToString("000000000"),
                io = ioValue,
                data = BitConverter.ToString(packet.RawData),
                osdpTraceVersion = "1",
                osdpSource = SourceIdentifier
            };

            string json = JsonSerializer.Serialize(entry);
            builder.AppendLine(json);
        }

        return Task.FromResult(Encoding.UTF8.GetBytes(builder.ToString()));
    }
}
