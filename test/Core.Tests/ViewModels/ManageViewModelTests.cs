using System;
using System.Threading.Tasks;
using Moq;
using NUnit.Framework;
using OSDP.Net.Connections;
using OSDP.Net.Model.ReplyData;
using OSDPBench.Core.Actions;
using OSDPBench.Core.Models;
using OSDPBench.Core.Services;
using OSDPBench.Core.ViewModels.Pages;

namespace OSDPBench.Core.Tests.ViewModels
{
    [TestFixture(TestOf = typeof(ManageViewModel))]
    public class ManageViewModelTests
    {
        private Mock<IDialogService> _dialogServiceMock;
        private Mock<IDeviceManagementService> _deviceManagementServiceMock;
        private Mock<ISerialPortConnectionService> _serialPortConnnectionServiceMock;
        private ManageViewModel _viewModel;

        [SetUp]
        public void Setup()
        {
            _dialogServiceMock = new Mock<IDialogService>();
            _deviceManagementServiceMock = new Mock<IDeviceManagementService>();
            _serialPortConnnectionServiceMock = new Mock<ISerialPortConnectionService>();
            
            // Setup device management service's properties
            _deviceManagementServiceMock.Setup(x => x.PortName).Returns("COM1");
            _deviceManagementServiceMock.Setup(x => x.BaudRate).Returns(9600u);
            _deviceManagementServiceMock.Setup(x => x.Address).Returns(1);
            _deviceManagementServiceMock.Setup(x => x.IsConnected).Returns(true);
            
            _viewModel = new ManageViewModel(
                _dialogServiceMock.Object,
                _deviceManagementServiceMock.Object,
                _serialPortConnnectionServiceMock.Object
            );
        }

        [Test]
        public void ManageViewModel_Constructor_InitializesProperties()
        {
            // Assert
            Assert.That(_viewModel.AvailableBaudRates, Has.Count.EqualTo(6));
            Assert.That(_viewModel.AvailableDeviceActions, Has.Count.EqualTo(7));
            Assert.That(_viewModel.LastCardNumberRead, Is.EqualTo(string.Empty));
            Assert.That(_viewModel.KeypadReadData, Is.EqualTo(string.Empty));
            
            // Since device management service IsConnected returns true
            Assert.That(_viewModel.StatusLevel, Is.EqualTo(StatusLevel.Connected));
        }

        #region ExecuteDeviceAction Tests

        [Test]
        public async Task ExecuteDeviceAction_ForNormalAction_CallsDeviceManagementService()
        {
            // Arrange
            var mockAction = new Mock<IDeviceAction>();
            var parameter = new object();
            var expectedResult = new object();
            
            mockAction.Setup(x => x.PerformAction(
                    It.IsAny<OSDP.Net.ControlPanel>(), 
                    It.IsAny<Guid>(), 
                    It.IsAny<byte>(),
                    It.Is<object>(p => p == parameter)))
                .ReturnsAsync(expectedResult);
                
            _deviceManagementServiceMock.Setup(x => x.ExecuteDeviceAction(mockAction.Object, parameter))
                .ReturnsAsync(expectedResult);
            
            // Set the selected device action and parameter
            _viewModel.SelectedDeviceAction = mockAction.Object;
            _viewModel.DeviceActionParameter = parameter;
            
            // Act
            await _viewModel.ExecuteDeviceActionCommand.ExecuteAsync(null);
            
            // Assert
            _deviceManagementServiceMock.Verify(x => x.ExecuteDeviceAction(mockAction.Object, parameter), Times.Once);
        }

        [Test]
        public async Task ExecuteDeviceAction_WhenExceptionThrown_ShowsExceptionDialog()
        {
            // Arrange
            var mockAction = new Mock<IDeviceAction>();
            var parameter = new object();
            var expectedException = new Exception("Test exception");
            
            _deviceManagementServiceMock.Setup(x => x.ExecuteDeviceAction(mockAction.Object, parameter))
                .ThrowsAsync(expectedException);
            
            // Set the selected device action and parameter
            _viewModel.SelectedDeviceAction = mockAction.Object;
            _viewModel.DeviceActionParameter = parameter;
            
            // Act
            await _viewModel.ExecuteDeviceActionCommand.ExecuteAsync(null);
            
            // Assert
            _dialogServiceMock.Verify(
                x => x.ShowExceptionDialog(
                    "Performing Action", 
                    It.Is<Exception>(e => e.Message.Contains(expectedException.Message))),
                Times.Once);
        }

