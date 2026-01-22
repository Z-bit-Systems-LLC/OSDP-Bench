using OSDP.Net;

namespace OSDPBench.Core.Actions;

/// <summary>
/// Represents an action that checks the local status (supervision) of a device.
/// </summary>
public class SupervisionAction : IDeviceAction
{
    /// <inheritdoc />
    public string Name => Resources.Resources.GetString("Supervision_Name");

    /// <inheritdoc />
    public string PerformActionName => Resources.Resources.GetString("Supervision_CheckStatus");

    /// <inheritdoc />
    public async Task<object> PerformAction(ControlPanel panel, Guid connectionId, byte address, object? parameter)
    {
        var result = await panel.LocalStatus(connectionId, address);
        return result;
    }
}
