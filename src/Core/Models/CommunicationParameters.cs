namespace OSDPBench.Core.Models
{
    public class CommunicationParameters
    {
        public CommunicationParameters(string portName, uint baudRate, int address)
        {
            PortName = portName;
            BaudRate = baudRate;
            Address = address;
        }

        public string PortName { get; }

        public uint BaudRate { get; }

        public int Address { get; }
    }
}
