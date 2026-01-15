using System.Threading.Tasks;
using Moq;
using NUnit.Framework;
using OSDPBench.Core.Services;
using OSDPBench.Core.ViewModels.Pages;

namespace OSDPBench.Core.Tests.ViewModels;

/// <summary>
/// Unit tests for ConfigurationViewModel button visibility logic.
/// Based on specifications in docs/ConnectionButtonBehavior.md
/// </summary>
[TestFixture(TestOf = typeof(ConfigurationViewModel))]
public class ConfigurationViewModelButtonVisibilityTests
{
    private Mock<IDialogService> _dialogServiceMock;
    private Mock<IDeviceManagementService> _deviceManagementServiceMock;
    private Mock<ISerialPortConnectionService> _serialPortConnectionServiceMock;
    private ConfigurationViewModel _viewModel;

    [SetUp]
    public void Setup()
    {
        _dialogServiceMock = new Mock<IDialogService>();
        _deviceManagementServiceMock = new Mock<IDeviceManagementService>();
        _serialPortConnectionServiceMock = new Mock<ISerialPortConnectionService>();

        // Set up a serial port service to return an empty list to avoid initialization issues
        _serialPortConnectionServiceMock.Setup(x => x.FindAvailableSerialPorts())
            .ReturnsAsync([]);

        _viewModel = new ConfigurationViewModel(
            _dialogServiceMock.Object,
            _deviceManagementServiceMock.Object,
            _serialPortConnectionServiceMock.Object);
    }

    #region Discover Mode Tests (IsConnectToPDSelected=true, IsDiscoverModeSelected=true)

    [Test]
    public async Task DiscoverMode_Disconnected_ShowsCorrectButtons()
    {
        // Arrange
        await _viewModel.InitializationComplete;
        _viewModel.IsConnectToPDSelected = true;
        _viewModel.IsDiscoverModeSelected = true;
        _viewModel.StatusLevel = StatusLevel.Disconnected;

        // Act & Assert
        Assert.That(_viewModel.StartDiscoveryVisible, Is.True, "Discovery button should be visible");
        Assert.That(_viewModel.ConnectVisible, Is.False, "Connect button should be hidden");
        Assert.That(_viewModel.DisconnectVisible, Is.False, "Disconnect button should be hidden");
        Assert.That(_viewModel.CancelDiscoveryVisible, Is.False, "Cancel button should be hidden");
        Assert.That(_viewModel.IsConnectionTypeEnabled, Is.True, "Connection mode should be enabled");
    }

    [Test]
    public async Task DiscoverMode_Discovering_ShowsCorrectButtons()
    {
        // Arrange
        await _viewModel.InitializationComplete;
        _viewModel.IsConnectToPDSelected = true;
        _viewModel.IsDiscoverModeSelected = true;
        _viewModel.StatusLevel = StatusLevel.Discovering;

        // Act & Assert
        Assert.That(_viewModel.StartDiscoveryVisible, Is.False, "Discovery button should be hidden");
        Assert.That(_viewModel.ConnectVisible, Is.False, "Connect button should be hidden");
        Assert.That(_viewModel.DisconnectVisible, Is.False, "Disconnect button should be hidden");
        Assert.That(_viewModel.CancelDiscoveryVisible, Is.True, "Cancel button should be visible");
        Assert.That(_viewModel.IsConnectionTypeEnabled, Is.False, "Connection mode should be disabled");
    }

    [Test]
    public async Task DiscoverMode_DiscoveryCancelled_ShowsCorrectButtons()
    {
        // Arrange - Bug #2 verification
        await _viewModel.InitializationComplete;
        _viewModel.IsConnectToPDSelected = true;
        _viewModel.IsDiscoverModeSelected = true;
        _viewModel.StatusLevel = StatusLevel.Disconnected;

        // Act & Assert
        Assert.That(_viewModel.StartDiscoveryVisible, Is.True, "Discovery button should be visible");
        Assert.That(_viewModel.ConnectVisible, Is.False, "Connect button should be hidden");
        Assert.That(_viewModel.DisconnectVisible, Is.False, "Disconnect button should be hidden (Bug #2 FIXED)");
        Assert.That(_viewModel.CancelDiscoveryVisible, Is.False, "Cancel button should be hidden");
        Assert.That(_viewModel.IsConnectionTypeEnabled, Is.True, "Connection mode should be enabled");
    }

