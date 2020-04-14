using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using Windows.Devices.Enumeration;
using Windows.Devices.SerialCommunication;
using OSDPBench.Core.Models;
using OSDPBench.Core.Platforms;

namespace OSDPBenchUWP.Platform
{
    public class UwpSerialPort : ISerialPort
    {
        public async Task<IEnumerable<SerialPort>> FindAvailableSerialPorts()
        {
            var availableSerialPorts = new List<SerialPort>();

            foreach (var item in await DeviceInformation.FindAllAsync(SerialDevice.GetDeviceSelector()))
            {
                using (var serialDevice = await SerialDevice.FromIdAsync(item.Id))
                {
                    availableSerialPorts.Add(new SerialPort(serialDevice.PortName, item.Name));
                }
            }

            return availableSerialPorts;
        }
    }
}
