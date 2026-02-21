using NUnit.Framework;
using OSDPBench.Core.ViewModels.Pages;

namespace OSDPBench.UI.Tests;

[TestFixture]
public class MonitorPageTests : UiTestBase
{
    [SetUp]
    public void NavigateToMonitorPage()
    {
        NavigateToPage("NavItem_Monitor", "ExportButton");
    }

    [Test]
    public void MonitorPageLoadsSuccessfully()
    {
        Assert.That(MainWindow!.IsAvailable, Is.True,
            "Main window should be available after navigating to Monitor page.");
    }

    [Test]
    public void InitialStatisticsAreZero()
    {
        var viewModel = InvokeOnUI(() => TestApp.GetService<MonitorViewModel>());

        Assert.Multiple(() =>
        {
            Assert.That(InvokeOnUI(() => viewModel.CommandsSent), Is.EqualTo(0), "CommandsSent should be 0.");
            Assert.That(InvokeOnUI(() => viewModel.RepliesReceived), Is.EqualTo(0), "RepliesReceived should be 0.");
            Assert.That(InvokeOnUI(() => viewModel.Polls), Is.EqualTo(0), "Polls should be 0.");
            Assert.That(InvokeOnUI(() => viewModel.Naks), Is.EqualTo(0), "Naks should be 0.");
        });
    }

    [Test]
    public void TraceDataGridExists()
    {
        var dataGrid = WaitForElement("TraceDataGrid");
        Assert.That(dataGrid, Is.Not.Null, "TraceDataGrid should exist on the Monitor page.");
    }

    [Test]
    public void InitialBufferIsEmpty()
    {
        var viewModel = InvokeOnUI(() => TestApp.GetService<MonitorViewModel>());

        Assert.Multiple(() =>
        {
            Assert.That(InvokeOnUI(() => viewModel.TotalPacketsCaptured), Is.EqualTo(0),
                "TotalPacketsCaptured should be 0 initially.");
            Assert.That(InvokeOnUI(() => viewModel.BufferUsagePercentage), Is.EqualTo(0),
                "BufferUsagePercentage should be 0 initially.");
        });
    }

    [Test]
    public void ExportButtonExists()
    {
        var exportButton = WaitForElement("ExportButton");
        Assert.That(exportButton, Is.Not.Null, "ExportButton should exist on the Monitor page.");

        var viewModel = InvokeOnUI(() => TestApp.GetService<MonitorViewModel>());
        Assert.That(InvokeOnUI(() => viewModel.CanExport), Is.False,
            "CanExport should be false with no trace data.");
    }
}
