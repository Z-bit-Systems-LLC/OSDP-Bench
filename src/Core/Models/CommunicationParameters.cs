namespace OSDPBench.Core.Models;

/// <summary>
/// Represents the communication parameters for updating a device.
/// </summary>
/// <param name="portName">The name of the port.</param>
/// <param name="baudRate">The baud rate for the communication.</param>
/// <param name="address">The address of the device.</param>
public class CommunicationParameters(string portName, uint baudRate, byte address)
{
    /// <summary>
    /// Gets the name of the port.
    /// </summary>
    public string PortName { get; } = portName;
    
    /// <summary>
    /// Gets the baud rate for the communication.
    /// </summary>
    public uint BaudRate { get; } = baudRate;
    
    /// <summary>
    /// Gets the address of the device.
    /// </summary>
    public byte Address { get; } = address;
}
