using NUnit.Framework;
using OSDP.Net.Model.ReplyData;
using OSDPBench.Core.Actions;

namespace OSDPBench.Core.Tests.Actions;

[TestFixture(TestOf = typeof(ControlBuzzerAction))]
public class ControlBuzzerActionTests
{
    private ControlBuzzerAction _action = null!;

    [SetUp]
    public void Setup()
    {
        _action = new ControlBuzzerAction();
    }

    [Test]
    public void RequiredCapability_ReturnsReaderAudibleOutput()
    {
        Assert.That(_action.RequiredCapability, Is.EqualTo(CapabilityFunction.ReaderAudibleOutput));
    }

    [Test]
    public void PerformActionName_ReturnsSend()
    {
        Assert.That(_action.PerformActionName, Is.EqualTo("Send"));
    }

    [Test]
    public void Name_ReturnsTestBuzzer()
    {
        Assert.That(_action.Name, Is.EqualTo("Test Buzzer"));
    }
}
