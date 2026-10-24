using System.Collections;
using System.Text;
using OSDP.Net;
using OSDP.Net.Connections;
using OSDP.Net.Messages;
using OSDP.Net.Model.ReplyData;
using OSDP.Net.PanelCommands.DeviceDiscover;
using OSDP.Net.Tracing;
using OSDPBench.Core.Actions;
using OSDPBench.Core.Models;

namespace OSDPBench.Core.Services;

/// <summary>
/// Class DeviceManagementService.
/// Implements the <see cref="IDeviceManagementService" />
/// </summary>
/// <seealso cref="IDeviceManagementService" />
public sealed class DeviceManagementService : IDeviceManagementService
{
    private readonly ControlPanel _panel = new();
    private readonly SynchronizationContext? _synchronizationContext;
    private readonly TimeSpan _defaultPollInterval = TimeSpan.FromMilliseconds(20);
    private readonly TimeSpan _defaultShutdownTimeout = TimeSpan.FromSeconds(5);
    private readonly TimeSpan _defaultResponseTimeout = TimeSpan.FromSeconds(1);

    private Guid _connectionId;
    private bool _isDiscovering;
    private bool _isPortInUse;
    private bool _invalidSecurityKey;
    private bool _isRestartingInClearText;
    private bool _isSecurityKeyRejected;
    private int _clientCryptogramReplies;
    private byte[]? _securityKey;
    private IOsdpConnection? _connection;

    // Passive monitoring state
    private CancellationTokenSource? _passiveMonitoringCts;
    private IOsdpConnection? _passiveConnection;
    private Task? _passiveMonitoringTask;

    /// <summary>
    /// Initializes a new instance of the <see cref="DeviceManagementService"/> class.
    /// </summary>
    /// <exception cref="ArgumentNullException">panel</exception>
    public DeviceManagementService()
    {
        _synchronizationContext = SynchronizationContext.Current;
        
        _panel.ConnectionStatusChanged += (_, args) =>
        {
            if (_isDiscovering || _isRestartingInClearText) return;

            // Once the PD has rejected the security key, the link only exists to read the ID
            if (_isSecurityKeyRejected && args.IsConnected) return;

            _clientCryptogramReplies = 0;
            IsConnected = args.IsConnected;
            if (IsConnected)
            {
                Task.Run(async () =>
                {
                    IdentityLookup = new IdentityLookup(await _panel.IdReport(_connectionId, Address));
                    CapabilitiesLookup =
                        new CapabilitiesLookup(await _panel.DeviceCapabilities(_connectionId, Address));

                    RaiseEvent(DeviceLookupsChanged);
                });
            }
            else
            {
                IdentityLookup = null;
                CapabilitiesLookup = null;

                RaiseEvent(DeviceLookupsChanged);
            }
            
            _invalidSecurityKey = false;

            RaiseEvent(ConnectionStatusChange, args.IsConnected ? ConnectionStatus.Connected : ConnectionStatus.Disconnected);
        };

        _panel.NakReplyReceived += (_, args) =>
        {
            RaiseEvent(NakReplyReceived, ToFormattedText(args.Nak.ErrorCode));
            if (args.Nak.ErrorCode == ErrorCode.CommunicationSecurityNotMet)
            {
                OnSecurityKeyRejected();
            }
        };

        _panel.RawCardDataReplyReceived += (_, args) =>
        {
            RaiseEvent(CardReadReceived, FormatData(args.RawCardData.Data));
        };
        _panel.KeypadReplyReceived += (_, args) => RaiseEvent(KeypadReadReceived, FormatKeypadData(args.KeypadData.Data));
        _panel.LocalStatusReportReplyReceived += (_, args) =>
        {
            RaiseEvent(LocalStatusReceived, new LocalStatusEventArgs(args.LocalStatus.Tamper, args.LocalStatus.PowerFailure));
        };
    }

    /// <inheritdoc />
    public IdentityLookup? IdentityLookup { get; private set; }

    /// <inheritdoc />
    public CapabilitiesLookup? CapabilitiesLookup { get; private set; }

    /// <inheritdoc />
    public string? PortName { get; set; }

    /// <inheritdoc />
    public byte Address { get; private set; }

    /// <inheritdoc />
    public uint BaudRate { get; private set; }
    
