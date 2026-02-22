using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Moq;
using OSDP.Net.Connections;
using OSDP.Net.PanelCommands.DeviceDiscover;
using OSDP.Net.Tracing;
using NUnit.Framework;
using OSDPBench.Core.Models;
using OSDPBench.Core.Services;
using OSDPBench.Core.ViewModels.Pages;
using static OSDPBench.Core.Tests.Helpers.TraceEntryTestHelper;

namespace OSDPBench.Core.Tests.ViewModels;

[TestFixture(TestOf = typeof(ConfigurationViewModel))]
public class ConfigurationViewModelTests
{
    // Constants for common test values
    private const string TestPortId = "COM1";
    private const string TestPortName = "Port 1";
    private const string TestPortDescription = "Description 1";
    private const int TestBaudRate = 9600;
    private const byte TestAddress = 1;
    
    // Mock objects
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
        
        _viewModel = new ConfigurationViewModel(
            _dialogServiceMock.Object,
            _deviceManagementServiceMock.Object,
            _serialPortConnectionServiceMock.Object
        );
    }

    [Test]
    public void ConfigurationViewModel_InitializedAvailableBaudRates()
    {
        // Arrange
        var expectedBaudRates = new[] { 9600, 19200, 38400, 57600, 115200, 230400 };
        
        // Assert
        Assert.That(expectedBaudRates.Length, Is.EqualTo(_viewModel.AvailableBaudRates.Count));
        Assert.That(expectedBaudRates , Is.EqualTo(_viewModel.AvailableBaudRates.ToArray()));
    }
    
    [Test]
    public async Task ConfigurationViewModel_InitializesSerialPortsOnStartup()
    {
        // Arrange
        var availablePorts = CreateTestSerialPorts();
        SetupSerialPortMockWithPorts(availablePorts);
        
        // Act - Create a new view model which should trigger initialization
        var newViewModel = new ConfigurationViewModel(
            _dialogServiceMock.Object,
            _deviceManagementServiceMock.Object,
            _serialPortConnectionServiceMock.Object);
        
        // Wait for initialization to complete
        await newViewModel.InitializationComplete;
        
        // Assert
        Assert.That(newViewModel.AvailableSerialPorts.Count, Is.GreaterThan(0));
        Assert.That(newViewModel.StatusLevel, Is.EqualTo(StatusLevel.Ready));
    }
    
    [Test]
    public async Task ConfigurationViewModel_InitializesSerialPortsOnStartup_NoPortsFound()
    {
        // Arrange
        var emptyPorts = new AvailableSerialPort[0];
        SetupSerialPortMockWithPorts(emptyPorts);
        
        // Act - Create a new view model which should trigger initialization
        var newViewModel = new ConfigurationViewModel(
            _dialogServiceMock.Object,
            _deviceManagementServiceMock.Object,
            _serialPortConnectionServiceMock.Object);
        
        // Wait for initialization to complete
        await newViewModel.InitializationComplete;
        
        // Assert
        Assert.That(newViewModel.AvailableSerialPorts.Count, Is.EqualTo(0));
        Assert.That(newViewModel.StatusLevel, Is.EqualTo(StatusLevel.NotReady));
    }
    
    [Test]
    public void ConfigurationViewModel_InitializesSerialPortsOnStartup_HandlesException()
    {
        // Arrange
        _serialPortConnectionServiceMock.Setup(x => x.FindAvailableSerialPorts())
            .ThrowsAsync(new Exception("Test exception"));
        
        // Act - Create a new view model which should trigger initialization
        var newViewModel = new ConfigurationViewModel(
            _dialogServiceMock.Object,
            _deviceManagementServiceMock.Object,
            _serialPortConnectionServiceMock.Object);
        
        // Assert - InitializationComplete should throw the exception
        Assert.ThrowsAsync<Exception>(async () => await newViewModel.InitializationComplete);
        Assert.That(newViewModel.StatusLevel, Is.EqualTo(StatusLevel.NotReady));
    }

    #region DiscoverDevice Tests
    
    [Test]
    public async Task ConfigurationViewModel_ExecuteDiscoverDeviceCommand()
    {
        // Arrange
        await _viewModel.InitializationComplete;
        SetupForDiscoveryTest(DiscoveryStatus.Started);
        
        // Act
        await _viewModel.DiscoverDeviceCommand.ExecuteAsync(null);

        // Assert
        VerifyDiscoveryWasCalled();
        Assert.That(_viewModel.StatusLevel, Is.EqualTo(StatusLevel.Discovering));
    }
    
    [Test]
    public async Task ConfigurationViewModel_ExecuteDiscoverDeviceCommand_Cancelled()
    {
        // Arrange
        await _viewModel.InitializationComplete;
        SetupConnectionService();
        SetupDiscoveryWithException(new OperationCanceledException());
        SelectTestSerialPortAndBaudRate();

        // Act
        await _viewModel.DiscoverDeviceCommand.ExecuteAsync(null);

        // Assert
        VerifyDiscoveryWasCalled();
    }
    
    [Test]
    public async Task ConfigurationViewModel_ExecuteDiscoverDeviceCommand_NoPortSelected()
    {
        // Arrange
        await _viewModel.InitializationComplete;
        _viewModel.SelectedSerialPort = null;
        _viewModel.SelectedBaudRate = TestBaudRate;

        // Act
        await _viewModel.DiscoverDeviceCommand.ExecuteAsync(null);

        // Assert
        VerifyDiscoveryWasNotCalled();
    }
    
    #endregion

    #region ConnectDevice Tests

    [Test]
    public async Task ConfigurationViewModel_ExecuteConnectDeviceCommand()
    {
        // Arrange
        await _viewModel.InitializationComplete;
        SetupConnectionServiceWithPort(TestPortName, TestBaudRate);
        SelectTestSerialPortAndBaudRate();
        _viewModel.SelectedAddress = TestAddress;

        // Act
        await _viewModel.ConnectDeviceCommand.ExecuteAsync(null);

        // Assert
        Assert.That(_viewModel.StatusLevel, Is.EqualTo(StatusLevel.ConnectingManually));
        _serialPortConnectionServiceMock.Verify(
            x => x.GetConnection(TestPortName, TestBaudRate),
            Times.Once);
        _deviceManagementServiceMock.Verify(x => x.Shutdown(), Times.Once);
        _deviceManagementServiceMock.Verify(
            x => x.Connect(_serialPortConnectionServiceMock.Object, TestAddress, false, true, null),
            Times.Once);
        Assert.That(_viewModel.ConnectedAddress, Is.EqualTo(TestAddress));
        Assert.That(_viewModel.ConnectedBaudRate, Is.EqualTo(TestBaudRate));
    }
    
    [Test]
    public async Task ConfigurationViewModel_ExecuteConnectDeviceCommand_NoSerialPortSelected()
    {
        // Arrange
        await _viewModel.InitializationComplete;
        _viewModel.SelectedSerialPort = null;
        _viewModel.SelectedBaudRate = TestBaudRate;
        _viewModel.SelectedAddress = TestAddress;
        SetupSecureChannelParameters("1234556", true, false);

        // Act
        await _viewModel.ConnectDeviceCommand.ExecuteAsync(null);

        // Assert
        _deviceManagementServiceMock.Verify(x => x.Shutdown(), Times.Never);
        _deviceManagementServiceMock.Verify(
            x => x.Connect(_serialPortConnectionServiceMock.Object, TestAddress, false, true, null),
            Times.Never);
    }
    
    [Test]
    public async Task ConfigurationViewModel_ExecuteConnectDeviceCommand_InvalidSecurityKey()
    {
        // Arrange
        await _viewModel.InitializationComplete;
        SetupConnectionServiceWithPort(TestPortName, TestBaudRate);
        SelectTestSerialPortAndBaudRate();
        _viewModel.SelectedAddress = TestAddress;
        SetupSecureChannelParameters("1234556", true, false);

        // Act
        await _viewModel.ConnectDeviceCommand.ExecuteAsync(null);

        // Assert
        Assert.That(_viewModel.StatusLevel, Is.EqualTo(StatusLevel.ConnectingManually));
        _serialPortConnectionServiceMock.Verify(
            x => x.GetConnection(TestPortName, TestBaudRate),
            Times.Never);
        _deviceManagementServiceMock.Verify(x => x.Shutdown(), Times.Never);
        _deviceManagementServiceMock.Verify(
            x => x.Connect(_serialPortConnectionServiceMock.Object, TestAddress, false, true, null),
            Times.Never);
        _dialogServiceMock.Verify(
            x => x.ShowMessageDialog(
                It.IsAny<string>(),  // Title
                It.IsAny<string>(),  // Message
                It.IsAny<MessageIcon>()),
            Times.Once);
    }
    
    #endregion

    #region Event Handler Tests
    
    [Test]
    public async Task ConfigurationViewModel_DeviceManagementServiceOnConnectionStatusChange_Connected()
    {
        // Arrange
        await _viewModel.InitializationComplete;
        
        // Act
        RaiseConnectionStatusEvent(ConnectionStatus.Connected);
        
        // Assert
        Assert.That(_viewModel.StatusText, Is.EqualTo("Connected"));
        Assert.That(_viewModel.NakText, Is.EqualTo(string.Empty));
        Assert.That(_viewModel.StatusLevel, Is.EqualTo(StatusLevel.Connected));
    }
    
    [Test]
    public async Task ConfigurationViewModel_DeviceManagementServiceOnConnectionStatusChange_Disconnected()
    {
        // Arrange
        await _viewModel.InitializationComplete;
        
        // Act
        RaiseConnectionStatusEvent(ConnectionStatus.Disconnected);
        
        // Assert
        Assert.That(_viewModel.StatusText, Is.EqualTo("Disconnected"));
        Assert.That(_viewModel.StatusLevel, Is.EqualTo(StatusLevel.Disconnected));
    }
    
    [Test]
    public async Task ConfigurationViewModel_DeviceManagementServiceOnConnectionStatusChange_InvalidSecurityKey()
    {
        // Arrange
        await _viewModel.InitializationComplete;

        // Act
        RaiseConnectionStatusEvent(ConnectionStatus.InvalidSecurityKey);

        // Assert
        Assert.That(_viewModel.StatusText, Is.EqualTo("Invalid security key"));
        Assert.That(_viewModel.StatusLevel, Is.EqualTo(StatusLevel.Error));
    }

    [Test]
    public async Task ConfigurationViewModel_DisconnectButtonVisible_WhenInvalidSecurityKeyError()
    {
        // Arrange
        await _viewModel.InitializationComplete;

        // Act
        RaiseConnectionStatusEvent(ConnectionStatus.InvalidSecurityKey);

        // Assert - Disconnect button should be visible to allow user to clean up connection state
        Assert.That(_viewModel.DisconnectVisible, Is.True);
        Assert.That(_viewModel.ConnectVisible, Is.False);
        Assert.That(_viewModel.StartDiscoveryVisible, Is.False);
    }
    
    [Test]
    public async Task ConfigurationViewModel_DeviceManagementServiceOnConnectionStatusChange_WhenDiscoveredStatus()
    {
        // Arrange
        // Wait for initialization to complete
        await _viewModel.InitializationComplete;
        
        _viewModel.StatusLevel = StatusLevel.Discovered;
        
        // Act
        RaiseConnectionStatusEvent(ConnectionStatus.Disconnected); // Any non-Connected status will do
        
        // Assert
        Assert.That(_viewModel.StatusText, Is.EqualTo("Attempting to connect"));
        Assert.That(_viewModel.StatusLevel, Is.EqualTo(StatusLevel.Connecting));
    }
    
    [Test]
    public void ConfigurationViewModel_DeviceManagementServiceOnNakReplyReceived()
    {
        // Arrange
        string expectedNakMessage = "Invalid checksum";
        
        // Act
        _deviceManagementServiceMock.Raise(
            d => d.NakReplyReceived += null!, 
            EventArgs.Empty, 
            expectedNakMessage);
        
        // Assert
        Assert.That(_viewModel.NakText, Is.EqualTo(expectedNakMessage));
    }
    
    #endregion
    
    #region Helper Methods
    
    /// <summary>
    /// Creates an array of test serial ports for use in tests
    /// </summary>
    private static AvailableSerialPort[] CreateTestSerialPorts()
    {
        return new[]
        {
            new AvailableSerialPort("id1", "test1", "desc1"),
            new AvailableSerialPort("id2", "test2", "desc2")
        };
    }
    
    /// <summary>
    /// Sets up the serial port connection service mock to return the specified ports
    /// </summary>
    private void SetupSerialPortMockWithPorts(AvailableSerialPort[] ports)
    {
        _serialPortConnectionServiceMock.Setup(expression => expression.FindAvailableSerialPorts())
            .ReturnsAsync(ports);
    }
    
    
    /// <summary>
    /// Sets up the connection service mock for discovery tests
    /// </summary>
    private void SetupConnectionService()
    {
        _serialPortConnectionServiceMock.Setup(x => x.GetConnection(It.IsAny<string>(), It.IsAny<int>()))
            .Returns(Mock.Of<ISerialPortConnectionService>());
    }
    
    /// <summary>
    /// Sets up the discovery service to return a result with the specified status
    /// </summary>
    private void SetupDiscoveryWithStatus(DiscoveryStatus status)
    {
        var discoveryResult = Mock.Of<DiscoveryResult>(r => r.Status == status);
        _deviceManagementServiceMock.Setup(x => x.DiscoverDevice(
                It.IsAny<IEnumerable<IOsdpConnection>>(),
                It.IsAny<DiscoveryProgress>(),
                It.IsAny<CancellationToken>()))
            .ReturnsAsync(discoveryResult);
    }
    
    /// <summary>
    /// Sets up the discovery service to throw the specified exception
    /// </summary>
    private void SetupDiscoveryWithException(Exception exception)
    {
        _deviceManagementServiceMock.Setup(x => x.DiscoverDevice(
                It.IsAny<IEnumerable<IOsdpConnection>>(),
                It.IsAny<DiscoveryProgress>(),
                It.IsAny<CancellationToken>()))
            .ThrowsAsync(exception);
    }
    
    /// <summary>
    /// Sets up a complete discovery test with both connection and discovery status
    /// </summary>
    private void SetupForDiscoveryTest(DiscoveryStatus status)
    {
        SetupConnectionService();
        SetupDiscoveryWithStatus(status);
        SelectTestSerialPortAndBaudRate();
    }
    
    /// <summary>
    /// Sets up the connection service with a specific port and baud rate
    /// </summary>
    private void SetupConnectionServiceWithPort(string portName, int baudRate)
    {
        _serialPortConnectionServiceMock.Setup(x => x.GetConnection(portName, baudRate))
            .Returns(_serialPortConnectionServiceMock.Object);
    }
    
    /// <summary>
    /// Selects a test serial port and baud rate in the view model
    /// </summary>
    private void SelectTestSerialPortAndBaudRate()
    {
        _viewModel.SelectedSerialPort = new AvailableSerialPort(TestPortId, TestPortName, TestPortDescription);
        _viewModel.SelectedBaudRate = TestBaudRate;
    }
    
    /// <summary>
    /// Sets up secure channel parameters in the view model
    /// </summary>
    private void SetupSecureChannelParameters(string key, bool useSecureChannel, bool useDefaultKey)
    {
        _viewModel.SecurityKey = key;
        _viewModel.UseSecureChannel = useSecureChannel;
        _viewModel.UseDefaultKey = useDefaultKey;
    }
    
    /// <summary>
    /// Raises the connection status change event with the specified status
    /// </summary>
    private void RaiseConnectionStatusEvent(ConnectionStatus status)
    {
        _deviceManagementServiceMock.Raise(
            d => d.ConnectionStatusChange += null!, 
            EventArgs.Empty, 
            status);
    }
    
    /// <summary>
    /// Verifies that the discovery method was called
    /// </summary>
    private void VerifyDiscoveryWasCalled()
    {
        _deviceManagementServiceMock.Verify(
            x => x.DiscoverDevice(
                It.IsAny<IEnumerable<IOsdpConnection>>(),
                It.IsAny<DiscoveryProgress>(),
                It.IsAny<CancellationToken>()),
            Times.Once);
    }
    
    /// <summary>
    /// Verifies that the discovery method was not called
    /// </summary>
    private void VerifyDiscoveryWasNotCalled()
    {
        _deviceManagementServiceMock.Verify(
            x => x.DiscoverDevice(
                It.IsAny<IEnumerable<IOsdpConnection>>(),
                It.IsAny<DiscoveryProgress>(),
                It.IsAny<CancellationToken>()),
            Times.Never);
    }
    
    #endregion

    #region Passive Monitoring Tests

    [Test]
    public async Task ConfigurationViewModel_IsPassiveMode_WhenPassiveMonitoringSelected()
    {
        // Arrange
        await _viewModel.InitializationComplete;

        // Act - Select passive monitoring
        _viewModel.IsConnectToPDSelected = false;

        // Assert
        Assert.That(_viewModel.IsPassiveMode, Is.True);
    }

    [Test]
    public async Task ConfigurationViewModel_IsPassiveMode_FalseWhenConnectToPDSelected()
    {
        // Arrange
        await _viewModel.InitializationComplete;

        // Act - Select Connect to PD (either Discover or Manual)
        _viewModel.IsConnectToPDSelected = true;

        // Assert
        Assert.That(_viewModel.IsPassiveMode, Is.False);
    }

    [Test]
    public async Task ConfigurationViewModel_IsDiscoverModeVisible_WhenDiscoverSelected()
    {
        // Arrange
        await _viewModel.InitializationComplete;

        // Act - Select Discover mode
        _viewModel.IsConnectToPDSelected = true;
        _viewModel.IsDiscoverModeSelected = true;

        // Assert
        Assert.That(_viewModel.IsDiscoverModeVisible, Is.True);
        Assert.That(_viewModel.IsManualModeVisible, Is.False);
    }

    [Test]
    public async Task ConfigurationViewModel_IsManualModeVisible_WhenManualSelected()
    {
        // Arrange
        await _viewModel.InitializationComplete;

        // Act - Select Manual mode
        _viewModel.IsConnectToPDSelected = true;
        _viewModel.IsDiscoverModeSelected = false;

        // Assert
        Assert.That(_viewModel.IsManualModeVisible, Is.True);
        Assert.That(_viewModel.IsDiscoverModeVisible, Is.False);
    }

    [Test]
    public async Task ConfigurationViewModel_PassiveMonitoring_SetsStatusLevelToPassiveMonitoring()
    {
        // Arrange
        await _viewModel.InitializationComplete;

        // Act
        RaiseConnectionStatusEvent(ConnectionStatus.PassiveMonitoring);

        // Assert
        Assert.That(_viewModel.StatusLevel, Is.EqualTo(StatusLevel.PassiveMonitoring));
    }

    [Test]
    public async Task ConfigurationViewModel_UseDefaultKey_DefaultsToTrue()
    {
        // Arrange
        await _viewModel.InitializationComplete;

        // Assert
        Assert.That(_viewModel.UseDefaultKey, Is.True);
    }

    [Test]
    public async Task ConfigurationViewModel_SecurityKey_DefaultsToEmpty()
    {
        // Arrange
        await _viewModel.InitializationComplete;

        // Assert
        Assert.That(_viewModel.SecurityKey, Is.Null.Or.Empty);
    }

    [Test]
    public async Task ConfigurationViewModel_StartPassiveMonitoringCommand_NotNull()
    {
        // Arrange
        await _viewModel.InitializationComplete;

        // Assert
        Assert.That(_viewModel.StartPassiveMonitoringCommand, Is.Not.Null);
    }

    [Test]
    public async Task ConfigurationViewModel_StopPassiveMonitoringCommand_NotNull()
    {
        // Arrange
        await _viewModel.InitializationComplete;

        // Assert
        Assert.That(_viewModel.StopPassiveMonitoringCommand, Is.Not.Null);
    }

    [Test]
    public async Task ConfigurationViewModel_ExecuteStartPassiveMonitoring_NoPortSelected_DoesNotStart()
    {
        // Arrange
        await _viewModel.InitializationComplete;
        _viewModel.IsConnectToPDSelected = false; // Passive mode
        _viewModel.SelectedSerialPort = null;
        _viewModel.SelectedBaudRate = TestBaudRate;

        // Act
        await _viewModel.StartPassiveMonitoringCommand.ExecuteAsync(null);

        // Assert - Should not call StartPassiveMonitoring on service
        _deviceManagementServiceMock.Verify(
            x => x.StartPassiveMonitoring(
                It.IsAny<IOsdpConnection>(),
                It.IsAny<bool>(),
                It.IsAny<bool>(),
                It.IsAny<byte[]>()),
            Times.Never);
    }

    [Test]
    public async Task ConfigurationViewModel_ExecuteStartPassiveMonitoring_WithPortSelected_CallsService()
    {
        // Arrange
        await _viewModel.InitializationComplete;
        _viewModel.IsConnectToPDSelected = false; // Passive mode
        SetupConnectionServiceWithPort(TestPortName, TestBaudRate);
        SelectTestSerialPortAndBaudRate();

        // Act
        await _viewModel.StartPassiveMonitoringCommand.ExecuteAsync(null);

        // Assert
        _deviceManagementServiceMock.Verify(
            x => x.StartPassiveMonitoring(
                It.IsAny<IOsdpConnection>(),
                It.IsAny<bool>(),
                It.IsAny<bool>(),
                It.IsAny<byte[]>()),
            Times.Once);
    }

    [Test]
    public async Task ConfigurationViewModel_ExecuteStopPassiveMonitoring_CallsService()
    {
        // Arrange
        await _viewModel.InitializationComplete;

        // Act
        await _viewModel.StopPassiveMonitoringCommand.ExecuteAsync(null);

        // Assert
        _deviceManagementServiceMock.Verify(
            x => x.StopPassiveMonitoring(),
            Times.Once);
    }

    #endregion

    #region Trace Entry Activity Indicator Tests

    [Test]
    public void TraceEntryReceived_OutputDirection_UpdatesLastTxActiveTime()
    {
        // Arrange
        var before = DateTime.Now;

        // Act
        RaiseTraceEntry(_deviceManagementServiceMock, TraceDirection.Output, ValidPollPacket);

        // Assert
        Assert.That(_viewModel.LastTxActiveTime, Is.GreaterThanOrEqualTo(before));
    }

    [Test]
    public void TraceEntryReceived_InputDirection_UpdatesLastRxActiveTime()
    {
        // Arrange
        var before = DateTime.Now;

        // Act
        RaiseTraceEntry(_deviceManagementServiceMock, TraceDirection.Input, ValidAckPacket);

        // Assert
        Assert.That(_viewModel.LastRxActiveTime, Is.GreaterThanOrEqualTo(before));
    }

    [Test]
    public void TraceEntryReceived_InvalidPacket_DoesNotThrow()
    {
        // Act & Assert
        Assert.DoesNotThrow(() =>
            RaiseTraceEntry(_deviceManagementServiceMock, TraceDirection.Output, InvalidPacket));
    }

    [Test]
    public void TraceEntryReceived_UpdatesSecureChannelStatus()
    {
        // Arrange
        _deviceManagementServiceMock.Setup(x => x.IsUsingSecureChannel).Returns(true);
        _deviceManagementServiceMock.Setup(x => x.UsesDefaultSecurityKey).Returns(false);

        // Act
        RaiseTraceEntry(_deviceManagementServiceMock, TraceDirection.Output, ValidPollPacket);

        // Assert
        Assert.That(_viewModel.UsingSecureChannel, Is.True);
        Assert.That(_viewModel.UsesDefaultSecurityKey, Is.False);
    }

    #endregion

    #region USB Device Monitoring Tests

    [Test]
    public async Task UsbDeviceChanged_RefreshesSerialPorts()
    {
        // Arrange
        var usbMonitorMock = new Mock<IUsbDeviceMonitorService>();
        var updatedPorts = new[]
        {
            new AvailableSerialPort("id1", "COM1", "desc1"),
            new AvailableSerialPort("id2", "COM2", "desc2"),
            new AvailableSerialPort("id3", "COM3", "desc3")
        };
        _serialPortConnectionServiceMock.Setup(x => x.FindAvailableSerialPorts())
            .ReturnsAsync(updatedPorts);

        using var viewModel = new ConfigurationViewModel(
            _dialogServiceMock.Object,
            _deviceManagementServiceMock.Object,
            _serialPortConnectionServiceMock.Object,
            usbMonitorMock.Object);

        await viewModel.InitializationComplete;

        // Act - Raise USB device changed event
        usbMonitorMock.Raise(
            d => d.UsbDeviceChanged += null!,
            new UsbDeviceChangedEventArgs(UsbDeviceChangeType.Connected, new[] { "COM1", "COM2", "COM3" }));

        // Allow async handler to complete
        await Task.Delay(100);

        // Assert
        Assert.That(viewModel.AvailableSerialPorts, Has.Count.EqualTo(3));
    }

    [Test]
    public async Task UsbDeviceChanged_PreservesSelectedPort()
    {
        // Arrange
        var usbMonitorMock = new Mock<IUsbDeviceMonitorService>();
        var initialPorts = new[]
        {
            new AvailableSerialPort("id1", "COM1", "desc1"),
            new AvailableSerialPort("id2", "COM2", "desc2")
        };
        _serialPortConnectionServiceMock.Setup(x => x.FindAvailableSerialPorts())
            .ReturnsAsync(initialPorts);

        using var viewModel = new ConfigurationViewModel(
            _dialogServiceMock.Object,
            _deviceManagementServiceMock.Object,
            _serialPortConnectionServiceMock.Object,
            usbMonitorMock.Object);

        await viewModel.InitializationComplete;

        // Select COM2
        viewModel.SelectedSerialPort = viewModel.AvailableSerialPorts.First(p => p.Name == "COM2");

        // Updated ports still include COM2
        var updatedPorts = new[]
        {
            new AvailableSerialPort("id1", "COM1", "desc1"),
            new AvailableSerialPort("id2", "COM2", "desc2"),
            new AvailableSerialPort("id3", "COM3", "desc3")
        };
        _serialPortConnectionServiceMock.Setup(x => x.FindAvailableSerialPorts())
            .ReturnsAsync(updatedPorts);

        // Act
        usbMonitorMock.Raise(
            d => d.UsbDeviceChanged += null!,
            new UsbDeviceChangedEventArgs(UsbDeviceChangeType.Connected, new[] { "COM1", "COM2", "COM3" }));

        // Allow async handler to complete
        await Task.Delay(100);

        // Assert - Previously selected COM2 should still be selected
        Assert.That(viewModel.SelectedSerialPort?.Name, Is.EqualTo("COM2"));
    }

    #endregion

    #region Security Key Validation Tests

    [Test]
    public void IsSecurityKeyValid_ReturnsTrue_WhenUsingDefaultKey()
    {
        // Arrange
        _viewModel.UseDefaultKey = true;
        _viewModel.UseSecureChannel = true;
        _viewModel.SecurityKey = "SHORT";

        // Assert
        Assert.That(_viewModel.IsSecurityKeyValid, Is.True);
        Assert.That(_viewModel.IsSecurityKeyInvalid, Is.False);
    }

    [Test]
    public void IsSecurityKeyValid_ReturnsTrue_WhenSecureChannelOff()
    {
        // Arrange - Connect to PD mode with secure channel off
        _viewModel.IsConnectToPDSelected = true;
        _viewModel.UseSecureChannel = false;
        _viewModel.UseDefaultKey = false;
        _viewModel.SecurityKey = "SHORT";

        // Assert
        Assert.That(_viewModel.IsSecurityKeyValid, Is.True);
    }

    [Test]
    public void IsSecurityKeyValid_ReturnsFalse_WhenKeyTooShort()
    {
        // Arrange
        _viewModel.UseSecureChannel = true;
        _viewModel.UseDefaultKey = false;
        _viewModel.SecurityKey = "0123456789ABCDEF"; // 16 chars, need 32

        // Assert
        Assert.That(_viewModel.IsSecurityKeyValid, Is.False);
        Assert.That(_viewModel.IsSecurityKeyInvalid, Is.True);
    }

    [Test]
    public void IsSecurityKeyValid_ReturnsTrue_WhenKeyExactly32HexChars()
    {
        // Arrange
        _viewModel.UseSecureChannel = true;
        _viewModel.UseDefaultKey = false;
        _viewModel.SecurityKey = "0123456789ABCDEF0123456789ABCDEF";

        // Assert
        Assert.That(_viewModel.IsSecurityKeyValid, Is.True);
        Assert.That(_viewModel.IsSecurityKeyInvalid, Is.False);
    }

    [Test]
    public void IsValidHexKey_ReturnsFalse_WhenContainsNonHexChars()
    {
        // Assert
        Assert.That(ConfigurationViewModel.IsValidHexKey("0123456789ABCDEF0123456789ABCDEG"), Is.False);
    }

    [Test]
    public void IsValidHexKey_ReturnsTrue_ForValidLowercaseHex()
    {
        // Assert
        Assert.That(ConfigurationViewModel.IsValidHexKey("0123456789abcdef0123456789abcdef"), Is.True);
    }

    [Test]
    public void CanConnectDevice_ReturnsFalse_WhenNoSerialPort()
    {
        // Arrange
        _viewModel.SelectedSerialPort = null;
        _viewModel.UseSecureChannel = true;
        _viewModel.UseDefaultKey = false;
        _viewModel.SecurityKey = "0123456789ABCDEF0123456789ABCDEF";

        // Assert
        Assert.That(_viewModel.CanConnectDevice, Is.False);
    }

    [Test]
    public void CanConnectDevice_ReturnsFalse_WhenKeyInvalid()
    {
        // Arrange
        SelectTestSerialPortAndBaudRate();
        _viewModel.UseSecureChannel = true;
        _viewModel.UseDefaultKey = false;
        _viewModel.SecurityKey = "SHORT";

        // Assert
        Assert.That(_viewModel.CanConnectDevice, Is.False);
    }

    [Test]
    public void CanConnectDevice_ReturnsTrue_WhenPortSelectedAndKeyValid()
    {
        // Arrange
        SelectTestSerialPortAndBaudRate();
        _viewModel.UseSecureChannel = true;
        _viewModel.UseDefaultKey = false;
        _viewModel.SecurityKey = "0123456789ABCDEF0123456789ABCDEF";

        // Assert
        Assert.That(_viewModel.CanConnectDevice, Is.True);
    }

    [Test]
    public void CanStartPassiveMonitoring_ReturnsFalse_WhenKeyInvalid()
    {
        // Arrange
        _viewModel.IsConnectToPDSelected = false;
        _viewModel.UseDefaultKey = false;
        _viewModel.SecurityKey = "SHORT";

        // Assert
        Assert.That(_viewModel.CanStartPassiveMonitoring, Is.False);
    }

    [Test]
    public void CanStartPassiveMonitoring_ReturnsTrue_WhenUsingDefaultKey()
    {
        // Arrange
        _viewModel.IsConnectToPDSelected = false;
        _viewModel.UseDefaultKey = true;

        // Assert
        Assert.That(_viewModel.CanStartPassiveMonitoring, Is.True);
    }

    #endregion

    #region Cancel Discovery Tests

    [Test]
    public async Task CancelDiscoveryCommand_CancelsInProgressDiscovery()
    {
        // Arrange
        await _viewModel.InitializationComplete;
        SetupConnectionService();
        SelectTestSerialPortAndBaudRate();

        // Set up discovery to delay so we can cancel it
        _deviceManagementServiceMock.Setup(x => x.DiscoverDevice(
                It.IsAny<IEnumerable<IOsdpConnection>>(),
                It.IsAny<DiscoveryProgress>(),
                It.IsAny<CancellationToken>()))
            .Returns<IEnumerable<IOsdpConnection>, DiscoveryProgress, CancellationToken>(
                async (_, _, ct) =>
                {
                    await Task.Delay(5000, ct);
                    return Mock.Of<DiscoveryResult>(r => r.Status == DiscoveryStatus.Succeeded);
                });

        // Act - Start discovery then cancel
        var discoverTask = _viewModel.DiscoverDeviceCommand.ExecuteAsync(null);
        await Task.Delay(50); // Let discovery start
        _viewModel.DiscoverDeviceCommand.Cancel();

        // Wait for the task to complete (should be cancelled)
        await discoverTask;

        // Assert - The cancel command exists and was invocable
        Assert.That(_viewModel.DiscoverDeviceCommand.IsCancellationRequested, Is.True);
    }

    #endregion
}