using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using Moq;
using NUnit.Framework;
using OSDP.Net.Connections;
using OSDP.Net.Tracing;
using OSDPBench.Core.Services;

namespace OSDPBench.Core.Tests.Services
{
    [TestFixture]
    public class ActivityReportingConnectionTests
    {
        private Mock<IRetunableOsdpConnection> _innerMock = null!;
        private ActivityReportingConnection _connection = null!;
        private List<TraceDirection> _reported = null!;

        [SetUp]
        public void Setup()
        {
            _innerMock = new Mock<IRetunableOsdpConnection>();
            _connection = new ActivityReportingConnection(_innerMock.Object);

            _reported = [];
            _connection.ActivityObserved += (_, direction) => _reported.Add(direction);
        }

        [Test]
        public void Constructor_RefusesAConnectionItCannotReportOn()
        {
            Assert.Throws<ArgumentNullException>(() => _ = new ActivityReportingConnection(null!));
        }

        [Test]
        public async Task WriteAsync_ReportsOutgoingTrafficAndStillWrites()
        {
            byte[] frame = [0x53, 0x00];

            await _connection.WriteAsync(frame);

            Assert.Multiple(() =>
            {
                Assert.That(_reported, Is.EqualTo(new[] { TraceDirection.Output }));
                _innerMock.Verify(inner => inner.WriteAsync(frame), Times.Once);
            });
        }

        [Test]
        public async Task ReadAsync_ReportsIncomingTrafficAndReturnsWhatWasRead()
        {
            _innerMock
                .Setup(inner => inner.ReadAsync(It.IsAny<byte[]>(), It.IsAny<CancellationToken>()))
                .ReturnsAsync(4);

            int count = await _connection.ReadAsync(new byte[8], CancellationToken.None);

            Assert.Multiple(() =>
            {
                Assert.That(count, Is.EqualTo(4), "The wrapper must not change what was read.");
                Assert.That(_reported, Is.EqualTo(new[] { TraceDirection.Input }));
            });
        }

        [Test]
        public async Task ReadAsync_ReportsNothingWhenTheReadTimedOut()
        {
            // A read returns zero bytes on timeout. Reporting that as traffic would leave the
            // indicator flashing on a line with nothing on the far end, which is the opposite of
            // what the run is about to conclude.
            _innerMock
                .Setup(inner => inner.ReadAsync(It.IsAny<byte[]>(), It.IsAny<CancellationToken>()))
                .ReturnsAsync(0);

            await _connection.ReadAsync(new byte[8], CancellationToken.None);

            Assert.That(_reported, Is.Empty);
        }

        [Test]
        public async Task WriteAsync_CollapsesABurstIntoASingleReport()
        {
            // A sweep sends hundreds of packets a second at the higher rates; the indicator cannot
            // show them individually, so the wrapper must not raise them individually either.
            for (int packet = 0; packet < 50; packet++)
            {
                await _connection.WriteAsync([0x53]);
            }

            Assert.Multiple(() =>
            {
                Assert.That(_reported, Has.Count.EqualTo(1),
                    "A burst inside one report interval should report once.");
                _innerMock.Verify(inner => inner.WriteAsync(It.IsAny<byte[]>()), Times.Exactly(50),
                    "Every packet must still be written.");
            });
        }

        [Test]
        public async Task Directions_AreThrottledIndependently()
        {
            _innerMock
                .Setup(inner => inner.ReadAsync(It.IsAny<byte[]>(), It.IsAny<CancellationToken>()))
                .ReturnsAsync(4);

            await _connection.WriteAsync([0x53]);
            await _connection.ReadAsync(new byte[8], CancellationToken.None);

            Assert.That(_reported, Is.EqualTo(new[] { TraceDirection.Output, TraceDirection.Input }),
                "One direction reporting must not silence the other.");
        }

        [Test]
        public async Task WriteAsync_ReportsAgainOnceTheIntervalHasPassed()
        {
            await _connection.WriteAsync([0x53]);
            await Task.Delay(ActivityReportingConnection.ReportInterval + TimeSpan.FromMilliseconds(30));
            await _connection.WriteAsync([0x53]);

            Assert.That(_reported, Has.Count.EqualTo(2));
        }

        [Test]
        public void Members_ForwardToTheConnectionBeingWrapped()
        {
            _innerMock.SetupGet(inner => inner.BaudRate).Returns(19200);
            _innerMock.SetupGet(inner => inner.IsOpen).Returns(true);
            _innerMock.SetupProperty(inner => inner.ReplyTimeout);
            _innerMock.SetupProperty(inner => inner.DiscardBuffersBeforeWrite);

            _connection.ReplyTimeout = TimeSpan.FromMilliseconds(200);
            _connection.DiscardBuffersBeforeWrite = true;
            _connection.SetBaudRate(115200);
            _connection.Dispose();

            Assert.Multiple(() =>
            {
                Assert.That(_connection.BaudRate, Is.EqualTo(19200));
                Assert.That(_connection.IsOpen, Is.True);
                Assert.That(_connection.ReplyTimeout, Is.EqualTo(TimeSpan.FromMilliseconds(200)));
                Assert.That(_connection.DiscardBuffersBeforeWrite, Is.True);
                _innerMock.Verify(inner => inner.SetBaudRate(115200), Times.Once);
                _innerMock.Verify(inner => inner.Dispose(), Times.Once);
            });
        }
    }
}
