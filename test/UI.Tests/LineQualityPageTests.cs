using System.Windows;
using System.Windows.Controls.Primitives;
using System.Windows.Media;
using FlaUI.Core.AutomationElements;
using FlaUI.Core.Definitions;
using FlaUI.Core.Input;
using Moq;
using NUnit.Framework;
using OSDP.Net.LineQuality;
using OSDP.Net.Tracing;
using OSDPBench.Core.ViewModels.Pages;
using OSDPBench.UI.Tests.Helpers;
using OSDPBench.Windows.Views.Controls;
using OSDPBench.Windows.Views.Pages;

namespace OSDPBench.UI.Tests;

[TestFixture]
public class LineQualityPageTests : UiTestBase
{
    [SetUp]
    public void NavigateToLineQualityPage()
    {
        NavigateToPage("NavItem_LineQuality", "LineQuality_StartTest");

        var viewModel = InvokeOnUI(() => TestApp.GetService<LineQualityViewModel>());

        // The page renders before the port scan lands, and a run started with no port selected
        // does nothing at all, so every test waits for the list rather than racing it.
        Assert.That(viewModel.InitializationComplete.Wait(TimeSpan.FromSeconds(5)), Is.True,
            "The serial port scan should finish before a test drives the page.");
        Assert.That(InvokeOnUI(() => viewModel.SelectedSerialPort), Is.Not.Null,
            "The scan should leave a port selected for the run to use.");

        // Each test starts from the controller role, which is the page's default.
        InvokeOnUI(() => viewModel.IsControllerMode = true);
    }

    [Test]
    public void LineQualityPageLoadsSuccessfully()
    {
        Assert.That(MainWindow!.IsAvailable, Is.True,
            "Main window should be available after navigating to the Line Quality page.");
    }

    [Test]
    public void ControllerControlsExist()
    {
        Assert.Multiple(() =>
        {
            Assert.That(WaitForElement("LineQuality_SerialPort"), Is.Not.Null,
                "Serial port selector should exist.");
            Assert.That(WaitForElement("LineQuality_Profile"), Is.Not.Null,
                "Profile selector should exist.");
            Assert.That(WaitForElement("LineQuality_Address"), Is.Not.Null,
                "Address entry should exist.");
            Assert.That(WaitForElement("LineQuality_StartTest"), Is.Not.Null,
                "Start Test button should exist.");
            Assert.That(WaitForElement("LineQuality_BaudRateTags"), Is.Not.Null,
                "Chosen baud rates should be shown as tags.");
            Assert.That(WaitForElement("LineQuality_BaudRatePicker"), Is.Not.Null,
                "Baud rate picker should exist.");
        });
    }

    /// <summary>
    /// Whether the baud rate picker is currently open.
    /// </summary>
    /// <remarks>
    /// Read from the popup itself rather than from the window's child popup handle: the handle
    /// outlives a close, so a test that watched it would race the window teardown instead of the
    /// state it means to assert on.
    /// </remarks>
    private bool PickerIsOpen() => InvokeOnUI(() =>
        ((Popup)TestApp.GetService<LineQualityPage>().FindName("BaudRatePickerPopup")!).IsOpen);

    private void OpenPicker(AutomationElement toggle)
    {
        toggle.Click();
        Assert.That(AssemblySetup.Retry(() => PickerIsOpen() ? "open" : null, TimeSpan.FromSeconds(2)),
            Is.EqualTo("open"), "The picker should open.");
    }

    private void ClosePicker(AutomationElement toggle)
    {
        if (!PickerIsOpen()) return;

        toggle.Click();
        AssemblySetup.Retry(() => PickerIsOpen() ? null : "closed", TimeSpan.FromSeconds(2));
    }

    [Test]
    public void RemovingATagDropsOnlyThatRate()
    {
        var viewModel = InvokeOnUI(() => TestApp.GetService<LineQualityViewModel>());
        InvokeOnUI(() => viewModel.SelectAllBaudRatesCommand.Execute(null));

        var tags = WaitForElement("LineQuality_BaudRateTags");
        Assert.That(tags, Is.Not.Null, "Tag list should exist.");

        try
        {
            // Only chosen rates have a tag, so the first remove button belongs to the first rate.
            var removeFirst = tags!.FindAllDescendants(cf => cf.ByControlType(ControlType.Button))[0];
            removeFirst.Click();

            Assert.That(AssemblySetup.Retry(
                    () => InvokeOnUI(() => viewModel.BaudRateOptions[0].IsSelected) ? null : "dropped",
                    TimeSpan.FromSeconds(2)),
                Is.EqualTo("dropped"), "The tag's remove button should drop that rate.");

            Assert.That(InvokeOnUI(() => viewModel.BaudRateOptions.Count(option => option.IsSelected)),
                Is.EqualTo(5), "Only the rate whose tag was removed should have changed.");
        }
        finally
        {
            InvokeOnUI(() => viewModel.SelectAllBaudRatesCommand.Execute(null));
        }
    }

