using NUnit.Framework;
using OSDP.Net.Model.CommandData;
using OSDPBench.Windows.Views.Controls;

namespace OSDPBench.UI.Tests;

[TestFixture]
public class SetReaderLedValidationTests : UiTestBase
{
    [Test]
    public void IsTemporaryTimingInvalid_WhenSetTemporaryAndBothZero_ReturnsTrue()
    {
        InvokeOnUI(() =>
        {
            var control = new SetReaderLedControl();
            control.SelectedTemporaryMode =
                new KeyValuePair<string, TemporaryReaderControlCode>(
                    "Set temporary", TemporaryReaderControlCode.SetTemporaryAndStartTimer);
            control.TemporaryOnTime = 0;
            control.TemporaryOffTime = 0;

            Assert.That(control.IsTemporaryTimingInvalid, Is.True);
        });
    }

    [Test]
    public void IsTemporaryTimingInvalid_WhenSetTemporaryAndOnTimeNonZero_ReturnsFalse()
    {
        InvokeOnUI(() =>
        {
            var control = new SetReaderLedControl();
            control.SelectedTemporaryMode =
                new KeyValuePair<string, TemporaryReaderControlCode>(
                    "Set temporary", TemporaryReaderControlCode.SetTemporaryAndStartTimer);
            control.TemporaryOnTime = 1;
            control.TemporaryOffTime = 0;

            Assert.That(control.IsTemporaryTimingInvalid, Is.False);
        });
    }

    [Test]
    public void IsTemporaryTimingInvalid_WhenNopAndBothZero_ReturnsFalse()
    {
        InvokeOnUI(() =>
        {
            var control = new SetReaderLedControl();
            control.SelectedTemporaryMode =
                new KeyValuePair<string, TemporaryReaderControlCode>(
                    "NOP", TemporaryReaderControlCode.Nop);
            control.TemporaryOnTime = 0;
            control.TemporaryOffTime = 0;

            Assert.That(control.IsTemporaryTimingInvalid, Is.False);
        });
    }

    [Test]
    public void IsPermanentTimingInvalid_WhenSetPermanentAndBothZero_ReturnsTrue()
    {
        InvokeOnUI(() =>
        {
            var control = new SetReaderLedControl();
            control.SelectedPermanentMode =
                new KeyValuePair<string, PermanentReaderControlCode>(
                    "Set permanent", PermanentReaderControlCode.SetPermanentState);
            control.PermanentOnTime = 0;
            control.PermanentOffTime = 0;

            Assert.That(control.IsPermanentTimingInvalid, Is.True);
        });
    }

    [Test]
    public void IsPermanentTimingInvalid_WhenNopAndBothZero_ReturnsFalse()
    {
        InvokeOnUI(() =>
        {
            var control = new SetReaderLedControl();
            control.SelectedPermanentMode =
                new KeyValuePair<string, PermanentReaderControlCode>(
                    "NOP", PermanentReaderControlCode.Nop);
            control.PermanentOnTime = 0;
            control.PermanentOffTime = 0;

            Assert.That(control.IsPermanentTimingInvalid, Is.False);
        });
    }

    [Test]
    public void HasErrors_WhenTemporaryTimingInvalid_ReturnsTrue()
    {
        InvokeOnUI(() =>
        {
            var control = new SetReaderLedControl();
            control.SelectedTemporaryMode =
                new KeyValuePair<string, TemporaryReaderControlCode>(
                    "Set temporary", TemporaryReaderControlCode.SetTemporaryAndStartTimer);
            control.TemporaryOnTime = 0;
            control.TemporaryOffTime = 0;

            Assert.That(control.HasErrors, Is.True);
        });
    }

    [Test]
    public void HasErrors_WhenPermanentTimingInvalid_ReturnsTrue()
    {
        InvokeOnUI(() =>
        {
            var control = new SetReaderLedControl();
            control.SelectedPermanentMode =
                new KeyValuePair<string, PermanentReaderControlCode>(
                    "Set permanent", PermanentReaderControlCode.SetPermanentState);
            control.PermanentOnTime = 0;
            control.PermanentOffTime = 0;

            Assert.That(control.HasErrors, Is.True);
        });
    }

    [Test]
    public void HasErrors_DefaultState_ReturnsFalse()
    {
        InvokeOnUI(() =>
        {
            var control = new SetReaderLedControl();

            Assert.That(control.HasErrors, Is.False,
                "Default state should not have errors (temporary has non-zero defaults, permanent mode is NOP).");
        });
    }

    [Test]
    public void HasErrors_ClearsWhenPermanentModeChangedToNop()
    {
        InvokeOnUI(() =>
        {
            var control = new SetReaderLedControl();
            control.SelectedPermanentMode =
                new KeyValuePair<string, PermanentReaderControlCode>(
                    "Set permanent", PermanentReaderControlCode.SetPermanentState);
            control.PermanentOnTime = 0;
            control.PermanentOffTime = 0;

            Assert.That(control.HasErrors, Is.True, "Should have errors with SetPermanentState and zero timing.");

            control.SelectedPermanentMode =
                new KeyValuePair<string, PermanentReaderControlCode>(
                    "NOP", PermanentReaderControlCode.Nop);

            Assert.That(control.HasErrors, Is.False, "Should clear errors when mode changes to NOP.");
        });
    }

    [Test]
    public void HasErrors_ClearsWhenTimingSetToNonZero()
    {
        InvokeOnUI(() =>
        {
            var control = new SetReaderLedControl();
            control.SelectedPermanentMode =
                new KeyValuePair<string, PermanentReaderControlCode>(
                    "Set permanent", PermanentReaderControlCode.SetPermanentState);
            control.PermanentOnTime = 0;
            control.PermanentOffTime = 0;

            Assert.That(control.HasErrors, Is.True, "Should have errors with zero timing.");

            control.PermanentOnTime = 1;

            Assert.That(control.HasErrors, Is.False, "Should clear errors when ON time set to non-zero.");
        });
    }

    [Test]
    public void PropertyChanged_RaisedForValidationProperties_WhenPermanentModeChanges()
    {
        InvokeOnUI(() =>
        {
            var control = new SetReaderLedControl();
            var changedProperties = new List<string>();
            control.PropertyChanged += (_, e) => changedProperties.Add(e.PropertyName!);

            control.SelectedPermanentMode =
                new KeyValuePair<string, PermanentReaderControlCode>(
                    "Set permanent", PermanentReaderControlCode.SetPermanentState);

            Assert.That(changedProperties, Does.Contain(nameof(control.IsPermanentTimingInvalid)));
            Assert.That(changedProperties, Does.Contain(nameof(control.HasErrors)));
        });
    }
}
