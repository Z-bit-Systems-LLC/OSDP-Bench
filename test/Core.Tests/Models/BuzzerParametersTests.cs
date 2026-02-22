using NUnit.Framework;
using OSDP.Net.Model.CommandData;
using OSDPBench.Core.Models;

namespace OSDPBench.Core.Tests.Models;

[TestFixture(TestOf = typeof(BuzzerParameters))]
public class BuzzerParametersTests
{
    [Test]
    public void IsOnTimeInvalid_WhenToneCodeDefaultAndOnTimeZero_ReturnsTrue()
    {
        var parameters = new BuzzerParameters { ToneCode = ToneCode.Default, OnTime = 0 };

        Assert.That(parameters.IsOnTimeInvalid, Is.True);
    }

    [Test]
    public void IsOnTimeInvalid_WhenToneCodeDefaultAndOnTimeNonZero_ReturnsFalse()
    {
        var parameters = new BuzzerParameters { ToneCode = ToneCode.Default, OnTime = 1 };

        Assert.That(parameters.IsOnTimeInvalid, Is.False);
    }

    [Test]
    public void IsOnTimeInvalid_WhenToneCodeOffAndOnTimeZero_ReturnsFalse()
    {
        var parameters = new BuzzerParameters { ToneCode = ToneCode.Off, OnTime = 0 };

        Assert.That(parameters.IsOnTimeInvalid, Is.False);
    }

    [Test]
    public void IsOnTimeInvalid_WhenToneCodeOffAndOnTimeNonZero_ReturnsFalse()
    {
        var parameters = new BuzzerParameters { ToneCode = ToneCode.Off, OnTime = 5 };

        Assert.That(parameters.IsOnTimeInvalid, Is.False);
    }
}
