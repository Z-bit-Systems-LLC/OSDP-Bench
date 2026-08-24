using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Moq;
using NUnit.Framework;
using OSDP.Net.LineQuality;
using OSDPBench.Core.Models;
using OSDPBench.Core.Services;
using OSDPBench.Core.ViewModels.Pages;

namespace OSDPBench.Core.Tests.ViewModels
{
    [TestFixture(TestOf = typeof(LineQualityViewModel))]
    public class LineQualityViewModelTests
    {
        private Mock<IDialogService> _dialogServiceMock;
        private Mock<ILineQualityService> _lineQualityServiceMock;
        private Mock<ISerialPortConnectionService> _serialPortConnectionServiceMock;
        private Mock<IDeviceManagementService> _deviceManagementServiceMock;

        [SetUp]
        public void Setup()
        {
            _dialogServiceMock = new Mock<IDialogService>();
            _lineQualityServiceMock = new Mock<ILineQualityService>();
            _serialPortConnectionServiceMock = new Mock<ISerialPortConnectionService>();
            _deviceManagementServiceMock = new Mock<IDeviceManagementService>();

            _serialPortConnectionServiceMock.Setup(service => service.FindAvailableSerialPorts())
                .ReturnsAsync([new AvailableSerialPort("COM3", "COM3", "COM3")]);
            _lineQualityServiceMock.Setup(service => service.IsSupported(It.IsAny<string>())).Returns(true);
        }

        private async Task<LineQualityViewModel> CreateViewModel()
        {
            var viewModel = new LineQualityViewModel(
                _dialogServiceMock.Object,
                _lineQualityServiceMock.Object,
                _serialPortConnectionServiceMock.Object,
                _deviceManagementServiceMock.Object);

            await viewModel.InitializationComplete;
            return viewModel;
        }

        [Test]
        public async Task Constructor_OffersEveryProfileAndTheSixOsdpBaudRates()
        {
            var viewModel = await CreateViewModel();

            Assert.Multiple(() =>
            {
                Assert.That(viewModel.AvailableProfiles.Select(profile => profile.Profile),
                    Is.EquivalentTo(Enum.GetValues<TestProfile>()));
                Assert.That(viewModel.SelectedProfile.Profile, Is.EqualTo(TestProfile.Screening));
                Assert.That(viewModel.BaudRateOptions.Select(option => option.BaudRate),
                    Is.EqualTo(new[] { 9600, 19200, 38400, 57600, 115200, 230400 }));
                Assert.That(viewModel.BaudRateOptions.All(option => option.IsSelected), Is.True);
                Assert.That(viewModel.Address, Is.EqualTo(LineQualityProtocol.TestAddress));
                Assert.That(viewModel.IsControllerMode, Is.True);
            });
        }

        [Test]
        public async Task Constructor_SelectsTheFirstAvailableSerialPort()
        {
            var viewModel = await CreateViewModel();

            Assert.That(viewModel.SelectedSerialPort?.Name, Is.EqualTo("COM3"));
        }

        [Test]
        public async Task CanStartTest_IsFalseWhenNoBaudRateIsSelected()
        {
            var viewModel = await CreateViewModel();

            foreach (var option in viewModel.BaudRateOptions)
            {
                option.IsSelected = false;
            }

            Assert.That(viewModel.CanStartTest, Is.False);
        }

        [Test]
        public async Task CanStartTest_IsFalseInResponderMode()
        {
            var viewModel = await CreateViewModel();

            viewModel.IsControllerMode = false;

            Assert.Multiple(() =>
            {
                Assert.That(viewModel.CanStartTest, Is.False);
                Assert.That(viewModel.CanStartResponder, Is.True);
                Assert.That(viewModel.IsResponderMode, Is.True);
            });
        }

        [Test]
        public async Task StartTest_SweepsOnlyTheSelectedBaudRatesAtTheSelectedProfile()
        {
            var viewModel = await CreateViewModel();
            LineQualityOptions captured = null;

            _lineQualityServiceMock
                .Setup(service => service.RunTestAsync("COM3", It.IsAny<LineQualityOptions>(),
                    It.IsAny<CancellationToken>()))
                .Callback<string, LineQualityOptions, CancellationToken>((_, options, _) => captured = options)
                .ReturnsAsync(() => throw new OperationCanceledException());

            viewModel.SelectedProfile = viewModel.AvailableProfiles
                .First(profile => profile.Profile == TestProfile.Qualification);
            viewModel.Address = 100;
            foreach (var option in viewModel.BaudRateOptions.Where(option => option.BaudRate > 19200))
            {
                option.IsSelected = false;
            }

            await viewModel.StartTestCommand.ExecuteAsync(null);

            Assert.That(captured, Is.Not.Null);
            Assert.Multiple(() =>
            {
                Assert.That(captured.Profile, Is.EqualTo(TestProfile.Qualification));
                Assert.That(captured.Address, Is.EqualTo(100));
                Assert.That(captured.BaudRates, Is.EqualTo(new[] { 9600, 19200 }));
            });
        }

