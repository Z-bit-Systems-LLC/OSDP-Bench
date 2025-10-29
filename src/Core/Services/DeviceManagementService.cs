using System.Collections;
using System.Text;
using OSDP.Net;
using OSDP.Net.Connections;
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
    private bool _invalidSecurityKey;
    private byte[]? _securityKey;

    /// <summary>
    /// Initializes a new instance of the <see cref="DeviceManagementService"/> class.
    /// </summary>
    /// <exception cref="ArgumentNullException">panel</exception>
    public DeviceManagementService()
    {
        _synchronizationContext = SynchronizationContext.Current;
        
        _panel.ConnectionStatusChanged += (_, args) =>
        {
            if (_isDiscovering) return;

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
            if (args.Nak.ErrorCode == ErrorCode.CommunicationSecurityNotMet && !_invalidSecurityKey)
            {
                _invalidSecurityKey = true;
                RaiseEvent(ConnectionStatusChange, ConnectionStatus.InvalidSecurityKey);
                Task.Run(async () =>
                {
                    IdentityLookup = new IdentityLookup(await _panel.IdReport(_connectionId, Address));

                    RaiseEvent(DeviceLookupsChanged);
                });
            }
        };

        _panel.RawCardDataReplyReceived += (_, args) =>
        {
            RaiseEvent(CardReadReceived, FormatData(args.RawCardData.Data));
        };
        _panel.KeypadReplyReceived += (_, args) => RaiseEvent(KeypadReadReceived, FormatKeypadData(args.KeypadData.Data));
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
    public bool IsConnected { get; private set; }

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

        _connectionId = _panel.StartConnection(connection, _defaultPollInterval, Tracer);
        _panel.AddDevice(_connectionId, address, true, useSecureChannel, 
            useDefaultSecurityKey ? null : securityKey);
    }
    
    private void Tracer(TraceEntry traceEntry)
    {
        RaiseEvent(TraceEntryReceived, traceEntry);
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
            IsUsingSecureChannel = UsesDefaultSecurityKey;

            RaiseEvent(DeviceLookupsChanged);

            _connectionId = _panel.StartConnection(results.Connection, _defaultPollInterval, Tracer);
            _panel.AddDevice(_connectionId, Address, CapabilitiesLookup.CRC, UsesDefaultSecurityKey);

            return results;
        }
        finally
        {
            _isDiscovering = false;
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

        await _panel.Shutdown();

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