    /// <inheritdoc />
    public bool IsUsingSecureChannel { get; private set; }

    /// <inheritdoc />
    public bool UsesDefaultSecurityKey { get; private set; }

    /// <inheritdoc />
    public byte[]? SecurityKey => _securityKey;

    /// <inheritdoc />
    public bool IsConnected { get; private set; }

    /// <inheritdoc />
    public bool IsPassiveMonitoring => _passiveMonitoringTask != null && !_passiveMonitoringTask.IsCompleted;

    /// <inheritdoc />
    public bool IsPortInUse => _isPortInUse;

    /// <inheritdoc />
    public event EventHandler? PortInUseChanged;

    /// <summary>
    /// Records whether a port is held, from the moment one is opened until it is released.
    /// </summary>
    private void SetPortInUse(bool inUse)
    {
        if (_isPortInUse == inUse) return;

        _isPortInUse = inUse;
        RaiseEvent(PortInUseChanged);
    }

    /// <inheritdoc />
    public async Task Connect(IOsdpConnection connection, byte address, bool useSecureChannel,
        bool useDefaultSecurityKey, byte[]? securityKey)
    {
        await Shutdown();
        
        Address = address;
        BaudRate = (uint)connection.BaudRate;
        IsUsingSecureChannel = useSecureChannel;
        UsesDefaultSecurityKey = useDefaultSecurityKey;
        _securityKey = securityKey;
        _connection = connection;

        _connectionId = _panel.StartConnection(connection, _defaultPollInterval, Tracer);
        _panel.AddDevice(_connectionId, address, true, useSecureChannel,
            useDefaultSecurityKey ? null : securityKey);

        SetPortInUse(true);
    }

    /// <summary>
    /// Reports an invalid security key and reads the device identity when the PD rejects the
    /// security of a link that has not come online.
    /// </summary>
    private void OnSecurityKeyRejected()
    {
        // Discovery expects a rejection while it checks for the default key
        if (_isDiscovering || IsConnected || _invalidSecurityKey) return;

        // Only the first rejection is on a secure channel link, later ones are on the clear text fallback
        bool restartInClearText = IsUsingSecureChannel && !_isSecurityKeyRejected;

        _invalidSecurityKey = true;
        _isSecurityKeyRejected = true;
        RaiseEvent(ConnectionStatusChange, ConnectionStatus.InvalidSecurityKey);
        Task.Run(() => ReadIdentityWithoutSecurityKey(restartInClearText));
    }

    /// <summary>
    /// Reads the device identity after the PD has rejected the security key. A secure channel
    /// connection keeps retrying the handshake and never sends the ID request, so it is restarted
    /// in clear text first. The secure channel settings are kept so a reconnect tries them again.
    /// </summary>
    /// <param name="restartInClearText">Restart the connection in clear text before reading the ID.</param>
    private async Task ReadIdentityWithoutSecurityKey(bool restartInClearText)
    {
        try
        {
            if (restartInClearText && _connection != null)
            {
                var connectionId = _connectionId;
                _isRestartingInClearText = true;
                try
                {
                    await _panel.Shutdown();

                    // The user disconnected or started another connection in the meantime
                    if (_connectionId != connectionId || !_isPortInUse) return;

                    _connectionId = _panel.StartConnection(_connection, _defaultPollInterval, Tracer);
                    _panel.AddDevice(_connectionId, Address, true, false);
                }
                finally
                {
                    _isRestartingInClearText = false;
                }
            }

            IdentityLookup = new IdentityLookup(await _panel.IdReport(_connectionId, Address));

            RaiseEvent(DeviceLookupsChanged);
        }
        catch (Exception)
        {
            // Some PDs refuse to identify themselves without a secure channel
        }
    }

    private void Tracer(TraceEntry traceEntry)
    {
        RaiseEvent(TraceEntryReceived, traceEntry);

        // A matching key establishes the secure channel on the first osdp_CCRYPT. OSDP.Net discards
        // one with a mismatched cryptogram and challenges again, without any NAK from the PD.
        if (traceEntry.Direction == TraceDirection.Input && IsUsingSecureChannel && !_isSecurityKeyRejected &&
            IsClientCryptogramReply(traceEntry.Data) && ++_clientCryptogramReplies > 1)
        {
            OnSecurityKeyRejected();
        }
    }

