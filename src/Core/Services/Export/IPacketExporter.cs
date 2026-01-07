using OSDPBench.Core.Models;

namespace OSDPBench.Core.Services.Export;

/// <summary>
/// Interface for exporting packet trace data to various formats.
/// </summary>
public interface IPacketExporter
{
    /// <summary>
    /// Gets the file extension for the exported file (including the dot, e.g., ".osdpcap").
    /// </summary>
    string FileExtension { get; }

    /// <summary>
    /// Gets the display name for the export format (e.g., "OSDP Capture File").
    /// </summary>
    string DisplayName { get; }

    /// <summary>
    /// Exports the given packet trace entries to a byte array.
    /// </summary>
    /// <param name="packets">The packet trace entries to export.</param>
    /// <returns>A byte array containing the exported data.</returns>
    Task<byte[]> ExportAsync(IEnumerable<PacketTraceEntry> packets);
}
