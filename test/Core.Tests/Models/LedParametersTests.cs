using NUnit.Framework;
using OSDP.Net.Model.CommandData;
using OSDPBench.Core.Models;

namespace OSDPBench.Core.Tests.Models;

[TestFixture(TestOf = typeof(LedParameters))]
public class LedParametersTests
{
    [Test]
    public void IsTemporaryTimingInvalid_WhenSetTemporaryAndBothZero_ReturnsTrue()
    {
        var parameters = new LedParameters
        {
            TemporaryMode = TemporaryReaderControlCode.SetTemporaryAndStartTimer,
            TemporaryOnTime = 0,
            TemporaryOffTime = 0
        };

        Assert.That(parameters.IsTemporaryTimingInvalid, Is.True);
    }

    [Test]
    public void IsTemporaryTimingInvalid_WhenSetTemporaryAndOnTimeNonZero_ReturnsFalse()
    {
        var parameters = new LedParameters
        {
            TemporaryMode = TemporaryReaderControlCode.SetTemporaryAndStartTimer,
            TemporaryOnTime = 1,
            TemporaryOffTime = 0
        };

        Assert.That(parameters.IsTemporaryTimingInvalid, Is.False);
    }

    [Test]
    public void IsTemporaryTimingInvalid_WhenSetTemporaryAndOffTimeNonZero_ReturnsFalse()
    {
        var parameters = new LedParameters
        {
            TemporaryMode = TemporaryReaderControlCode.SetTemporaryAndStartTimer,
            TemporaryOnTime = 0,
            TemporaryOffTime = 1
        };

        Assert.That(parameters.IsTemporaryTimingInvalid, Is.False);
    }

    [Test]
    public void IsTemporaryTimingInvalid_WhenNopAndBothZero_ReturnsFalse()
    {
        var parameters = new LedParameters
        {
            TemporaryMode = TemporaryReaderControlCode.Nop,
            TemporaryOnTime = 0,
            TemporaryOffTime = 0
        };

        Assert.That(parameters.IsTemporaryTimingInvalid, Is.False);
    }

    [Test]
    public void IsTemporaryTimingInvalid_WhenCancelAndBothZero_ReturnsFalse()
    {
        var parameters = new LedParameters
        {
            TemporaryMode = TemporaryReaderControlCode.CancelAnyTemporaryAndDisplayPermanent,
            TemporaryOnTime = 0,
            TemporaryOffTime = 0
        };

        Assert.That(parameters.IsTemporaryTimingInvalid, Is.False);
    }

    [Test]
    public void IsPermanentTimingInvalid_WhenSetPermanentAndBothZero_ReturnsTrue()
    {
        var parameters = new LedParameters
        {
            PermanentMode = PermanentReaderControlCode.SetPermanentState,
            PermanentOnTime = 0,
            PermanentOffTime = 0
        };

        Assert.That(parameters.IsPermanentTimingInvalid, Is.True);
    }

    [Test]
    public void IsPermanentTimingInvalid_WhenSetPermanentAndOnTimeNonZero_ReturnsFalse()
    {
        var parameters = new LedParameters
        {
            PermanentMode = PermanentReaderControlCode.SetPermanentState,
            PermanentOnTime = 1,
            PermanentOffTime = 0
        };

        Assert.That(parameters.IsPermanentTimingInvalid, Is.False);
    }

    [Test]
    public void IsPermanentTimingInvalid_WhenNopAndBothZero_ReturnsFalse()
    {
        var parameters = new LedParameters
        {
            PermanentMode = PermanentReaderControlCode.Nop,
            PermanentOnTime = 0,
            PermanentOffTime = 0
        };

        Assert.That(parameters.IsPermanentTimingInvalid, Is.False);
    }
}
