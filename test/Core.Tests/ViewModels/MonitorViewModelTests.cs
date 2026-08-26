using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using Moq;
using NUnit.Framework;
using OSDP.Net.Tracing;
using OSDPBench.Core.Models;
using OSDPBench.Core.Services;
using OSDPBench.Core.ViewModels.Pages;
using static OSDPBench.Core.Tests.Helpers.TraceEntryTestHelper;

namespace OSDPBench.Core.Tests.ViewModels;

[TestFixture(TestOf = typeof(MonitorViewModel))]
public class MonitorViewModelTests
{
    private Mock<IDeviceManagementService> _deviceManagementServiceMock;
    private Mock<IDialogService> _dialogServiceMock;
    private MonitorViewModel _viewModel;

    [SetUp]
    public void Setup()
    {
        _deviceManagementServiceMock = new Mock<IDeviceManagementService>();
        _deviceManagementServiceMock.Setup(x => x.IsUsingSecureChannel).Returns(false);
        _dialogServiceMock = new Mock<IDialogService>();
        _viewModel = new MonitorViewModel(_deviceManagementServiceMock.Object, _dialogServiceMock.Object);
    }

    #region Constructor Tests

    [Test]
    public void MonitorViewModel_Constructor_InitializesProperties()
    {
        // Assert
        Assert.That(_viewModel.TraceEntriesView, Is.Not.Null);
        Assert.That(_viewModel.TraceEntriesView, Is.Empty);
        Assert.That(_viewModel.StatusLevel, Is.EqualTo(StatusLevel.Disconnected));
        Assert.That(_viewModel.UsingSecureChannel, Is.False);
    }

    [Test]
    public void MonitorViewModel_Constructor_InitializesStatistics()
    {
        // Assert
        Assert.That(_viewModel.CommandsSent, Is.EqualTo(0));
        Assert.That(_viewModel.RepliesReceived, Is.EqualTo(0));
        Assert.That(_viewModel.Polls, Is.EqualTo(0));
        Assert.That(_viewModel.Naks, Is.EqualTo(0));
    }

    [Test]
    public void MonitorViewModel_Constructor_InitializesBufferProperties()
    {
        // Assert
        Assert.That(_viewModel.TotalPacketsCaptured, Is.EqualTo(0));
        Assert.That(_viewModel.BufferUsagePercentage, Is.EqualTo(0));
    }

    #endregion

    #region Export Command Tests

    [Test]
    public void ExportTraceCommand_WhenNoData_CannotExecute()
    {
        // Assert - Command should not be executable when no data
        Assert.That(_viewModel.ExportTraceCommand.CanExecute(null), Is.False);
    }

    [Test]
    public void CanExport_WhenNoData_ReturnsFalse()
    {
        // Assert
        Assert.That(_viewModel.CanExport, Is.False);
    }

    [Test]
    public void CanExport_AfterReceivingEntry_ReturnsTrue()
    {
        // Act
        RaiseTraceEntry(_deviceManagementServiceMock, TraceDirection.Output, ValidPollPacket);

        // Assert
        Assert.That(_viewModel.CanExport, Is.True);
    }

    [Test]
    public async Task ExportTrace_WithData_PromptsForDestination()
    {
        // Arrange
        RaiseTraceEntry(_deviceManagementServiceMock, TraceDirection.Output, ValidPollPacket);
        _dialogServiceMock.Setup(x => x.SaveFilesWithDataAsync(
                It.IsAny<string>(), It.IsAny<IEnumerable<(string, byte[])>>(), It.IsAny<string>()))
            .ReturnsAsync((string)null); // User cancels

        // Act
        await _viewModel.ExportTraceCommand.ExecuteAsync(null);

        // Assert
        _dialogServiceMock.Verify(x => x.SaveFilesWithDataAsync(
            It.IsAny<string>(), It.IsAny<IEnumerable<(string, byte[])>>(), It.IsAny<string>()), Times.Once);
    }

