using OSDP.Net;
using OSDP.Net.Model.CommandData;
using OSDPBench.Core.Models;

namespace OSDPBench.Core.Actions;

/// <summary>
/// Represents an action to set the communication parameters of a device.
/// </summary>
public class SetCommunicationAction : IDeviceAction
{
    /// <inheritdoc />
    public string Name => "Set Communications";

    /// <inheritdoc />
    public string PerformActionName => "Update";

    /// <inheritdoc />
    public async Task<object> PerformAction(ControlPanel panel, Guid connectionId, byte address,
        object? parameter)
    {
        var communicationParameters = parameter as CommunicationParameters ??
                                      throw new ArgumentException(@"Invalid type", nameof(parameter));
        
        var result = await panel.CommunicationConfiguration(connectionId, address,
            new CommunicationConfiguration(communicationParameters.Address,
                (int)communicationParameters.BaudRate));

        return new CommunicationParameters(communicationParameters.PortName, (uint)result.BaudRate, result.Address);
    }
}