using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using Windows.Devices.Enumeration;
using Windows.Devices.SerialCommunication;
using Windows.Storage.Streams;
using OSDPBench.Core;
using OSDPBench.Core.Models;
using OSDPBench.Core.Platforms;

namespace OSDPBenchUWP.Platform
{
    public class UwpSerialPort : ISerialPortConnection
    {
        private SerialDevice _serialDevice;

        public async Task<IEnumerable<SerialPort>> FindAvailableSerialPorts()
        {
            var availableSerialPorts = new List<SerialPort>();

            foreach (var item in await DeviceInformation.FindAllAsync(SerialDevice.GetDeviceSelector()))
            {
                using (var serialDevice = await SerialDevice.FromIdAsync(item.Id))
                {
                    availableSerialPorts.Add(new SerialPort(item.Id, serialDevice.PortName, item.Name));
                }
            }

            return availableSerialPorts;
        }

        public SerialPort SelectedSerialPort { get; set; }

        public void SetBaudRate(int baudRate)
        {
            BaudRate = baudRate;
        }

        public void Open()
        {
            _serialDevice?.Dispose();

            _serialDevice = AsyncHelper.RunSync(() => SerialDevice.FromIdAsync(SelectedSerialPort.Id).AsTask());

            if (_serialDevice != null)
            {
                _serialDevice.BaudRate = (uint) BaudRate;
                _serialDevice.Parity = SerialParity.None;
                _serialDevice.StopBits = SerialStopBitCount.One;
                _serialDevice.DataBits = 8;
                _serialDevice.Handshake = SerialHandshake.None;
            }
        }

        public void Close()
        {
            _serialDevice.Dispose();
            _serialDevice = null;
        }

        public async Task WriteAsync(byte[] buffer)
        {
            using (var dataWriter = new DataWriter(_serialDevice.OutputStream))
            {
                try
                {
                    dataWriter.WriteBytes(buffer);
                    await dataWriter.StoreAsync();
                }
                finally
                {
                    dataWriter.DetachStream();
                }
            }
        }

        public async Task<int> ReadAsync(byte[] buffer, CancellationToken token)
        {
            uint result;
            using (var dataReader = new DataReader(_serialDevice.InputStream) {InputStreamOptions = InputStreamOptions.Partial})
            {
                try
                {
                    result = await dataReader.LoadAsync((uint) buffer.Length).AsTask(token);
                    dataReader.ReadBytes(buffer);
                }
                finally
                {
                    dataReader.DetachStream();
                }
            }

            return (int)result;
        }

        public int BaudRate { get; private set; } = 9600;

        public bool IsOpen => _serialDevice != null;

        public TimeSpan ReplyTimeout { get; set; } = TimeSpan.FromMilliseconds(200);
    }
}
