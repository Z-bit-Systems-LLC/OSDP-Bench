using System.Collections.ObjectModel;
using CommunityToolkit.Mvvm.ComponentModel;
using OSDP.Net.Messages;
using OSDP.Net.Tracing;
using OSDPBench.Core.Models;
using OSDPBench.Core.Services;
using static OSDP.Net.Tracing.TraceDirection;

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

    private void OnDeviceManagementServiceOnConnectionStatusChange(object? _, ConnectionStatus connectionStatus)
    {
        if (connectionStatus == ConnectionStatus.Connected) InitializePollingMetrics();
        
        StatusLevel = connectionStatus == ConnectionStatus.Connected ? StatusLevel.Connected : StatusLevel.Disconnected;
    }

    private void InitializePollingMetrics()
    {
        TraceEntriesView.Clear();
        _lastPacketEntry = null;
    }

    private void OnDeviceManagementServiceOnTraceEntryReceived(object? _, TraceEntry traceEntry)
    {
        UsingSecureChannel = _deviceManagementService.IsUsingSecureChannel;
        
        if (UsingSecureChannel) return;

        var build = new PacketTraceEntryBuilder();
        PacketTraceEntry packetTraceEntry;
        try
        {
            packetTraceEntry = build.FromTraceEntry(traceEntry, _lastPacketEntry).Build();
        }
        catch (Exception)
        {
            return;
        }

        switch (packetTraceEntry.Direction)
        {
            // Flash appropriate LED based on direction
            case Output:
                LastTxActiveTime = DateTime.Now;
                break;
            case Input or Trace:
                LastRxActiveTime = DateTime.Now;
                break;
        }

        bool notDisplaying = packetTraceEntry.Packet.CommandType == CommandType.Poll ||
                             _lastPacketEntry?.Packet.CommandType == CommandType.Poll &&
                             packetTraceEntry.Packet.ReplyType == ReplyType.Ack;

        _lastPacketEntry = packetTraceEntry;

        if (notDisplaying) return;

        TraceEntriesView.Insert(0, packetTraceEntry);
        if (TraceEntriesView.Count > 20)
        {
            TraceEntriesView.RemoveAt(TraceEntriesView.Count - 1);
        }
    }

    [ObservableProperty] private ObservableCollection<PacketTraceEntry> _traceEntriesView = [];
    
    [ObservableProperty] private StatusLevel _statusLevel = StatusLevel.Disconnected;

    [ObservableProperty] private DateTime _lastTxActiveTime;
    
    [ObservableProperty] private DateTime _lastRxActiveTime;
    
    [ObservableProperty] private bool _usingSecureChannel;
}