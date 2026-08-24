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

    /// <summary>
    /// Retrieves a connection whose baud rate can be changed while it stays open, or null when
    /// the platform's serial implementation cannot do that.
    /// </summary>
    /// <param name="portName">The name of the serial port.</param>
    /// <param name="baudRate">The baud rate to open the port at.</param>
    /// <returns>A retunable connection, or null when the platform does not support retuning.</returns>
    /// <remarks>
    /// The line quality test sweeps baud rates and must retune the port in place; closing and
    /// reopening between rates is slow, can leave the handle briefly unavailable, and toggles the
    /// control lines in a way that disturbs the bus. Platforms whose connection cannot do this
    /// return null and the caller reports the feature as unavailable rather than failing later.
    /// </remarks>
    IRetunableOsdpConnection? GetRetunableConnection(string portName, int baudRate) =>
        GetConnection(portName, baudRate) as IRetunableOsdpConnection;
}