        [Test]
        public async Task StartTest_WarnsAndStopsWhenThePlatformCannotRetuneThePort()
        {
            var viewModel = await CreateViewModel();
            _lineQualityServiceMock.Setup(service => service.IsSupported("COM3")).Returns(false);

            await viewModel.StartTestCommand.ExecuteAsync(null);

            _lineQualityServiceMock.Verify(
                service => service.RunTestAsync(It.IsAny<string>(), It.IsAny<LineQualityOptions>(),
                    It.IsAny<CancellationToken>()), Times.Never);
            _dialogServiceMock.Verify(
                dialog => dialog.ShowMessageDialog(It.IsAny<string>(), It.IsAny<string>(), MessageIcon.Error),
                Times.Once);
        }

        [Test]
        public async Task StartTest_DoesNotRunWhenTheUserDeclinesToDisconnect()
        {
            var viewModel = await CreateViewModel();
            _deviceManagementServiceMock.Setup(service => service.IsConnected).Returns(true);
            _dialogServiceMock.Setup(dialog => dialog.ShowConfirmationDialog(
                It.IsAny<string>(), It.IsAny<string>(), It.IsAny<MessageIcon>())).ReturnsAsync(false);

            await viewModel.StartTestCommand.ExecuteAsync(null);

            _deviceManagementServiceMock.Verify(service => service.Shutdown(), Times.Never);
            _lineQualityServiceMock.Verify(
                service => service.RunTestAsync(It.IsAny<string>(), It.IsAny<LineQualityOptions>(),
                    It.IsAny<CancellationToken>()), Times.Never);
        }

        [Test]
        public async Task StartTest_ShutsDownAConnectedDeviceOnceTheUserAgrees()
        {
            var viewModel = await CreateViewModel();
            _deviceManagementServiceMock.Setup(service => service.IsConnected).Returns(true);
            _dialogServiceMock.Setup(dialog => dialog.ShowConfirmationDialog(
                It.IsAny<string>(), It.IsAny<string>(), It.IsAny<MessageIcon>())).ReturnsAsync(true);
            _lineQualityServiceMock
                .Setup(service => service.RunTestAsync(It.IsAny<string>(), It.IsAny<LineQualityOptions>(),
                    It.IsAny<CancellationToken>()))
                .ReturnsAsync(() => throw new OperationCanceledException());

            await viewModel.StartTestCommand.ExecuteAsync(null);

            _deviceManagementServiceMock.Verify(service => service.Shutdown(), Times.Once);
        }

        [Test]
        public async Task StartTest_ReportsProgressAsAFractionOfTheWholeSweep()
        {
            var viewModel = await CreateViewModel();
            var progressReports = new List<double>();

            _lineQualityServiceMock
                .Setup(service => service.RunTestAsync(It.IsAny<string>(), It.IsAny<LineQualityOptions>(),
                    It.IsAny<CancellationToken>()))
                .Returns<string, LineQualityOptions, CancellationToken>((_, options, _) =>
                {
                    // The progress callback marshals through a SynchronizationContext, so the
                    // reports are read back after the run rather than as they arrive.
                    options.Progress.Report(CreateProgress(9600, 80, 160, 0, 4));
                    options.Progress.Report(CreateProgress(19200, 160, 160, 1, 4));
                    throw new OperationCanceledException();
                });

            viewModel.PropertyChanged += (_, args) =>
            {
                if (args.PropertyName == nameof(LineQualityViewModel.ProgressPercent))
                {
                    progressReports.Add(viewModel.ProgressPercent);
                }
            };

            // Progress reaches the view model through a SynchronizationContext. Without one the
            // reports land on the thread pool at an unpredictable time; an inline context makes
            // them arrive in order on this thread, which is what the UI thread does anyway.
            await RunWithInlineSynchronizationContext(
                () => viewModel.StartTestCommand.ExecuteAsync(null));

            Assert.Multiple(() =>
            {
                // 80 of 160 packets into the first of four rates.
                Assert.That(progressReports, Does.Contain(12.5));

                // The second rate complete, so two of four.
                Assert.That(progressReports, Does.Contain(50.0));
            });
        }

