using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Moq;
using OSDPBench.Core.Models;
using OSDPBench.Core.Services;
using OSDPBench.Core.ViewModels.Pages;
using NUnit.Framework;
using OSDP.Net.Connections;
using OSDP.Net.PanelCommands.DeviceDiscover;

namespace OSDPBench.Core.Tests.ViewModels;

[TestFixture(TestOf = typeof(ConnectViewModel))]
public class ConnectViewModelTests
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
    private ConnectViewModel _viewModel;

    [SetUp]
    public void Setup()
    {
        _dialogServiceMock = new Mock<IDialogService>();
        _deviceManagementServiceMock = new Mock<IDeviceManagementService>();
        _serialPortConnectionServiceMock = new Mock<ISerialPortConnectionService>();
        
        _viewModel = new ConnectViewModel(
            _dialogServiceMock.Object,
            _deviceManagementServiceMock.Object,
            _serialPortConnectionServiceMock.Object
        );
    }

    [Test]
    public void ConnectViewModel_InitializedAvailableBaudRates()
    {
        // Arrange
        var expectedBaudRates = new[] { 9600, 19200, 38400, 57600, 115200, 230400 };
        
        // Assert
        Assert.That(expectedBaudRates.Length, Is.EqualTo(_viewModel.AvailableBaudRates.Count));
        Assert.That(expectedBaudRates , Is.EqualTo(_viewModel.AvailableBaudRates.ToArray()));
    }
    
    [Test]
    public async Task ConnectViewModel_InitializesSerialPortsOnStartup()
    {
        // Arrange
        var availablePorts = CreateTestSerialPorts();
        SetupSerialPortMockWithPorts(availablePorts);
        
        // Act - Create a new view model which should trigger initialization
        var newViewModel = new ConnectViewModel(
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
    public async Task ConnectViewModel_InitializesSerialPortsOnStartup_NoPortsFound()
    {
        // Arrange
        var emptyPorts = new AvailableSerialPort[0];
        SetupSerialPortMockWithPorts(emptyPorts);
        
        // Act - Create a new view model which should trigger initialization
        var newViewModel = new ConnectViewModel(
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
    public void ConnectViewModel_InitializesSerialPortsOnStartup_HandlesException()
    {
        // Arrange
        _serialPortConnectionServiceMock.Setup(x => x.FindAvailableSerialPorts())
            .ThrowsAsync(new Exception("Test exception"));
        
        // Act - Create a new view model which should trigger initialization
        var newViewModel = new ConnectViewModel(
            _dialogServiceMock.Object,
            _deviceManagementServiceMock.Object,
            _serialPortConnectionServiceMock.Object);
        
        // Assert - InitializationComplete should throw the exception
        Assert.ThrowsAsync<Exception>(async () => await newViewModel.InitializationComplete);
        Assert.That(newViewModel.StatusLevel, Is.EqualTo(StatusLevel.NotReady));
    }

    #region DiscoverDevice Tests
    
    [Test]
    public async Task ConnectViewModel_ExecuteDiscoverDeviceCommand()
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
    public async Task ConnectViewModel_ExecuteDiscoverDeviceCommand_Cancelled()
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
    public async Task ConnectViewModel_ExecuteDiscoverDeviceCommand_NoPortSelected()
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
    public async Task ConnectViewModel_ExecuteConnectDeviceCommand()
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
    public async Task ConnectViewModel_ExecuteConnectDeviceCommand_NoSerialPortSelected()
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
    public async Task ConnectViewModel_ExecuteConnectDeviceCommand_InvalidSecurityKey()
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
    public async Task ConnectViewModel_DeviceManagementServiceOnConnectionStatusChange_Connected()
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
    public async Task ConnectViewModel_DeviceManagementServiceOnConnectionStatusChange_Disconnected()
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
    public async Task ConnectViewModel_DeviceManagementServiceOnConnectionStatusChange_InvalidSecurityKey()
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
    public async Task ConnectViewModel_DeviceManagementServiceOnConnectionStatusChange_WhenDiscoveredStatus()
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
    public void ConnectViewModel_DeviceManagementServiceOnNakReplyReceived()
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
    
    // Future enhancements: Add trace entry tests which require more complex mocking
    // For now we'll focus on the refactoring opportunities
}