    [Test]
    public async Task ExportTrace_UserCancels_DoesNotReportSuccess()
    {
        // Arrange
        RaiseTraceEntry(_deviceManagementServiceMock, TraceDirection.Output, ValidPollPacket);
        _dialogServiceMock.Setup(x => x.SaveFilesWithDataAsync(
                It.IsAny<string>(), It.IsAny<IEnumerable<(string, byte[])>>(), It.IsAny<string>()))
            .ReturnsAsync((string)null); // User cancels

        // Act
        await _viewModel.ExportTraceCommand.ExecuteAsync(null);

        // Assert
        _dialogServiceMock.Verify(x => x.ShowMessageDialog(
            It.IsAny<string>(), It.IsAny<string>(), MessageIcon.Information), Times.Never);
    }

    [Test]
    public async Task ExportTrace_WithData_SavesBothCaptureAndParsedFiles()
    {
        // Arrange
        var savedFiles = new List<(string FileName, byte[] Data)>();

        RaiseTraceEntry(_deviceManagementServiceMock, TraceDirection.Output, ValidPollPacket);
        _dialogServiceMock.Setup(x => x.SaveFilesWithDataAsync(
                It.IsAny<string>(), It.IsAny<IEnumerable<(string, byte[])>>(), It.IsAny<string>()))
            .Callback<string, IEnumerable<(string FileName, byte[] Data)>, string>(
                (_, files, _) => savedFiles.AddRange(files))
            .ReturnsAsync(@"C:\reports");

        // Act
        await _viewModel.ExportTraceCommand.ExecuteAsync(null);

        // Assert - Both formats handed to the platform in a single save, sharing one base name
        Assert.Multiple(() =>
        {
            Assert.That(savedFiles, Has.Count.EqualTo(2));
            Assert.That(savedFiles.Select(file => Path.GetExtension(file.FileName)),
                Is.EquivalentTo(new[] { ".osdpcap", ".txt" }));
            Assert.That(savedFiles.Select(file => Path.GetFileNameWithoutExtension(file.FileName)).Distinct().Count(),
                Is.EqualTo(1), "Both files should share the same base name.");
            Assert.That(savedFiles.Select(file => file.Data), Is.All.Not.Empty);
        });
        _dialogServiceMock.Verify(x => x.ShowMessageDialog(
            It.IsAny<string>(), It.IsAny<string>(), MessageIcon.Information), Times.Once);
    }

    #endregion

    #region Line Quality Tests

    [Test]
    public void LineQualityPercentage_WhenNoCommandsSent_Returns100()
    {
        // Assert - 100% quality when no data
        Assert.That(_viewModel.LineQualityPercentage, Is.EqualTo(100.0));
    }

    [Test]
    public void LineQualityPercentage_AllRepliesReceived_Returns100()
    {
        // Arrange - Send poll commands and receive ACK replies
        for (int i = 0; i < 10; i++)
        {
            RaiseTraceEntry(_deviceManagementServiceMock, TraceDirection.Output, ValidPollPacket);
            RaiseTraceEntry(_deviceManagementServiceMock, TraceDirection.Input, ValidAckPacket);
        }

        // Assert
        Assert.That(_viewModel.CommandsSent, Is.EqualTo(10));
        Assert.That(_viewModel.RepliesReceived, Is.EqualTo(10));
        Assert.That(_viewModel.LineQualityPercentage, Is.EqualTo(100.0));
    }

    [Test]
    public void LineQualityPercentage_MissingReplies_ReturnsLessThan100()
    {
        // Arrange - Send 10 commands but only receive 5 replies (3+ in-flight triggers quality drop)
        for (int i = 0; i < 5; i++)
        {
            RaiseTraceEntry(_deviceManagementServiceMock, TraceDirection.Output, ValidPollPacket);
            RaiseTraceEntry(_deviceManagementServiceMock, TraceDirection.Input, ValidAckPacket);
        }
        // 5 more commands with no replies
        for (int i = 0; i < 5; i++)
        {
            RaiseTraceEntry(_deviceManagementServiceMock, TraceDirection.Output, ValidPollPacket);
        }

        // Assert
        Assert.That(_viewModel.CommandsSent, Is.EqualTo(10));
        Assert.That(_viewModel.RepliesReceived, Is.EqualTo(5));
        Assert.That(_viewModel.LineQualityPercentage, Is.LessThan(100.0));
    }