    /// <summary>
    /// Determines whether a packet is an osdp_CCRYPT reply, which is never encrypted so its reply
    /// code directly follows the security control block.
    /// </summary>
    private static bool IsClientCryptogramReply(byte[] packet)
    {
        const int controlIndex = 4;
        const byte securityControlBlockFlag = 0x08;

        if (packet.Length <= controlIndex + 1 || (packet[controlIndex] & securityControlBlockFlag) == 0) return false;

        int replyCodeIndex = controlIndex + 1 + packet[controlIndex + 1];
        return replyCodeIndex < packet.Length && packet[replyCodeIndex] == (byte)ReplyType.CrypticData;
    }

    /// <inheritdoc />
    public async Task<DiscoveryResult> DiscoverDevice(IEnumerable<IOsdpConnection> connections,
        DiscoveryProgress progress, CancellationToken cancellationToken)
    {
        IdentityLookup = null;
        CapabilitiesLookup = null;
        RaiseEvent(DeviceLookupsChanged);

        await Shutdown();

        _isDiscovering = true;
        SetPortInUse(true);
        bool connectionStarted = false;
        DiscoveryResult results;
        try
        {
            results = await DiscoveryRoutines(connections, progress, cancellationToken);

            if (results == null)
            {
                throw new Exception("Unable to discover device");
            }

            if (results.Status != DiscoveryStatus.Succeeded) return results;

            Address = results.Address;
            BaudRate = (uint)results.Connection.BaudRate;
            IdentityLookup = new IdentityLookup(results.Id);
            CapabilitiesLookup = new CapabilitiesLookup(results.Capabilities);
            UsesDefaultSecurityKey = results.UsesDefaultSecurityKey;
            // Don't auto-enable secure channel after discovery - let user choose
            IsUsingSecureChannel = false;

            RaiseEvent(DeviceLookupsChanged);

            _connectionId = _panel.StartConnection(results.Connection, _defaultPollInterval, Tracer);
            // Use 5-parameter overload to explicitly disable secure channel after discovery
            _panel.AddDevice(_connectionId, Address, CapabilitiesLookup.CRC, false);
            connectionStarted = true;

            return results;
        }
        finally
        {
            _isDiscovering = false;

            // A sweep that found nothing, was cancelled, or threw leaves no connection behind, so
            // the port goes back on the shelf. Only a sweep that went on to start a connection
            // still holds one.
            SetPortInUse(connectionStarted);
        }
    }

    private async Task<DiscoveryResult> DiscoveryRoutines(IEnumerable<IOsdpConnection> connections,
        DiscoveryProgress progress, CancellationToken cancellationToken)
    {
        var options = new DiscoveryOptions
        {
            ProgressCallback = progress,
            ResponseTimeout = _defaultResponseTimeout,
            CancellationToken = cancellationToken
        };

        var results = await _panel.DiscoverDevice(connections, options);

        return results;
    }

    private static string ToFormattedText(ErrorCode value)
    {
        var builder = new StringBuilder();

        foreach (var character in value.ToString())
        {
            if (char.IsUpper(character))
            {
                builder.Append(" ");
            }

            builder.Append(character);
        }

        return builder.ToString().TrimStart();
    }

    /// <inheritdoc />
    public Task<object> ExecuteDeviceAction(IDeviceAction deviceAction, object? parameter)
    {
        return deviceAction.PerformAction(_panel, _connectionId, Address, parameter);
    }

    /// <inheritdoc />
    public async Task Shutdown()
    {
        await Shutdown(waitForOffline: true);
    }

    private async Task Shutdown(bool waitForOffline)
    {
        IdentityLookup = null;
        CapabilitiesLookup = null;
        _isSecurityKeyRejected = false;
        _clientCryptogramReplies = 0;

        await _panel.Shutdown();

        SetPortInUse(false);

        if (waitForOffline)
        {
            await WaitUntilDeviceIsOffline();
        }
    }

