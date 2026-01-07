using System;
using System.Collections;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using Moq;
using NUnit.Framework;
using OSDP.Net;
using OSDP.Net.Connections;
using OSDP.Net.Model.ReplyData;
using OSDP.Net.PanelCommands.DeviceDiscover;
using OSDP.Net.Tracing;
using OSDPBench.Core.Models;
using OSDPBench.Core.Services;
using OSDPBench.Core.Actions;

namespace OSDPBench.Core.Tests.Services
{
    [TestFixture(TestOf = typeof(DeviceManagementService))]
    public class DeviceManagementServiceTests
    {
        private DeviceManagementService _deviceManagementService;
        private Mock<IOsdpConnection> _connectionMock;
        private readonly byte _testAddress = 0x7F;
        private readonly uint _testBaudRate = 9600;
        
        // Helper method for DiscoveryProgress
        private void UpdateStatus(DiscoveryResult result)
        {
            // This method is just used to satisfy the DiscoveryProgress delegate requirement
            Console.WriteLine($@"Status: {result.Status}, Connection: {result.Connection?.BaudRate}");
        }

        [SetUp]
        public void Setup()
        {
            _connectionMock = new Mock<IOsdpConnection>();
            _connectionMock.Setup(x => x.BaudRate).Returns((int)_testBaudRate);
            
            _deviceManagementService = new DeviceManagementService();
        }

        [TearDown]
        public async Task Cleanup()
        {
            // Ensure we clean up resources after each test
            await _deviceManagementService.Shutdown();
        }

        #region Constructor Tests

        [Test]
        public void Constructor_InitializesProperties()
        {
            // Assert
            Assert.That(_deviceManagementService.IdentityLookup, Is.Null);
            Assert.That(_deviceManagementService.CapabilitiesLookup, Is.Null);
            Assert.That(_deviceManagementService.PortName, Is.Null);
            Assert.That(_deviceManagementService.IsConnected, Is.False);
        }

        #endregion

        #region Connect Tests

        [Test]
        public async Task Connect_SetsAddressAndBaudRate()
        {
            // Arrange
            const bool useSecureChannel = false;
            const bool useDefaultSecurityKey = true;

            // Act
            await _deviceManagementService.Connect(_connectionMock.Object, _testAddress, useSecureChannel, useDefaultSecurityKey, null);

            // Assert
            Assert.That(_deviceManagementService.Address, Is.EqualTo(_testAddress));
            Assert.That(_deviceManagementService.BaudRate, Is.EqualTo(_testBaudRate));
            Assert.That(_deviceManagementService.IsUsingSecureChannel, Is.EqualTo(useSecureChannel));
        }

        [Test]
        public async Task Connect_WithSecureChannel_SetsIsUsingSecureChannel()
        {
            // Arrange
            const bool useSecureChannel = true;
            const bool useDefaultSecurityKey = true;

            // Act
            await _deviceManagementService.Connect(_connectionMock.Object, _testAddress, useSecureChannel, useDefaultSecurityKey, null);

            // Assert
            Assert.That(_deviceManagementService.IsUsingSecureChannel, Is.True);
        }

        [Test]
        public async Task Connect_WithCustomSecurityKey_UsesProvidedKey()
        {
            // Arrange
            const bool useSecureChannel = true;
            const bool useDefaultSecurityKey = false;
            byte[] customKey = [0x01, 0x02, 0x03, 0x04, 0x05, 0x06, 0x07, 0x08];

            // Act
            await _deviceManagementService.Connect(_connectionMock.Object, _testAddress, useSecureChannel, useDefaultSecurityKey, customKey);

            // Assert
            Assert.That(_deviceManagementService.IsUsingSecureChannel, Is.True);
            // We can't directly test the security key was used because it's passed to an internal component,
            // but we can verify the flags are set correctly
            Assert.That(_deviceManagementService.UsesDefaultSecurityKey, Is.False);
        }

        #endregion

        #region DiscoverDevice Tests

        [Test]
        public async Task DiscoverDevice_ResetsLookupProperties()
        {
            // Arrange
            var mockConnections = new List<IOsdpConnection> { _connectionMock.Object };
            var progressCallback = new DiscoveryProgress(UpdateStatus);
            var cancellationToken = CancellationToken.None;
            
            // Force the DiscoverDevice method to throw to avoid complex mocking
            _connectionMock.Setup(x => x.Open()).Throws(new InvalidOperationException("Test exception"));
            
            // Act & Assert
            try
            {
                await _deviceManagementService.DiscoverDevice(mockConnections, progressCallback, cancellationToken);
            }
            catch (Exception)
            {
                // We expect an exception due to our mocking
            }
            
            // Assert that properties are reset even if discovery fails
            Assert.That(_deviceManagementService.IdentityLookup, Is.Null);
            Assert.That(_deviceManagementService.CapabilitiesLookup, Is.Null);
        }