        [Test]
        public async Task ExecuteDeviceAction_WhenNoActionSelected_DoesNotCallExecuteDeviceAction()
        {
            // Arrange
            _viewModel.SelectedDeviceAction = null;
            
            // Act
            await _viewModel.ExecuteDeviceActionCommand.ExecuteAsync(null);
            
            // Assert
            _deviceManagementServiceMock.Verify(
                x => x.ExecuteDeviceAction(It.IsAny<IDeviceAction>(), It.IsAny<object>()),
                Times.Never);
        }

        [Test]
        public async Task ExecuteDeviceAction_ForSetCommunicationAction_WithChangedParameters_ReconnectsWithNewSettings()
        {
            // Arrange
            var setCommunicationAction = new SetCommunicationAction();
            byte newAddress = 2;
            uint newBaudRate = 19200;
            string portName = "COM1";
            
            var parameter = new CommunicationParameters(portName, newBaudRate, newAddress);
            
            // Mock the ExecuteDeviceAction to return the same parameters
            _deviceManagementServiceMock.Setup(x => x.ExecuteDeviceAction(setCommunicationAction, parameter))
                .ReturnsAsync(parameter);
            
            // Set the selected device action and parameter
            _viewModel.SelectedDeviceAction = setCommunicationAction;
            _viewModel.DeviceActionParameter = parameter;
            
            // Act
            await _viewModel.ExecuteDeviceActionCommand.ExecuteAsync(null);
            
            // Assert
            _dialogServiceMock.Verify(
                x => x.ShowMessageDialog(
                    "Update Communications", 
                    "Successfully update communications, reconnecting with new settings.", 
                    MessageIcon.Information),
                Times.Once);
            
            _deviceManagementServiceMock.Verify(
                x => x.Reconnect(
                    It.IsAny<SerialPortOsdpConnection>(),
                    newAddress),
                Times.Once);
        }

        [Test]
        public async Task ExecuteDeviceAction_ForSetCommunicationAction_WithUnchangedParameters_ShowsWarningAndDoesNotReconnect()
        {
            // Arrange
            var setCommunicationAction = new SetCommunicationAction();
            byte address = 1; // Same as in setup
            uint baudRate = 9600; // Same as in setup
            string portName = "COM1";
            
            var parameter = new CommunicationParameters(portName, baudRate, address);
            
            // Mock the ExecuteDeviceAction to return the same parameters
            _deviceManagementServiceMock.Setup(x => x.ExecuteDeviceAction(setCommunicationAction, parameter))
                .ReturnsAsync(parameter);
            
            // Set the selected device action and parameter
            _viewModel.SelectedDeviceAction = setCommunicationAction;
            _viewModel.DeviceActionParameter = parameter;
            
            // Act
            await _viewModel.ExecuteDeviceActionCommand.ExecuteAsync(null);
            
            // Assert
            _dialogServiceMock.Verify(
                x => x.ShowMessageDialog(
                    "Update Communications", 
                    "Communication parameters didn't change.", 
                    MessageIcon.Warning),
                Times.Once);
                
            _deviceManagementServiceMock.Verify(x => x.Shutdown(), Times.Never);
            _deviceManagementServiceMock.Verify(
                x => x.Connect(
                    It.IsAny<SerialPortOsdpConnection>(),
                    It.IsAny<byte>(),
                    It.IsAny<bool>(),
                    It.IsAny<bool>(),
                    It.IsAny<byte[]>()),
                Times.Never);
        }

        [Test]
        public async Task ExecuteDeviceAction_ForResetCypressDevice_WithoutCanSendResetCommand_ShowsInstructions()
        {
            // Arrange
            var resetCypressDeviceAction = new ResetCypressDeviceAction();
            string testResetInstructions = "Test reset instructions";
            
            // Set up an IdentityLookup with CanSendResetCommand = false
            SetupMockIdentityLookup(false, testResetInstructions);
            
            // Make sure the _viewModel property for IdentityLookup is updated
            _viewModel.IdentityLookup = CreateTestIdentityLookup(false, testResetInstructions);
            
            // Set the selected device action
            _viewModel.SelectedDeviceAction = resetCypressDeviceAction;
            
            // Act
            await _viewModel.ExecuteDeviceActionCommand.ExecuteAsync(null);
            
            // Assert
            _dialogServiceMock.Verify(
                x => x.ShowMessageDialog(
                    "Reset Device", 
                    testResetInstructions, 
                    MessageIcon.Information),
                Times.Once);
                
            // Should not try to execute the action
            _deviceManagementServiceMock.Verify(
                x => x.ExecuteDeviceAction(resetCypressDeviceAction, It.IsAny<object>()),
                Times.Never);
        }

