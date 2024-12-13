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
    public string PerformActionName => "Flash";
    
    /// <summary>
    /// Available LED colors for selection
    /// </summary>
    public static readonly Dictionary<string, LedColor> AvailableColors = new()
    {
        { "Red", LedColor.Red },
        { "Green", LedColor.Green },
        { "Amber", LedColor.Amber },
        { "Blue", LedColor.Blue }
    };

    /// <inheritdoc />
    public async Task<object> PerformAction(ControlPanel panel, Guid connectionId, byte address, object? parameter)
    {
        // Default to red if no color is specified
        var selectedColor = LedColor.Red;
        
        // Parse the color parameter if provided
        if (parameter is string colorName && AvailableColors.TryGetValue(colorName, out var color))
        {
            selectedColor = color;
        }

        var result = await panel.ReaderLedControl(connectionId, address,
            new ReaderLedControls([
                new ReaderLedControl(
                    0,                                          // LED number
                    0,                                          // reader number
                    TemporaryReaderControlCode.SetTemporaryAndStartTimer,
                    10,                                         // temporary timer
                    10,                                         // temporary timer count
                    selectedColor,                              // temporary on color
                    LedColor.Black,                            // temporary off color
                    50,                                         // temporary blink rate
                    PermanentReaderControlCode.Nop,
                    0,                                          // permanent timer
                    0,                                          // permanent timer count
                    LedColor.Black,                            // permanent on color
                    LedColor.Black                             // permanent off color
                )
            ]));

        return result;
    }
}