using OSDP.Net.Model.CommandData;

namespace OSDPBench.Core.Models;

/// <summary>
/// Represents user-configurable parameters for the buzzer control command.
/// </summary>
public class BuzzerParameters
{
    /// <summary>
    /// The reader number (0 = first reader).
    /// </summary>
    public byte ReaderNumber { get; set; }

    /// <summary>
    /// The tone code to use (Off or Default).
    /// </summary>
    public ToneCode ToneCode { get; set; } = ToneCode.Default;

    /// <summary>
    /// ON time in units of 100ms.
    /// </summary>
    public byte OnTime { get; set; } = 1;

    /// <summary>
    /// OFF time in units of 100ms.
    /// </summary>
    public byte OffTime { get; set; } = 1;

    /// <summary>
    /// Repeat count (0 = infinite).
    /// </summary>
    public byte Count { get; set; } = 3;

    /// <summary>
    /// Gets a value indicating whether the ON time is invalid.
    /// ON time must be non-zero unless the tone code is set to off.
    /// </summary>
    public bool IsOnTimeInvalid => ToneCode != ToneCode.Off && OnTime == 0;
}
