using MvvmCore.Models;
using OSDP.Net.Connections;

namespace MvvmCore.Services;

public interface ISerialPortConnectionService : IOsdpConnection
{
    /// <summary>
    /// Available the serial ports
    /// </summary>
    /// <returns></returns>
    Task<IEnumerable<AvailableSerialPort>> FindAvailableSerialPorts();

    IEnumerable<ISerialPortConnectionService> GetConnectionsForDiscovery(string portName, int[]? rates = null);

    ISerialPortConnectionService GetConnection(string portName, int baudRate);
}
