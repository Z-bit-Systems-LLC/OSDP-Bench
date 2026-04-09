using Moq;
using NUnit.Framework;
using OSDP.Net.Model.CommandData;
using OSDPBench.Core.Actions;
using OSDPBench.Core.Models;
using OSDPBench.Core.ViewModels.Pages;
using OSDPBench.Windows.Views.Controls;
using OSDPBench.Windows.Views.Pages;

namespace OSDPBench.UI.Tests;

[TestFixture]
public class ManagePageActionTests : UiTestBase
{
    [SetUp]
    public void NavigateAndConnect()
    {
        NavigateToPage("NavItem_Manage");

        // Set up mock so ConnectedPortName is populated when connection event fires
        TestApp.MockDeviceManagement.Setup(m => m.PortName).Returns("COM3");

        InvokeOnUI(() =>
        {
            TestApp.MockDeviceManagement.Raise(
                m => m.ConnectionStatusChange += null,
                TestApp.MockDeviceManagement.Object,
                ConnectionStatus.Connected);
        });
    }

    [TearDown]
    public void ResetConnectionState()
    {
        TestApp.MockDeviceManagement.Setup(m => m.PortName).Returns((string?)null);

        InvokeOnUI(() =>
        {
            TestApp.MockDeviceManagement.Raise(
                m => m.ConnectionStatusChange += null,
                TestApp.MockDeviceManagement.Object,
                ConnectionStatus.Disconnected);
        });
    }

    [Test]
    public void SelectingBuzzerAction_LoadsBuzzerControl()
    {
        var page = InvokeOnUI(() => TestApp.GetService<ManagePage>());
        var viewModel = InvokeOnUI(() => TestApp.GetService<ManageViewModel>());

        InvokeOnUI(() =>
        {
            viewModel.SelectedDeviceAction =
                viewModel.AvailableDeviceActions.OfType<ControlBuzzerAction>().First();
        });

        var hasBuzzerControl = InvokeOnUI(() =>
            page.FindName("DeviceActionControl") is System.Windows.Controls.Grid grid &&
            grid.Children.OfType<ControlBuzzerControl>().Any());

        Assert.That(hasBuzzerControl, Is.True,
            "Selecting the buzzer action should load ControlBuzzerControl into DeviceActionControl.");
    }

    [Test]
    public async Task SendButton_ExecutesBuzzerAction_WithCorrectParameters()
    {
        var viewModel = InvokeOnUI(() => TestApp.GetService<ManageViewModel>());

        TestApp.MockDeviceManagement
            .Setup(m => m.ExecuteDeviceAction(It.IsAny<ControlBuzzerAction>(), It.IsAny<BuzzerParameters>()))
            .ReturnsAsync(new object());

        InvokeOnUI(() =>
        {
            viewModel.SelectedDeviceAction =
                viewModel.AvailableDeviceActions.OfType<ControlBuzzerAction>().First();
        });

        await InvokeOnUI(async () =>
        {
            await viewModel.ExecuteDeviceActionCommand.ExecuteAsync(null);
        });

        TestApp.MockDeviceManagement.Verify(
            m => m.ExecuteDeviceAction(It.IsAny<ControlBuzzerAction>(), It.IsAny<BuzzerParameters>()),
            Times.Once,
            "ExecuteDeviceAction should be called with a ControlBuzzerAction and BuzzerParameters.");
    }

    [Test]
    public void SendButton_DisabledWhenBuzzerValidationFails()
    {
        var page = InvokeOnUI(() => TestApp.GetService<ManagePage>());
        var viewModel = InvokeOnUI(() => TestApp.GetService<ManageViewModel>());

        InvokeOnUI(() =>
        {
            viewModel.SelectedDeviceAction =
                viewModel.AvailableDeviceActions.OfType<ControlBuzzerAction>().First();
        });

        // Set invalid state: ToneCode.Default with OnTime=0
        InvokeOnUI(() =>
        {
            var grid = (System.Windows.Controls.Grid)page.FindName("DeviceActionControl")!;
            var buzzerControl = grid.Children.OfType<ControlBuzzerControl>().First();
            buzzerControl.SelectedToneCode = ToneCode.Default;
            buzzerControl.OnTime = 0;
        });

        var isEnabled = InvokeOnUI(() =>
        {
            var button = (Wpf.Ui.Controls.Button)page.FindName("PerformActionButton")!;
            return button.IsEnabled;
        });

        Assert.That(isEnabled, Is.False,
            "PerformActionButton should be disabled when buzzer validation fails.");
    }

    [Test]
    public void SelectingLedAction_LoadsLedControl()
    {
        var page = InvokeOnUI(() => TestApp.GetService<ManagePage>());
        var viewModel = InvokeOnUI(() => TestApp.GetService<ManageViewModel>());

        InvokeOnUI(() =>
        {
            viewModel.SelectedDeviceAction =
                viewModel.AvailableDeviceActions.OfType<SetReaderLedAction>().First();
        });

        var hasLedControl = InvokeOnUI(() =>
            page.FindName("DeviceActionControl") is System.Windows.Controls.Grid grid &&
            grid.Children.OfType<SetReaderLedControl>().Any());

        Assert.That(hasLedControl, Is.True,
            "Selecting the LED action should load SetReaderLedControl into DeviceActionControl.");
    }

    [Test]
    public async Task SendButton_ExecutesLedAction_WithCorrectParameters()
    {
        var viewModel = InvokeOnUI(() => TestApp.GetService<ManageViewModel>());

        TestApp.MockDeviceManagement
            .Setup(m => m.ExecuteDeviceAction(It.IsAny<SetReaderLedAction>(), It.IsAny<LedParameters>()))
            .ReturnsAsync(new object());

        InvokeOnUI(() =>
        {
            viewModel.SelectedDeviceAction =
                viewModel.AvailableDeviceActions.OfType<SetReaderLedAction>().First();
        });

        await InvokeOnUI(async () =>
        {
            await viewModel.ExecuteDeviceActionCommand.ExecuteAsync(null);
        });

        TestApp.MockDeviceManagement.Verify(
            m => m.ExecuteDeviceAction(It.IsAny<SetReaderLedAction>(), It.IsAny<LedParameters>()),
            Times.Once,
            "ExecuteDeviceAction should be called with a SetReaderLedAction and LedParameters.");
    }

    [Test]
    public void SendButton_DisabledWhenLedValidationFails()
    {
        var page = InvokeOnUI(() => TestApp.GetService<ManagePage>());
        var viewModel = InvokeOnUI(() => TestApp.GetService<ManageViewModel>());

        InvokeOnUI(() =>
        {
            viewModel.SelectedDeviceAction =
                viewModel.AvailableDeviceActions.OfType<SetReaderLedAction>().First();
        });

        // Set invalid state: SetTemporaryAndStartTimer with both times at 0
        InvokeOnUI(() =>
        {
            var grid = (System.Windows.Controls.Grid)page.FindName("DeviceActionControl")!;
            var ledControl = grid.Children.OfType<SetReaderLedControl>().First();
            ledControl.SelectedTemporaryMode =
                new KeyValuePair<string, TemporaryReaderControlCode>(
                    "Set temporary", TemporaryReaderControlCode.SetTemporaryAndStartTimer);
            ledControl.TemporaryOnTime = 0;
            ledControl.TemporaryOffTime = 0;
        });

        var isEnabled = InvokeOnUI(() =>
        {
            var button = (Wpf.Ui.Controls.Button)page.FindName("PerformActionButton")!;
            return button.IsEnabled;
        });

        Assert.That(isEnabled, Is.False,
            "PerformActionButton should be disabled when LED validation fails.");
    }
}
