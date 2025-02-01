using System.Collections.ObjectModel;
using CommunityToolkit.Mvvm.ComponentModel;
using OSDP.Net.Messages;
using OSDPBench.Core.Models;
using OSDPBench.Core.Services;

namespace OSDPBench.Core.ViewModels.Pages;

/// <summary>
/// Represents the ViewModel for the Monitor page, providing bindings and interaction logic for the associated view.
/// </summary>
public partial class MonitorViewModel : ObservableObject
{
    private readonly IDeviceManagementService _deviceManagementService;
    
    private PacketTraceEntry? _lastPacketEntry;

    /// <inheritdoc />
    public MonitorViewModel(IDeviceManagementService deviceManagementService)
    {
        _deviceManagementService = deviceManagementService ??
                                   throw new ArgumentNullException(nameof(deviceManagementService));

        _deviceManagementService.ConnectionStatusChange += (_, status) =>
        {
            if (status)
            {
                TraceEntriesView.Clear();
                _lastPacketEntry = null;
            }
        };
            
        _deviceManagementService.TraceEntryReceived += (_, traceEntry) =>
        {
            var build = new PacketTraceEntryBuilder();
            var packetTraceEntry = build.FromTraceEntry(traceEntry, _lastPacketEntry).Build();

            if (packetTraceEntry.Packet.CommandType == CommandType.Poll ||
                (_lastPacketEntry?.Packet.CommandType == CommandType.Poll && packetTraceEntry.Packet.ReplyType == ReplyType.Ack))
            {
                _lastPacketEntry = packetTraceEntry;
                return;
            }

            _lastPacketEntry = packetTraceEntry;

            TraceEntriesView.Add(packetTraceEntry);
            if (TraceEntriesView.Count > 20)
            {
                TraceEntriesView.RemoveAt(0);
            }
        };
    }

    [ObservableProperty] private ObservableCollection<PacketTraceEntry> _traceEntriesView = [];
}