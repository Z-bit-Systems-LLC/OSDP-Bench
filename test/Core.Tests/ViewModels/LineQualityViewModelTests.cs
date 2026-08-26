using System;
using System.Collections.Generic;
using System.IO;
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
        private Mock<IUserSettingsService> _userSettingsServiceMock;

        [SetUp]
        public void Setup()
        {
            _dialogServiceMock = new Mock<IDialogService>();
            _lineQualityServiceMock = new Mock<ILineQualityService>();
            _serialPortConnectionServiceMock = new Mock<ISerialPortConnectionService>();
            _deviceManagementServiceMock = new Mock<IDeviceManagementService>();
            _userSettingsServiceMock = new Mock<IUserSettingsService>();

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

        /// <summary>
        /// Builds a view model that finds the given settings left behind by an earlier session.
        /// </summary>
        private async Task<LineQualityViewModel> CreateViewModel(LineQualityUserSettings savedSettings)
        {
            _userSettingsServiceMock.Setup(service => service.LineQualitySettings).Returns(savedSettings);

            var viewModel = new LineQualityViewModel(
                _dialogServiceMock.Object,
                _lineQualityServiceMock.Object,
                _serialPortConnectionServiceMock.Object,
                _deviceManagementServiceMock.Object,
                userSettingsService: _userSettingsServiceMock.Object);

            await viewModel.InitializationComplete;
            return viewModel;
        }

        /// <summary>
        /// Makes a run end without producing a report, which is enough for any test that only cares
        /// about what the page did on the way in.
        /// </summary>
        private void SetUpCancelledRun()
        {
            _lineQualityServiceMock
                .Setup(service => service.RunTestAsync(It.IsAny<string>(), It.IsAny<LineQualityOptions>(),
                    It.IsAny<CancellationToken>()))
                .ReturnsAsync(() => throw new OperationCanceledException());
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
            _deviceManagementServiceMock.Setup(service => service.IsPortInUse).Returns(true);
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
            _deviceManagementServiceMock.Setup(service => service.IsPortInUse).Returns(true);
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

        [Test]
        public async Task Constructor_RestoresTheSetupTheLastSessionLeftBehind()
        {
            var viewModel = await CreateViewModel(new LineQualityUserSettings
            {
                Profile = nameof(TestProfile.Qualification),
                BaudRates = [19200, 57600],
                Address = 42,
                TesterName = "A. Tech",
                AdapterDescription = "FTDI FT232R",
                AcuDescription = "Controller 1.2",
                PdDescription = "Reader 3.4",
                AdapterLatencyTimerAdjusted = true,
                InstallationLocation = "Panel 3, Drop 2",
                CableDescription = "22 AWG, 150 m"
            });

            Assert.Multiple(() =>
            {
                Assert.That(viewModel.SelectedProfile.Profile, Is.EqualTo(TestProfile.Qualification));
                Assert.That(viewModel.BaudRateOptions.Where(option => option.IsSelected)
                    .Select(option => option.BaudRate), Is.EqualTo(new[] { 19200, 57600 }));
                Assert.That(viewModel.Address, Is.EqualTo(42));
                Assert.That(viewModel.TesterName, Is.EqualTo("A. Tech"));
                Assert.That(viewModel.AdapterDescription, Is.EqualTo("FTDI FT232R"));
                Assert.That(viewModel.AcuDescription, Is.EqualTo("Controller 1.2"));
                Assert.That(viewModel.PdDescription, Is.EqualTo("Reader 3.4"));
                Assert.That(viewModel.AdapterLatencyTimerAdjusted, Is.True);
                Assert.That(viewModel.InstallationLocation, Is.EqualTo("Panel 3, Drop 2"));
                Assert.That(viewModel.CableDescription, Is.EqualTo("22 AWG, 150 m"));
            });
        }

        [Test]
        public async Task Constructor_ComesBackInTheRoleTheLastSessionUsed()
        {
            var viewModel = await CreateViewModel(new LineQualityUserSettings { IsControllerMode = false });

            Assert.Multiple(() =>
            {
                Assert.That(viewModel.IsResponderMode, Is.True);
                Assert.That(viewModel.CanStartResponder, Is.True);
            });
        }

        [Test]
        public async Task Constructor_KeepsEveryBaudRateWhenTheSavedSetNamesNoneThatIsOffered()
        {
            // A rate the library no longer offers, which on its own would leave every box cleared
            // and a Start button that could never enable.
            var viewModel = await CreateViewModel(new LineQualityUserSettings { BaudRates = [4800] });

            Assert.Multiple(() =>
            {
                Assert.That(viewModel.BaudRateOptions.All(option => option.IsSelected), Is.True);
                Assert.That(viewModel.CanStartTest, Is.True);
            });
        }

        [Test]
        public async Task Constructor_FallsBackToScreeningWhenTheSavedProfileIsNoLongerKnown()
        {
            var viewModel = await CreateViewModel(new LineQualityUserSettings { Profile = "Exhaustive" });

            Assert.That(viewModel.SelectedProfile.Profile, Is.EqualTo(TestProfile.Screening));
        }

        [Test]
        public async Task StartTest_CarriesTheSetupOverToTheNextLaunch()
        {
            var viewModel = await CreateViewModel(new LineQualityUserSettings());
            SetUpCancelledRun();

            LineQualityUserSettings persisted = null;
            _userSettingsServiceMock
                .Setup(service => service.UpdateLineQualitySettingsAsync(It.IsAny<LineQualityUserSettings>()))
                .Callback<LineQualityUserSettings>(settings => persisted = settings)
                .Returns(Task.CompletedTask);

            viewModel.SelectedProfile = viewModel.AvailableProfiles
                .First(profile => profile.Profile == TestProfile.Extended);
            viewModel.Address = 7;
            viewModel.TesterName = "A. Tech";
            viewModel.InstallationLocation = "Panel 3, Drop 2";
            foreach (var option in viewModel.BaudRateOptions.Where(option => option.BaudRate != 9600))
            {
                option.IsSelected = false;
            }

            await viewModel.StartTestCommand.ExecuteAsync(null);

            Assert.That(persisted, Is.Not.Null);
            Assert.Multiple(() =>
            {
                Assert.That(persisted.Profile, Is.EqualTo(nameof(TestProfile.Extended)));
                Assert.That(persisted.BaudRates, Is.EqualTo(new[] { 9600 }));
                Assert.That(persisted.Address, Is.EqualTo((byte)7));
                Assert.That(persisted.IsControllerMode, Is.True);
                Assert.That(persisted.TesterName, Is.EqualTo("A. Tech"));
                Assert.That(persisted.InstallationLocation, Is.EqualTo("Panel 3, Drop 2"));
            });
        }

        [Test]
        public async Task StartTest_KeepsTheFolderTheLastReportWasSavedTo()
        {
            var viewModel = await CreateViewModel(new LineQualityUserSettings
            {
                ReportDestination = @"C:\jobs\site-a"
            });
            SetUpCancelledRun();

            LineQualityUserSettings persisted = null;
            _userSettingsServiceMock
                .Setup(service => service.UpdateLineQualitySettingsAsync(It.IsAny<LineQualityUserSettings>()))
                .Callback<LineQualityUserSettings>(settings => persisted = settings)
                .Returns(Task.CompletedTask);

            await viewModel.StartTestCommand.ExecuteAsync(null);

            // The destination appears nowhere on the page, so a run that rewrites the rest of the
            // setup is exactly where it would be dropped without anyone noticing.
            Assert.That(persisted?.ReportDestination, Is.EqualTo(@"C:\jobs\site-a"));
        }

        [Test]
        public async Task StartResponder_CarriesTheRoleOverToTheNextLaunch()
        {
            var viewModel = await CreateViewModel(new LineQualityUserSettings());
            viewModel.IsControllerMode = false;

            LineQualityUserSettings persisted = null;
            _userSettingsServiceMock
                .Setup(service => service.UpdateLineQualitySettingsAsync(It.IsAny<LineQualityUserSettings>()))
                .Callback<LineQualityUserSettings>(settings => persisted = settings)
                .Returns(Task.CompletedTask);

            await viewModel.StartResponderCommand.ExecuteAsync(null);

            Assert.That(persisted, Is.Not.Null);
            Assert.That(persisted.IsControllerMode, Is.False);
        }

        [Test]
        public async Task StartTest_ClearsTheNotesButKeepsTheLineTheyDescribe()
        {
            var viewModel = await CreateViewModel(new LineQualityUserSettings());
            SetUpCancelledRun();

            viewModel.InstallationLocation = "Panel 3, Drop 2";
            viewModel.CableDescription = "22 AWG, 150 m";
            viewModel.Notes = "Splice at the halfway point looked corroded.";

            await viewModel.StartTestCommand.ExecuteAsync(null);

            Assert.Multiple(() =>
            {
                Assert.That(viewModel.Notes, Is.Empty);
                Assert.That(viewModel.InstallationLocation, Is.EqualTo("Panel 3, Drop 2"));
                Assert.That(viewModel.CableDescription, Is.EqualTo("22 AWG, 150 m"));
            });
        }

        [Test]
        public async Task StartTest_RunsEvenWhenTheSetupCannotBeSaved()
        {
            var viewModel = await CreateViewModel(new LineQualityUserSettings());
            SetUpCancelledRun();

            _userSettingsServiceMock
                .Setup(service => service.UpdateLineQualitySettingsAsync(It.IsAny<LineQualityUserSettings>()))
                .ThrowsAsync(new IOException("settings file locked"));

            await viewModel.StartTestCommand.ExecuteAsync(null);

            _lineQualityServiceMock.Verify(
                service => service.RunTestAsync("COM3", It.IsAny<LineQualityOptions>(),
                    It.IsAny<CancellationToken>()), Times.Once);
        }

        [Test]
        public async Task ClearLineDetails_EmptiesTheLineAndIsOfferedOnlyWhenThereIsSomethingToClear()
        {
            var viewModel = await CreateViewModel(new LineQualityUserSettings());

            Assert.That(viewModel.ClearLineDetailsCommand.CanExecute(null), Is.False);

            viewModel.InstallationLocation = "Panel 3, Drop 2";
            viewModel.CableDescription = "22 AWG, 150 m";
            viewModel.Notes = "Retest once the splice is redone.";

            Assert.That(viewModel.ClearLineDetailsCommand.CanExecute(null), Is.True);

            viewModel.ClearLineDetailsCommand.Execute(null);

            Assert.Multiple(() =>
            {
                Assert.That(viewModel.InstallationLocation, Is.Empty);
                Assert.That(viewModel.CableDescription, Is.Empty);
                Assert.That(viewModel.Notes, Is.Empty);
                Assert.That(viewModel.ClearLineDetailsCommand.CanExecute(null), Is.False);
            });
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
