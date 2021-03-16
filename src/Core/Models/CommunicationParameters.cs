namespace OSDPBench.Core.Models
{
    public class CommunicationParameters
    {
        public CommunicationParameters(uint baudRate, int address)
        {
            BaudRate = baudRate;
            Address = address;
        }

        public uint BaudRate { get; }

        public int Address { get; }
    }
}
