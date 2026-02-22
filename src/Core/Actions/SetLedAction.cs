using OSDP.Net;
using OSDP.Net.Model.CommandData;
using OSDP.Net.Model.ReplyData;
using OSDPBench.Core.Models;

namespace OSDPBench.Core.Actions;

/// <summary>
/// Represents an action to set the reader LED of a device.
/// </summary>
public class SetReaderLedAction : IDeviceAction
{
    /// <inheritdoc />
    public string Name => "Test LED";

    /// <inheritdoc />
    public string PerformActionName => Resources.Resources.GetString("Manage_Send");

    /// <inheritdoc />
    public CapabilityFunction? RequiredCapability => CapabilityFunction.ReaderLEDControl;

    /// <inheritdoc />
    public async Task<object> PerformAction(ControlPanel panel, Guid connectionId, byte address, object? parameter)
    {
        var ledParams = parameter as LedParameters;

        var result = await panel.ReaderLedControl(connectionId, address,
            new ReaderLedControls([
                new ReaderLedControl(
                    ledParams?.ReaderNumber ?? 0,
                    ledParams?.LedNumber ?? 0,
                    ledParams?.TemporaryMode ?? TemporaryReaderControlCode.SetTemporaryAndStartTimer,
                    ledParams?.TemporaryOnTime ?? 10,
                    ledParams?.TemporaryOffTime ?? 10,
                    ledParams?.TemporaryOnColor ?? LedColor.Red,
                    ledParams?.TemporaryOffColor ?? LedColor.Black,
                    ledParams?.TemporaryTimer ?? 50,
                    ledParams?.PermanentMode ?? PermanentReaderControlCode.Nop,
                    ledParams?.PermanentOnTime ?? 0,
                    ledParams?.PermanentOffTime ?? 0,
                    ledParams?.PermanentOnColor ?? LedColor.Black,
                    ledParams?.PermanentOffColor ?? LedColor.Black
                )
            ]));

        return result;
    }
}
