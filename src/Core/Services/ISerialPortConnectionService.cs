using OSDP.Net.Connections;
using OSDPBench.Core.Models;

namespace OSDPBench.Core.Services;

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