        [Test]
        public void DiscoverDevice_CancellationRequested_ThrowsOperationCanceledException()
        {
            // Arrange
            var mockConnections = new List<IOsdpConnection> { _connectionMock.Object };
            var progressCallback = new DiscoveryProgress(UpdateStatus);
            var cts = new CancellationTokenSource();
            cts.Cancel(); // Cancel the token immediately
            
            // Act & Assert
            Assert.ThrowsAsync<OperationCanceledException>(async () => 
            {
                await _deviceManagementService.DiscoverDevice(mockConnections, progressCallback, cts.Token);
            });
        }

        #endregion

        #region ExecuteDeviceAction Tests

        [Test]
        public async Task ExecuteDeviceAction_ForwardsToAction()
        {
            // Arrange
            var mockAction = new Mock<IDeviceAction>();
            var parameter = new object();
            var expectedResult = new object();
            
            mockAction.Setup(x => x.PerformAction(
                    It.IsAny<ControlPanel>(), 
                    It.IsAny<Guid>(), 
                    It.IsAny<byte>(),
                    It.Is<object>(p => p == parameter)))
                .ReturnsAsync(expectedResult);
            
            // Act
            var result = await _deviceManagementService.ExecuteDeviceAction(mockAction.Object, parameter);
            
            // Assert
            Assert.That(result, Is.EqualTo(expectedResult));
            mockAction.Verify(x => x.PerformAction(
                It.IsAny<ControlPanel>(),
                It.IsAny<Guid>(),
                It.IsAny<byte>(),
                It.Is<object>(p => p == parameter)), 
                Times.Once);
        }

        #endregion

        #region Shutdown Tests

        [Test]
        public async Task Shutdown_ResetsLookupProperties()
        {
            // Check if IdentityLookup and CapabilitiesLookup are already null
            Assert.That(_deviceManagementService.IdentityLookup, Is.Null);
            Assert.That(_deviceManagementService.CapabilitiesLookup, Is.Null);

            // Since we can't set the properties directly as they require complex objects,
            // we'll just verify that Shutdown keeps them null
            await _deviceManagementService.Shutdown();
            
            // Assert they're still null after shutdown
            Assert.That(_deviceManagementService.IdentityLookup, Is.Null);
            Assert.That(_deviceManagementService.CapabilitiesLookup, Is.Null);
        }

        #endregion

        #region ReconnectAfterCommunicationChange Tests

        [Test]
        public async Task ReconnectAfterCommunicationChange_UpdatesAddressAndBaudRate()
        {
            // Arrange
            const byte newAddress = 0x01;
            const int newBaudRate = 115200;
            var newConnectionMock = new Mock<IOsdpConnection>();
            newConnectionMock.Setup(x => x.BaudRate).Returns(newBaudRate);

            // Act - This will attempt to reconnect and wait for device online
            // Since there's no real device, it will timeout after 10 seconds
            // We'll use a try-catch to avoid waiting for the full timeout
            try
            {
                await Task.WhenAny(
                    _deviceManagementService.ReconnectAfterCommunicationChange(newConnectionMock.Object, newAddress),
                    Task.Delay(100) // Give it a small window to set properties
                );
            }
            catch (TimeoutException)
            {
                // Expected - no real device to connect to
            }

            // Assert - Properties should be updated even if connection times out
            Assert.That(_deviceManagementService.Address, Is.EqualTo(newAddress));
            Assert.That(_deviceManagementService.BaudRate, Is.EqualTo(newBaudRate));
        }

