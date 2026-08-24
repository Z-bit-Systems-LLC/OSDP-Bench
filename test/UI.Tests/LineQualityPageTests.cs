using FlaUI.Core.AutomationElements;
using Moq;
using NUnit.Framework;
using OSDP.Net.LineQuality;
using OSDPBench.Core.ViewModels.Pages;
using OSDPBench.UI.Tests.Helpers;

namespace OSDPBench.UI.Tests;

[TestFixture]
public class LineQualityPageTests : UiTestBase
{
    [SetUp]
    public void NavigateToLineQualityPage()
    {
        NavigateToPage("NavItem_LineQuality", "LineQuality_StartTest");

        // Each test starts from the controller role, which is the page's default.
        var viewModel = InvokeOnUI(() => TestApp.GetService<LineQualityViewModel>());
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
        });
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
            TestApp.MockLineQuality.Reset();
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
            TestApp.MockLineQuality.Reset();
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
}
