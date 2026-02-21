using NUnit.Framework;
using OSDPBench.Core.Models;
using OSDPBench.Core.ViewModels.Pages;

namespace OSDPBench.UI.Tests;

[TestFixture]
public class ManagePageTests : UiTestBase
{
    [SetUp]
    public void NavigateToManagePage()
    {
        NavigateToPage("NavItem_Manage");
    }

    [TearDown]
    public void ResetConnectionState()
    {
        InvokeOnUI(() =>
        {
            TestApp.MockDeviceManagement.Raise(
                m => m.ConnectionStatusChange += null,
                TestApp.MockDeviceManagement.Object,
                ConnectionStatus.Disconnected);
        });
    }

    [Test]
    public void ManagePageLoadsSuccessfully()
    {
        Assert.That(MainWindow!.IsAvailable, Is.True,
            "Main window should be available after navigating to Manage page.");
    }

    [Test]
    public void DeviceActionsComboBoxExistsWhenConnected()
    {
        // The actions panel is only visible when connected
        InvokeOnUI(() =>
        {
            TestApp.MockDeviceManagement.Raise(
                m => m.ConnectionStatusChange += null,
                TestApp.MockDeviceManagement.Object,
                ConnectionStatus.Connected);
        });

        var comboBox = WaitForElement("DeviceActionsComboBox");
        Assert.That(comboBox, Is.Not.Null, "DeviceActionsComboBox should exist when connected.");
    }

    [Test]
    public void ShowsDisconnectedStateByDefault()
    {
        var viewModel = InvokeOnUI(() => TestApp.GetService<ManageViewModel>());
        var statusLevel = InvokeOnUI(() => viewModel.StatusLevel);

        Assert.That(statusLevel, Is.Not.EqualTo(StatusLevel.Connected),
            "StatusLevel should not be Connected by default.");
    }

    [Test]
    public void StatusLevelUpdatesOnConnectionEvent()
    {
        var viewModel = InvokeOnUI(() => TestApp.GetService<ManageViewModel>());

        // Raise Connected event via mock
        InvokeOnUI(() =>
        {
            TestApp.MockDeviceManagement.Raise(
                m => m.ConnectionStatusChange += null,
                TestApp.MockDeviceManagement.Object,
                ConnectionStatus.Connected);
        });

        var statusLevel = InvokeOnUI(() => viewModel.StatusLevel);
        Assert.That(statusLevel, Is.EqualTo(StatusLevel.Connected),
            "StatusLevel should update to Connected on connection event.");
    }
}
