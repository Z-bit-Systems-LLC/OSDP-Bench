using OSDP.Net;
using OSDP.Net.Model.CommandData;
using OSDPBench.Core.Services;

namespace OSDPBench.Core.Actions;

/// <summary>
/// Represents an action to reset a Cypress device.
/// </summary>
public class ResetCypressDeviceAction : IDeviceAction
{
    /// <inheritdoc />
    public string Name => "Reset Device";

    /// <inheritdoc />
    public async Task<object> PerformAction(ControlPanel panel, Guid connectionId, byte address, object? parameter)
    {
        await panel.Shutdown();
        
        var connectionService = parameter as ISerialPortConnectionService ??
                                throw new ArgumentException("Invalid type", nameof(parameter));
        
        connectionId = panel.StartConnection(connectionService, TimeSpan.Zero);

        panel.AddDevice(connectionId, address, false, false);

        const int maximumAttempts = 15;
        const int requiredNumberOfAcks = 10;
        int totalAcks = 0;
        int totalAttempts = 0;
        while (totalAttempts++ < maximumAttempts && totalAcks < requiredNumberOfAcks)
        {
            try
            {
                var result = await panel.ManufacturerSpecificCommand(connectionId, address,
                    new ManufacturerSpecific(new byte[] { 0xCA, 0x44, 0x6C }, new byte[] { 0x05 }));

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