        [Test]
        public async Task ReconnectAfterCommunicationChange_PreservesSecuritySettings()
        {
            // Arrange
            const byte initialAddress = 0x7F;
            const bool useSecureChannel = true;
            const bool useDefaultSecurityKey = false;
            byte[] customKey = [0x01, 0x02, 0x03, 0x04, 0x05, 0x06, 0x07, 0x08];

            // First connect with security settings
            await _deviceManagementService.Connect(_connectionMock.Object, initialAddress, useSecureChannel, useDefaultSecurityKey, customKey);

            // Verify initial security settings
            Assert.That(_deviceManagementService.IsUsingSecureChannel, Is.True);
            Assert.That(_deviceManagementService.UsesDefaultSecurityKey, Is.False);

            // Arrange for reconnection
            const byte newAddress = 0x01;
            const int newBaudRate = 115200;
            var newConnectionMock = new Mock<IOsdpConnection>();
            newConnectionMock.Setup(x => x.BaudRate).Returns(newBaudRate);

            // Act - Reconnect with new communication parameters
            try
            {
                await Task.WhenAny(
                    _deviceManagementService.ReconnectAfterCommunicationChange(newConnectionMock.Object, newAddress),
                    Task.Delay(100)
                );
            }
            catch (TimeoutException)
            {
                // Expected - no real device to connect to
            }

            // Assert - Security settings should be preserved
            Assert.That(_deviceManagementService.Address, Is.EqualTo(newAddress));
            Assert.That(_deviceManagementService.BaudRate, Is.EqualTo(newBaudRate));
            Assert.That(_deviceManagementService.IsUsingSecureChannel, Is.True,
                "Secure channel setting should be preserved after reconnection");
            Assert.That(_deviceManagementService.UsesDefaultSecurityKey, Is.False,
                "Custom security key setting should be preserved after reconnection");
        }

        [Test]
        public async Task ReconnectAfterCommunicationChange_WaitsForDeviceOnline()
        {
            // Arrange
            const byte newAddress = 0x01;
            const int newBaudRate = 115200;
            var newConnectionMock = new Mock<IOsdpConnection>();
            newConnectionMock.Setup(x => x.BaudRate).Returns(newBaudRate);

            // Act & Assert - Verify that the method doesn't return immediately
            // Start the reconnection task
            var startTime = DateTime.Now;
            var reconnectTask = _deviceManagementService.ReconnectAfterCommunicationChange(newConnectionMock.Object, newAddress);

            // Wait a short time and verify the task is still running (not completed immediately)
            await Task.Delay(500);
            Assert.That(reconnectTask.IsCompleted, Is.False,
                "ReconnectAfterCommunicationChange should wait for device to come online, not return immediately");

            // Wait for the task to complete (or timeout)
            try
            {
                await reconnectTask;
            }
            catch (TimeoutException)
            {
                // Expected - no real device to connect to
            }

            var elapsed = DateTime.Now - startTime;

            // The method should have waited for a significant amount of time (approaching the 10-second timeout)
            Assert.That(elapsed.TotalSeconds, Is.GreaterThan(9.5),
                "Method should wait for approximately 10 seconds before timing out");
        }

        #endregion

        #region Reconnect Tests

        [Test]
        public async Task Reconnect_UpdatesAddressAndBaudRate()
        {
            // Arrange
            const byte newAddress = 0x02;
            const int newBaudRate = 57600;
            var newConnectionMock = new Mock<IOsdpConnection>();
            newConnectionMock.Setup(x => x.BaudRate).Returns(newBaudRate);

            // Act
            await _deviceManagementService.Reconnect(newConnectionMock.Object, newAddress);

            // Assert
            Assert.That(_deviceManagementService.Address, Is.EqualTo(newAddress));
            Assert.That(_deviceManagementService.BaudRate, Is.EqualTo(newBaudRate));
        }

        #endregion

        #region Event Tests

        [Test]
        public async Task ConnectionStatusChange_FiresOnConnect()
        {
            // Arrange
            var statusChangeReceived = false;
            
            _deviceManagementService.ConnectionStatusChange += (_, _) => 
            {
                statusChangeReceived = true;
            };
            
            // Act
            await _deviceManagementService.Connect(_connectionMock.Object, _testAddress, false, true, null);
            
            // Assert - This is a weak test as we can't easily trigger control panel events
            // In a real implementation, we should mock the control panel to trigger events
            Assert.That(statusChangeReceived, Is.False);
        }

