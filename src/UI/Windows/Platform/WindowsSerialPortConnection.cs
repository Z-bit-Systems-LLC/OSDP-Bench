using System.IO.Ports;
using MvvmCore.Models;
using MvvmCore.Platform;
using OSDP.Net.Connections;

namespace OSDPBench.Windows.Platform;

internal class WindowsSerialPortConnection : SerialPortOsdpConnection, ISerialPortConnection
{
    private WindowsSerialPortConnection(string portName, int baudRate) : base(portName, baudRate)
    {
    }

    public WindowsSerialPortConnection() : base("Temp", 9600)
    {
    }

    /// <inheritdoc />
    public async Task<IEnumerable<AvailableSerialPort>> FindAvailableSerialPorts()
    {
        return await Task.FromResult(SerialPort.GetPortNames()
            .Select(name => new AvailableSerialPort(string.Empty, name, name)).OrderBy(port => port.Name));
    }

    /// <inheritdoc />
    public IEnumerable<ISerialPortConnection> GetConnectionsForDiscovery(string portName, int[]? rates = null)
    {
        foreach (var serialPortOsdpConnection in EnumBaudRates(portName, rates))
        {
            yield return new WindowsSerialPortConnection(portName, serialPortOsdpConnection.BaudRate);
        }
    }

    /// <inheritdoc />
    public ISerialPortConnection GetConnection(string portName, int baudRate)
    {
        return new WindowsSerialPortConnection(portName, baudRate);
    }
}