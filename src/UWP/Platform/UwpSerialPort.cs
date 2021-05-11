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

        /// <inheritdoc />
        public async Task<IEnumerable<AvailableSerialPort>> FindAvailableSerialPorts()
        {
            var availableSerialPorts = new List<AvailableSerialPort>();

            foreach (var item in await DeviceInformation.FindAllAsync(SerialDevice.GetDeviceSelector()))
            {
                using (var serialDevice = await SerialDevice.FromIdAsync(item.Id))
                {
                    if (serialDevice != null)
                    {
                        availableSerialPorts.Add(new AvailableSerialPort(item.Id, serialDevice.PortName, item.Name));
                    }
                }
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
            try
            {
                var selectedSerialPortId = SelectedSerialPort.Id;

                if (selectedSerialPortId != null)
                {
                    _serialDevice = AsyncHelper.RunSync(() => SerialDevice.FromIdAsync(selectedSerialPortId).AsTask());
                }
            }
            catch
            {
                return;
            }

            if (_serialDevice != null)
            {
                _serialDevice.BaudRate = (uint) BaudRate;
                _serialDevice.Parity = SerialParity.None;
                _serialDevice.StopBits = SerialStopBitCount.One;
                _serialDevice.DataBits = 8;
                _serialDevice.Handshake = SerialHandshake.None;
            }
        }

        /// <inheritdoc />
        public void Close()
        {
            _serialDevice?.InputStream?.Dispose();
            _serialDevice?.OutputStream?.Dispose();
            _serialDevice?.Dispose();
            _serialDevice = null;
        }

        /// <inheritdoc />
        public async Task WriteAsync(byte[] buffer)
        {
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

        /// <inheritdoc />
        public async Task<int> ReadAsync(byte[] buffer, CancellationToken token)
        {
            uint result;
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

        /// <inheritdoc />
        public int BaudRate { get; private set; } = 9600;

        /// <inheritdoc />
        public bool IsOpen => _serialDevice != null;

        /// <inheritdoc />
        public TimeSpan ReplyTimeout { get; set; } = TimeSpan.FromMilliseconds(200);
    }
}
