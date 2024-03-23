using System.Threading.Tasks;
using Moq;
using OSDPBench.Core.Models;
using OSDPBench.Core.Services;
using OSDPBench.Core.ViewModels.Pages;
using NUnit.Framework;

namespace OSDPBench.Core.Tests.ViewModels;

public class ConnectViewModelTests
{
    [Test]
    public void ConnectViewModel_InitializedAvailableBaudRates()
    {
        // Arrange
        var dialogService = new Mock<IDialogService>();
        var deviceManagementService = new Mock<IDeviceManagementService>();
        var serialPortConnectionService = new Mock<ISerialPortConnectionService>();
        
        // Act
        var connectViewModel = new ConnectViewModel(dialogService.Object, deviceManagementService.Object, serialPortConnectionService.Object);

        // Assert
        Assert.That(6, Is.EqualTo(connectViewModel.AvailableBaudRates.Count));
    }

    [Test]
    public async Task ConnectViewModel_ExecuteScanSerialPortsCommand()
    {
        // Arrange
        var dialogService = new Mock<IDialogService>();
        var deviceManagementService = new Mock<IDeviceManagementService>();
        var serialPortConnectionService = new Mock<ISerialPortConnectionService>();
        serialPortConnectionService.Setup(expression => expression.FindAvailableSerialPorts())
            .ReturnsAsync(new[]
            {
                new AvailableSerialPort("id1", "test1", "desc1"), new AvailableSerialPort("id2", "test2", "desc2")
            });

        var connectViewModel = new ConnectViewModel(dialogService.Object, deviceManagementService.Object, serialPortConnectionService.Object);

        // Act
        await connectViewModel.ScanSerialPortsCommand.ExecuteAsync(null);

        // Assert
        Assert.That(2, Is.EqualTo(connectViewModel.AvailableSerialPorts.Count));
    }
}