    [Test]
    public async Task DiscoverMode_Discovered_ShowsCorrectButtons()
    {
        // Arrange - After successful discovery, before auto-connect
        await _viewModel.InitializationComplete;
        _viewModel.IsConnectToPDSelected = true;
        _viewModel.IsDiscoverModeSelected = true;
        _viewModel.StatusLevel = StatusLevel.Discovered;

        // Act & Assert
        Assert.That(_viewModel.StartDiscoveryVisible, Is.False, "Discovery button should be hidden");
        Assert.That(_viewModel.ConnectVisible, Is.False, "Connect button should be hidden");
        Assert.That(_viewModel.DisconnectVisible, Is.True, "Disconnect button should be visible to allow cancellation");
        Assert.That(_viewModel.CancelDiscoveryVisible, Is.False, "Cancel button should be hidden");
        Assert.That(_viewModel.IsConnectionTypeEnabled, Is.False, "Connection mode should be disabled");
    }

    [Test]
    public async Task DiscoverMode_Connecting_ShowsCorrectButtons()
    {
        // Arrange - Bug #3 verification
        await _viewModel.InitializationComplete;
        _viewModel.IsConnectToPDSelected = true;
        _viewModel.IsDiscoverModeSelected = true;
        _viewModel.StatusLevel = StatusLevel.Connecting;

        // Act & Assert
        Assert.That(_viewModel.StartDiscoveryVisible, Is.False, "Discovery button should be hidden");
        Assert.That(_viewModel.ConnectVisible, Is.False, "Connect button should be hidden");
        Assert.That(_viewModel.DisconnectVisible, Is.True, "Disconnect button should be visible (Bug #3 FIXED)");
        Assert.That(_viewModel.CancelDiscoveryVisible, Is.False, "Cancel button should be hidden");
        Assert.That(_viewModel.IsConnectionTypeEnabled, Is.False, "Connection mode should be disabled");
    }

    [Test]
    public async Task DiscoverMode_Connected_ShowsCorrectButtons()
    {
        // Arrange
        await _viewModel.InitializationComplete;
        _viewModel.IsConnectToPDSelected = true;
        _viewModel.IsDiscoverModeSelected = true;
        _viewModel.StatusLevel = StatusLevel.Connected;

        // Act & Assert
        Assert.That(_viewModel.StartDiscoveryVisible, Is.False, "Discovery button should be hidden");
        Assert.That(_viewModel.ConnectVisible, Is.False, "Connect button should be hidden");
        Assert.That(_viewModel.DisconnectVisible, Is.True, "Disconnect button should be visible");
        Assert.That(_viewModel.CancelDiscoveryVisible, Is.False, "Cancel button should be hidden");
        Assert.That(_viewModel.IsConnectionTypeEnabled, Is.False, "Connection mode should be disabled");
    }

    #endregion

    #region Manual Mode Tests (IsConnectToPDSelected=true, IsDiscoverModeSelected=false)

    [Test]
    public async Task ManualMode_Disconnected_ShowsCorrectButtons()
    {
        // Arrange
        await _viewModel.InitializationComplete;
        _viewModel.IsConnectToPDSelected = true;
        _viewModel.IsDiscoverModeSelected = false;
        _viewModel.StatusLevel = StatusLevel.Disconnected;

        // Act & Assert
        Assert.That(_viewModel.StartDiscoveryVisible, Is.False, "Discovery button should be hidden");
        Assert.That(_viewModel.ConnectVisible, Is.True, "Connect button should be visible");
        Assert.That(_viewModel.DisconnectVisible, Is.False, "Disconnect button should be hidden");
        Assert.That(_viewModel.CancelDiscoveryVisible, Is.False, "Cancel button should be hidden");
        Assert.That(_viewModel.IsConnectionTypeEnabled, Is.True, "Connection mode should be enabled");
    }