    [Test]
    public void PickerOpensAndClosesFromTheSameControl()
    {
        // The picker closes itself when a press lands outside it, and the control that opens it is
        // outside it. Without care that press also reads as a request to open it again, which
        // leaves the picker impossible to close from the control that opened it.
        var toggle = WaitForElement("LineQuality_BaudRatePicker");
        Assert.That(toggle, Is.Not.Null, "Baud rate picker should exist.");

        try
        {
            OpenPicker(toggle!);

            toggle!.Click();
            Assert.That(AssemblySetup.Retry(
                    () => PickerIsOpen() ? null : "closed", TimeSpan.FromSeconds(2)),
                Is.EqualTo("closed"), "Clicking again should close the picker rather than reopen it.");
        }
        finally
        {
            ClosePicker(toggle!);
        }
    }

    [Test]
    public void PickerBulkActionsChangeWhichRatesAreTagged()
    {
        var viewModel = InvokeOnUI(() => TestApp.GetService<LineQualityViewModel>());
        var toggle = WaitForElement("LineQuality_BaudRatePicker");
        Assert.That(toggle, Is.Not.Null, "Baud rate picker should exist.");

        try
        {
            OpenPicker(toggle!);

            var clearButton = WaitForElement("LineQuality_SelectNoBaudRates");
            Assert.That(clearButton, Is.Not.Null, "Select None should be inside the picker.");
            clearButton!.Click();

            Assert.That(AssemblySetup.Retry(
                    () => InvokeOnUI(() => viewModel.HasNoBaudRateSelected) ? "empty" : null,
                    TimeSpan.FromSeconds(2)),
                Is.EqualTo("empty"), "Select None should clear every rate.");
            Assert.That(WaitForElement("LineQuality_BaudRatePlaceholder"), Is.Not.Null,
                "The box should say so once it holds no rates.");

            var selectAllButton = WaitForElement("LineQuality_SelectAllBaudRates");
            Assert.That(selectAllButton, Is.Not.Null, "Select All should be inside the picker.");
            selectAllButton!.Click();

            Assert.That(AssemblySetup.Retry(
                    () => InvokeOnUI(() => viewModel.BaudRateOptions.All(option => option.IsSelected))
                        ? "all"
                        : null,
                    TimeSpan.FromSeconds(2)),
                Is.EqualTo("all"), "Select All should put every rate back.");
        }
        finally
        {
            InvokeOnUI(() => viewModel.SelectAllBaudRatesCommand.Execute(null));
            ClosePicker(toggle!);
        }
    }

    [Test]
    public void PickerRowTogglesOnlyThatRate()
    {
        // Each row in the picker is one stretched check box, so a click anywhere along the row has
        // to toggle that rate and only that rate.
        var viewModel = InvokeOnUI(() => TestApp.GetService<LineQualityViewModel>());
        InvokeOnUI(() => viewModel.SelectAllBaudRatesCommand.Execute(null));

        var toggle = WaitForElement("LineQuality_BaudRatePicker");
        Assert.That(toggle, Is.Not.Null, "Baud rate picker should exist.");

        try
        {
            OpenPicker(toggle!);

            var list = WaitForElement("LineQuality_BaudRateList");
            Assert.That(list, Is.Not.Null, "Baud rate list should be inside the picker.");

            // Deliberately the far end of the row, well clear of the check box and its label,
            // which is what proves the whole row is the hit area.
            var rowBounds = list!.AsListBox().Items[0].BoundingRectangle;
            var farEdge = new System.Drawing.Point(rowBounds.Right - 10, rowBounds.Top + rowBounds.Height / 2);

            Mouse.Click(farEdge);
            Assert.That(AssemblySetup.Retry(
                    () => InvokeOnUI(() => viewModel.BaudRateOptions[0].IsSelected) ? null : "cleared",
                    TimeSpan.FromSeconds(2)),
                Is.EqualTo("cleared"), "Clicking a row should leave that rate out of the sweep.");

            Mouse.Click(farEdge);
            Assert.That(AssemblySetup.Retry(
                    () => InvokeOnUI(() => viewModel.BaudRateOptions[0].IsSelected) ? "selected" : null,
                    TimeSpan.FromSeconds(2)),
                Is.EqualTo("selected"), "Clicking it again should put the rate back.");

            Assert.That(InvokeOnUI(() => viewModel.BaudRateOptions.Count(option => option.IsSelected)),
                Is.EqualTo(6), "Only the row that was clicked should have changed.");
        }
        finally
        {
            InvokeOnUI(() => viewModel.SelectAllBaudRatesCommand.Execute(null));
            ClosePicker(toggle!);
        }
    }
    [Test]
    public void DefaultsToScreeningAtTheDedicatedTestAddress()
    {
        var viewModel = InvokeOnUI(() => TestApp.GetService<LineQualityViewModel>());

        Assert.Multiple(() =>
        {
            Assert.That(InvokeOnUI(() => viewModel.SelectedProfile.Profile),
                Is.EqualTo(TestProfile.Screening), "Screening is the default profile.");
            Assert.That(InvokeOnUI(() => viewModel.Address),
                Is.EqualTo(LineQualityProtocol.TestAddress), "Address 125 is the dedicated test address.");
            Assert.That(InvokeOnUI(() => viewModel.BaudRateOptions.Count), Is.EqualTo(6),
                "The six OSDP baud rates are offered.");
        });
    }

