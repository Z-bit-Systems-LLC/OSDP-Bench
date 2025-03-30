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
    public async Task ConnectViewModel_ExecuteScanSerialPortsCommand()
    {
        // Arrange
        _viewModel.StatusLevel = StatusLevel.Ready;
        var expectedAvailableSerialPorts = new[]
        {
            new AvailableSerialPort("id1", "test1", "desc1"),
            new AvailableSerialPort("id2", "test2", "desc2")
        };
        _serialPortConnectionServiceMock.Setup(expression => expression.FindAvailableSerialPorts())
            .ReturnsAsync(expectedAvailableSerialPorts);

        // Act
        await _viewModel.ScanSerialPortsCommand.ExecuteAsync(null);

        // Assert
        Assert.That(expectedAvailableSerialPorts.Length, Is.EqualTo(_viewModel.AvailableSerialPorts.Count));
        Assert.That(expectedAvailableSerialPorts, Is.EqualTo(_viewModel.AvailableSerialPorts));
        Assert.That(_viewModel.StatusLevel, Is.EqualTo(StatusLevel.Ready));
    }
    
    [Test]
    public async Task ConnectViewModel_ExecuteScanSerialPortsCommand_NoPortsFound()
    {
        // Arrange
        _viewModel.StatusLevel = StatusLevel.Ready;
        _serialPortConnectionServiceMock.Setup(x => x.FindAvailableSerialPorts())
            .ReturnsAsync(Array.Empty<AvailableSerialPort>());

        // Act
        await _viewModel.ScanSerialPortsCommand.ExecuteAsync(null);

        // Assert
        Assert.That(_viewModel.AvailableSerialPorts.Count, Is.EqualTo(0));
        Assert.That(_viewModel.AvailableSerialPorts, Is.Empty);
        
        // Verify that the dialog service was called to show a message to the user
        _dialogServiceMock.Verify(
            x => x.ShowMessageDialog(
                It.IsAny<string>(),  // Title
                It.IsAny<string>(),  // Message
                MessageIcon.Error),
            Times.Once);
        Assert.That(_viewModel.StatusLevel, Is.EqualTo(StatusLevel.NotReady));
    }
    
    [Test]
    public async Task ConnectViewModel_ExecuteScanSerialPortsCommand_AlreadyConnected()
    {
        // Arrange
        _viewModel.StatusLevel = StatusLevel.Connected;
        var expectedAvailableSerialPorts = new[]
        {
            new AvailableSerialPort("id1", "test1", "desc1"),
            new AvailableSerialPort("id2", "test2", "desc2")
        };
        _serialPortConnectionServiceMock.Setup(expression => expression.FindAvailableSerialPorts())
            .ReturnsAsync(expectedAvailableSerialPorts);
        _dialogServiceMock.Setup(expression => expression.ShowConfirmationDialog(
            It.IsAny<string>(), // Message
            It.IsAny<string>(), // Message
            MessageIcon.Warning)).ReturnsAsync(true);

        // Act
        await _viewModel.ScanSerialPortsCommand.ExecuteAsync(null);

        // Assert
        Assert.That(expectedAvailableSerialPorts.Length, Is.EqualTo(_viewModel.AvailableSerialPorts.Count));
        Assert.That(expectedAvailableSerialPorts, Is.EqualTo(_viewModel.AvailableSerialPorts));
        Assert.That(_viewModel.StatusLevel, Is.EqualTo(StatusLevel.Ready));
    }
    
    [Test]
    public async Task ConnectViewModel_ExecuteScanSerialPortsCommand_CancelAlreadyConnected()
    {
        // Arrange
        _viewModel.StatusLevel = StatusLevel.Connected;
        var expectedAvailableSerialPorts = new[]
        {
            new AvailableSerialPort("id1", "test1", "desc1"),
            new AvailableSerialPort("id2", "test2", "desc2")
        };
        _serialPortConnectionServiceMock.Setup(expression => expression.FindAvailableSerialPorts())
            .ReturnsAsync(expectedAvailableSerialPorts);
        _dialogServiceMock.Setup(expression => expression.ShowConfirmationDialog(
            It.IsAny<string>(), // Message
            It.IsAny<string>(), // Message
            MessageIcon.Warning)).ReturnsAsync(false);

        // Act
        await _viewModel.ScanSerialPortsCommand.ExecuteAsync(null);

        // Assert
        Assert.That(_viewModel.StatusLevel, Is.EqualTo(StatusLevel.Connected));
    }
    
    [Test]
    public async Task ConnectViewModel_ExecuteDiscoverDeviceCommand()
    {
        // Arrange
        var discoveryResult = Mock.Of<DiscoveryResult>(r => r.Status == DiscoveryStatus.Started);
        
        _serialPortConnectionServiceMock.Setup(x => x.GetConnection(It.IsAny<string>(), It.IsAny<int>()))
            .Returns(Mock.Of<ISerialPortConnectionService>());
            
        _deviceManagementServiceMock.Setup(x => x.DiscoverDevice(
                It.IsAny<IEnumerable<IOsdpConnection>>(),
                It.IsAny<DiscoveryProgress>(),
                It.IsAny<CancellationToken>()))
            .ReturnsAsync(discoveryResult);
        
        // Select a serial port and baud rate in the viewmodel
        _viewModel.SelectedSerialPort = new AvailableSerialPort("COM1", "Port 1", "Description 1");
        _viewModel.SelectedBaudRate = 9600;

        // Act
        await _viewModel.DiscoverDeviceCommand.ExecuteAsync(null);

        // Assert
        // Verify the device management service's DiscoverDevice method was called
        _deviceManagementServiceMock.Verify(
            x => x.DiscoverDevice(
                It.IsAny<IEnumerable<IOsdpConnection>>(),
                It.IsAny<DiscoveryProgress>(),
                It.IsAny<CancellationToken>()),
            Times.Once);
        Assert.That(_viewModel.StatusLevel, Is.EqualTo(StatusLevel.Discovering));
    }
    
    [Test]
    public async Task ConnectViewModel_ExecuteDiscoverDeviceCommand_Cancelled()
    {
        // Arrange
        _serialPortConnectionServiceMock.Setup(x => x.GetConnection(It.IsAny<string>(), It.IsAny<int>()))
            .Returns(Mock.Of<ISerialPortConnectionService>());
            
        _deviceManagementServiceMock.Setup(x => x.DiscoverDevice(
                It.IsAny<IEnumerable<IOsdpConnection>>(),
                It.IsAny<DiscoveryProgress>(),
                It.IsAny<CancellationToken>()))
            .ThrowsAsync(new OperationCanceledException());
        
        // Select a serial port and baud rate in the viewmodel
        _viewModel.SelectedSerialPort = new AvailableSerialPort("COM1", "Port 1", "Description 1");
        _viewModel.SelectedBaudRate = 9600;

        // Act
        await _viewModel.DiscoverDeviceCommand.ExecuteAsync(null);

        // Assert
        // Verify the device management service's DiscoverDevice method was called
        _deviceManagementServiceMock.Verify(
            x => x.DiscoverDevice(
                It.IsAny<IEnumerable<IOsdpConnection>>(),
                It.IsAny<DiscoveryProgress>(),
                It.IsAny<CancellationToken>()),
            Times.Once);
    }
    
    [Test]
    public async Task ConnectViewModel_ExecuteDiscoverDeviceCommand_NoPortSelected()
    {
        // Arrange
        _viewModel.SelectedSerialPort = null;
        _viewModel.SelectedBaudRate = 9600;

        // Act
        await _viewModel.DiscoverDeviceCommand.ExecuteAsync(null);

        // Assert
        // Verify that the device management service was not called
        _deviceManagementServiceMock.Verify(
            x => x.DiscoverDevice(
                It.IsAny<IEnumerable<IOsdpConnection>>(),
                It.IsAny<DiscoveryProgress>(),
                It.IsAny<CancellationToken>()),
            Times.Never);
    }

    [Test]
    public async Task ConnectViewModel_ExecuteConnectDeviceCommand()
    {
        // Arrange
        string selectedPort = "COM1";
        int selectedBaudRate = 9600;
        byte selectedAddress = 1;
        _serialPortConnectionServiceMock.Setup(x => x.GetConnection(selectedPort, selectedBaudRate))
            .Returns(_serialPortConnectionServiceMock.Object);
        
        _viewModel.SelectedSerialPort = new AvailableSerialPort("COM1", selectedPort, "Description 1");
        _viewModel.SelectedBaudRate = selectedBaudRate;
        _viewModel.SelectedAddress = selectedAddress;

        // Act
        await _viewModel.ConnectDeviceCommand.ExecuteAsync(null);

        // Assert
        Assert.That(_viewModel.StatusLevel, Is.EqualTo(StatusLevel.ConnectingManually));
        _serialPortConnectionServiceMock.Verify(
            x => x.GetConnection(selectedPort, selectedBaudRate),
            Times.Once);
        _deviceManagementServiceMock.Verify(x => x.Shutdown(),
            Times.Once);
        _deviceManagementServiceMock.Verify(
            x => x.Connect(_serialPortConnectionServiceMock.Object, selectedAddress, false, true, null),
            Times.Once);
        Assert.That(_viewModel.ConnectedAddress, Is.EqualTo(selectedAddress));
        Assert.That(_viewModel.ConnectedBaudRate, Is.EqualTo(selectedBaudRate));
    }
    
    [Test]
    public async Task ConnectViewModel_ExecuteConnectDeviceCommand_NoSerialPortSelected()
    {
        // Arrange
        int selectedBaudRate = 9600;
        byte selectedAddress = 1;
        
        _viewModel.SelectedSerialPort = null;
        _viewModel.SelectedBaudRate = selectedBaudRate;
        _viewModel.SelectedAddress = selectedAddress;
        _viewModel.SecurityKey = "1234556";
        _viewModel.UseSecureChannel = true;
        _viewModel.UseDefaultKey = false;

        // Act
        await _viewModel.ConnectDeviceCommand.ExecuteAsync(null);

        // Assert
        _deviceManagementServiceMock.Verify(x => x.Shutdown(),
            Times.Never);
        _deviceManagementServiceMock.Verify(
            x => x.Connect(_serialPortConnectionServiceMock.Object, selectedAddress, false, true, null),
            Times.Never);
    }
    
    [Test]
    public async Task ConnectViewModel_ExecuteConnectDeviceCommand_InvalidSecurityKey()
    {
        // Arrange
        string selectedPort = "COM1";
        int selectedBaudRate = 9600;
        byte selectedAddress = 1;
        _serialPortConnectionServiceMock.Setup(x => x.GetConnection(selectedPort, selectedBaudRate))
            .Returns(_serialPortConnectionServiceMock.Object);
        
        _viewModel.SelectedSerialPort = new AvailableSerialPort("COM1", selectedPort, "Description 1");
        _viewModel.SelectedBaudRate = selectedBaudRate;
        _viewModel.SelectedAddress = selectedAddress;
        _viewModel.SecurityKey = "1234556";
        _viewModel.UseSecureChannel = true;
        _viewModel.UseDefaultKey = false;

        // Act
        await _viewModel.ConnectDeviceCommand.ExecuteAsync(null);

        // Assert
        Assert.That(_viewModel.StatusLevel, Is.EqualTo(StatusLevel.ConnectingManually));
        _serialPortConnectionServiceMock.Verify(
            x => x.GetConnection(selectedPort, selectedBaudRate),
            Times.Never);
        _deviceManagementServiceMock.Verify(x => x.Shutdown(),
            Times.Never);
        _deviceManagementServiceMock.Verify(
            x => x.Connect(_serialPortConnectionServiceMock.Object, selectedAddress, false, true, null),
            Times.Never);
        _dialogServiceMock.Verify(
            x => x.ShowMessageDialog(
                It.IsAny<string>(),  // Title
                It.IsAny<string>(),  // Message
                It.IsAny<MessageIcon>()),
            Times.Once);
    }
}