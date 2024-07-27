using OSDP.Net.Connections;
using OSDPBench.Core.Models;

namespace OSDPBench.Core.Services;

/// <summary>
/// Represents a service that provides serial port connections for OSDP devices.
/// </summary>
public interface ISerialPortConnectionService : IOsdpConnection
{
    /// <summary>
    /// Available the serial ports
    /// </summary>
    /// <returns></returns>
    Task<IEnumerable<AvailableSerialPort>> FindAvailableSerialPorts();

    /// <summary>
    /// Retrieves a collection of serial port connections for OSDP discovery.
    /// </summary>
    /// <param name="portName">The name of the serial port to retrieve connections for.</param>
    /// <param name="rates">Optional array of baud rates to filter the connections by. If null, all baud rates will be included.</param>
    /// <returns>A collection of ISerialPortConnectionService objects representing the available connections for OSDP devices.</returns>
    IEnumerable<ISerialPortConnectionService> GetConnectionsForDiscovery(string portName, int[]? rates = null);

    /// <summary>
    /// Retrieves a serial port connection for the specified port name and baud rate.
    /// </summary>
    /// <param name="portName">The name of the serial port.</param>
    /// <param name="baudRate">The baud rate for the serial port.</param>
    /// <returns>An ISerialPortConnectionService object representing the serial port connection.</returns>
    ISerialPortConnectionService GetConnection(string portName, int baudRate);
}
