using System;
using Moq;
using OSDP.Net.Tracing;
using OSDPBench.Core.Services;

namespace OSDPBench.Core.Tests.Helpers;

/// <summary>
/// Shared factory for creating TraceEntry instances and raising trace events via Moq.
/// Packet bytes are generated with correct OSDP CRC-16/AUG-CCITT checksums.
/// </summary>
public static class TraceEntryTestHelper
{
    /// <summary>
    /// Valid OSDP Poll command packet (osdp_POLL 0x60).
    /// SOM=0x53, Addr=0x00, Len=0x08, CTRL=0x04 (CRC, seq=0), CMD=0x60, CRC16
    /// </summary>
    public static readonly byte[] ValidPollPacket = [0x53, 0x00, 0x08, 0x00, 0x04, 0x60, 0xEB, 0xAA];

    /// <summary>
    /// Valid OSDP ACK reply packet (osdp_ACK 0x40).
    /// SOM=0x53, Addr=0x80, Len=0x08, CTRL=0x04 (CRC, seq=0), Reply=0x40, CRC16
    /// </summary>
    public static readonly byte[] ValidAckPacket = [0x53, 0x80, 0x08, 0x00, 0x04, 0x40, 0x59, 0xAC];

    /// <summary>
    /// Valid OSDP NAK reply packet (osdp_NAK 0x41) with error code 0x01.
    /// SOM=0x53, Addr=0x80, Len=0x09, CTRL=0x04 (CRC, seq=0), Reply=0x41, ErrCode=0x01, CRC16
    /// </summary>
    public static readonly byte[] ValidNakPacket = [0x53, 0x80, 0x09, 0x00, 0x04, 0x41, 0x01, 0x27, 0xA4];

    /// <summary>
    /// Valid OSDP ID Report command packet (osdp_ID 0x61).
    /// SOM=0x53, Addr=0x00, Len=0x09, CTRL=0x04, CMD=0x61, Data=0x00, CRC16
    /// </summary>
    public static readonly byte[] ValidIdReportPacket = [0x53, 0x00, 0x09, 0x00, 0x04, 0x61, 0x00, 0xC0, 0x66];

    /// <summary>
    /// Invalid/malformed packet data (too short to be a valid OSDP packet).
    /// </summary>
    public static readonly byte[] InvalidPacket = [0x53, 0x00, 0x05];

    /// <summary>
    /// Creates a TraceEntry with the specified direction and data.
    /// </summary>
    public static TraceEntry CreateTraceEntry(TraceDirection direction, byte[] data)
    {
        return new TraceEntry(direction, Guid.Empty, data);
    }

    /// <summary>
    /// Raises the TraceEntryReceived event on a mocked IDeviceManagementService.
    /// </summary>
    public static void RaiseTraceEntryEvent(Mock<IDeviceManagementService> mock, TraceEntry traceEntry)
    {
        mock.Raise(d => d.TraceEntryReceived += null!, EventArgs.Empty, traceEntry);
    }

    /// <summary>
    /// Creates a TraceEntry and raises it on the mocked service in one call.
    /// </summary>
    public static void RaiseTraceEntry(Mock<IDeviceManagementService> mock, TraceDirection direction, byte[] data)
    {
        var entry = CreateTraceEntry(direction, data);
        RaiseTraceEntryEvent(mock, entry);
    }
}
