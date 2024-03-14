namespace MvvmCore.Models;

public class CommunicationParameters(string portName, uint baudRate, byte address)
{
    public string PortName { get; } = portName;

    public uint BaudRate { get; } = baudRate;

    public byte Address { get; } = address;
}