    private async Task WaitUntilDeviceIsOffline()
    {
        // Skip waiting if we never had a valid connection
        if (_connectionId == Guid.Empty)
        {
            return;
        }

        using var cts = new CancellationTokenSource(_defaultShutdownTimeout);

        // Check if the connection exists before querying its status
        try
        {
            while (_panel.IsOnline(_connectionId, Address))
            {
                if (cts.Token.IsCancellationRequested)
                {
                    throw new TimeoutException("The device did not go offline within the specified timeout.");
                }

                await Task.Delay(TimeSpan.FromMilliseconds(100), cts.Token);
            }
        }
        catch (KeyNotFoundException)
        {
            // Connection was already removed from the panel, which is fine during shutdown
            return;
        }

        await Task.Delay(TimeSpan.FromSeconds(1), cts.Token);
    }

    private async Task WaitUntilDeviceIsOnline()
    {
        // Skip waiting if we don't have a valid connection
        if (_connectionId == Guid.Empty)
        {
            return;
        }

        using var cts = new CancellationTokenSource(TimeSpan.FromSeconds(10));

        try
        {
            while (!cts.Token.IsCancellationRequested)
            {
                try
                {
                    if (_panel.IsOnline(_connectionId, Address))
                    {
                        return;
                    }
                }
                catch (KeyNotFoundException)
                {
                    // Connection doesn't exist yet, keep waiting
                }

                await Task.Delay(TimeSpan.FromMilliseconds(100), cts.Token);
            }
        }
        catch (OperationCanceledException)
        {
            // Timeout occurred
        }

        throw new TimeoutException("The device did not come online within the specified timeout.");
    }

    /// <inheritdoc />
    public event EventHandler<ConnectionStatus>? ConnectionStatusChange;

    /// <inheritdoc />
    public event EventHandler? DeviceLookupsChanged;

    /// <inheritdoc />
    public event EventHandler<string>? NakReplyReceived;

    /// <inheritdoc />
    public event EventHandler<string>? CardReadReceived;

    /// <inheritdoc />
    public event EventHandler<string>? KeypadReadReceived;
    
    /// <inheritdoc />
    public event EventHandler<TraceEntry>? TraceEntryReceived;

    /// <inheritdoc />
    public event EventHandler<LocalStatusEventArgs>? LocalStatusReceived;

    /// <inheritdoc />
    public async Task Reconnect(IOsdpConnection osdpConnection, byte connectionParametersAddress)
    {
        await Connect(osdpConnection, connectionParametersAddress, IsUsingSecureChannel, UsesDefaultSecurityKey, _securityKey);
    }

    /// <inheritdoc />
    public async Task ReconnectAfterCommunicationChange(IOsdpConnection osdpConnection, byte connectionParametersAddress)
    {
        // Don't wait for device to go offline since it has already switched to new communication settings
        await Shutdown(waitForOffline: false);

        Address = connectionParametersAddress;
        BaudRate = (uint)osdpConnection.BaudRate;

        _connectionId = _panel.StartConnection(osdpConnection, _defaultPollInterval, Tracer);
        _panel.AddDevice(_connectionId, Address, true, IsUsingSecureChannel,
            UsesDefaultSecurityKey ? null : _securityKey);

        SetPortInUse(true);

        // Wait for the connection to fully establish before returning
        await WaitUntilDeviceIsOnline();
    }

    /// <inheritdoc />
    public async Task StartPassiveMonitoring(IOsdpConnection connection, bool useSecureChannel = false,
        bool useDefaultSecurityKey = true, byte[]? securityKey = null)
    {
        // Stop any active connection or monitoring
        await Shutdown();
        await StopPassiveMonitoring();

        _securityKey = securityKey;
        _passiveConnection = connection;
        _passiveMonitoringCts = new CancellationTokenSource();

        BaudRate = (uint)connection.BaudRate;
        IsUsingSecureChannel = useSecureChannel;
        UsesDefaultSecurityKey = useDefaultSecurityKey;

        // Notify that passive monitoring is starting
        SetPortInUse(true);
        RaiseEvent(ConnectionStatusChange, ConnectionStatus.PassiveMonitoring);

        // Start the background monitoring task
        _passiveMonitoringTask = Task.Run(() => PassiveMonitoringLoop(_passiveMonitoringCts.Token));
    }

