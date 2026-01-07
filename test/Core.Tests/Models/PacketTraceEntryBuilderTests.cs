using System;
using NUnit.Framework;
using OSDP.Net.Messages;
using OSDP.Net.Tracing;
using OSDPBench.Core.Models;

namespace OSDPBench.Core.Tests.Models;

[TestFixture(TestOf = typeof(PacketTraceEntryBuilder))]
public class PacketTraceEntryBuilderTests
{
    private PacketTraceEntryBuilder _builder;

    // Valid OSDP Poll command packet: SOM, Addr, Len_LSB, Len_MSB, CTRL, CMD, CRC_LSB, CRC_MSB
    private static readonly byte[] ValidPollPacket = [0x53, 0x00, 0x08, 0x00, 0x04, 0x60, 0x03, 0x1D];

    // Valid OSDP ACK reply packet
    private static readonly byte[] ValidAckPacket = [0x53, 0x00, 0x08, 0x00, 0x04, 0x40, 0x53, 0x4D];

    // Invalid packet data (too short, wrong checksum)
    private static readonly byte[] InvalidPacket = [0x53, 0x00, 0x05];

    [SetUp]
    public void Setup()
    {
        _builder = new PacketTraceEntryBuilder();
    }

    /// <summary>
    /// Helper to create a TraceEntry with the correct signature.
    /// </summary>
    private static TraceEntry CreateTraceEntry(TraceDirection direction, byte[] data)
    {
        return new TraceEntry(direction, Guid.Empty, data);
    }

    #region Constructor Tests

    [Test]
    public void Constructor_DefaultConstructor_CreatesInstance()
    {
        // Act
        var builder = new PacketTraceEntryBuilder();

        // Assert
        Assert.That(builder, Is.Not.Null);
    }

    [Test]
    public void Constructor_WithMessageSpy_CreatesInstance()
    {
        // Arrange
        var messageSpy = new MessageSpy();

        // Act
        var builder = new PacketTraceEntryBuilder(messageSpy);

        // Assert
        Assert.That(builder, Is.Not.Null);
    }

    #endregion

    #region WithSecurityKey Tests

    [Test]
    public void WithSecurityKey_NullKey_ReturnsBuilder()
    {
        // Act
        var result = _builder.WithSecurityKey(null);

        // Assert
        Assert.That(result, Is.SameAs(_builder));
    }

    [Test]
    public void WithSecurityKey_ValidKey_ReturnsBuilder()
    {
        // Arrange
        byte[] key = [0x01, 0x02, 0x03, 0x04, 0x05, 0x06, 0x07, 0x08,
                      0x09, 0x0A, 0x0B, 0x0C, 0x0D, 0x0E, 0x0F, 0x10];

        // Act
        var result = _builder.WithSecurityKey(key);

        // Assert
        Assert.That(result, Is.SameAs(_builder));
    }

    [Test]
    public void WithSecurityKey_AllowsMethodChaining()
    {
        // Arrange
        byte[] key = [0x01, 0x02, 0x03, 0x04, 0x05, 0x06, 0x07, 0x08,
                      0x09, 0x0A, 0x0B, 0x0C, 0x0D, 0x0E, 0x0F, 0x10];
        var traceEntry = CreateTraceEntry(TraceDirection.Output, ValidPollPacket);

        // Act - Chain methods together
        var result = _builder
            .WithSecurityKey(key)
            .FromTraceEntry(traceEntry, null)
            .Build();

        // Assert
        Assert.That(result, Is.Not.Null);
    }

    #endregion

    #region FromTraceEntry Tests

    [Test]
    public void FromTraceEntry_ValidTraceEntry_ReturnsBuilder()
    {
        // Arrange
        var traceEntry = CreateTraceEntry(TraceDirection.Output, ValidPollPacket);

        // Act
        var result = _builder.FromTraceEntry(traceEntry, null);

        // Assert
        Assert.That(result, Is.SameAs(_builder));
    }

    [Test]
    public void FromTraceEntry_WithLastTraceEntry_ReturnsBuilder()
    {
        // Arrange
        var firstTraceEntry = CreateTraceEntry(TraceDirection.Output, ValidPollPacket);
        var firstEntry = _builder.FromTraceEntry(firstTraceEntry, null).Build();

        var secondTraceEntry = CreateTraceEntry(TraceDirection.Input, ValidPollPacket);

        // Act
        var result = _builder.FromTraceEntry(secondTraceEntry, firstEntry);

        // Assert
        Assert.That(result, Is.SameAs(_builder));
    }

    #endregion

    #region Build Tests

    [Test]
    public void Build_ValidPollPacket_ReturnsPacketTraceEntry()
    {
        // Arrange
        var traceEntry = CreateTraceEntry(TraceDirection.Output, ValidPollPacket);

        // Act
        var result = _builder.FromTraceEntry(traceEntry, null).Build();

        // Assert
        Assert.That(result, Is.Not.Null);
    }

    [Test]
    public void Build_ValidPacket_SetsDirection()
    {
        // Arrange
        var traceEntry = CreateTraceEntry(TraceDirection.Output, ValidPollPacket);

        // Act
        var result = _builder.FromTraceEntry(traceEntry, null).Build();

        // Assert
        Assert.That(result!.Direction, Is.EqualTo(TraceDirection.Output));
    }

