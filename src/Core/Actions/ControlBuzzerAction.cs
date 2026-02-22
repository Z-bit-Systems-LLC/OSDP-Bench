using OSDP.Net;
using OSDP.Net.Model.CommandData;
using OSDP.Net.Model.ReplyData;
using OSDPBench.Core.Models;

namespace OSDPBench.Core.Actions;

/// <summary>
/// Represents an action that controls the buzzer on a device.
/// </summary>
public class ControlBuzzerAction : IDeviceAction
{
    /// <inheritdoc />
    public string Name => "Test Buzzer";

    /// <inheritdoc />
    public string PerformActionName => Resources.Resources.GetString("Manage_Send");

    /// <inheritdoc />
    public CapabilityFunction? RequiredCapability => CapabilityFunction.ReaderAudibleOutput;

    /// <inheritdoc />
    public async Task<object> PerformAction(ControlPanel panel, Guid connectionId, byte address, object? parameter)
    {
        var buzzerParams = parameter as BuzzerParameters;

        var result = await panel.ReaderBuzzerControl(connectionId, address,
            new ReaderBuzzerControl(
                buzzerParams?.ReaderNumber ?? 0,
                buzzerParams?.ToneCode ?? ToneCode.Default,
                buzzerParams?.OnTime ?? 1,
                buzzerParams?.OffTime ?? 1,
                buzzerParams?.Count ?? 3));

        return result;
    }
}