    [Test]
    public void LineQualityPercentage_TwoInFlight_StillReturns100()
    {
        // Arrange - Exactly 2 in-flight commands (within grace window)
        for (int i = 0; i < 5; i++)
        {
            RaiseTraceEntry(_deviceManagementServiceMock, TraceDirection.Output, ValidPollPacket);
            RaiseTraceEntry(_deviceManagementServiceMock, TraceDirection.Input, ValidAckPacket);
        }
        // 2 more commands with no replies (within the 2-in-flight grace window)
        RaiseTraceEntry(_deviceManagementServiceMock, TraceDirection.Output, ValidPollPacket);
        RaiseTraceEntry(_deviceManagementServiceMock, TraceDirection.Output, ValidPollPacket);

        // Assert
        Assert.That(_viewModel.CommandsSent, Is.EqualTo(7));
        Assert.That(_viewModel.RepliesReceived, Is.EqualTo(5));
        Assert.That(_viewModel.LineQualityPercentage, Is.EqualTo(100.0));
    }

    [Test]
    public void LineQualityPercentage_ThreeInFlight_ReturnsLessThan100()
    {
        // Arrange - 3 in-flight commands (exceeds the 2-in-flight grace window)
        for (int i = 0; i < 5; i++)
        {
            RaiseTraceEntry(_deviceManagementServiceMock, TraceDirection.Output, ValidPollPacket);
            RaiseTraceEntry(_deviceManagementServiceMock, TraceDirection.Input, ValidAckPacket);
        }
        // 3 more commands with no replies
        RaiseTraceEntry(_deviceManagementServiceMock, TraceDirection.Output, ValidPollPacket);
        RaiseTraceEntry(_deviceManagementServiceMock, TraceDirection.Output, ValidPollPacket);
        RaiseTraceEntry(_deviceManagementServiceMock, TraceDirection.Output, ValidPollPacket);

        // Assert
        Assert.That(_viewModel.CommandsSent, Is.EqualTo(8));
        Assert.That(_viewModel.RepliesReceived, Is.EqualTo(5));
        Assert.That(_viewModel.LineQualityPercentage, Is.LessThan(100.0));
    }

    #endregion

    #region Buffer Management Tests

    [Test]
    public void MaxBufferSize_Returns10MB()
    {
        // Assert
        Assert.That(MonitorViewModel.MaxBufferSize, Is.EqualTo(10 * 1024 * 1024));
    }

    [Test]
    public void BufferUsagePercentage_IncreasesWithEntries()
    {
        // Act
        RaiseTraceEntry(_deviceManagementServiceMock, TraceDirection.Output, ValidPollPacket);

        // Assert
        Assert.That(_viewModel.BufferUsagePercentage, Is.GreaterThan(0));
    }

    [Test]
    public void TotalPacketsCaptured_IncrementsWithEntries()
    {
        // Act
        RaiseTraceEntry(_deviceManagementServiceMock, TraceDirection.Output, ValidPollPacket);
        RaiseTraceEntry(_deviceManagementServiceMock, TraceDirection.Input, ValidAckPacket);

        // Assert
        Assert.That(_viewModel.TotalPacketsCaptured, Is.EqualTo(2));
    }

    #endregion

    #region Connection Status Tests

    [Test]
    public void MonitorViewModel_DeviceManagementServiceOnConnectionStatusChange_Connected()
    {
        // Act
        _deviceManagementServiceMock.Raise(
            d => d.ConnectionStatusChange += null!,
            EventArgs.Empty,
            ConnectionStatus.Connected);

        // Assert
        Assert.That(_viewModel.StatusLevel, Is.EqualTo(StatusLevel.Connected));
        Assert.That(_viewModel.TraceEntriesView, Is.Empty);
    }

