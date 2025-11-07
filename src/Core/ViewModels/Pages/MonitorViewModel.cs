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
        
        UpdateConnectionInfo();
        StatusLevel = _deviceManagementService.IsConnected ? StatusLevel.Connected : StatusLevel.Disconnected;

        _deviceManagementService.ConnectionStatusChange += OnDeviceManagementServiceOnConnectionStatusChange;
        _deviceManagementService.TraceEntryReceived += OnDeviceManagementServiceOnTraceEntryReceived;
    }

    private void OnDeviceManagementServiceOnConnectionStatusChange(object? _, ConnectionStatus connectionStatus)
    {
        if (connectionStatus == ConnectionStatus.Connected) InitializePollingMetrics();

        UpdateConnectionInfo();

        switch (connectionStatus)
        {
            case ConnectionStatus.Connected:
                StatusLevel = StatusLevel.Connected;
                break;
            case ConnectionStatus.InvalidSecurityKey:
                StatusLevel = StatusLevel.Error;
                break;
            default:
                StatusLevel = StatusLevel.Disconnected;
                break;
        }
    }

    private void UpdateConnectionInfo()
    {
        ConnectedAddress = _deviceManagementService.Address;
        ConnectedBaudRate = _deviceManagementService.BaudRate;
    }

    private void InitializePollingMetrics()
    {
        TraceEntriesView.Clear();
        _lastPacketEntry = null;
    }

    private void OnDeviceManagementServiceOnTraceEntryReceived(object? _, TraceEntry traceEntry)
    {
        UsingSecureChannel = _deviceManagementService.IsUsingSecureChannel;

        // Update activity indicators based on raw trace entry direction (works for encrypted packets too)
        switch (traceEntry.Direction)
        {
            // Flash appropriate LED based on direction
            case Output:
                LastTxActiveTime = DateTime.Now;
                break;
            case Input or Trace:
                LastRxActiveTime = DateTime.Now;
                break;
        }

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
    
    [ObservableProperty] private byte _connectedAddress;

    [ObservableProperty] private uint _connectedBaudRate;
}