    [Test]
    public void CancelledRunLeavesNothingToReport()
    {
        // The view model is a singleton shared across this fixture, so this drives a run to a
        // known empty state rather than assuming no earlier test produced results.
        var viewModel = InvokeOnUI(() => TestApp.GetService<LineQualityViewModel>());

        TestApp.MockLineQuality.Setup(service => service.IsSupported(It.IsAny<string>())).Returns(true);
        TestApp.MockLineQuality
            .Setup(service => service.RunTestAsync(It.IsAny<string>(), It.IsAny<LineQualityOptions>(),
                It.IsAny<CancellationToken>()))
            .ReturnsAsync(() => throw new OperationCanceledException());

        try
        {
            InvokeOnUI(() => viewModel.StartTestCommand.Execute(null));

            Assert.That(AssemblySetup.Retry(
                    () => InvokeOnUI(() => viewModel.IsTestRunning) ? null : "done",
                    TimeSpan.FromSeconds(5)),
                Is.EqualTo("done"), "The run should finish.");

            Assert.Multiple(() =>
            {
                Assert.That(InvokeOnUI(() => viewModel.HasResults), Is.False,
                    "A cancelled run produces no results.");
                Assert.That(FindByAutomationId("LineQuality_SaveReport"), Is.Null,
                    "Save Report button should be hidden with nothing to report.");
                Assert.That(FindByAutomationId("ResultsDataGrid"), Is.Null,
                    "Results table should be hidden with nothing to show.");
            });
        }
        finally
        {
            TestApp.RestoreLineQualityDefaults();
        }
    }

    [Test]
    public void AddressEntryReflectsTheViewModel()
    {
        // The address entry is a NumberBox, which works in nullable doubles rather than the byte
        // the view model holds. This proves the two stay in step in both directions.
        var viewModel = InvokeOnUI(() => TestApp.GetService<LineQualityViewModel>());
        var addressBox = WaitForElement("LineQuality_Address");
        Assert.That(addressBox, Is.Not.Null, "Address entry should exist.");

        try
        {
            InvokeOnUI(() => viewModel.Address = 100);
            Assert.That(AssemblySetup.Retry(
                    () => addressBox!.AsTextBox().Text == "100" ? "matched" : null,
                    TimeSpan.FromSeconds(2)),
                Is.EqualTo("matched"), "The entry should show the address the view model holds.");

            InvokeOnUI(() => viewModel.AddressValue = null);
            Assert.That(InvokeOnUI(() => viewModel.Address), Is.EqualTo(0),
                "Clearing the entry should coerce the address rather than leave it stale.");
        }
        finally
        {
            InvokeOnUI(() => viewModel.Address = LineQualityProtocol.TestAddress);
        }
    }