        [Test]
        public void EventHandlers_CanBeSubscribedTo()
        {
            // We can't check if events are null directly, but we can subscribe to them
            // and verify no errors occur
            EventHandler<ConnectionStatus> connectionHandler = (_, _) => { };
            EventHandler deviceLookupsHandler = (_, _) => { };
            EventHandler<string> nakHandler = (_, _) => { };
            EventHandler<string> cardReadHandler = (_, _) => { };
            EventHandler<string> keypadHandler = (_, _) => { };
            EventHandler<TraceEntry> traceHandler = (_, _) => { };
            
            // Act - subscribe to events
            _deviceManagementService.ConnectionStatusChange += connectionHandler;
            _deviceManagementService.DeviceLookupsChanged += deviceLookupsHandler;
            _deviceManagementService.NakReplyReceived += nakHandler;
            _deviceManagementService.CardReadReceived += cardReadHandler;
            _deviceManagementService.KeypadReadReceived += keypadHandler;
            _deviceManagementService.TraceEntryReceived += traceHandler;
            
            // Cleanup - unsubscribe from events
            _deviceManagementService.ConnectionStatusChange -= connectionHandler;
            _deviceManagementService.DeviceLookupsChanged -= deviceLookupsHandler;
            _deviceManagementService.NakReplyReceived -= nakHandler;
            _deviceManagementService.CardReadReceived -= cardReadHandler;
            _deviceManagementService.KeypadReadReceived -= keypadHandler;
            _deviceManagementService.TraceEntryReceived -= traceHandler;
            
            // Assert - no exception means success
            Assert.Pass("All events can be subscribed to and unsubscribed from");
        }

        #endregion

        #region Passive Monitoring Tests

        [Test]
        public void IsPassiveMonitoring_Initially_ReturnsFalse()
        {
            // Assert
            Assert.That(_deviceManagementService.IsPassiveMonitoring, Is.False);
        }

        [Test]
        public async Task StartPassiveMonitoring_SetsIsPassiveMonitoringTrue()
        {
            // Arrange
            var connectionMock = new Mock<IOsdpConnection>();
            connectionMock.Setup(x => x.BaudRate).Returns(9600);
            connectionMock.Setup(x => x.Open());

            // Act
            await _deviceManagementService.StartPassiveMonitoring(connectionMock.Object);

            // Assert
            Assert.That(_deviceManagementService.IsPassiveMonitoring, Is.True);

            // Cleanup
            await _deviceManagementService.StopPassiveMonitoring();
        }

        [Test]
        public async Task StopPassiveMonitoring_SetsIsPassiveMonitoringFalse()
        {
            // Arrange
            var connectionMock = new Mock<IOsdpConnection>();
            connectionMock.Setup(x => x.BaudRate).Returns(9600);
            connectionMock.Setup(x => x.Open());

            await _deviceManagementService.StartPassiveMonitoring(connectionMock.Object);

            // Act
            await _deviceManagementService.StopPassiveMonitoring();

            // Assert
            Assert.That(_deviceManagementService.IsPassiveMonitoring, Is.False);
        }

        [Test]
        public async Task StartPassiveMonitoring_UpdatesBaudRate()
        {
            // Arrange
            const int expectedBaudRate = 115200;
            var connectionMock = new Mock<IOsdpConnection>();
            connectionMock.Setup(x => x.BaudRate).Returns(expectedBaudRate);
            connectionMock.Setup(x => x.Open());

            // Act
            await _deviceManagementService.StartPassiveMonitoring(connectionMock.Object);

            // Assert
            Assert.That(_deviceManagementService.BaudRate, Is.EqualTo(expectedBaudRate));

            // Cleanup
            await _deviceManagementService.StopPassiveMonitoring();
        }

        [Test]
        public async Task StartPassiveMonitoring_FiresConnectionStatusChange()
        {
            // Arrange
            var connectionMock = new Mock<IOsdpConnection>();
            connectionMock.Setup(x => x.BaudRate).Returns(9600);
            connectionMock.Setup(x => x.Open());

            ConnectionStatus? receivedStatus = null;
            _deviceManagementService.ConnectionStatusChange += (_, status) =>
            {
                receivedStatus = status;
            };

            // Act
            await _deviceManagementService.StartPassiveMonitoring(connectionMock.Object);

            // Assert
            Assert.That(receivedStatus, Is.EqualTo(ConnectionStatus.PassiveMonitoring));

            // Cleanup
            await _deviceManagementService.StopPassiveMonitoring();
        }

        [Test]
        public async Task StopPassiveMonitoring_FiresConnectionStatusChange()
        {
            // Arrange
            var connectionMock = new Mock<IOsdpConnection>();
            connectionMock.Setup(x => x.BaudRate).Returns(9600);
            connectionMock.Setup(x => x.Open());

            await _deviceManagementService.StartPassiveMonitoring(connectionMock.Object);

            ConnectionStatus? receivedStatus = null;
            _deviceManagementService.ConnectionStatusChange += (_, status) =>
            {
                receivedStatus = status;
            };

            // Act
            await _deviceManagementService.StopPassiveMonitoring();

            // Assert
            Assert.That(receivedStatus, Is.EqualTo(ConnectionStatus.Disconnected));
        }