        [Test]
        public async Task StartTest_LeavesNoResultsToReportWhenItWasCancelled()
        {
            var viewModel = await CreateViewModel();
            _lineQualityServiceMock
                .Setup(service => service.RunTestAsync(It.IsAny<string>(), It.IsAny<LineQualityOptions>(),
                    It.IsAny<CancellationToken>()))
                .ReturnsAsync(() => throw new OperationCanceledException());

            await viewModel.StartTestCommand.ExecuteAsync(null);

            Assert.Multiple(() =>
            {
                Assert.That(viewModel.HasResults, Is.False);
                Assert.That(viewModel.IsTestRunning, Is.False);
                Assert.That(viewModel.SaveReportCommand.CanExecute(null), Is.False);
            });
        }

        [Test]
        public async Task StartResponder_StartsAndStopsTheResponderOnTheSelectedPort()
        {
            var viewModel = await CreateViewModel();
            viewModel.IsControllerMode = false;
            viewModel.Address = 125;

            await viewModel.StartResponderCommand.ExecuteAsync(null);

            Assert.Multiple(() =>
            {
                Assert.That(viewModel.IsResponderRunning, Is.True);
                Assert.That(viewModel.IsBusy, Is.True);
                Assert.That(viewModel.CanStartResponder, Is.False);
            });
            _lineQualityServiceMock.Verify(service => service.StartResponderAsync("COM3", 125), Times.Once);

            await viewModel.StopResponderCommand.ExecuteAsync(null);

            Assert.Multiple(() =>
            {
                Assert.That(viewModel.IsResponderRunning, Is.False);
                Assert.That(viewModel.IsBusy, Is.False);
            });
            _lineQualityServiceMock.Verify(service => service.StopResponderAsync(), Times.Once);
        }

        [Test]
        public async Task StartResponder_ReportsAFailureAndLeavesTheResponderStopped()
        {
            var viewModel = await CreateViewModel();
            viewModel.IsControllerMode = false;
            _lineQualityServiceMock.Setup(service => service.StartResponderAsync(It.IsAny<string>(), It.IsAny<byte>()))
                .ThrowsAsync(new InvalidOperationException("port busy"));

            await viewModel.StartResponderCommand.ExecuteAsync(null);

            Assert.That(viewModel.IsResponderRunning, Is.False);
            _dialogServiceMock.Verify(
                dialog => dialog.ShowExceptionDialog(It.IsAny<string>(), It.IsAny<Exception>()), Times.Once);
        }

        [Test]
        public async Task RecommendedBaudRateText_ReadsAsNoneWhenNoRatePassed()
        {
            var viewModel = await CreateViewModel();

            Assert.That(viewModel.RecommendedBaudRateText, Is.EqualTo("None"));

            viewModel.RecommendedBaudRate = 115200;

            Assert.That(viewModel.RecommendedBaudRateText, Is.EqualTo("115200"));
        }

        [Test]
        public async Task ProfileDetail_StatesTheDetectionLimitTheProfileBuys()
        {
            var viewModel = await CreateViewModel();

            // Screening sends 160 packets per rate, which rules out loss above roughly 1.9%.
            Assert.That(viewModel.ProfileDetail, Does.Contain("160"));

            viewModel.SelectedProfile = viewModel.AvailableProfiles
                .First(profile => profile.Profile == TestProfile.Qualification);

            Assert.That(viewModel.ProfileDetail, Does.Contain("960"));
        }

        private static async Task RunWithInlineSynchronizationContext(Func<Task> action)
        {
            var previous = SynchronizationContext.Current;
            SynchronizationContext.SetSynchronizationContext(new InlineSynchronizationContext());
            try
            {
                await action();
            }
            finally
            {
                SynchronizationContext.SetSynchronizationContext(previous);
            }
        }

        /// <summary>
        /// Runs posted callbacks on the calling thread, so progress reports are observable in the
        /// order they were made rather than whenever the thread pool gets to them.
        /// </summary>
        private sealed class InlineSynchronizationContext : SynchronizationContext
        {
            public override void Post(SendOrPostCallback callback, object state) => callback(state);

            public override void Send(SendOrPostCallback callback, object state) => callback(state);
        }

        private static LineQualityProgress CreateProgress(int baudRate, int packetsSentAtRate,
            int totalPacketsAtRate, int completedBaudRates, int totalBaudRates)
        {
            // LineQualityProgress has no public constructor, so instances are built the way the
            // library builds them internally.
            return (LineQualityProgress)Activator.CreateInstance(typeof(LineQualityProgress),
                System.Reflection.BindingFlags.Instance | System.Reflection.BindingFlags.NonPublic,
                null,
                [string.Empty, baudRate, packetsSentAtRate, totalPacketsAtRate, completedBaudRates, totalBaudRates],
                null)!;
        }
    }
}
