using NUnit.Framework;
using Moq;
using OSDPBench.Core.Services;
using OSDPBench.Core.ViewModels.Pages;

namespace OSDPBench.Core.Tests.ViewModels;

[TestFixture]
public class UsbMonitoringTests
{
    private Mock<IDialogService> _mockDialogService;
    private Mock<IDeviceManagementService> _mockDeviceManagementService;
    private Mock<ISerialPortConnectionService> _mockSerialPortConnectionService;
    private Mock<IUsbDeviceMonitorService> _mockUsbDeviceMonitorService;
    
    [SetUp]
    public void Setup()
    {
        _mockDialogService = new Mock<IDialogService>();
        _mockDeviceManagementService = new Mock<IDeviceManagementService>();
        _mockSerialPortConnectionService = new Mock<ISerialPortConnectionService>();
        _mockUsbDeviceMonitorService = new Mock<IUsbDeviceMonitorService>();
    }
    
    [Test]
    public void ConfigurationViewModel_WithUsbMonitorService_StartsMonitoring()
    {
        // Act
        using var viewModel = new ConfigurationViewModel(
            _mockDialogService.Object,
            _mockDeviceManagementService.Object,
            _mockSerialPortConnectionService.Object,
            _mockUsbDeviceMonitorService.Object);
        
        // Assert
        _mockUsbDeviceMonitorService.Verify(x => x.StartMonitoring(), Times.Once);
    }
    
    [Test]
    public void ConfigurationViewModel_WithoutUsbMonitorService_DoesNotThrow()
    {
        // Act & Assert
        // ReSharper disable once ObjectCreationAsStatement
        Assert.DoesNotThrow(() =>
        {
            using var viewModel = new ConfigurationViewModel(
                _mockDialogService.Object,
                _mockDeviceManagementService.Object,
                _mockSerialPortConnectionService.Object);
        });
    }
    
    [Test]
    public void ConfigurationViewModel_Dispose_StopsUsbMonitoring()
    {
        // Arrange
        var viewModel = new ConfigurationViewModel(
            _mockDialogService.Object,
            _mockDeviceManagementService.Object,
            _mockSerialPortConnectionService.Object,
            _mockUsbDeviceMonitorService.Object);
        
        // Act
        viewModel.Dispose();
        
        // Assert
        _mockUsbDeviceMonitorService.Verify(x => x.StopMonitoring(), Times.Once);
    }
}