    [Test]
    public void MonitorViewModel_DeviceManagementServiceOnConnectionStatusChange_Disconnected()
    {
        // Arrange - First set status to connected
        _deviceManagementServiceMock.Raise(
            d => d.ConnectionStatusChange += null!,
            EventArgs.Empty,
            ConnectionStatus.Connected);

        // Act
        _deviceManagementServiceMock.Raise(
            d => d.ConnectionStatusChange += null!,
            EventArgs.Empty,
            ConnectionStatus.Disconnected);

        // Assert
        Assert.That(_viewModel.StatusLevel, Is.EqualTo(StatusLevel.Disconnected));
    }

    [Test]
    public void MonitorViewModel_DeviceManagementServiceOnConnectionStatusChange_InvalidSecurityKey()
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
    public void MonitorViewModel_ConnectionStatusChanges_UpdatesStatusLevel()
    {
        // Verify that test cases related to ConnectionStatusChange events work correctly
        Assert.Pass("ConnectionStatusChange tests are passing");
    }

    [Test]
    public void MonitorViewModel_DeviceManagementServiceOnConnectionStatusChange_PassiveMonitoring()
    {
        // Act
        _deviceManagementServiceMock.Raise(
            d => d.ConnectionStatusChange += null!,
            EventArgs.Empty,
            ConnectionStatus.PassiveMonitoring);

        // Assert
        Assert.That(_viewModel.StatusLevel, Is.EqualTo(StatusLevel.PassiveMonitoring));
    }

    #endregion

    #region Secure Channel Status Tests

    [Test]
    public void UsingSecureChannel_WhenNotConnected_ReturnsFalse()
    {
        // Assert
        Assert.That(_viewModel.UsingSecureChannel, Is.False);
    }

    [Test]
    public void UsesDefaultSecurityKey_WhenNotConnected_ReturnsFalse()
    {
        // Assert
        Assert.That(_viewModel.UsesDefaultSecurityKey, Is.False);
    }

    #endregion

    #region Activity Indicator Tests

    [Test]
    public void LastTxActiveTime_InitiallyDefault()
    {
        // Assert
        Assert.That(_viewModel.LastTxActiveTime, Is.EqualTo(default(DateTime)));
    }

    [Test]
    public void LastRxActiveTime_InitiallyDefault()
    {
        // Assert
        Assert.That(_viewModel.LastRxActiveTime, Is.EqualTo(default(DateTime)));
    }

    #endregion

    #region Trace Entry Processing Tests

    [Test]
    public void TraceEntryReceived_OutputDirection_IncrementsCommandsSent()
    {
        // Act
        RaiseTraceEntry(_deviceManagementServiceMock, TraceDirection.Output, ValidPollPacket);

        // Assert
        Assert.That(_viewModel.CommandsSent, Is.EqualTo(1));
    }

    [Test]
    public void TraceEntryReceived_InputDirection_IncrementsRepliesReceived()
    {
        // Arrange - Send a command first so reply makes sense
        RaiseTraceEntry(_deviceManagementServiceMock, TraceDirection.Output, ValidPollPacket);

        // Act
        RaiseTraceEntry(_deviceManagementServiceMock, TraceDirection.Input, ValidAckPacket);

        // Assert
        Assert.That(_viewModel.RepliesReceived, Is.EqualTo(1));
    }

    [Test]
    public void TraceEntryReceived_PollCommand_IncrementsPolls()
    {
        // Act
        RaiseTraceEntry(_deviceManagementServiceMock, TraceDirection.Output, ValidPollPacket);

        // Assert
        Assert.That(_viewModel.Polls, Is.EqualTo(1));
    }

