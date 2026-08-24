using System;
using System.Collections.Generic;
using System.Globalization;
using Moq;
using NUnit.Framework;
using OSDPBench.Core.Services;
using OSDPBench.Core.ViewModels.Windows;

namespace OSDPBench.Core.Tests.ViewModels
{
    [TestFixture(TestOf = typeof(MainWindowViewModel))]
    public class MainWindowViewModelTests
    {
        private Mock<ILocalizationService> _localizationServiceMock;
        private Mock<IDeviceManagementService> _deviceManagementServiceMock;
        private Mock<ILineQualityService> _lineQualityServiceMock;

        [SetUp]
        public void Setup()
        {
            _localizationServiceMock = new Mock<ILocalizationService>();
            _localizationServiceMock.Setup(service => service.SupportedCultures)
                .Returns(new List<CultureInfo> { new("en-US") });
            _localizationServiceMock.Setup(service => service.CurrentCulture).Returns(new CultureInfo("en-US"));

            _deviceManagementServiceMock = new Mock<IDeviceManagementService>();
            _lineQualityServiceMock = new Mock<ILineQualityService>();
        }

        private MainWindowViewModel CreateViewModel() => new(
            _localizationServiceMock.Object,
            _deviceManagementServiceMock.Object,
            _lineQualityServiceMock.Object);

        [Test]
        public void EverythingIsReachableWhenNothingHoldsThePort()
        {
            var viewModel = CreateViewModel();

            Assert.Multiple(() =>
            {
                Assert.That(viewModel.IsNavigationEnabled, Is.True);
                Assert.That(viewModel.IsLineQualityEnabled, Is.True);
                Assert.That(viewModel.NavigationDisabledReason, Is.Null);
                Assert.That(viewModel.LineQualityDisabledReason, Is.Null);
            });
        }

        [Test]
        public void LineQualityIsUnreachableWhileADeviceIsConnected()
        {
            _deviceManagementServiceMock.Setup(service => service.IsPortInUse).Returns(true);

            var viewModel = CreateViewModel();

            Assert.Multiple(() =>
            {
                Assert.That(viewModel.IsLineQualityEnabled, Is.False,
                    "The test needs the port to itself.");
                Assert.That(viewModel.LineQualityDisabledReason, Is.Not.Null.And.Not.Empty,
                    "The user should be told why.");
                Assert.That(viewModel.IsNavigationEnabled, Is.True,
                    "A connection does not restrict the other pages.");
            });
        }

        [Test]
        public void LineQualityIsUnreachableWhileTheBusHoldsThePortWithNothingAnswering()
        {
            // The case the narrower IsConnected check missed: a discovery sweep, or a manual
            // connection to an address nothing answers on, holds the port with IsConnected false.
            _deviceManagementServiceMock.Setup(service => service.IsConnected).Returns(false);
            _deviceManagementServiceMock.Setup(service => service.IsPassiveMonitoring).Returns(false);
            _deviceManagementServiceMock.Setup(service => service.IsPortInUse).Returns(true);

            var viewModel = CreateViewModel();

            Assert.That(viewModel.IsLineQualityEnabled, Is.False);
        }

        [Test]
        public void LineQualityBecomesReachableAgainOnDisconnect()
        {
            _deviceManagementServiceMock.Setup(service => service.IsPortInUse).Returns(true);
            var viewModel = CreateViewModel();
            Assert.That(viewModel.IsLineQualityEnabled, Is.False, "Precondition: port held.");

            _deviceManagementServiceMock.Setup(service => service.IsPortInUse).Returns(false);
            _deviceManagementServiceMock.Raise(service => service.PortInUseChanged += null,
                _deviceManagementServiceMock.Object, EventArgs.Empty);

            Assert.Multiple(() =>
            {
                Assert.That(viewModel.IsLineQualityEnabled, Is.True);
                Assert.That(viewModel.LineQualityDisabledReason, Is.Null);
            });
        }

        [Test]
        public void OtherPagesAreUnreachableWhileALineQualityRunHoldsThePort()
        {
            var viewModel = CreateViewModel();

            _lineQualityServiceMock.Setup(service => service.IsBusy).Returns(true);
            _lineQualityServiceMock.Raise(service => service.BusyChanged += null,
                _lineQualityServiceMock.Object, EventArgs.Empty);

            Assert.Multiple(() =>
            {
                Assert.That(viewModel.IsNavigationEnabled, Is.False,
                    "A run cannot be paused, so it must not be navigated away from.");
                Assert.That(viewModel.NavigationDisabledReason, Is.Not.Null.And.Not.Empty);
                Assert.That(viewModel.IsLineQualityEnabled, Is.True,
                    "The page the run is on stays reachable.");
            });

            _lineQualityServiceMock.Setup(service => service.IsBusy).Returns(false);
            _lineQualityServiceMock.Raise(service => service.BusyChanged += null,
                _lineQualityServiceMock.Object, EventArgs.Empty);

            Assert.That(viewModel.IsNavigationEnabled, Is.True, "The run releasing the port reopens them.");
        }

        [Test]
        public void DisposeStopsListeningToBothServices()
        {
            var viewModel = CreateViewModel();
            viewModel.Dispose();

            _lineQualityServiceMock.Setup(service => service.IsBusy).Returns(true);
            _lineQualityServiceMock.Raise(service => service.BusyChanged += null,
                _lineQualityServiceMock.Object, EventArgs.Empty);
            _deviceManagementServiceMock.Setup(service => service.IsPortInUse).Returns(true);
            _deviceManagementServiceMock.Raise(service => service.PortInUseChanged += null,
                _deviceManagementServiceMock.Object, EventArgs.Empty);

            Assert.Multiple(() =>
            {
                Assert.That(viewModel.IsNavigationEnabled, Is.True);
                Assert.That(viewModel.IsLineQualityEnabled, Is.True);
            });
        }
    }
}
