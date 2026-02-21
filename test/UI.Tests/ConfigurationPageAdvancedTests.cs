using System.Windows;
using System.Windows.Automation;
using System.Windows.Media;
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
            vm.UseDefaultKey = true;
            vm.SecurityKey = string.Empty;
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

    [Test]
    public void PassiveSecurityKeyDisabledWhenDefaultKeyChecked()
    {
        // Switch to passive mode
        InvokeOnUI(() =>
        {
            var vm = TestApp.GetService<ConfigurationViewModel>();
            vm.IsConnectToPDSelected = false;
            vm.UseDefaultKey = true;
        });

        var passiveKeyTextBox = WaitForElement("PassiveSecurityKeyTextBox");
        Assert.That(passiveKeyTextBox, Is.Not.Null, "PassiveSecurityKeyTextBox should exist.");

        var isEnabled = InvokeOnUI(() =>
        {
            var element = FindWpfElement<Wpf.Ui.Controls.TextBox>("PassiveSecurityKeyTextBox");
            return element?.IsEnabled ?? true;
        });

        Assert.That(isEnabled, Is.False,
            "Passive security key should be disabled when default key is checked.");
    }

    [Test]
    public void PassiveSecurityKeyEnabledWhenDefaultKeyUnchecked()
    {
        // Switch to passive mode and uncheck default key
        InvokeOnUI(() =>
        {
            var vm = TestApp.GetService<ConfigurationViewModel>();
            vm.IsConnectToPDSelected = false;
            vm.SecurityKey = "00112233445566778899AABBCCDDEEFF";
            vm.UseDefaultKey = false;
        });

        var isEnabled = InvokeOnUI(() =>
        {
            var element = FindWpfElement<Wpf.Ui.Controls.TextBox>("PassiveSecurityKeyTextBox");
            return element?.IsEnabled ?? false;
        });

        Assert.That(isEnabled, Is.True,
            "Passive security key should be enabled when default key is unchecked.");
    }

    [Test]
    public void SecurityKeyPlaceholderVisible()
    {
        // Switch to manual mode with secure channel
        InvokeOnUI(() =>
        {
            var vm = TestApp.GetService<ConfigurationViewModel>();
            vm.IsConnectToPDSelected = true;
            vm.IsDiscoverModeSelected = false;
            vm.UseSecureChannel = true;
            vm.UseDefaultKey = false;
        });

        var placeholderText = InvokeOnUI(() =>
        {
            var element = FindWpfElement<Wpf.Ui.Controls.TextBox>("SecurityKeyTextBox");
            return element?.PlaceholderText ?? string.Empty;
        });

        Assert.That(placeholderText, Is.Not.Empty,
            "Security key TextBox should have placeholder text.");
    }

    [Test]
    public void ActionButtonsHaveMinWidth()
    {
        // Ensure initialization is complete
        var viewModel = InvokeOnUI(() => TestApp.GetService<ConfigurationViewModel>());
        viewModel.InitializationComplete.Wait(TimeSpan.FromSeconds(5));

        // Switch to manual mode so Connect button is visible
        InvokeOnUI(() =>
        {
            var vm = TestApp.GetService<ConfigurationViewModel>();
            vm.IsConnectToPDSelected = true;
            vm.IsDiscoverModeSelected = false;
        });

        var minWidth = InvokeOnUI(() =>
        {
            var element = FindWpfElement<Wpf.Ui.Controls.Button>("ConnectButton");
            return element?.MinWidth ?? 0;
        });

        Assert.That(minWidth, Is.GreaterThanOrEqualTo(120),
            "Connect button should have MinWidth of at least 120.");
    }

    /// <summary>
    /// Finds a WPF element by AutomationId within the current page's visual tree.
    /// </summary>
    private T? FindWpfElement<T>(string automationId) where T : FrameworkElement
    {
        var mainWindow = Application.Current.MainWindow;
        if (mainWindow == null) return null;

        return FindByAutomationIdInVisualTree<T>(mainWindow, automationId);
    }

    private static T? FindByAutomationIdInVisualTree<T>(DependencyObject parent, string automationId)
        where T : FrameworkElement
    {
        var childCount = VisualTreeHelper.GetChildrenCount(parent);
        for (var i = 0; i < childCount; i++)
        {
            var child = VisualTreeHelper.GetChild(parent, i);
            if (child is T element &&
                AutomationProperties.GetAutomationId(element) == automationId)
            {
                return element;
            }

            var found = FindByAutomationIdInVisualTree<T>(child, automationId);
            if (found != null) return found;
        }

        return null;
    }
}
