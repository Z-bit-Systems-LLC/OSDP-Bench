using System.Collections.Generic;
using System.IO.Ports;
using System.Linq;
using System.Threading.Tasks;
using OSDP.Net.Connections;
using OSDPBench.Core.Models;
using OSDPBench.Core.Platforms;

namespace OSDP_Bench_WinUI.Platform;

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

    public ISerialPortConnection CreateSerialPort(string name, int baudRate)
    {
        return new WinUISerialPortConnection(name, baudRate);
    }
}