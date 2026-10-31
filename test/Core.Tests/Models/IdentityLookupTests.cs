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

    private static DeviceIdentification CreateDeviceIdentification(byte[] vendorCode, byte modelNumber) =>
        DeviceIdentification.ParseData([.. vendorCode, modelNumber, 1, 1, 0, 0, 0, 1, 8, 1]);

    [Test]
    public void KnownVendorAndModel_AreNamed()
    {
        var lookup = new IdentityLookup(CreateDeviceIdentification([0x5C, 0x26, 0x23], 20));

        Assert.That(lookup.VendorCode, Is.EqualTo("5C-26-23"));
        Assert.That(lookup.VendorName, Is.EqualTo("WaveLynx Technologies Corporation"));
        Assert.That(lookup.Model, Is.EqualTo("Ethos ET20"));
        Assert.That(lookup.CanSendResetCommand, Is.False);
        Assert.That(lookup.ResetInstructions, Does.StartWith("To return to OSDP auto-detect mode"));
    }

    [Test]
    public void KnownVendor_UnknownModel_KeepsTheVendorsResetDetails()
    {
        var lookup = new IdentityLookup(CreateDeviceIdentification([0xDA, 0x0D, 0x38], 9));

        Assert.That(lookup.VendorName, Is.EqualTo("Farpointe Data, Inc."));
        Assert.That(lookup.Model, Is.EqualTo("Unknown Model"));
        Assert.That(lookup.CanSendResetCommand, Is.True);
    }

    [Test]
    public void UnknownVendor_UsesTheDefaults()
    {
        var lookup = new IdentityLookup(CreateDeviceIdentification([0x5A, 0x42, 0x01], 1));

        Assert.That(lookup.VendorCode, Is.EqualTo("5A-42-01"));
        Assert.That(lookup.VendorName, Is.EqualTo("Unknown Vendor"));
        Assert.That(lookup.Model, Is.EqualTo("Unknown Model"));
        Assert.That(lookup.CanSendResetCommand, Is.False);
        Assert.That(lookup.ResetInstructions, Is.EqualTo("No reset instructions are available for this device."));
    }

    [Test]
    public void Version_JoinsTheFirmwareParts()
    {
        var lookup = new IdentityLookup(CreateDeviceIdentification([0xDA, 0x0D, 0x38], 1));

        Assert.That(lookup.VersionNumber, Is.EqualTo("1.8.1"));
    }
}
