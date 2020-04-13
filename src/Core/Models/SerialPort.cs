namespace OSDPBench.Core.Models
{
    public class SerialPort
    {
        public SerialPort(string name, string description)
        {
            Name = name;
            Description = description;
        }

        public string Name { get; }
        public string Description { get; }
    }
}