    [Test]
    public void CompletedRunRendersTheResultsTableAndReportForm()
    {
        var viewModel = InvokeOnUI(() => TestApp.GetService<LineQualityViewModel>());
        var report = LineQualityReportBuilder.CreateSampleReport();

        TestApp.MockLineQuality.Setup(service => service.IsSupported(It.IsAny<string>())).Returns(true);
        TestApp.MockLineQuality
            .Setup(service => service.RunTestAsync(It.IsAny<string>(), It.IsAny<LineQualityOptions>(),
                It.IsAny<CancellationToken>()))
            .ReturnsAsync(report);

        try
        {
            InvokeOnUI(() => viewModel.StartTestCommand.Execute(null));

            Assert.That(AssemblySetup.Retry(
                    () => InvokeOnUI(() => viewModel.HasResults) ? "done" : null,
                    TimeSpan.FromSeconds(5)),
                Is.EqualTo("done"), "The run should complete and produce results.");

            var resultsGrid = WaitForElement("ResultsDataGrid");
            var saveButton = WaitForElement("LineQuality_SaveReport");

            Assert.Multiple(() =>
            {
                Assert.That(resultsGrid, Is.Not.Null, "Results table should be shown once a run completes.");
                Assert.That(saveButton, Is.Not.Null, "Save Report button should be shown once a run completes.");
                Assert.That(InvokeOnUI(() => viewModel.Results.Count), Is.EqualTo(2),
                    "Both exercised rates should appear.");

                // The clean rate is the recommendation; the rate that never completed its
                // transition is a failure of that rate, not merely untested.
                Assert.That(InvokeOnUI(() => viewModel.RecommendedBaudRateText), Is.EqualTo("9600"));
                Assert.That(InvokeOnUI(() => viewModel.Results[1].Verdict),
                    Is.EqualTo(LineQualityVerdict.Fail));
                Assert.That(InvokeOnUI(() => viewModel.SaveReportCommand.CanExecute(null)), Is.True);
            });

            // Every row must be laid out, which is what proves the column bindings resolve.
            var rows = resultsGrid!.AsDataGridView().Rows;
            Assert.That(rows, Has.Length.EqualTo(2), "Both result rows should be rendered.");
            Assert.That(rows[0].Cells[0].Value, Is.EqualTo("9600"),
                "The first column should show the baud rate.");
        }
        finally
        {
            TestApp.RestoreLineQualityDefaults();
        }
    }

    [Test]
    public void SwitchingToResponderRoleShowsTheResponderControls()
    {
        var viewModel = InvokeOnUI(() => TestApp.GetService<LineQualityViewModel>());

        InvokeOnUI(() => viewModel.IsControllerMode = false);

        Assert.Multiple(() =>
        {
            Assert.That(WaitForElement("LineQuality_StartResponder"), Is.Not.Null,
                "Start Responder button should appear in responder mode.");
            Assert.That(FindByAutomationId("LineQuality_StartTest"), Is.Null,
                "Start Test button should be hidden in responder mode.");
        });

        InvokeOnUI(() => viewModel.IsControllerMode = true);
    }

    [Test]
    public void TrafficLightsTheHeaderIndicators()
    {
        // The header's indicators are bound by the shared page header template, which every page
        // uses. This page had no properties behind that binding until the run reported its own
        // traffic, so the proof is that the value reaches the indicator, not just the view model.
        var viewModel = InvokeOnUI(() => TestApp.GetService<LineQualityViewModel>());
        var leds = InvokeOnUI(() => FindLeds(TestApp.GetService<LineQualityPage>()));

        Assert.That(leds, Has.Count.EqualTo(2), "The header should carry a Tx and an Rx indicator.");

        try
        {
            InvokeOnUI(() => TestApp.MockLineQuality.Raise(
                service => service.TrafficObserved += null,
                TestApp.MockLineQuality.Object,
                TraceDirection.Output));

            Assert.Multiple(() =>
            {
                Assert.That(InvokeOnUI(() => leds[0].LastActivityTime), Is.Not.EqualTo(DateTime.MinValue),
                    "Outgoing traffic should reach the Tx indicator.");
                Assert.That(InvokeOnUI(() => leds[1].LastActivityTime), Is.EqualTo(DateTime.MinValue),
                    "Outgoing traffic should leave the Rx indicator dark.");
            });

            InvokeOnUI(() => TestApp.MockLineQuality.Raise(
                service => service.TrafficObserved += null,
                TestApp.MockLineQuality.Object,
                TraceDirection.Input));

            Assert.That(InvokeOnUI(() => leds[1].LastActivityTime), Is.Not.EqualTo(DateTime.MinValue),
                "Incoming traffic should reach the Rx indicator.");
        }
        finally
        {
            InvokeOnUI(() =>
            {
                viewModel.LastTxActiveTime = DateTime.MinValue;
                viewModel.LastRxActiveTime = DateTime.MinValue;
            });
        }
    }

    /// <summary>
    /// Collects the activity indicators the page header renders, in the order the template lays
    /// them out: transmit first, then receive.
    /// </summary>
    private static List<LedControl> FindLeds(DependencyObject root)
    {
        var found = new List<LedControl>();

        for (int child = 0; child < VisualTreeHelper.GetChildrenCount(root); child++)
        {
            var candidate = VisualTreeHelper.GetChild(root, child);

            if (candidate is LedControl led)
            {
                found.Add(led);
            }
            else
            {
                found.AddRange(FindLeds(candidate));
            }
        }

        return found;
    }
}
