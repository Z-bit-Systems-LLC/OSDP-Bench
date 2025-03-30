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
                It.IsAny<string>(),  // Message content
                It.IsAny<string>(),  // Title
                It.IsAny<MessageIcon>()),  // Button options 
            Times.Once);
        Assert.That(_viewModel.StatusLevel, Is.EqualTo(StatusLevel.NotReady));
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
}