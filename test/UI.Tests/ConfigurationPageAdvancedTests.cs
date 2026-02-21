using NUnit.Framework;
using OSDPBench.Core.Models;
using OSDPBench.Core.ViewModels.Pages;

namespace OSDPBench.UI.Tests;

[TestFixture]
public class ConfigurationPageAdvancedTests : UiTestBase
{
    [SetUp]
    public void NavigateToConfigurationPage()
    {
        NavigateToPage("NavItem_Connect", "SerialPortComboBox");
    }

    [TearDown]
    public void RestoreDefaultState()
    {
        // Restore default mode selections
        InvokeOnUI(() =>
        {
            var vm = TestApp.GetService<ConfigurationViewModel>();
            vm.IsConnectToPDSelected = true;
            vm.IsDiscoverModeSelected = true;
        });

        // Ensure we reset back to disconnected state
        InvokeOnUI(() =>
        {
            TestApp.MockDeviceManagement.Raise(
                m => m.ConnectionStatusChange += null,
                TestApp.MockDeviceManagement.Object,
                ConnectionStatus.Disconnected);
        });
    }

    [Test]
    public void SerialPortComboBoxPopulatesFromMock()
    {
        var viewModel = InvokeOnUI(() => TestApp.GetService<ConfigurationViewModel>());

        // Wait for async initialization to complete
        viewModel.InitializationComplete.Wait(TimeSpan.FromSeconds(5));

        var ports = InvokeOnUI(() => viewModel.AvailableSerialPorts);
        var selectedPort = InvokeOnUI(() => viewModel.SelectedSerialPort);

        Assert.Multiple(() =>
        {
            Assert.That(ports, Has.Count.EqualTo(2), "Should have 2 serial ports from mock.");
            Assert.That(ports.Any(p => p.Name == "COM3"), Is.True, "COM3 should be available.");
            Assert.That(ports.Any(p => p.Name == "COM4"), Is.True, "COM4 should be available.");
            Assert.That(selectedPort, Is.Not.Null, "A port should be auto-selected.");
        });
    }

    [Test]
    public void BaudRateComboBoxHasDefaultValues()
    {
        // Switch to manual mode so baud rate combo is visible
        InvokeOnUI(() =>
        {
            var vm = TestApp.GetService<ConfigurationViewModel>();
            vm.IsConnectToPDSelected = true;
            vm.IsDiscoverModeSelected = false;
        });

        var viewModel = InvokeOnUI(() => TestApp.GetService<ConfigurationViewModel>());
        var baudRates = InvokeOnUI(() => viewModel.AvailableBaudRates);
        var selectedBaudRate = InvokeOnUI(() => viewModel.SelectedBaudRate);

        Assert.Multiple(() =>
        {
            Assert.That(baudRates, Does.Contain(9600));
            Assert.That(baudRates, Does.Contain(19200));
            Assert.That(baudRates, Does.Contain(38400));
            Assert.That(baudRates, Does.Contain(57600));
            Assert.That(baudRates, Does.Contain(115200));
            Assert.That(baudRates, Does.Contain(230400));
            Assert.That(selectedBaudRate, Is.EqualTo(9600), "Default baud rate should be 9600.");
        });
    }

    [Test]
    public void ConnectionModeToggleUpdatesViewModel()
    {
        // Switch to Passive Monitor mode
        InvokeOnUI(() =>
        {
            var vm = TestApp.GetService<ConfigurationViewModel>();
            vm.IsConnectToPDSelected = false;
        });

        var viewModel = InvokeOnUI(() => TestApp.GetService<ConfigurationViewModel>());

        Assert.Multiple(() =>
        {
            Assert.That(InvokeOnUI(() => viewModel.IsConnectToPDSelected), Is.False,
                "Connect to PD should not be selected.");
            Assert.That(InvokeOnUI(() => viewModel.IsPassiveMode), Is.True,
                "Passive mode should be active.");
        });
    }

    [Test]
    public void DiscoveryManualSubModeToggle()
    {
        // Switch to Manual mode
        InvokeOnUI(() =>
        {
            var vm = TestApp.GetService<ConfigurationViewModel>();
            vm.IsConnectToPDSelected = true;
            vm.IsDiscoverModeSelected = false;
        });

        var viewModel = InvokeOnUI(() => TestApp.GetService<ConfigurationViewModel>());

        Assert.Multiple(() =>
        {
            Assert.That(InvokeOnUI(() => viewModel.IsManualModeVisible), Is.True,
                "Manual mode should be visible.");
            Assert.That(InvokeOnUI(() => viewModel.ShowConnectionSettings), Is.True,
                "Connection settings should be visible in manual mode.");
        });

        var baudRateComboBox = WaitForElement("BaudRateComboBox");
        Assert.That(baudRateComboBox, Is.Not.Null, "BaudRateComboBox should be findable in manual mode.");
    }

    [Test]
    public void ConnectButtonVisibleInManualMode()
    {
        // Ensure initialization is complete so status is Ready
        var viewModel = InvokeOnUI(() => TestApp.GetService<ConfigurationViewModel>());
        viewModel.InitializationComplete.Wait(TimeSpan.FromSeconds(5));

        // Switch to Manual mode
        InvokeOnUI(() =>
        {
            var vm = TestApp.GetService<ConfigurationViewModel>();
            vm.IsConnectToPDSelected = true;
            vm.IsDiscoverModeSelected = false;
        });

        Assert.That(InvokeOnUI(() => viewModel.ConnectVisible), Is.True,
            "Connect should be visible in manual mode with Ready status.");

        var connectButton = WaitForElement("ConnectButton");
        Assert.That(connectButton, Is.Not.Null, "ConnectButton element should exist.");
    }

    [Test]
    public void ConnectionStatusChangeUpdatesViewModel()
    {
        var viewModel = InvokeOnUI(() => TestApp.GetService<ConfigurationViewModel>());

        // Raise Connected event via mock
        InvokeOnUI(() =>
        {
            TestApp.MockDeviceManagement.Raise(
                m => m.ConnectionStatusChange += null,
                TestApp.MockDeviceManagement.Object,
                ConnectionStatus.Connected);
        });

        Assert.Multiple(() =>
        {
            Assert.That(InvokeOnUI(() => viewModel.StatusLevel), Is.EqualTo(StatusLevel.Connected),
                "StatusLevel should be Connected.");
            Assert.That(InvokeOnUI(() => viewModel.DisconnectVisible), Is.True,
                "Disconnect should be visible when connected.");
        });
    }
}
