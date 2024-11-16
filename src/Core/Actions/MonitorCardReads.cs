using OSDP.Net;

namespace OSDPBench.Core.Actions;

/// <summary>
/// Represents a device action that monitors card reads.
/// </summary>
public class MonitorCardReads : IDeviceAction
{
    /// <inheritdoc />
    public string Name => "Monitor Card Reads";

    /// <inheritdoc />
    public string PerformActionName => string.Empty;

    /// <inheritdoc />
    public async Task<object> PerformAction(ControlPanel panel, Guid connectionId, byte address, object? parameter)
    {
        return await Task.FromResult<object>(null);
    }
}