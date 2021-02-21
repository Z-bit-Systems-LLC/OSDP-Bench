namespace OSDPBench.Core.Models
{
    public class SerialPort
    {
        public SerialPort(string id, string name, string description)
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
