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
    private readonly PacketTraceEntryBuilder _traceEntryBuilder = new();

    private PacketTraceEntry? _lastPacketEntry;
    private byte[]? _lastConfiguredSecurityKey;
    private bool _securityKeyConfigured;
    private bool _wasConnected;

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
        // Only clear trace when transitioning from connected to disconnected (session ended)
        if (connectionStatus == ConnectionStatus.Disconnected && _wasConnected)
        {
            InitializePollingMetrics();
            _wasConnected = false;
        }
        else if (connectionStatus == ConnectionStatus.Connected)
        {
            _wasConnected = true;
        }

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

        // Configure security key on first trace entry or when key changes
        // This must happen before processing any packets so MessageSpy can track secure channel state
        var currentKey = _deviceManagementService.SecurityKey;
        if (!_securityKeyConfigured || !SecurityKeysEqual(_lastConfiguredSecurityKey, currentKey))
        {
            _traceEntryBuilder.WithSecurityKey(currentKey);
            _lastConfiguredSecurityKey = currentKey;
            _securityKeyConfigured = true;
        }

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

        PacketTraceEntry? packetTraceEntry;
        try
        {
            packetTraceEntry = _traceEntryBuilder.FromTraceEntry(traceEntry, _lastPacketEntry).Build();
        }
        catch (Exception)
        {
            return;
        }

        // If parsing failed, skip this packet but still count it for statistics
        if (packetTraceEntry == null)
        {
            if (traceEntry.Direction == Output)
            {
                CommandsSent++;
            }
            else if (traceEntry.Direction == Input)
            {
                RepliesReceived++;
            }
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

        // Filter out duplicate NAK messages
        if (packetTraceEntry.Packet.ReplyType == ReplyType.Nak && TraceEntriesView.Count > 0)
        {
            var lastDisplayedEntry = TraceEntriesView[0];
            if (lastDisplayedEntry.Packet.ReplyType == ReplyType.Nak &&
                lastDisplayedEntry.Details == packetTraceEntry.Details)
            {
                // Skip adding duplicate NAK message
                return;
            }
        }

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

    private static bool SecurityKeysEqual(byte[]? key1, byte[]? key2)
    {
        if (key1 == null && key2 == null) return true;
        if (key1 == null || key2 == null) return false;
        if (key1.Length != key2.Length) return false;
        return key1.AsSpan().SequenceEqual(key2);
    }
}