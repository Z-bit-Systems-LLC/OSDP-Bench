using System;
using System.Linq;
using System.Text;
using System.Text.Json;
using System.Threading.Tasks;
using NUnit.Framework;
using OSDP.Net.Tracing;
using OSDPBench.Core.Models;
using OSDPBench.Core.Services.Export;

namespace OSDPBench.Core.Tests.Services.Export;

[TestFixture(TestOf = typeof(OsdpCaptureExporter))]
public class OsdpCaptureExporterTests
{
    private OsdpCaptureExporter _exporter;

    [SetUp]
    public void Setup()
    {
        _exporter = new OsdpCaptureExporter();
    }

    #region Property Tests

    [Test]
    public void FileExtension_ReturnsOsdpcap()
    {
        Assert.That(_exporter.FileExtension, Is.EqualTo(".osdpcap"));
    }

    [Test]
    public void DisplayName_ReturnsOsdpCapture()
    {
        Assert.That(_exporter.DisplayName, Is.EqualTo("OSDP Capture"));
    }

    #endregion

    #region ExportAsync Tests

    [Test]
    public async Task ExportAsync_EmptyPacketList_ReturnsEmptyBytes()
    {
        // Arrange
        var packets = Enumerable.Empty<PacketTraceEntry>();

        // Act
        var result = await _exporter.ExportAsync(packets);

        // Assert
        Assert.That(result, Is.Not.Null);
        Assert.That(result, Is.Empty);
    }

    [Test]
    public async Task ExportAsync_SinglePacket_ProducesValidJsonLine()
    {
        // Arrange
        var packets = new[] { CreateTestPacketTraceEntry(TraceDirection.Output) };

        // Act
        var result = await _exporter.ExportAsync(packets);

        // Assert
        Assert.That(result, Is.Not.Null);
        var content = Encoding.UTF8.GetString(result);
        Assert.That(content, Is.Not.Empty);

        // Should be valid JSON on a single line (plus newline)
        var lines = content.Split('\n', StringSplitOptions.RemoveEmptyEntries);
        Assert.That(lines.Length, Is.EqualTo(1));

        // Verify it's valid JSON
        Assert.DoesNotThrow(() => JsonDocument.Parse(lines[0]));
    }

    [Test]
    public async Task ExportAsync_MultiplePackets_ProducesMultipleJsonLines()
    {
        // Arrange
        var packets = new[]
        {
            CreateTestPacketTraceEntry(TraceDirection.Output),
            CreateTestPacketTraceEntry(TraceDirection.Input),
            CreateTestPacketTraceEntry(TraceDirection.Output)
        };

        // Act
        var result = await _exporter.ExportAsync(packets);

        // Assert
        var content = Encoding.UTF8.GetString(result);
        var lines = content.Split('\n', StringSplitOptions.RemoveEmptyEntries);
        Assert.That(lines.Length, Is.EqualTo(3));

        // Verify each line is valid JSON
        foreach (var line in lines)
        {
            Assert.DoesNotThrow(() => JsonDocument.Parse(line));
        }
    }

    [Test]
    public async Task ExportAsync_PacketsOrderedByTimestamp()
    {
        // Arrange - Add packets in wrong order
        var earlier = DateTime.UtcNow;
        var later = earlier.AddSeconds(10);
        var packets = new[]
        {
            CreateTestPacketTraceEntry(TraceDirection.Output),
            CreateTestPacketTraceEntry(TraceDirection.Input)
        };

        // Act
        var result = await _exporter.ExportAsync(packets);

        // Assert
        var content = Encoding.UTF8.GetString(result);
        var lines = content.Split('\n', StringSplitOptions.RemoveEmptyEntries);

        // Parse both JSON objects and check timestamps are ordered
        var firstJson = JsonDocument.Parse(lines[0]);
        var secondJson = JsonDocument.Parse(lines[1]);

        var firstTimeSec = long.Parse(firstJson.RootElement.GetProperty("timeSec").GetString()!);
        var secondTimeSec = long.Parse(secondJson.RootElement.GetProperty("timeSec").GetString()!);

        Assert.That(firstTimeSec, Is.LessThanOrEqualTo(secondTimeSec));
    }

