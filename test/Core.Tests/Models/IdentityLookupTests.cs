using NUnit.Framework;
using OSDP.Net.Model.ReplyData;
using OSDPBench.Core.Models;

namespace OSDPBench.Core.Tests.Models;

[TestFixture(TestOf = typeof(IdentityLookup))]
public class IdentityLookupTests
{
    private static DeviceIdentification CreateDeviceIdentification(uint serialNumber)
    {
        byte[] data =
        [
            0xCA, 0x44, 0x6C, // Vendor code
            1, // Model number
            1, // Version
            (byte)serialNumber, (byte)(serialNumber >> 8), (byte)(serialNumber >> 16), (byte)(serialNumber >> 24), // Serial number (LSB first)
            1, 0, 0 // Firmware version
        ];

        return DeviceIdentification.ParseData(data);
    }

    [Test]
    public void SerialNumber_WhenSmallValue_ShowsDecimalAndHex()
    {
        var lookup = new IdentityLookup(CreateDeviceIdentification(42));

        Assert.That(lookup.SerialNumber, Is.EqualTo(42u));
        Assert.That(lookup.SerialNumberDisplay, Is.EqualTo("42 (0x0000002A)"));
    }

    [Test]
    public void SerialNumber_WhenHighBitSet_ShowsUnsignedValue()
    {
        var lookup = new IdentityLookup(CreateDeviceIdentification(0xF2345678));

        Assert.That(lookup.SerialNumber, Is.EqualTo(4063516280u));
        Assert.That(lookup.SerialNumberDisplay, Is.EqualTo("4063516280 (0xF2345678)"));
    }
}
