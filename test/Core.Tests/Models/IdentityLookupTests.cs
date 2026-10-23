using NUnit.Framework;
using OSDP.Net.Model.ReplyData;
using OSDPBench.Core.Models;

namespace OSDPBench.Core.Tests.Models;

[TestFixture(TestOf = typeof(IdentityLookup))]
public class IdentityLookupTests
{
    private static DeviceIdentification CreateDeviceIdentification(params byte[] serialNumber)
    {
        byte[] data =
        [
            0xCA, 0x44, 0x6C, // Vendor code
            1, // Model number
            1, // Version
            serialNumber[0], serialNumber[1], serialNumber[2], serialNumber[3], // Serial number (LSB first)
            1, 0, 0 // Firmware version
        ];

        return DeviceIdentification.ParseData(data);
    }

    [Test]
    public void SerialNumber_WhenSmallValue_ShowsDecimalAndHex()
    {
        var lookup = new IdentityLookup(CreateDeviceIdentification(0x2A, 0x00, 0x00, 0x00));

        Assert.That(lookup.SerialNumber, Is.EqualTo("42 (0x0000002A)"));
    }

    [Test]
    public void SerialNumber_WhenHighBitSet_ShowsUnsignedValue()
    {
        var lookup = new IdentityLookup(CreateDeviceIdentification(0x78, 0x56, 0x34, 0xF2));

        Assert.That(lookup.SerialNumber, Is.EqualTo("4063516280 (0xF2345678)"));
    }
}
