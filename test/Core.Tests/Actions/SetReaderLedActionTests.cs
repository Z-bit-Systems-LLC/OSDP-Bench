using NUnit.Framework;
using OSDP.Net.Model.ReplyData;
using OSDPBench.Core.Actions;

namespace OSDPBench.Core.Tests.Actions;

[TestFixture(TestOf = typeof(SetReaderLedAction))]
public class SetReaderLedActionTests
{
    private SetReaderLedAction _action = null!;

    [SetUp]
    public void Setup()
    {
        _action = new SetReaderLedAction();
    }

    [Test]
    public void RequiredCapability_ReturnsReaderLedControl()
    {
        Assert.That(_action.RequiredCapability, Is.EqualTo(CapabilityFunction.ReaderLEDControl));
    }

    [Test]
    public void PerformActionName_ReturnsSend()
    {
        Assert.That(_action.PerformActionName, Is.EqualTo("Send"));
    }

    [Test]
    public void Name_ReturnsTestLed()
    {
        Assert.That(_action.Name, Is.EqualTo("Test LED"));
    }
}
