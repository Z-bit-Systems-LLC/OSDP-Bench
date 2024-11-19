using OSDP.Net;
using OSDP.Net.Model.CommandData;

namespace OSDPBench.Core.Actions;

/// <summary>
/// Represents an action to set the reader LED of a device.
/// </summary>
public class SetReaderLedAction : IDeviceAction
{
    /// <inheritdoc />
    public string Name => "Test LED";

    /// <inheritdoc />
    public string PerformActionName => "Flash Red";

    /// <inheritdoc />
    public async Task<object> PerformAction(ControlPanel panel, Guid connectionId, byte address, object? parameter)
    {
        var result = await panel.ReaderLedControl(connectionId, address,
            new ReaderLedControls([
                new ReaderLedControl(0, 0, TemporaryReaderControlCode.SetTemporaryAndStartTimer, 10, 10, LedColor.Red,
                    LedColor.Black, 50, PermanentReaderControlCode.Nop, 0, 0, LedColor.Black, LedColor.Black)
            ]));

        return result;
    }
}