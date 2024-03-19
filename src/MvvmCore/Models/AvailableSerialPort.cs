namespace MvvmCore.Models;

/// <summary>
/// Represents a serial port available in the system.
/// </summary>
/// <remarks>
/// Initializes a new instance of the <see cref="AvailableSerialPort"/> class.
/// </remarks>
/// <param name="id">The identifier of the serial port.</param>
/// <param name="name">The name of the serial port.</param>
/// <param name="description">The description of the serial port.</param>
public class AvailableSerialPort(string id, string name, string description)
{
    /// <summary>
    /// Gets the name of the serial port.
    /// </summary>
    public string Name { get; } = name;

    /// <summary>
    /// Gets the description of the serial port.
    /// </summary>
    public string Description { get; } = description;

    /// <summary>
    /// Gets the identifier of the serial port.
    /// </summary>
    public string Id { get; } = id;
}
