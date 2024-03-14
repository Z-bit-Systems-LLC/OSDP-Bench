using System.IO.Ports;
using MvvmCore.Models;
using MvvmCore.Services;
using OSDP.Net.Connections;

namespace OSDPBench.Windows.Services;

internal class WindowsSerialPortConnectionService : SerialPortOsdpConnection, ISerialPortConnectionService
{
    private WindowsSerialPortConnectionService(string portName, int baudRate) : base(portName, baudRate)
    {
    }

    public WindowsSerialPortConnectionService() : base("Temp", 9600)
    {
    }

    /// <inheritdoc />
    public async Task<IEnumerable<AvailableSerialPort>> FindAvailableSerialPorts()
    {
        return await Task.FromResult(SerialPort.GetPortNames()
            .Select(name => new AvailableSerialPort(string.Empty, name, name)).OrderBy(port => port.Name));
    }

    /// <inheritdoc />
    public IEnumerable<ISerialPortConnectionService> GetConnectionsForDiscovery(string portName, int[]? rates = null)
    {
        foreach (var serialPortOsdpConnection in EnumBaudRates(portName, rates))
        {
            yield return new WindowsSerialPortConnectionService(portName, serialPortOsdpConnection.BaudRate);
        }
    }

    /// <inheritdoc />
    public ISerialPortConnectionService GetConnection(string portName, int baudRate)
    {
        return new WindowsSerialPortConnectionService(portName, baudRate);
    }
}