namespace OSDPBench.Core.Models
{
    public class AvailableSerialPort
    {
        public AvailableSerialPort(string id, string name, string description)
        {
            Id = id;
            Name = name;
            Description = description;
        }

        public string Name { get; }
        public string Description { get; }
        public string Id { get; }
    }
}
