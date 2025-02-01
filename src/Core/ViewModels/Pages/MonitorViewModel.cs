using System.Collections.ObjectModel;
using CommunityToolkit.Mvvm.ComponentModel;
using OSDP.Net.Messages;
using OSDP.Net.Tracing;
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

        _deviceManagementService.ConnectionStatusChange += OnDeviceManagementServiceOnConnectionStatusChange;
        _deviceManagementService.TraceEntryReceived += OnDeviceManagementServiceOnTraceEntryReceived;
    }

    private void OnDeviceManagementServiceOnConnectionStatusChange(object? _, bool isOnline)
    {
        if (isOnline) InitializePollingMetrics();
        
        StatusLevel = isOnline ? StatusLevel.Connected : StatusLevel.Disconnected;
    }

    private void InitializePollingMetrics()
    {
        TraceEntriesView.Clear();
        _lastPacketEntry = null;
        NumberOfPolls = 0;
        NumberOfAcksToPolls = 0;
    }

    private void OnDeviceManagementServiceOnTraceEntryReceived(object? _, TraceEntry traceEntry)
    {
        if (_deviceManagementService.IsUsingSecureChannel) return;
        
        var build = new PacketTraceEntryBuilder();
        var packetTraceEntry = build.FromTraceEntry(traceEntry, _lastPacketEntry).Build();

        bool notDisplaying = false;
        if (packetTraceEntry.Packet.CommandType == CommandType.Poll)
        {
            NumberOfPolls++;
            notDisplaying = true;
        }

        if (_lastPacketEntry?.Packet.CommandType == CommandType.Poll && packetTraceEntry.Packet.ReplyType == ReplyType.Ack)
        {
            NumberOfAcksToPolls++;
            notDisplaying = true;
        }

        _lastPacketEntry = packetTraceEntry;

        if (notDisplaying) return;

        TraceEntriesView.Add(packetTraceEntry);
        if (TraceEntriesView.Count > 20)
        {
            TraceEntriesView.RemoveAt(0);
        }
    }

    [ObservableProperty] private ObservableCollection<PacketTraceEntry> _traceEntriesView = [];
    
    [ObservableProperty] private StatusLevel _statusLevel = StatusLevel.Disconnected;

    [ObservableProperty] private uint _numberOfPolls;
    
    [ObservableProperty] private uint _numberOfAcksToPolls;
}