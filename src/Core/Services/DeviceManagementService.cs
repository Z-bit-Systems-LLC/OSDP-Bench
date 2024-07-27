using System.Collections;
using System.Text;
using OSDP.Net;
using OSDP.Net.Connections;
using OSDP.Net.Model.ReplyData;
using OSDP.Net.PanelCommands.DeviceDiscover;
using OSDPBench.Core.Actions;
using OSDPBench.Core.Models;

namespace OSDPBench.Core.Services;

/// <summary>
/// Class DeviceManagementService.
/// Implements the <see cref="IDeviceManagementService" />
/// </summary>
/// <seealso cref="IDeviceManagementService" />
public class DeviceManagementService : IDeviceManagementService
{
    private readonly ControlPanel _panel = new ();

    private Guid _connectionId;
    private bool _isDiscovering;

    /// <summary>
    /// Initializes a new instance of the <see cref="DeviceManagementService"/> class.
    /// </summary>
    /// <exception cref="ArgumentNullException">panel</exception>
    public DeviceManagementService()
    {
        _panel.ConnectionStatusChanged += (_, args) =>
        {
            if (_isDiscovering) return;

            IsConnected = args.IsConnected;
            if (IsConnected)
            {
                Task.Run(async () =>
                {
                    IdentityLookup = new IdentityLookup(await _panel.IdReport(_connectionId, Address));
                    CapabilitiesLookup = new CapabilitiesLookup(await _panel.DeviceCapabilities(_connectionId, Address));

                    OnDeviceLookupsChanged();
                });
            }
            else
            {
                IdentityLookup = null;
                CapabilitiesLookup = null;

                OnDeviceLookupsChanged();
            }
            
            OnConnectionStatusChange(args.IsConnected);
        };

        _panel.NakReplyReceived += (_, args) =>
        {
            OnNakReplyReceived(ToFormattedText(args.Nak.ErrorCode));
        };

        _panel.RawCardDataReplyReceived += (_, args) => OnCardReadReceived(FormatData(args.RawCardData.Data));
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
    public bool UsesDefaultSecurityKey { get; private set; }
    
    /// <inheritdoc />
    public bool IsConnected { get; private set; }

    /// <inheritdoc />
    public async Task Connect(IOsdpConnection connection, byte address)
    {
        await Shutdown();

        Address = address;
        BaudRate = (uint)connection.BaudRate;

        _connectionId = _panel.StartConnection(connection);
        _panel.AddDevice(_connectionId, address, true, false);
    }

    /// <inheritdoc />
    public async Task<DiscoveryResult> DiscoverDevice(IEnumerable<IOsdpConnection> connections, DiscoveryProgress progress, CancellationToken cancellationToken)
    {
        IdentityLookup = null;
        CapabilitiesLookup = null;
        OnDeviceLookupsChanged();

        await Shutdown();

        _isDiscovering = true;
        DiscoveryResult results;
        try
        {
            results = await DiscoveryRoutines(connections, progress, cancellationToken);
        }
        finally
        {
            _isDiscovering = false;
        }

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

        OnDeviceLookupsChanged();

        _connectionId = _panel.StartConnection(results.Connection);
        _panel.AddDevice(_connectionId, Address, CapabilitiesLookup.CRC, false);

        return results;
    }

    private async Task<DiscoveryResult> DiscoveryRoutines(IEnumerable<IOsdpConnection> connections, DiscoveryProgress progress, CancellationToken cancellationToken)
    {
        var options = new DiscoveryOptions
        {
            ProgressCallback = progress,
            ResponseTimeout = TimeSpan.FromSeconds(1),
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
        IdentityLookup = null;
        CapabilitiesLookup = null;
        
        await _panel.Shutdown();
    }

    /// <inheritdoc />
    public event EventHandler<bool>? ConnectionStatusChange;

    /// <summary>
    /// Event handler for the connection status change.
    /// </summary>
    /// <param name="isConnected">A boolean value indicating if the connection status is connected or not.</param>
    protected virtual void OnConnectionStatusChange(bool isConnected)
    {
        ConnectionStatusChange?.Invoke(this, isConnected);
    }

    /// <inheritdoc />
    public event EventHandler? DeviceLookupsChanged;

    /// <summary>
    /// Raises the <see cref="DeviceLookupsChanged"/> event.
    /// </summary>
    protected virtual void OnDeviceLookupsChanged()
    {
        DeviceLookupsChanged?.Invoke(this, EventArgs.Empty);
    }

    /// <inheritdoc />
    public event EventHandler<string>? NakReplyReceived;
    /// <summary>
    /// Event handler for Nak reply received.
    /// </summary>
    /// <param name="errorMessage">The error message.</param>
    protected virtual void OnNakReplyReceived(string errorMessage)
    {
        NakReplyReceived?.Invoke(this, errorMessage);
    }

    /// <inheritdoc />
    public event EventHandler<string>? CardReadReceived;

    /// <summary>
    /// Raises the CardReadReceived event when card data is received.
    /// </summary>
    /// <param name="data">The card data received.</param>
    protected virtual void OnCardReadReceived(string data)
    {
        CardReadReceived?.Invoke(this, data);
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