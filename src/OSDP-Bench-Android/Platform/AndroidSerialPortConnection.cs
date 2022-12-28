using System.Collections.Concurrent;
using Android.Hardware.Usb;
using Hoho.Android.UsbSerial.driver;
using Hoho.Android.UsbSerial.Extensions;
using OSDPBench.Core.Models;
using OSDPBench.Core.Platforms;

namespace OSDPBench.UI.Android.Platform;

internal class AndroidSerialPortConnection : ISerialPortConnection
{
    private readonly ConcurrentBag<AvailableSerialPort> _ports = new();
    private UsbManager? _usbManager;
    private UsbSerialPort? _usbSerialPort;
    private SerialInputOutputManager? _serialIoManager;
    private readonly ConcurrentQueue<byte> _readData = new();

    public AndroidSerialPortConnection()
    {
    }

    private AndroidSerialPortConnection(UsbManager usbManager, UsbSerialPort usbSerialPort, int baudRate)
    {
        _usbManager = usbManager;
        _usbSerialPort = usbSerialPort;
        BaudRate = baudRate;
    }
    
    /// <inheritdoc />
    public async Task<IEnumerable<AvailableSerialPort>> FindAvailableSerialPorts()
    {
        return await Task.FromResult(_ports);
    }

    public IEnumerable<ISerialPortConnection> GetConnectionsForDiscovery(string portName, int[]? rates = null)
    {
        rates ??= new[] { 9600, 19200, 38400, 57600, 115200, 230400 };
        return rates.AsEnumerable().Select((rate) => new AndroidSerialPortConnection(_usbManager, _usbSerialPort, rate));
    }

    public ISerialPortConnection GetConnection(string portName, int baudRate)
    {
        return new AndroidSerialPortConnection(_usbManager, _usbSerialPort, baudRate);
    }

    public void Open()
    {
        _serialIoManager = new SerialInputOutputManager(_usbSerialPort)
        {
            BaudRate = BaudRate,
            DataBits = 8,
            StopBits = StopBits.One,
            Parity = Parity.None
        };

        _serialIoManager.DataReceived += (_, eventArgs) =>
        {
            foreach (var data in eventArgs.Data)
            {
                _readData.Enqueue(data);
            }
        };

        try
        {
            _serialIoManager?.Open(_usbManager);
        }
        catch (Exception)
        {
            _serialIoManager?.Dispose();
            _serialIoManager = null;
        }
    }

    public void Close()
    {
        try
        {
            if (_serialIoManager is { IsOpen: true })  _serialIoManager?.Close();
            _serialIoManager?.Dispose();
            _readData.Clear();
        }
        catch (Exception)
        {
            // ignore
        }
    }

    public async Task WriteAsync(byte[] buffer)
    {
        await Task.Run(() => _usbSerialPort?.Write(buffer, (int)ReplyTimeout.TotalMilliseconds));
    }

    public async Task<int> ReadAsync(byte[] buffer, CancellationToken token)
    {
        while (!token.IsCancellationRequested)
        {
            if (_readData.Count > 0)
            {
                int index = 0;
                while (index < buffer.Length && _readData.TryDequeue(out buffer[index]))
                { 
                    index++;
                }

                return index;
            }

            await Task.Delay(1, token);
        }

        return 0;
    }

    public int BaudRate { get; } 

    public bool IsOpen => _serialIoManager?.IsOpen ?? false;

    public TimeSpan ReplyTimeout { get; set; } = TimeSpan.FromMilliseconds(200);

    public void GetSerialPorts(IEnumerable<IUsbSerialDriver> drivers)
    {
        _ports.Clear();
        foreach (var driver in drivers)
        {
            foreach (var port in driver.Ports)
            {
                _ports.Add(new AvailableSerialPort(port.Driver.Device.DeviceName, port.Driver.Device.ProductName, string.Empty));
            }
        }
    }

    public void LoadPort(UsbManager usbManager, UsbSerialPort usbSerialPort)
    {
        _usbManager = usbManager;
        _usbSerialPort = usbSerialPort;
    }
}