    [Test]
    public async Task ManualMode_Connecting_ShowsCorrectButtons()
    {
        // Arrange - Bug #3 verification
        await _viewModel.InitializationComplete;
        _viewModel.IsConnectToPDSelected = true;
        _viewModel.IsDiscoverModeSelected = false;
        _viewModel.StatusLevel = StatusLevel.ConnectingManually;

        // Act & Assert
        Assert.That(_viewModel.StartDiscoveryVisible, Is.False, "Discovery button should be hidden");
        Assert.That(_viewModel.ConnectVisible, Is.False, "Connect button should be hidden (Bug #3 FIXED)");
        Assert.That(_viewModel.DisconnectVisible, Is.True, "Disconnect button should be visible (Bug #3 FIXED)");
        Assert.That(_viewModel.CancelDiscoveryVisible, Is.False, "Cancel button should be hidden");
        Assert.That(_viewModel.IsConnectionTypeEnabled, Is.False, "Connection mode should be disabled");
    }

    [Test]
    public async Task ManualMode_Connected_ShowsCorrectButtons()
    {
        // Arrange
        await _viewModel.InitializationComplete;
        _viewModel.IsConnectToPDSelected = true;
        _viewModel.IsDiscoverModeSelected = false;
        _viewModel.StatusLevel = StatusLevel.Connected;

        // Act & Assert
        Assert.That(_viewModel.StartDiscoveryVisible, Is.False, "Discovery button should be hidden");
        Assert.That(_viewModel.ConnectVisible, Is.False, "Connect button should be hidden");
        Assert.That(_viewModel.DisconnectVisible, Is.True, "Disconnect button should be visible");
        Assert.That(_viewModel.CancelDiscoveryVisible, Is.False, "Cancel button should be hidden");
        Assert.That(_viewModel.IsConnectionTypeEnabled, Is.False, "Connection mode should be disabled");
    }

    #endregion

    #region ConnectionTypeComboBox Enable/Disable Tests (Bug #1)

    [Test]
    public async Task ConnectionTypeComboBox_Disconnected_IsEnabled()
    {
        // Arrange
        await _viewModel.InitializationComplete;
        _viewModel.StatusLevel = StatusLevel.Disconnected;

        // Act & Assert
        Assert.That(_viewModel.IsConnectionTypeEnabled, Is.True, "ConnectionTypeComboBox should be enabled when disconnected");
    }

    [Test]
    public async Task ConnectionTypeComboBox_Discovering_IsDisabled()
    {
        // Arrange - Bug #1 verification
        await _viewModel.InitializationComplete;
        _viewModel.StatusLevel = StatusLevel.Discovering;

        // Act & Assert
        Assert.That(_viewModel.IsConnectionTypeEnabled, Is.False, "ConnectionTypeComboBox should be disabled when discovering (Bug #1 FIXED)");
    }

    [Test]
    public async Task ConnectionTypeComboBox_Discovered_IsDisabled()
    {
        // Arrange - Bug #1 verification
        await _viewModel.InitializationComplete;
        _viewModel.StatusLevel = StatusLevel.Discovered;

        // Act & Assert
        Assert.That(_viewModel.IsConnectionTypeEnabled, Is.False, "ConnectionTypeComboBox should be disabled when discovered (Bug #1 FIXED)");
    }

    [Test]
    public async Task ConnectionTypeComboBox_Connecting_IsDisabled()
    {
        // Arrange - Bug #1 verification
        await _viewModel.InitializationComplete;
        _viewModel.StatusLevel = StatusLevel.Connecting;

        // Act & Assert
        Assert.That(_viewModel.IsConnectionTypeEnabled, Is.False, "ConnectionTypeComboBox should be disabled when connecting (Bug #1 FIXED)");
    }