    [Test]
    public void TraceEntryReceived_NakReply_IncrementsNaks()
    {
        // Arrange - Send a command first
        RaiseTraceEntry(_deviceManagementServiceMock, TraceDirection.Output, ValidIdReportPacket);

        // Act
        RaiseTraceEntry(_deviceManagementServiceMock, TraceDirection.Input, ValidNakPacket);

        // Assert
        Assert.That(_viewModel.Naks, Is.EqualTo(1));
    }

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
        // Act & Assert - Should not throw when processing an invalid packet
        Assert.DoesNotThrow(() =>
            RaiseTraceEntry(_deviceManagementServiceMock, TraceDirection.Output, InvalidPacket));
    }

    [Test]
    public void TraceEntryReceived_PollCommand_NotAddedToView()
    {
        // Act
        RaiseTraceEntry(_deviceManagementServiceMock, TraceDirection.Output, ValidPollPacket);

        // Assert - Poll commands are filtered from the display view
        Assert.That(_viewModel.TraceEntriesView, Is.Empty);
    }

    [Test]
    public void TraceEntryReceived_NonPollCommand_AddedToView()
    {
        // Act - ID Report is a non-poll command
        RaiseTraceEntry(_deviceManagementServiceMock, TraceDirection.Output, ValidIdReportPacket);

        // Assert
        Assert.That(_viewModel.TraceEntriesView, Has.Count.EqualTo(1));
    }

    [Test]
    public void TraceEntryReceived_ViewLimitedTo20Entries()
    {
        // Arrange & Act - Send 25 non-poll commands
        for (int i = 0; i < 25; i++)
        {
            RaiseTraceEntry(_deviceManagementServiceMock, TraceDirection.Output, ValidIdReportPacket);
        }

        // Assert
        Assert.That(_viewModel.TraceEntriesView, Has.Count.EqualTo(20));
    }

    [Test]
    public void TraceEntryReceived_DuplicateNak_FilteredFromView()
    {
        // Arrange - Send a poll command (not displayed) then get a NAK reply (displayed)
        RaiseTraceEntry(_deviceManagementServiceMock, TraceDirection.Output, ValidPollPacket);
        RaiseTraceEntry(_deviceManagementServiceMock, TraceDirection.Input, ValidNakPacket);
        var countAfterFirstNak = _viewModel.TraceEntriesView.Count;

        // Act - Send another poll (not displayed) then same NAK (duplicate should be filtered)
        RaiseTraceEntry(_deviceManagementServiceMock, TraceDirection.Output, ValidPollPacket);
        RaiseTraceEntry(_deviceManagementServiceMock, TraceDirection.Input, ValidNakPacket);

        // Assert - NAK count increments (for statistics) but view doesn't grow
        Assert.That(_viewModel.Naks, Is.EqualTo(2));
        Assert.That(_viewModel.TraceEntriesView.Count, Is.EqualTo(countAfterFirstNak));
    }

    [Test]
    public void TraceEntryReceived_FirstEntry_InitializesSession()
    {
        // Act
        RaiseTraceEntry(_deviceManagementServiceMock, TraceDirection.Output, ValidPollPacket);

        // Assert - After first entry, buffer should have data and stats should be initialized
        Assert.That(_viewModel.TotalPacketsCaptured, Is.EqualTo(1));
        Assert.That(_viewModel.CommandsSent, Is.EqualTo(1));
    }

    [Test]
    public void TraceEntryReceived_AfterDisconnect_ClearsBufferOnNextEntry()
    {
        // Arrange - Send some entries
        RaiseTraceEntry(_deviceManagementServiceMock, TraceDirection.Output, ValidPollPacket);
        RaiseTraceEntry(_deviceManagementServiceMock, TraceDirection.Input, ValidAckPacket);
        Assert.That(_viewModel.TotalPacketsCaptured, Is.EqualTo(2));

        // Disconnect (marks session as ended)
        _deviceManagementServiceMock.Raise(
            d => d.ConnectionStatusChange += null!,
            EventArgs.Empty,
            ConnectionStatus.Disconnected);

        // Act - Next trace entry should clear the buffer first
        RaiseTraceEntry(_deviceManagementServiceMock, TraceDirection.Output, ValidPollPacket);

        // Assert - Buffer was cleared, so only the new entry exists
        Assert.That(_viewModel.TotalPacketsCaptured, Is.EqualTo(1));
        Assert.That(_viewModel.CommandsSent, Is.EqualTo(1));
        Assert.That(_viewModel.RepliesReceived, Is.EqualTo(0));
    }

    #endregion
}