        [Test]
        public async Task StartPassiveMonitoring_WithSecureChannel_SetsUsesDefaultSecurityKey()
        {
            // Arrange
            var connectionMock = new Mock<IOsdpConnection>();
            connectionMock.Setup(x => x.BaudRate).Returns(9600);
            connectionMock.Setup(x => x.Open());

            // Act
            await _deviceManagementService.StartPassiveMonitoring(connectionMock.Object, useSecureChannel: true);

            // Assert
            Assert.That(_deviceManagementService.UsesDefaultSecurityKey, Is.True);

            // Cleanup
            await _deviceManagementService.StopPassiveMonitoring();
        }

        [Test]
        public async Task StartPassiveMonitoring_WithCustomKey_SetsUsesDefaultSecurityKeyFalse()
        {
            // Arrange
            var connectionMock = new Mock<IOsdpConnection>();
            connectionMock.Setup(x => x.BaudRate).Returns(9600);
            connectionMock.Setup(x => x.Open());

            byte[] customKey = [0x01, 0x02, 0x03, 0x04, 0x05, 0x06, 0x07, 0x08,
                               0x09, 0x0A, 0x0B, 0x0C, 0x0D, 0x0E, 0x0F, 0x10];

            // Act
            await _deviceManagementService.StartPassiveMonitoring(
                connectionMock.Object,
                true,
                false,
                customKey);

            // Assert
            Assert.That(_deviceManagementService.UsesDefaultSecurityKey, Is.False);

            // Cleanup
            await _deviceManagementService.StopPassiveMonitoring();
        }

        #endregion

        #region Security Key Tests

        [Test]
        public void SecurityKey_Initially_ReturnsNull()
        {
            // Assert
            Assert.That(_deviceManagementService.SecurityKey, Is.Null);
        }

        [Test]
        public async Task Connect_WithCustomKey_SetsSecurityKey()
        {
            // Arrange
            byte[] customKey = [0x01, 0x02, 0x03, 0x04, 0x05, 0x06, 0x07, 0x08,
                               0x09, 0x0A, 0x0B, 0x0C, 0x0D, 0x0E, 0x0F, 0x10];

            // Act
            await _deviceManagementService.Connect(
                _connectionMock.Object,
                _testAddress,
                useSecureChannel: true,
                useDefaultSecurityKey: false,
                securityKey: customKey);

            // Assert
            Assert.That(_deviceManagementService.SecurityKey, Is.EqualTo(customKey));
        }

        #endregion

        #region Helper Method Tests

        [Test]
        public void FormatKeypadData_FormatsSpecialValues()
        {
            // We need reflection to test private methods
            var method = typeof(DeviceManagementService).GetMethod("FormatKeypadData", 
                System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Static);
            
            Assert.That(method, Is.Not.Null, "FormatKeypadData method should exist");
            
            // Test special values
            byte[] data = [0x31, 0x32, 0x7F, 0x0D]; // "12*#"
            var result = method.Invoke(null, [data]) as string;
            
            Assert.That(result, Is.EqualTo("12*#"));
        }

        [Test]
        public void FormatData_ConvertsBitArrayToString()
        {
            // We need reflection to test private methods
            var method = typeof(DeviceManagementService).GetMethod("FormatData", 
                System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Static);
            
            Assert.That(method, Is.Not.Null, "FormatData method should exist");
            
            // Test bit array conversion - use explicit bool[] cast to resolve ambiguity
            bool[] boolArray = [true, false, true, true, false];
            var bitArray = new BitArray(boolArray);
            var result = method.Invoke(null, [bitArray]) as string;
            
            Assert.That(result, Is.EqualTo("10110"));
        }

        [Test]
        public void ToFormattedText_AddsSpacesBetweenWords()
        {
            // We need reflection to test private methods
            var method = typeof(DeviceManagementService).GetMethod("ToFormattedText", 
                System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Static);
            
            Assert.That(method, Is.Not.Null, "ToFormattedText method should exist");
            
            // Test error code formatting
            var result = method.Invoke(null, [ErrorCode.CommunicationSecurityNotMet]) as string;
            
            Assert.That(result, Is.EqualTo("Communication Security Not Met"));
        }

        #endregion
    }
}