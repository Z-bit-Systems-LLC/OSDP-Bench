using MvvmCore.Models;
using OSDP.Net.Connections;

namespace MvvmCore.Platform;

public interface ISerialPortConnection : ISerialPort, IOsdpConnection;

/// <summary>Platform specific serial port implementation</summary>
public interface ISerialPort
{
    /// <summary>
    /// Available the serial ports
    /// </summary>
    /// <returns></returns>
    Task<IEnumerable<AvailableSerialPort>> FindAvailableSerialPorts();

    IEnumerable<ISerialPortConnection> GetConnectionsForDiscovery(string portName, int[]? rates = null);

    ISerialPortConnection GetConnection(string portName, int baudRate);
}