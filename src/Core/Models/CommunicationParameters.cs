namespace OSDPBench.Core.Models
{
    public class CommunicationParameters
    {
        public CommunicationParameters(string portName, uint baudRate, byte address)
        {
            PortName = portName;
            BaudRate = baudRate;
            Address = address;
        }

        public string PortName { get; }

        public uint BaudRate { get; }

        public byte Address { get; }
    }
}
