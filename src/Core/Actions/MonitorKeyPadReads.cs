using OSDP.Net;

namespace OSDPBench.Core.Actions;

/// <summary>
/// Represents the action of monitoring keypad reads on a device.
/// </summary>
public class MonitorKeypadReads : IDeviceAction
{
    /// <inheritdoc />
    public string Name => "Monitor Keypad Reads";

    /// <inheritdoc />
    public string PerformActionName => string.Empty;

    /// <inheritdoc />
    public async Task<object> PerformAction(ControlPanel panel, Guid connectionId, byte address, object? parameter)
    {
        return await Task.FromResult(true);
    }
}