    [Test]
    public void Build_ValidPacket_SetsTimestamp()
    {
        // Arrange
        var timestamp = DateTime.UtcNow;
        var traceEntry = CreateTraceEntry(TraceDirection.Output, ValidPollPacket);
        var beforeBuild = DateTime.UtcNow;

        // Act
        var result = _builder.FromTraceEntry(traceEntry, null).Build();
        var afterBuild = DateTime.UtcNow;

        // Assert - Timestamp should be set to current time during Build
        Assert.That(result!.Timestamp, Is.GreaterThanOrEqualTo(beforeBuild));
        Assert.That(result.Timestamp, Is.LessThanOrEqualTo(afterBuild));
    }

    [Test]
    public void Build_ValidPacket_SetsRawData()
    {
        // Arrange
        var traceEntry = CreateTraceEntry(TraceDirection.Output, ValidPollPacket);

        // Act
        var result = _builder.FromTraceEntry(traceEntry, null).Build();

        // Assert
        Assert.That(result!.RawData, Is.EqualTo(ValidPollPacket));
    }

    [Test]
    public void Build_FirstPacket_SetsIntervalToZero()
    {
        // Arrange
        var traceEntry = CreateTraceEntry(TraceDirection.Output, ValidPollPacket);

        // Act
        var result = _builder.FromTraceEntry(traceEntry, null).Build();

        // Assert
        Assert.That(result!.Interval, Is.EqualTo(TimeSpan.Zero));
    }

    [Test]
    public void Build_SubsequentPacket_CalculatesInterval()
    {
        // Arrange
        var firstTraceEntry = CreateTraceEntry(TraceDirection.Output, ValidPollPacket);
        var firstEntry = _builder.FromTraceEntry(firstTraceEntry, null).Build();

        // Wait a bit to create measurable interval
        var delay = TimeSpan.FromMilliseconds(100);
        System.Threading.Thread.Sleep(delay);

        var secondTraceEntry = CreateTraceEntry(TraceDirection.Input, ValidPollPacket);

        // Act
        var result = _builder.FromTraceEntry(secondTraceEntry, firstEntry).Build();

        // Assert - Interval should be approximately the delay
        Assert.That(result!.Interval.TotalMilliseconds, Is.GreaterThanOrEqualTo(delay.TotalMilliseconds - 10));
    }

    [Test]
    public void Build_InvalidPacket_ReturnsNull()
    {
        // Arrange
        var traceEntry = CreateTraceEntry(TraceDirection.Output, InvalidPacket);

        // Act
        var result = _builder.FromTraceEntry(traceEntry, null).Build();

        // Assert
        Assert.That(result, Is.Null);
    }

    [Test]
    public void Build_PollCommand_SetsCorrectType()
    {
        // Arrange
        var traceEntry = CreateTraceEntry(TraceDirection.Output, ValidPollPacket);

        // Act
        var result = _builder.FromTraceEntry(traceEntry, null).Build();

        // Assert - Poll command should have CommandType.Poll
        Assert.That(result!.Packet.CommandType, Is.EqualTo(CommandType.Poll));
    }

    [Test]
    public void Build_ReplyPacket_ParsesSuccessfully()
    {
        // Arrange - Using the same Poll packet structure but treating it as input
        // This tests that the builder can handle input direction packets
        var traceEntry = CreateTraceEntry(TraceDirection.Input, ValidPollPacket);

        // Act
        var result = _builder.FromTraceEntry(traceEntry, null).Build();

        // Assert - Should successfully parse and set direction
        Assert.That(result, Is.Not.Null);
        Assert.That(result!.Direction, Is.EqualTo(TraceDirection.Input));
    }

    [Test]
    public void Build_CanBeCalledMultipleTimes()
    {
        // Arrange
        var traceEntry1 = CreateTraceEntry(TraceDirection.Output, ValidPollPacket);
        var traceEntry2 = CreateTraceEntry(TraceDirection.Input, ValidPollPacket);

        // Act
        var result1 = _builder.FromTraceEntry(traceEntry1, null).Build();
        var result2 = _builder.FromTraceEntry(traceEntry2, result1).Build();

        // Assert
        Assert.That(result1, Is.Not.Null);
        Assert.That(result2, Is.Not.Null);
        Assert.That(result1, Is.Not.SameAs(result2));
    }

    #endregion

    #region PacketTraceEntry Property Tests

    [Test]
    public void Build_ValidPacket_TypePropertyReturnsDisplayName()
    {
        // Arrange
        var traceEntry = CreateTraceEntry(TraceDirection.Output, ValidPollPacket);

        // Act
        var result = _builder.FromTraceEntry(traceEntry, null).Build();

        // Assert - Type should return the display name
        Assert.That(result!.Type, Is.Not.Null.And.Not.Empty);
        Assert.That(result.Type, Is.Not.EqualTo("Unknown"));
    }

    [Test]
    public void Build_ValidPacket_LocalTimestampReturnsLocalTime()
    {
        // Arrange
        var traceEntry = CreateTraceEntry(TraceDirection.Output, ValidPollPacket);

        // Act
        var result = _builder.FromTraceEntry(traceEntry, null).Build();

        // Assert
        Assert.That(result!.LocalTimestamp.Kind, Is.EqualTo(DateTimeKind.Local));
    }

    [Test]
    public void Build_NonSecurePacket_IsSecureMessageReturnsFalse()
    {
        // Arrange
        var traceEntry = CreateTraceEntry(TraceDirection.Output, ValidPollPacket);

        // Act
        var result = _builder.FromTraceEntry(traceEntry, null).Build();

        // Assert
        Assert.That(result!.IsSecureMessage, Is.False);
    }

    #endregion
}
