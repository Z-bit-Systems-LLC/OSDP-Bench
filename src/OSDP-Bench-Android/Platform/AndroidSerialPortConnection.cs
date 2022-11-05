using System.Collections.Concurrent;
using Android.Hardware.Usb;
using Hoho.Android.UsbSerial.Driver;
using Hoho.Android.UsbSerial.Extensions;
using OSDPBench.Core.Models;
using OSDPBench.Core.Platforms;

namespace OSDP_Bench_Android.Platform;

internal class AndroidSerialPortConnection : ISerialPortConnection
{
    private readonly ConcurrentBag<AvailableSerialPort> _ports = new();
    private UsbManager? _usbManager;
    private UsbSerialPort? _usbSerialPort;
    private SerialInputOutputManager? _serialIoManager;
    private readonly ConcurrentQueue<byte> _readData = new();

    /// <inheritdoc />
    public async Task<IEnumerable<AvailableSerialPort>> FindAvailableSerialPorts()
    {
        return await Task.FromResult(_ports);
    }

    public ISerialPortConnection CreateSerialPort(string name, int baudRate)
    {
        BaudRate = baudRate;      
        
        return this;
    }

    public void Open()
    {
        if (BaudRate == 0) return;
        
        try
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
        if (_serialIoManager is not { IsOpen: true }) return;
        
        try
        {
            _serialIoManager?.Close();
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

    public int BaudRate { get; private set; } 

    public bool IsOpen => _serialIoManager?.IsOpen ?? false;
    public TimeSpan ReplyTimeout { get; set; } = TimeSpan.FromSeconds(8);

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