        [Test]
        public async Task ExecuteDeviceAction_ForResetCypressDevice_WithCanSendResetCommand_UserCancels_ReconnectsDevice()
        {
            // Arrange
            var resetCypressDeviceAction = new ResetCypressDeviceAction();
            string testResetInstructions = "Test reset instructions";
            
            // Set up an IdentityLookup with CanSendResetCommand = true
            SetupMockIdentityLookup(true, testResetInstructions);
            
            // Make sure the _viewModel property for IdentityLookup is updated
            _viewModel.IdentityLookup = CreateTestIdentityLookup(true, testResetInstructions);
            
            // Configure dialog service to return false (user cancels)
            _dialogServiceMock.Setup(x => x.ShowConfirmationDialog(
                    "Reset Device",
                    "Do you want to reset device, if so power cycle then click yes when the device boots up.",
                    MessageIcon.Warning))
                .ReturnsAsync(false);
            
            // Set the selected device action
            _viewModel.SelectedDeviceAction = resetCypressDeviceAction;
            
            // Act
            await _viewModel.ExecuteDeviceActionCommand.ExecuteAsync(null);
            
            // Assert
            _deviceManagementServiceMock.Verify(x => x.Shutdown(), Times.Once);
            
            _deviceManagementServiceMock.Verify(
                x => x.Reconnect(
                    It.IsAny<SerialPortOsdpConnection>(),
                    _deviceManagementServiceMock.Object.Address),
                Times.Once);
                
            _deviceManagementServiceMock.Verify(
                x => x.ExecuteDeviceAction(resetCypressDeviceAction, It.IsAny<object>()),
                Times.Never);
        }

        [Test]
        public async Task ExecuteDeviceAction_ForResetCypressDevice_WithCanSendResetCommand_UserConfirms_ExecutesAction()
        {
            // Arrange
            var resetCypressDeviceAction = new ResetCypressDeviceAction();
            string testResetInstructions = "Test reset instructions";
            
            // Set up an IdentityLookup with CanSendResetCommand = true
            SetupMockIdentityLookup(true, testResetInstructions);
            
            // Make sure the _viewModel property for IdentityLookup is updated
            _viewModel.IdentityLookup = CreateTestIdentityLookup(true, testResetInstructions);
            
            // Configure dialog service to return true (user confirms)
            _dialogServiceMock.Setup(x => x.ShowConfirmationDialog(
                    "Reset Device",
                    "Do you want to reset device, if so power cycle then click yes when the device boots up.",
                    MessageIcon.Warning))
                .ReturnsAsync(true);
            
            // Set the selected device action
            _viewModel.SelectedDeviceAction = resetCypressDeviceAction;
            
            // Act
            await _viewModel.ExecuteDeviceActionCommand.ExecuteAsync(null);
            
            // Assert
            _deviceManagementServiceMock.Verify(x => x.Shutdown(), Times.Once);
            
            _deviceManagementServiceMock.Verify(
                x => x.ExecuteDeviceAction(
                    resetCypressDeviceAction, 
                    It.IsAny<SerialPortOsdpConnection>()),
                Times.Once);
                
            _dialogServiceMock.Verify(
                x => x.ShowMessageDialog(
                    "Reset Device", 
                    "Successfully sent reset commands. Power cycle device again and then perform a discovery.",
                    MessageIcon.Information),
                Times.Once);
        }

