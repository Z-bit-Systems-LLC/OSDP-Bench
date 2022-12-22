using System.Collections.Generic;
using System.Threading.Tasks;
using OSDP.Net.Connections;
using OSDPBench.Core.Models;

namespace OSDPBench.Core.Platforms
{
    public interface ISerialPortConnection : ISerialPort, IOsdpConnection
    {
    }

    /// <summary>Platform specific serial port implementation</summary>
    public interface ISerialPort
    {
        /// <summary>
        /// Available the serial ports
        /// </summary>
        /// <returns></returns>
        Task<IEnumerable<AvailableSerialPort>> FindAvailableSerialPorts();

        IEnumerable<ISerialPortConnection> GetConnectionsForDiscovery(string portName, int[] rates = null);

        ISerialPortConnection GetConnection(string portName, int baudRate);
    }
}