    [Test]
    public async Task ConnectionTypeComboBox_ConnectingManually_IsDisabled()
    {
        // Arrange - Bug #1 verification
        await _viewModel.InitializationComplete;
        _viewModel.StatusLevel = StatusLevel.ConnectingManually;

        // Act & Assert
        Assert.That(_viewModel.IsConnectionTypeEnabled, Is.False, "ConnectionTypeComboBox should be disabled when connecting manually (Bug #1 FIXED)");
    }

    [Test]
    public async Task ConnectionTypeComboBox_Connected_IsDisabled()
    {
        // Arrange - Bug #1 verification
        await _viewModel.InitializationComplete;
        _viewModel.StatusLevel = StatusLevel.Connected;

        // Act & Assert
        Assert.That(_viewModel.IsConnectionTypeEnabled, Is.False, "ConnectionTypeComboBox should be disabled when connected (Bug #1 FIXED)");
    }

    #endregion

    #region Property Change Notification Tests

    [Test]
    public async Task StatusLevelChange_NotifiesButtonVisibilityProperties()
    {
        // Arrange
        await _viewModel.InitializationComplete;
        _viewModel.IsConnectToPDSelected = true;
        _viewModel.IsDiscoverModeSelected = false;
        _viewModel.StatusLevel = StatusLevel.Disconnected;

        bool connectVisibleChanged = false;
        bool disconnectVisibleChanged = false;
        bool startDiscoveryVisibleChanged = false;
        bool cancelDiscoveryVisibleChanged = false;
        bool isConnectionTypeEnabledChanged = false;

        _viewModel.PropertyChanged += (_, args) =>
        {
            if (args.PropertyName == nameof(ConfigurationViewModel.ConnectVisible)) connectVisibleChanged = true;
            if (args.PropertyName == nameof(ConfigurationViewModel.DisconnectVisible)) disconnectVisibleChanged = true;
            if (args.PropertyName == nameof(ConfigurationViewModel.StartDiscoveryVisible)) startDiscoveryVisibleChanged = true;
            if (args.PropertyName == nameof(ConfigurationViewModel.CancelDiscoveryVisible)) cancelDiscoveryVisibleChanged = true;
            if (args.PropertyName == nameof(ConfigurationViewModel.IsConnectionTypeEnabled)) isConnectionTypeEnabledChanged = true;
        };

        // Act
        _viewModel.StatusLevel = StatusLevel.Connected;

        // Assert
        Assert.That(connectVisibleChanged, Is.True, "ConnectVisible should notify change");
        Assert.That(disconnectVisibleChanged, Is.True, "DisconnectVisible should notify change");
        Assert.That(startDiscoveryVisibleChanged, Is.True, "StartDiscoveryVisible should notify change");
        Assert.That(cancelDiscoveryVisibleChanged, Is.True, "CancelDiscoveryVisible should notify change");
        Assert.That(isConnectionTypeEnabledChanged, Is.True, "IsConnectionTypeEnabled should notify change");
    }

    [Test]
    public async Task ConnectionModeChange_NotifiesButtonVisibilityProperties()
    {
        // Arrange
        await _viewModel.InitializationComplete;
        _viewModel.IsConnectToPDSelected = true;
        _viewModel.IsDiscoverModeSelected = true;
        _viewModel.StatusLevel = StatusLevel.Disconnected;

        bool connectVisibleChanged = false;
        bool startDiscoveryVisibleChanged = false;

        _viewModel.PropertyChanged += (_, args) =>
        {
            if (args.PropertyName == nameof(ConfigurationViewModel.ConnectVisible)) connectVisibleChanged = true;
            if (args.PropertyName == nameof(ConfigurationViewModel.StartDiscoveryVisible)) startDiscoveryVisibleChanged = true;
        };

        // Act - Switch from Discovery to Manual mode
        _viewModel.IsDiscoverModeSelected = false;

        // Assert
        Assert.That(connectVisibleChanged, Is.True, "ConnectVisible should notify change when mode changes");
        Assert.That(startDiscoveryVisibleChanged, Is.True, "StartDiscoveryVisible should notify change when mode changes");
    }

    #endregion
}