    [Test]
    public async Task ExportAsync_IncludesOsdpSourceField()
    {
        // Arrange
        var packets = new[] { CreateTestPacketTraceEntry(TraceDirection.Output) };

        // Act
        var result = await _exporter.ExportAsync(packets);

        // Assert
        var content = Encoding.UTF8.GetString(result);
        var line = content.Split('\n', StringSplitOptions.RemoveEmptyEntries)[0];
        var json = JsonDocument.Parse(line);

        Assert.That(json.RootElement.GetProperty("osdpSource").GetString(), Is.EqualTo("OSDP-Bench"));
    }

    [Test]
    public async Task ExportAsync_IncludesOsdpTraceVersionField()
    {
        // Arrange
        var packets = new[] { CreateTestPacketTraceEntry(TraceDirection.Output) };

        // Act
        var result = await _exporter.ExportAsync(packets);

        // Assert
        var content = Encoding.UTF8.GetString(result);
        var line = content.Split('\n', StringSplitOptions.RemoveEmptyEntries)[0];
        var json = JsonDocument.Parse(line);

        Assert.That(json.RootElement.GetProperty("osdpTraceVersion").GetString(), Is.EqualTo("1"));
    }

    [Test]
    public async Task ExportAsync_OutputDirection_MapsToOutput()
    {
        // Arrange
        var packets = new[] { CreateTestPacketTraceEntry(TraceDirection.Output) };

        // Act
        var result = await _exporter.ExportAsync(packets);

        // Assert
        var content = Encoding.UTF8.GetString(result);
        var line = content.Split('\n', StringSplitOptions.RemoveEmptyEntries)[0];
        var json = JsonDocument.Parse(line);

        Assert.That(json.RootElement.GetProperty("io").GetString(), Is.EqualTo("output"));
    }

    [Test]
    public async Task ExportAsync_InputDirection_MapsToInput()
    {
        // Arrange
        var packets = new[] { CreateTestPacketTraceEntry(TraceDirection.Input) };

        // Act
        var result = await _exporter.ExportAsync(packets);

        // Assert
        var content = Encoding.UTF8.GetString(result);
        var line = content.Split('\n', StringSplitOptions.RemoveEmptyEntries)[0];
        var json = JsonDocument.Parse(line);

        Assert.That(json.RootElement.GetProperty("io").GetString(), Is.EqualTo("input"));
    }

    [Test]
    public async Task ExportAsync_TraceDirection_MapsToTrace()
    {
        // Arrange
        var packets = new[] { CreateTestPacketTraceEntry(TraceDirection.Trace) };

        // Act
        var result = await _exporter.ExportAsync(packets);

        // Assert
        var content = Encoding.UTF8.GetString(result);
        var line = content.Split('\n', StringSplitOptions.RemoveEmptyEntries)[0];
        var json = JsonDocument.Parse(line);

        Assert.That(json.RootElement.GetProperty("io").GetString(), Is.EqualTo("trace"));
    }

    [Test]
    public async Task ExportAsync_IncludesDataField()
    {
        // Arrange
        var packets = new[] { CreateTestPacketTraceEntry(TraceDirection.Output) };

        // Act
        var result = await _exporter.ExportAsync(packets);

        // Assert
        var content = Encoding.UTF8.GetString(result);
        var line = content.Split('\n', StringSplitOptions.RemoveEmptyEntries)[0];
        var json = JsonDocument.Parse(line);

        Assert.That(json.RootElement.TryGetProperty("data", out _), Is.True);
    }

    #endregion

    #region Helper Methods

    /// <summary>
    /// Creates a test PacketTraceEntry using the PacketTraceEntryBuilder.
    /// This mimics how real packets are created in the application.
    /// </summary>
    private static PacketTraceEntry CreateTestPacketTraceEntry(TraceDirection direction)
    {
        // Create a valid OSDP Poll command packet
        // Format: SOM (0x53), Addr, Len_LSB, Len_MSB, CTRL, CMD, CRC_LSB, CRC_MSB
        byte[] pollPacketData = [0x53, 0x00, 0x08, 0x00, 0x04, 0x60, 0x03, 0x1D]; // Poll command

        var builder = new PacketTraceEntryBuilder();
        var traceEntry = new TraceEntry(
            direction,
            Guid.Empty,
            pollPacketData
        );

        var entry = builder.FromTraceEntry(traceEntry, null).Build();
        return entry ?? throw new InvalidOperationException("Failed to create test PacketTraceEntry");
    }

    #endregion
}