        [Test]
        public async Task ExecuteDeviceAction_ForResetCypressDevice_ExecuteDeviceActionThrowsException_ShowsErrorDialog()
        {
            // Arrange
            var resetCypressDeviceAction = new ResetCypressDeviceAction();
            var expectedException = new Exception("Test exception");
            string testResetInstructions = "Test reset instructions";
            
            // Set up an IdentityLookup with CanSendResetCommand = true
            SetupMockIdentityLookup(true, testResetInstructions);
            
            // Make sure the _viewModel property for IdentityLookup is updated
            _viewModel.IdentityLookup = CreateTestIdentityLookup(true, testResetInstructions);
            
            // Configure a dialog service to return true (user confirms)
            _dialogServiceMock.Setup(x => x.ShowConfirmationDialog(
                    "Reset Device",
                    "Do you want to reset device, if so power cycle then click yes when the device boots up.",
                    MessageIcon.Warning))
                .ReturnsAsync(true);
                
            // Configure ExecuteDeviceAction to throw an exception
            _deviceManagementServiceMock.Setup(x => x.ExecuteDeviceAction(
                    resetCypressDeviceAction, 
                    It.IsAny<SerialPortOsdpConnection>()))
                .ThrowsAsync(expectedException);
            
            // Set the selected device action
            _viewModel.SelectedDeviceAction = resetCypressDeviceAction;
            
            // Act
            await _viewModel.ExecuteDeviceActionCommand.ExecuteAsync(null);
            
            // Assert
            _deviceManagementServiceMock.Verify(x => x.Shutdown(), Times.Once);
            
            _deviceManagementServiceMock.Verify(
                x => x.ExecuteDeviceAction(
                    resetCypressDeviceAction, 
                    It.IsAny<SerialPortOsdpConnection>()),
                Times.Once);
                
            _dialogServiceMock.Verify(
                x => x.ShowExceptionDialog(
                    "Reset Device", 
                    It.Is<Exception>(e => e.Message.Contains(expectedException.Message))),
                Times.Once);
                
            _dialogServiceMock.Verify(
                x => x.ShowMessageDialog(
                    "Reset Device",
                    "Failed to reset the device. Perform a discovery to reconnect to the device.",
                    MessageIcon.Error),
                Times.Once);
        }

        #endregion

        #region Event Handler Tests
        
        [Test]
        public void DeviceManagementServiceOnConnectionStatusChange_Connected_SetsStatusLevel()
        {
            // Act
            _deviceManagementServiceMock.Raise(
                d => d.ConnectionStatusChange += null!, 
                EventArgs.Empty, 
                ConnectionStatus.Connected);
                
            // Assert
            Assert.That(_viewModel.StatusLevel, Is.EqualTo(StatusLevel.Connected));
        }
        
        [Test]
        public void DeviceManagementServiceOnConnectionStatusChange_Disconnected_SetsStatusLevel()
        {
            // Act
            _deviceManagementServiceMock.Raise(
                d => d.ConnectionStatusChange += null!, 
                EventArgs.Empty, 
                ConnectionStatus.Disconnected);
                
            // Assert
            Assert.That(_viewModel.StatusLevel, Is.EqualTo(StatusLevel.Disconnected));
        }
        
        [Test]
        public void DeviceManagementServiceOnConnectionStatusChange_InvalidSecurityKey_SetsStatusLevel()
        {
            // Act
            _deviceManagementServiceMock.Raise(
                d => d.ConnectionStatusChange += null!, 
                EventArgs.Empty, 
                ConnectionStatus.InvalidSecurityKey);
                
            // Assert
            Assert.That(_viewModel.StatusLevel, Is.EqualTo(StatusLevel.Error));
        }
        
        [Test]
        public void DeviceManagementServiceOnCardReadReceived_UpdatesLastCardNumberRead()
        {
            // Arrange
            string expectedCardNumber = "1234567890";
            
            // Act
            _deviceManagementServiceMock.Raise(
                d => d.CardReadReceived += null!,
                EventArgs.Empty,
                expectedCardNumber);
                
            // Assert
            Assert.That(_viewModel.LastCardNumberRead, Is.EqualTo(expectedCardNumber));
            Assert.That(_viewModel.CardReadEntries, Has.Count.EqualTo(1));
            Assert.That(_viewModel.CardReadEntries[0].CardNumber, Is.EqualTo(expectedCardNumber));
        }
        
