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
    /// <summary>
    /// Class UwpSerialPort.
    /// Implements the <see cref="OSDPBench.Core.Platforms.ISerialPortConnection" />
    /// </summary>
    /// <seealso cref="OSDPBench.Core.Platforms.ISerialPortConnection" />
    public class UwpSerialPort : ISerialPortConnection
    {
        private SerialDevice _serialDevice;

        private static readonly SemaphoreSlim SerialDeviceSemaphore = new SemaphoreSlim(1, 1);

        /// <inheritdoc />
        public async Task<IEnumerable<AvailableSerialPort>> FindAvailableSerialPorts()
        {
            var availableSerialPorts = new List<AvailableSerialPort>();

            await SerialDeviceSemaphore.WaitAsync();

            try
            {
                foreach (var item in await DeviceInformation.FindAllAsync(SerialDevice.GetDeviceSelector()))
                {
                    using (var serialDevice = await SerialDevice.FromIdAsync(item.Id))
                    {
                        if (serialDevice != null)
                        {
                            availableSerialPorts.Add(new AvailableSerialPort(item.Id, serialDevice.PortName,
                                item.Name));
                        }
                    }
                }
            }
            finally
            {
                SerialDeviceSemaphore.Release();
            }

            return availableSerialPorts;
        }

        /// <inheritdoc />
        public AvailableSerialPort SelectedSerialPort { get; set; }

        /// <inheritdoc />
        public void SetBaudRate(int baudRate)
        {
            BaudRate = baudRate;
        }

        /// <inheritdoc />
        public void Open()
        {
            SerialDeviceSemaphore.WaitAsync().GetAwaiter().GetResult();

            try
            {
                var selectedSerialPortId = SelectedSerialPort.Id;

                if (selectedSerialPortId != null)
                {
                    _serialDevice = AsyncHelper.RunSync(() => SerialDevice.FromIdAsync(selectedSerialPortId).AsTask());
                }
                
                if (_serialDevice == null) return;

                _serialDevice.BaudRate = (uint)BaudRate;
                _serialDevice.Parity = SerialParity.None;
                _serialDevice.StopBits = SerialStopBitCount.One;
                _serialDevice.DataBits = 8;
                _serialDevice.Handshake = SerialHandshake.None;
            }
            finally
            {
                SerialDeviceSemaphore.Release();
            }
        }

        /// <inheritdoc />
        public void Close()
        {
            SerialDeviceSemaphore.WaitAsync().GetAwaiter().GetResult();

            try
            {
                _serialDevice?.Dispose();
                _serialDevice = null;
            }
            finally
            {
                SerialDeviceSemaphore.Release();
            }
        }

        /// <inheritdoc />
        public async Task WriteAsync(byte[] buffer)
        {
            await SerialDeviceSemaphore.WaitAsync();

            try
            {
                if (_serialDevice == null) return;

                using (var dataWriter = new DataWriter(_serialDevice?.OutputStream))
                {
                    try
                    {
                        dataWriter.WriteBytes(buffer);
                        await dataWriter.StoreAsync().AsTask();
                    }
                    finally
                    {
                        dataWriter.DetachStream();
                    }
                }
            }
            finally
            {
                SerialDeviceSemaphore.Release();
            }
        }

        /// <inheritdoc />
        public async Task<int> ReadAsync(byte[] buffer, CancellationToken token)
        {
            await SerialDeviceSemaphore.WaitAsync(token);

            try
            {
                uint result;

                if (_serialDevice == null) return 0;

                using (var dataReader = new DataReader(_serialDevice?.InputStream)
                    {InputStreamOptions = InputStreamOptions.Partial})
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

                return (int) result;
            }
            finally
            {
                SerialDeviceSemaphore.Release();
            }
        }

        /// <inheritdoc />
        public int BaudRate { get; private set; } = 9600;

        /// <inheritdoc />
        public bool IsOpen => _serialDevice != null;

        /// <inheritdoc />
        public TimeSpan ReplyTimeout { get; set; } = TimeSpan.FromMilliseconds(200);
    }
}
