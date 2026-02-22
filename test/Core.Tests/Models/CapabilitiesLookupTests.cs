using NUnit.Framework;
using OSDP.Net.Model.ReplyData;
using OSDPBench.Core.Models;

namespace OSDPBench.Core.Tests.Models;

[TestFixture(TestOf = typeof(CapabilitiesLookup))]
public class CapabilitiesLookupTests
{
    private static DeviceCapabilities CreateCapabilities(params DeviceCapability[] capabilities)
    {
        return new DeviceCapabilities(capabilities);
    }

    [Test]
    public void AudioOutput_WhenComplianceIsOne_ReturnsTrue()
    {
        var capabilities = CreateCapabilities(
            new DeviceCapability(CapabilityFunction.ReaderAudibleOutput, 1, 1));

        var lookup = new CapabilitiesLookup(capabilities);

        Assert.That(lookup.AudioOutput, Is.True);
    }

    [Test]
    public void AudioOutput_WhenComplianceIsTwo_ReturnsTrue()
    {
        var capabilities = CreateCapabilities(
            new DeviceCapability(CapabilityFunction.ReaderAudibleOutput, 2, 1));

        var lookup = new CapabilitiesLookup(capabilities);

        Assert.That(lookup.AudioOutput, Is.True);
    }

    [Test]
    public void AudioOutput_WhenComplianceIsZero_ReturnsFalse()
    {
        var capabilities = CreateCapabilities(
            new DeviceCapability(CapabilityFunction.ReaderAudibleOutput, 0, 1));

        var lookup = new CapabilitiesLookup(capabilities);

        Assert.That(lookup.AudioOutput, Is.False);
    }

    [Test]
    public void AudioOutput_WhenCapabilityNotPresent_ReturnsFalse()
    {
        var capabilities = CreateCapabilities();

        var lookup = new CapabilitiesLookup(capabilities);

        Assert.That(lookup.AudioOutput, Is.False);
    }

    [Test]
    public void AudioOutputComplianceLevel_ReturnsCorrectValue()
    {
        var capabilities = CreateCapabilities(
            new DeviceCapability(CapabilityFunction.ReaderAudibleOutput, 2, 1));

        var lookup = new CapabilitiesLookup(capabilities);

        Assert.That(lookup.AudioOutputComplianceLevel, Is.EqualTo(2));
    }

    [Test]
    public void AudioOutputComplianceLevel_WhenNotPresent_ReturnsZero()
    {
        var capabilities = CreateCapabilities();

        var lookup = new CapabilitiesLookup(capabilities);

        Assert.That(lookup.AudioOutputComplianceLevel, Is.EqualTo(0));
    }

    [Test]
    public void LedControl_WhenComplianceIsOne_ReturnsTrue()
    {
        var capabilities = CreateCapabilities(
            new DeviceCapability(CapabilityFunction.ReaderLEDControl, 1, 1));

        var lookup = new CapabilitiesLookup(capabilities);

        Assert.That(lookup.LedControl, Is.True);
    }

    [Test]
    public void LedControl_WhenComplianceIsZero_ReturnsFalse()
    {
        var capabilities = CreateCapabilities(
            new DeviceCapability(CapabilityFunction.ReaderLEDControl, 0, 1));

        var lookup = new CapabilitiesLookup(capabilities);

        Assert.That(lookup.LedControl, Is.False);
    }

    [Test]
    public void LedControl_WhenCapabilityNotPresent_ReturnsFalse()
    {
        var capabilities = CreateCapabilities();

        var lookup = new CapabilitiesLookup(capabilities);

        Assert.That(lookup.LedControl, Is.False);
    }
}
