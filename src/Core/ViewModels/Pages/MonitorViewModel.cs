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

        // Reset statistics
        CommandsSent = 0;
        RepliesReceived = 0;
        Polls = 0;
        Naks = 0;
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

        // Update statistics
        if (traceEntry.Direction == Output)
        {
            CommandsSent++;
            if (packetTraceEntry.Packet.CommandType == CommandType.Poll)
            {
                Polls++;
            }
        }
        else if (traceEntry.Direction == Input)
        {
            RepliesReceived++;
            if (packetTraceEntry.Packet.ReplyType == ReplyType.Nak)
            {
                Naks++;
            }
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

    // Packet Statistics
    [ObservableProperty] private int _commandsSent;

    [ObservableProperty] private int _repliesReceived;

    [ObservableProperty] private int _polls;

    [ObservableProperty] private int _naks;

    /// <summary>
    /// Line quality percentage based on commands sent vs replies received
    /// Accounts for 2 in-flight commands to prevent jumping during normal operation
    /// </summary>
    public double LineQualityPercentage
    {
        get
        {
            if (CommandsSent == 0) return 100.0;

            // Allow for 2 commands to be in-flight without penalizing quality
            int inFlight = CommandsSent - RepliesReceived;

            // If we have more than 2 commands without a reply, count the excess as failures
            int missedReplies = Math.Max(0, inFlight - 2);
            int effectiveCommandsSent = CommandsSent - Math.Min(inFlight, 2);

            if (effectiveCommandsSent == 0) return 100.0;

            int successfulCommands = RepliesReceived;
            return (successfulCommands / (double)(successfulCommands + missedReplies)) * 100.0;
        }
    }

    partial void OnCommandsSentChanged(int value)
    {
        _ = value; // Intentionally unused - only triggering dependent property notification
        OnPropertyChanged(nameof(LineQualityPercentage));
    }

    partial void OnRepliesReceivedChanged(int value)
    {
        _ = value; // Intentionally unused - only triggering dependent property notification
        OnPropertyChanged(nameof(LineQualityPercentage));
    }
}