    /// <inheritdoc />
    public async Task StopPassiveMonitoring()
    {
        if (_passiveMonitoringCts != null)
        {
            await _passiveMonitoringCts.CancelAsync();

            if (_passiveMonitoringTask != null)
            {
                try
                {
                    await _passiveMonitoringTask;
                }
                catch (OperationCanceledException)
                {
                    // Expected when cancellation is requested
                }
            }

            _passiveMonitoringCts.Dispose();
            _passiveMonitoringCts = null;
        }

        if (_passiveConnection != null)
        {
            try
            {
                await _passiveConnection.Close();
            }
            catch
            {
                // Ignore errors when closing connection
            }
            _passiveConnection = null;
        }

        _passiveMonitoringTask = null;
        SetPortInUse(false);
        RaiseEvent(ConnectionStatusChange, ConnectionStatus.Disconnected);
    }

    private async Task PassiveMonitoringLoop(CancellationToken cancellationToken)
    {
        const int bufferSize = 1024;
        var readBuffer = new byte[bufferSize];
        var packetBuffer = new PacketBuffer();

        try
        {
            await _passiveConnection!.Open();

            while (!cancellationToken.IsCancellationRequested)
            {
                int bytesRead;
                try
                {
                    bytesRead = await _passiveConnection.ReadAsync(readBuffer, cancellationToken);
                }
                catch (OperationCanceledException)
                {
                    break;
                }
                catch (TimeoutException)
                {
                    // Normal timeout - no data available, continue
                    continue;
                }
                catch (Exception)
                {
                    // Connection error - stop monitoring
                    break;
                }

                if (bytesRead > 0)
                {
                    // Add received bytes to the packet buffer
                    packetBuffer.Append(readBuffer, bytesRead);

                    // Extract all complete packets from buffer
                    while (packetBuffer.TryExtractPacket(out byte[]? packet) && packet != null)
                    {
                        EmitPacketTraceEntry(packet);
                    }
                }
            }
        }
        catch (OperationCanceledException)
        {
            // Normal cancellation
        }
        catch (Exception)
        {
            // Unexpected error during monitoring
        }
        finally
        {
            try
            {
                await _passiveConnection!.Close();
            }
            catch
            {
                // Ignore close errors
            }
        }
    }

    private void EmitPacketTraceEntry(byte[] packetData)
    {
        // For passive monitoring, use TraceDirection.Trace since we're observing the bus
        // The existing packet parsing in PacketTraceEntryBuilder will determine actual direction
        var traceEntry = new TraceEntry(TraceDirection.Trace, Guid.Empty, packetData);
        RaiseEvent(TraceEntryReceived, traceEntry);
    }

    /// <summary>
    /// Helper method to raise events with proper synchronization context handling
    /// </summary>
    /// <param name="eventHandler">The event handler to raise</param>
    private void RaiseEvent(EventHandler? eventHandler)
    {
        if (eventHandler == null) return;
        
        if (_synchronizationContext != null)
        {
            _synchronizationContext.Post(_ => eventHandler.Invoke(this, EventArgs.Empty), null);
        }
        else
        {
            eventHandler.Invoke(this, EventArgs.Empty);
        }
    }
    
    /// <summary>
    /// Helper method to raise events with proper synchronization context handling
    /// </summary>
    /// <typeparam name="T">The type of event argument</typeparam>
    /// <param name="eventHandler">The event handler to raise</param>
    /// <param name="arg">The event argument</param>
    private void RaiseEvent<T>(EventHandler<T>? eventHandler, T arg)
    {
        if (eventHandler == null) return;
        
        if (_synchronizationContext != null)
        {
            _synchronizationContext.Post(_ => eventHandler.Invoke(this, arg), null);
        }
        else
        {
            eventHandler.Invoke(this, arg);
        }
    }
    
    /// <summary>
    /// Format keypad data from byte array to string representation
    /// </summary>
    /// <param name="data">Keypad data as a byte array</param>
    /// <returns>Formatted keypad string</returns>
    private static string FormatKeypadData(byte[] data)
    {
        var keypadData = new StringBuilder();
        foreach (var keypadByte in data)
        {
            if (keypadByte == 0x7F)
            {
                keypadData.Append("*");
            }
            else if (keypadByte == 0x0D)
            {
                keypadData.Append("#");
            }
            else
            {
                keypadData.Append(char.ConvertFromUtf32(keypadByte));
            }
        }
        
        return keypadData.ToString();
    }

    private static string FormatData(BitArray bitArray)
    {
        var builder = new StringBuilder();
        foreach (bool bit in bitArray)
        {
            builder.Append(bit ? "1" : "0");
        }

        return builder.ToString();
    }
}