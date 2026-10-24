using System.Linq;
using System.Threading.Tasks;
using Moq;
using NUnit.Framework;
using OSDP.Net.Tracing;
using OSDPBench.Core.Services;
using OSDPBench.Core.Services.Analysis;
using OSDPBench.Core.ViewModels.Pages;
using static OSDPBench.Core.Tests.Helpers.TraceEntryTestHelper;

namespace OSDPBench.Core.Tests.ViewModels;

[TestFixture(TestOf = typeof(MonitorViewModel))]
public class MonitorViewModelCaptureAnalysisTests
{
    private Mock<IDeviceManagementService> _deviceManagementServiceMock;
    private Mock<IDialogService> _dialogServiceMock;
    private Mock<ICaptureAnalysisLauncher> _launcherMock;

    [SetUp]
    public void Setup()
    {
        _deviceManagementServiceMock = new Mock<IDeviceManagementService>();
        _deviceManagementServiceMock.Setup(x => x.IsUsingSecureChannel).Returns(false);
        _dialogServiceMock = new Mock<IDialogService>();
        _launcherMock = new Mock<ICaptureAnalysisLauncher>();
    }

    [Test]
    public void WithoutAnalysisService_AnalysisIsUnavailable()
    {
        var viewModel = new MonitorViewModel(_deviceManagementServiceMock.Object, _dialogServiceMock.Object);
        RaiseTraceEntry(_deviceManagementServiceMock, TraceDirection.Output, ValidPollPacket);

        Assert.That(viewModel.IsCaptureAnalysisAvailable, Is.False);
        Assert.That(viewModel.AnalyzeCaptureCommand.CanExecute(null), Is.False);
        Assert.That(viewModel.ExportTraceCommand.CanExecute(null), Is.True);
    }

    [Test]
    public void WithAnalysisService_CanAnalyzeOnceThereIsData()
    {
        var viewModel = CreateViewModel();
        Assert.That(viewModel.IsCaptureAnalysisAvailable, Is.True);
        Assert.That(viewModel.AnalyzeCaptureCommand.CanExecute(null), Is.False);

        RaiseTraceEntry(_deviceManagementServiceMock, TraceDirection.Output, ValidPollPacket);

        Assert.That(viewModel.AnalyzeCaptureCommand.CanExecute(null), Is.True);
    }

    [Test]
    public void ReceivingFirstEntry_NotifiesCanExport()
    {
        var viewModel = CreateViewModel();
        var notified = false;
        viewModel.PropertyChanged += (_, e) => notified |= e.PropertyName == nameof(MonitorViewModel.CanExport);

        RaiseTraceEntry(_deviceManagementServiceMock, TraceDirection.Output, ValidPollPacket);

        Assert.That(notified, Is.True);
    }

    [Test]
    public async Task AnalyzeCapture_OpensLauncherWithOsdpCapture()
    {
        var viewModel = CreateViewModel();
        RaiseTraceEntry(_deviceManagementServiceMock, TraceDirection.Output, ValidPollPacket);
        RaiseTraceEntry(_deviceManagementServiceMock, TraceDirection.Input, ValidAckPacket);

        await viewModel.AnalyzeCaptureCommand.ExecuteAsync(null);

        _launcherMock.Verify(launcher => launcher.OpenAsync(It.Is<CaptureForAnalysis>(capture =>
            capture.FileName.EndsWith(".osdpcap") &&
            capture.Capture.Length > 0 &&
            capture.SecureChannelKey == null)), Times.Once);
    }

    [Test]
    public async Task AnalyzeCapture_WithCustomKey_OffersKey()
    {
        var key = Enumerable.Repeat((byte)0xAB, 16).ToArray();
        _deviceManagementServiceMock.Setup(x => x.IsUsingSecureChannel).Returns(true);
        _deviceManagementServiceMock.Setup(x => x.UsesDefaultSecurityKey).Returns(false);
        _deviceManagementServiceMock.Setup(x => x.SecurityKey).Returns(key);
        _deviceManagementServiceMock.Setup(x => x.IsConnected).Returns(true);
        var viewModel = CreateViewModel();
        RaiseTraceEntry(_deviceManagementServiceMock, TraceDirection.Output, ValidPollPacket);

        await viewModel.AnalyzeCaptureCommand.ExecuteAsync(null);

        _launcherMock.Verify(launcher => launcher.OpenAsync(It.Is<CaptureForAnalysis>(capture =>
            capture.SecureChannelKey == key)), Times.Once);
    }

    private MonitorViewModel CreateViewModel() =>
        new(_deviceManagementServiceMock.Object, _dialogServiceMock.Object, _launcherMock.Object);
}
