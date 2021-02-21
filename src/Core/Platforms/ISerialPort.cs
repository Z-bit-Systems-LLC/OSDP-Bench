using System.Collections.Generic;
using System.Threading.Tasks;
using OSDP.Net.Connections;
using OSDPBench.Core.Models;

namespace OSDPBench.Core.Platforms
{
    public interface ISerialPortConnection : ISerialPort, IOsdpConnection
    {

    }
    
    public interface ISerialPort
    {
        /// <summary>
        /// Available the serial ports
        /// </summary>
        /// <returns></returns>
        Task<IEnumerable<SerialPort>> FindAvailableSerialPorts();

        /// <summary>
        /// Gets or sets the selected serial port.
        /// </summary>
        SerialPort SelectedSerialPort { get; set; }

        /// <summary>
        /// Sets the baud rate.
        /// </summary>
        /// <param name="baudRate">The baud rate.</param>
        void SetBaudRate(int baudRate);
    }
}
