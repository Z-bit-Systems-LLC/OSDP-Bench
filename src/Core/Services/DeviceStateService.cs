using OSDPBench.Core.Models;
using OSDP.Net.Tracing;

namespace OSDPBench.Core.Services;

/// <summary>
/// Implementation of the <see cref="IDeviceStateService"/> interface.
/// This service provides a decoupled way for ViewModels to access device state
/// without directly depending on <see cref="DeviceManagementService"/>.
/// </summary>
public class DeviceStateService : IDeviceStateService
{
    private readonly IDeviceManagementService _deviceManagementService;

    /// <summary>
    /// Initializes a new instance of the <see cref="DeviceStateService"/> class.
    /// </summary>
    /// <param name="deviceManagementService">The device management service.</param>
    public DeviceStateService(IDeviceManagementService deviceManagementService)
    {
        _deviceManagementService = deviceManagementService ?? 
                                 throw new ArgumentNullException(nameof(deviceManagementService));
        
        // Forward events from the device management service
        _deviceManagementService.ConnectionStatusChange += (_, args) => 
            ConnectionStatusChange?.Invoke(this, args);
        
        _deviceManagementService.KeypadReadReceived += (_, args) => 
            KeypadReadReceived?.Invoke(this, args);
        
        _deviceManagementService.CardReadReceived += (_, args) => 
            CardReadReceived?.Invoke(this, args);
        
        _deviceManagementService.TraceEntryReceived += (_, args) => 
            TraceEntryReceived?.Invoke(this, args);
        
        _deviceManagementService.NakReplyReceived += (_, args) => 
            NakReplyReceived?.Invoke(this, args);
        
        _deviceManagementService.DeviceLookupsChanged += (_, args) => 
            DeviceLookupsChanged?.Invoke(this, args);
    }

    /// <inheritdoc />
    public bool IsConnected => _deviceManagementService.IsConnected;

    /// <inheritdoc />
    public string? PortName => _deviceManagementService.PortName;

    /// <inheritdoc />
    public byte Address => _deviceManagementService.Address;

    /// <inheritdoc />
    public uint BaudRate => _deviceManagementService.BaudRate;

    /// <inheritdoc />
    public IdentityLookup? IdentityLookup => _deviceManagementService.IdentityLookup;

    /// <inheritdoc />
    public CapabilitiesLookup? CapabilitiesLookup => _deviceManagementService.CapabilitiesLookup;

    /// <inheritdoc />
    public bool IsUsingSecureChannel => _deviceManagementService.IsUsingSecureChannel;

    /// <inheritdoc />
    public event EventHandler<ConnectionStatus>? ConnectionStatusChange;

    /// <inheritdoc />
    public event EventHandler<string>? KeypadReadReceived;

    /// <inheritdoc />
    public event EventHandler<string>? CardReadReceived;

    /// <inheritdoc />
    public event EventHandler<TraceEntry>? TraceEntryReceived;

    /// <inheritdoc />
    public event EventHandler<string>? NakReplyReceived;

    /// <inheritdoc />
    public event EventHandler? DeviceLookupsChanged;
}