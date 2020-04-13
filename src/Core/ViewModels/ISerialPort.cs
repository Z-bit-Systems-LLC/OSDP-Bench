using System.Collections.Generic;
using System.Threading.Tasks;
using OSDPBench.Core.Models;

namespace OSDPBench.Core.ViewModels
{
    public interface ISerialPort
    {
        /// <summary>
        /// Available the serial ports
        /// </summary>
        /// <returns></returns>
        Task<IEnumerable<SerialPort>> FindAvailableSerialPorts();
    }
}
