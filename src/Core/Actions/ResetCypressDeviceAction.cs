using OSDP.Net;
using OSDP.Net.Connections;
using OSDP.Net.Model.CommandData;

namespace OSDPBench.Core.Actions;

/// <summary>
/// Represents an action to reset a Cypress device.
/// </summary>
public class ResetCypressDeviceAction : IDeviceAction
{
    /// <inheritdoc />
    public string Name => "Reset Device";

    /// <inheritdoc />
    public string PerformActionName => "Reset";

    /// <inheritdoc />
    public async Task<object> PerformAction(ControlPanel panel, Guid connectionId, byte address, object? parameter)
    {
        await panel.Shutdown();
        
        var connectionService = parameter as IOsdpConnection ??
                                throw new ArgumentException(@"Invalid type", nameof(parameter));
        
        connectionId = panel.StartConnection(connectionService, TimeSpan.Zero);

        panel.AddDevice(connectionId, address, false, false);

        const int maximumFailedAttempts = 3;
        const int requiredNumberOfAcks = 10;
        int totalAcks = 0;
        int totalAttempts = 0;
        while (totalAttempts++ < maximumFailedAttempts + totalAcks && totalAcks < requiredNumberOfAcks)
        {
            try
            {
                var result = await panel.ManufacturerSpecificCommand(connectionId, address,
                    new ManufacturerSpecific([0xCA, 0x44, 0x6C], [0x05]));

                if (result.Ack)
                {
                    totalAcks++;
                }
            }
            catch 
            {
                await Task.Delay(TimeSpan.FromSeconds(1));
            }
        }

        if (totalAcks < requiredNumberOfAcks)
        {
            throw new Exception("Reset commands were not accepted.");
        }

        return connectionId;
    }
}