using OSDP.Net.Model.CommandData;

namespace OSDPBench.Core.Models;

/// <summary>
/// Represents user-configurable parameters for the LED control command.
/// </summary>
public class LedParameters
{
    /// <summary>
    /// The reader number (0 = first reader).
    /// </summary>
    public byte ReaderNumber { get; set; }

    /// <summary>
    /// The LED number (0 = first LED).
    /// </summary>
    public byte LedNumber { get; set; }

    /// <summary>
    /// The temporary reader control code.
    /// </summary>
    public TemporaryReaderControlCode TemporaryMode { get; set; } = TemporaryReaderControlCode.SetTemporaryAndStartTimer;

    /// <summary>
    /// Temporary ON time in units of 100ms.
    /// </summary>
    public byte TemporaryOnTime { get; set; } = 10;

    /// <summary>
    /// Temporary OFF time in units of 100ms.
    /// </summary>
    public byte TemporaryOffTime { get; set; } = 10;

    /// <summary>
    /// The color displayed during the temporary ON state.
    /// </summary>
    public LedColor TemporaryOnColor { get; set; } = LedColor.Red;

    /// <summary>
    /// The color displayed during the temporary OFF state.
    /// </summary>
    public LedColor TemporaryOffColor { get; set; } = LedColor.Black;

    /// <summary>
    /// Total duration of the temporary state in units of 100ms.
    /// </summary>
    public ushort TemporaryTimer { get; set; } = 50;

    /// <summary>
    /// The permanent reader control code.
    /// </summary>
    public PermanentReaderControlCode PermanentMode { get; set; } = PermanentReaderControlCode.Nop;

    /// <summary>
    /// Permanent ON time in units of 100ms.
    /// </summary>
    public byte PermanentOnTime { get; set; }

    /// <summary>
    /// Permanent OFF time in units of 100ms.
    /// </summary>
    public byte PermanentOffTime { get; set; }

    /// <summary>
    /// The color displayed during the permanent ON state.
    /// </summary>
    public LedColor PermanentOnColor { get; set; } = LedColor.Black;

    /// <summary>
    /// The color displayed during the permanent OFF state.
    /// </summary>
    public LedColor PermanentOffColor { get; set; } = LedColor.Black;

    /// <summary>
    /// Gets a value indicating whether the temporary timing values are invalid.
    /// Both ON and OFF time cannot be zero when the mode sets a temporary state.
    /// </summary>
    public bool IsTemporaryTimingInvalid =>
        TemporaryMode == TemporaryReaderControlCode.SetTemporaryAndStartTimer &&
        TemporaryOnTime == 0 && TemporaryOffTime == 0;

    /// <summary>
    /// Gets a value indicating whether the permanent timing values are invalid.
    /// Both ON and OFF time cannot be zero when the mode sets a permanent state.
    /// </summary>
    public bool IsPermanentTimingInvalid =>
        PermanentMode == PermanentReaderControlCode.SetPermanentState &&
        PermanentOnTime == 0 && PermanentOffTime == 0;
}
