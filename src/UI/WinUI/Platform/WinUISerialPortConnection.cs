using System.Collections.Generic;
using System.IO.Ports;
using System.Linq;
using System.Threading.Tasks;
using OSDP.Net.Connections;
using OSDPBench.Core.Models;
using OSDPBench.Core.Platforms;

namespace WinUI.Platform;

internal class WinUISerialPortConnection : SerialPortOsdpConnection, ISerialPortConnection
{
    /// <inheritdoc />
    private WinUISerialPortConnection(string portName, int baudRate) : base(portName, baudRate)
    {
    }

    public WinUISerialPortConnection() : base("Temp", 9600)
    {

    }

    /// <inheritdoc />
    public async Task<IEnumerable<AvailableSerialPort>> FindAvailableSerialPorts()
    {
        return await Task.FromResult(SerialPort.GetPortNames()
            .Select(name => new AvailableSerialPort(string.Empty, name, name)).OrderBy(port => port.Name));
    }

    public IEnumerable<ISerialPortConnection> GetConnectionsForDiscovery(string portName, int[] rates = null)
    {
        foreach (var serialPortOsdpConnection in EnumBaudRates(portName, rates))
        {
            yield return new WinUISerialPortConnection(portName, serialPortOsdpConnection.BaudRate);
        }
    }

    public ISerialPortConnection GetConnection(string portName, int baudRate)
    {
        return new WinUISerialPortConnection(portName, baudRate);
    }
}