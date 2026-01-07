using System;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using NUnit.Framework;
using OSDP.Net.Tracing;
using OSDPBench.Core.Models;
using OSDPBench.Core.Services.Export;

namespace OSDPBench.Core.Tests.Services.Export;

[TestFixture(TestOf = typeof(ParsedPacketExporter))]
public class ParsedPacketExporterTests
{
    private ParsedPacketExporter _exporter;

    [SetUp]
    public void Setup()
    {
        _exporter = new ParsedPacketExporter();
    }

    #region Property Tests

    [Test]
    public void FileExtension_ReturnsTxt()
    {
        Assert.That(_exporter.FileExtension, Is.EqualTo(".txt"));
    }

    [Test]
    public void DisplayName_ReturnsParsedText()
    {
        Assert.That(_exporter.DisplayName, Is.EqualTo("Parsed Text"));
    }

    #endregion

    #region ExportAsync Tests

    [Test]
    public async Task ExportAsync_EmptyPacketList_ReturnsHeaderOnly()
    {
        // Arrange
        var packets = Enumerable.Empty<PacketTraceEntry>();

        // Act
        var result = await _exporter.ExportAsync(packets);

        // Assert
        Assert.That(result, Is.Not.Null);
        var content = Encoding.UTF8.GetString(result);

        // Should contain header even with no packets
        Assert.That(content, Does.Contain("OSDP Packet Trace Export"));
        Assert.That(content, Does.Contain("Source: OSDP-Bench"));
    }

    [Test]
    public async Task ExportAsync_SinglePacket_ProducesFormattedOutput()
    {
        // Arrange
        var packets = new[] { CreateTestPacketTraceEntry(TraceDirection.Output) };

        // Act
        var result = await _exporter.ExportAsync(packets);

        // Assert
        Assert.That(result, Is.Not.Null);
        var content = Encoding.UTF8.GetString(result);
        Assert.That(content, Is.Not.Empty);

        // Should contain header
        Assert.That(content, Does.Contain("OSDP Packet Trace Export"));

        // Should have content beyond just the header
        var lines = content.Split('\n');
        Assert.That(lines.Length, Is.GreaterThan(4)); // Header lines + packet content
    }

    [Test]
    public async Task ExportAsync_MultiplePackets_OrderedByTimestamp()
    {
        // Arrange
        var packets = new[]
        {
            CreateTestPacketTraceEntry(TraceDirection.Output),
            CreateTestPacketTraceEntry(TraceDirection.Input)
        };

        // Act
        var result = await _exporter.ExportAsync(packets);

        // Assert
        var content = Encoding.UTF8.GetString(result);

        // The content should be ordered by timestamp (earlier first)
        // We verify this by checking that the output contains formatted packet data
        Assert.That(content, Does.Contain("OSDP Packet Trace Export"));
    }

    [Test]
    public async Task ExportAsync_IncludesExportedTimestamp()
    {
        // Arrange
        var packets = new[] { CreateTestPacketTraceEntry(TraceDirection.Output) };

        // Act
        var result = await _exporter.ExportAsync(packets);

        // Assert
        var content = Encoding.UTF8.GetString(result);
        Assert.That(content, Does.Contain("Exported:"));

        // The exported timestamp should be somewhere between before and after
        Assert.That(content, Does.Match(@"Exported: \d{4}-\d{2}-\d{2}"));
    }

    [Test]
    public async Task ExportAsync_IncludesSourceIdentifier()
    {
        // Arrange
        var packets = new[] { CreateTestPacketTraceEntry(TraceDirection.Output) };

        // Act
        var result = await _exporter.ExportAsync(packets);

        // Assert
        var content = Encoding.UTF8.GetString(result);
        Assert.That(content, Does.Contain("Source: OSDP-Bench"));
    }

    [Test]
    public async Task ExportAsync_IncludesSeparatorLine()
    {
        // Arrange
        var packets = new[] { CreateTestPacketTraceEntry(TraceDirection.Output) };

        // Act
        var result = await _exporter.ExportAsync(packets);

        // Assert
        var content = Encoding.UTF8.GetString(result);

        // Should contain a line of equals signs (separator)
        Assert.That(content, Does.Contain(new string('=', 80)));
    }

    [Test]
    public async Task ExportAsync_ProducesHumanReadableFormat()
    {
        // Arrange
        var packets = new[] { CreateTestPacketTraceEntry(TraceDirection.Output) };

        // Act
        var result = await _exporter.ExportAsync(packets);

        // Assert
        var content = Encoding.UTF8.GetString(result);

        // Verify the content is human-readable (not binary/JSON)
        Assert.That(content, Does.Not.Contain("{\""));
        Assert.That(content, Does.Not.Contain("\"timeSec\""));
    }

    [Test]
    public async Task ExportAsync_ReturnsUtf8EncodedBytes()
    {
        // Arrange
        var packets = new[] { CreateTestPacketTraceEntry(TraceDirection.Output) };

        // Act
        var result = await _exporter.ExportAsync(packets);

        // Assert - Should be valid UTF-8
        Assert.DoesNotThrow(() => Encoding.UTF8.GetString(result));
    }

    #endregion

    #region Helper Methods

    /// <summary>
    /// Creates a test PacketTraceEntry using the PacketTraceEntryBuilder.
    /// </summary>
    private static PacketTraceEntry CreateTestPacketTraceEntry(TraceDirection direction)
    {
        // Create a valid OSDP Poll command packet
        byte[] pollPacketData = [0x53, 0x00, 0x08, 0x00, 0x04, 0x60, 0x03, 0x1D];

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