        [Test]
        public void DeviceManagementServiceOnCardReadReceived_LimitsCardReadEntries()
        {
            // Arrange - Add 6 card reads
            for (int i = 0; i < 6; i++)
            {
                _deviceManagementServiceMock.Raise(
                    d => d.CardReadReceived += null!,
                    EventArgs.Empty,
                    $"Card{i}");
            }
            
            // Assert
            Assert.That(_viewModel.CardReadEntries, Has.Count.EqualTo(5)); // Max 5 entries
            Assert.That(_viewModel.CardReadEntries[0].CardNumber, Is.EqualTo("Card5")); // Most recent at the beginning
            Assert.That(_viewModel.CardReadEntries[4].CardNumber, Is.EqualTo("Card1")); // Oldest at the end
        }
        
        [Test]
        public void DeviceManagementServiceOnKeypadReadReceived_AppendsToKeypadReadData()
        {
            // Arrange
            string initialKeypadData = "123";
            string newKeypadData = "456";
            
            // Set initial keypad data
            _deviceManagementServiceMock.Raise(
                d => d.KeypadReadReceived += null!,
                EventArgs.Empty,
                initialKeypadData);
                
            // Act - Add more keypad data
            _deviceManagementServiceMock.Raise(
                d => d.KeypadReadReceived += null!,
                EventArgs.Empty,
                newKeypadData);
                
            // Assert
            Assert.That(_viewModel.KeypadReadData, Is.EqualTo(initialKeypadData + newKeypadData));
        }
        
        [Test]
        public void DeviceManagementServiceOnDeviceLookupsChanged_UpdatesFields()
        {
            // Arrange
            const bool canSendResetCommand = true;
            const string testResetInstructions = "Test instructions";
            
            SetupMockIdentityLookup(canSendResetCommand, testResetInstructions);
            
            // Act
            _deviceManagementServiceMock.Raise(
                d => d.DeviceLookupsChanged += null!,
                EventArgs.Empty);
                
            // Assert
            Assert.That(_viewModel.IdentityLookup, Is.Not.Null);
            Assert.That(_viewModel.IdentityLookup.CanSendResetCommand, Is.EqualTo(canSendResetCommand));
            Assert.That(_viewModel.IdentityLookup.ResetInstructions, Is.EqualTo(testResetInstructions));
        }
        
        #endregion
        
        #region Test Helpers
        
        /// <summary>
        /// Creates a simplified IdentityLookup for testing
        /// </summary>
        private IdentityLookup CreateTestIdentityLookup(bool canSendResetCommand, string resetInstructions)
        {
            // Create and return our completely custom IdentityLookup implementation
            return new StubIdentityLookup(canSendResetCommand, resetInstructions);
        }
        
        /// <summary>
        /// A stub implementation of IdentityLookup that builds DeviceIdentification using the static factory method
        /// </summary>
        private class StubIdentityLookup : IdentityLookup
        {
            private readonly bool _canSendResetCommand;
            private readonly string _resetInstructions;
            
            // Constructor that uses the static factory method to create DeviceIdentification
            public StubIdentityLookup(bool canSendResetCommand, string resetInstructions) 
                : base(CreateDeviceId())
            {
                _canSendResetCommand = canSendResetCommand;
                _resetInstructions = resetInstructions;
            }
            
            // Override the properties we need to control for testing
            public override bool CanSendResetCommand => _canSendResetCommand;
            public override string ResetInstructions => _resetInstructions;
            
            // Helper method to create a DeviceIdentification using the ParseData static factory method
            private static DeviceIdentification CreateDeviceId()
            {
                // Create with minimal data needed for tests - using the ParseData method
                byte[] testData = new byte[] {
                    0xCA, 0x44, 0x6C, // Vendor code (Cypress)
                    1, // Model number 
                    1, // Version
                    0, 0, 0, 1, // Serial number (1)
                    1, 0, 0 // Firmware version 1.0.0
                };
                
                return DeviceIdentification.ParseData(testData);
            }
        }
        
        /// <summary>
        /// Configures the mock DeviceManagementService with an IdentityLookup
        /// </summary>
        private void SetupMockIdentityLookup(bool canSendResetCommand, string resetInstructions)
        {
            // Create a proper IdentityLookup for testing with our desired property values
            var identityLookup = CreateTestIdentityLookup(canSendResetCommand, resetInstructions);
            
            // Set up the mock to return our real IdentityLookup instance
            _deviceManagementServiceMock.Setup(x => x.IdentityLookup).Returns(identityLookup);
        }
        #endregion
    }
}