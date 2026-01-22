namespace OSDPBench.Core.Models;

/// <summary>
/// Event arguments for local status events received from the device.
/// </summary>
public class LocalStatusEventArgs : EventArgs
{
    /// <summary>
    /// Gets a value indicating whether the device is in a tampered state.
    /// </summary>
    public bool Tamper { get; }

    /// <summary>
    /// Gets a value indicating whether the device has a power failure.
    /// </summary>
    public bool PowerFailure { get; }

    /// <summary>
    /// Initializes a new instance of the <see cref="LocalStatusEventArgs"/> class.
    /// </summary>
    /// <param name="tamper">Whether the device is tampered.</param>
    /// <param name="powerFailure">Whether the device has a power failure.</param>
    public LocalStatusEventArgs(bool tamper, bool powerFailure)
    {
        Tamper = tamper;
        PowerFailure = powerFailure;
    }
}
