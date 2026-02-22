using NUnit.Framework;
using OSDP.Net.Model.CommandData;
using OSDPBench.Windows.Views.Controls;

namespace OSDPBench.UI.Tests;

[TestFixture]
public class ControlBuzzerValidationTests : UiTestBase
{
    [Test]
    public void IsOnTimeInvalid_WhenToneCodeDefaultAndOnTimeZero_ReturnsTrue()
    {
        InvokeOnUI(() =>
        {
            var control = new ControlBuzzerControl();
            control.SelectedToneCode = ToneCode.Default;
            control.OnTime = 0;

            Assert.That(control.IsOnTimeInvalid, Is.True);
        });
    }

    [Test]
    public void IsOnTimeInvalid_WhenToneCodeDefaultAndOnTimeNonZero_ReturnsFalse()
    {
        InvokeOnUI(() =>
        {
            var control = new ControlBuzzerControl();
            control.SelectedToneCode = ToneCode.Default;
            control.OnTime = 5;

            Assert.That(control.IsOnTimeInvalid, Is.False);
        });
    }

    [Test]
    public void IsOnTimeInvalid_WhenToneCodeOffAndOnTimeZero_ReturnsFalse()
    {
        InvokeOnUI(() =>
        {
            var control = new ControlBuzzerControl();
            control.SelectedToneCode = ToneCode.Off;
            control.OnTime = 0;

            Assert.That(control.IsOnTimeInvalid, Is.False);
        });
    }

    [Test]
    public void HasErrors_WhenOnTimeInvalid_ReturnsTrue()
    {
        InvokeOnUI(() =>
        {
            var control = new ControlBuzzerControl();
            control.SelectedToneCode = ToneCode.Default;
            control.OnTime = 0;

            Assert.That(control.HasErrors, Is.True);
        });
    }

    [Test]
    public void HasErrors_WhenOnTimeValid_ReturnsFalse()
    {
        InvokeOnUI(() =>
        {
            var control = new ControlBuzzerControl();
            control.SelectedToneCode = ToneCode.Default;
            control.OnTime = 1;

            Assert.That(control.HasErrors, Is.False);
        });
    }

    [Test]
    public void HasErrors_ClearsWhenToneCodeChangedToOff()
    {
        InvokeOnUI(() =>
        {
            var control = new ControlBuzzerControl();
            control.SelectedToneCode = ToneCode.Default;
            control.OnTime = 0;

            Assert.That(control.HasErrors, Is.True, "Should have errors with Default tone and zero ON time.");

            control.SelectedToneCode = ToneCode.Off;

            Assert.That(control.HasErrors, Is.False, "Should clear errors when tone code changes to Off.");
        });
    }

    [Test]
    public void HasErrors_ClearsWhenOnTimeSetToNonZero()
    {
        InvokeOnUI(() =>
        {
            var control = new ControlBuzzerControl();
            control.SelectedToneCode = ToneCode.Default;
            control.OnTime = 0;

            Assert.That(control.HasErrors, Is.True, "Should have errors with zero ON time.");

            control.OnTime = 1;

            Assert.That(control.HasErrors, Is.False, "Should clear errors when ON time set to non-zero.");
        });
    }

    [Test]
    public void PropertyChanged_RaisedForValidationProperties_WhenOnTimeChanges()
    {
        InvokeOnUI(() =>
        {
            var control = new ControlBuzzerControl();
            var changedProperties = new List<string>();
            control.PropertyChanged += (_, e) => changedProperties.Add(e.PropertyName!);

            control.OnTime = 0;

            Assert.That(changedProperties, Does.Contain(nameof(control.IsOnTimeInvalid)));
            Assert.That(changedProperties, Does.Contain(nameof(control.HasErrors)));
        });
    }

    [Test]
    public void PropertyChanged_RaisedForValidationProperties_WhenToneCodeChanges()
    {
        InvokeOnUI(() =>
        {
            var control = new ControlBuzzerControl();
            var changedProperties = new List<string>();
            control.PropertyChanged += (_, e) => changedProperties.Add(e.PropertyName!);

            control.SelectedToneCode = ToneCode.Off;

            Assert.That(changedProperties, Does.Contain(nameof(control.IsOnTimeInvalid)));
            Assert.That(changedProperties, Does.Contain(nameof(control.HasErrors)));
        });
    }
}
