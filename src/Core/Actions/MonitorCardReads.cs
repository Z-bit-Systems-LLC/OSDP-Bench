using OSDP.Net;

namespace OSDPBench.Core.Actions;

public class MonitorCardReads : IDeviceAction
{
    /// <inheritdoc />
    public string Name => "Monitor Card Reads";
    
    /// <inheritdoc />
    public async Task<object> PerformAction(ControlPanel panel, Guid connectionId, byte address, object? parameter)
    {
        return await Task.FromResult<object>(null);
    }
}