using OSDP.Net.Connections;
using OSDP.Net.Tracing;

namespace OSDPBench.Core.Services;

/// <summary>
/// Wraps a retunable connection and reports that traffic passed, so work that does not go through
/// <c>ControlPanel</c> can still drive a page's activity indicators.
/// </summary>
/// <remarks>
/// The indicators elsewhere in the application are fed by the panel's trace. The line quality test
/// deliberately bypasses the panel, so nothing is tracing its traffic and its indicators would
/// otherwise stay dark for the whole run. Wrapping the connection catches both directions in one
/// place, for whichever role happens to own the port.
///
/// This reports activity rather than packets, and is deliberately not a trace: it carries a
/// direction and nothing else. A sweep sends hundreds of packets a second at the higher rates and
/// the indicator fades on its own timer, so reporting more often than <see cref="ReportInterval"/>
/// would be work nobody can see.
/// </remarks>
public sealed class ActivityReportingConnection : IRetunableOsdpConnection
{
    /// <summary>
    /// The shortest gap between two reports in the same direction.
    /// </summary>
    /// <remarks>
    /// Short enough that the indicator keeps up with a running sweep, long enough that the fastest
    /// rate does not raise an event per packet.
    /// </remarks>
    public static readonly TimeSpan ReportInterval = TimeSpan.FromMilliseconds(50);

    private readonly IRetunableOsdpConnection _connection;

    // Ticks rather than timestamps because this sits in the read and write path of a measurement,
    // where the cost of reporting has to stay close to nothing.
    private long _lastTransmitTick;
    private long _lastReceiveTick;

    /// <summary>
    /// Initializes the wrapper around a connection.
    /// </summary>
    /// <param name="connection">The connection to report on and forward to.</param>
    /// <exception cref="ArgumentNullException">The connection is null.</exception>
    public ActivityReportingConnection(IRetunableOsdpConnection connection)
    {
        _connection = connection ?? throw new ArgumentNullException(nameof(connection));
    }

    /// <summary>
    /// Occurs when traffic has passed, at most once per <see cref="ReportInterval"/> in each
    /// direction.
    /// </summary>
    /// <remarks>
    /// Raised on whichever thread is driving the connection, which for both line quality roles is
    /// their own loop rather than the thread that created them.
    /// </remarks>
    public event EventHandler<TraceDirection>? ActivityObserved;

    /// <inheritdoc />
    public int BaudRate => _connection.BaudRate;

    /// <inheritdoc />
    public bool IsOpen => _connection.IsOpen;

    /// <inheritdoc />
    public TimeSpan ReplyTimeout
    {
        get => _connection.ReplyTimeout;
        set => _connection.ReplyTimeout = value;
    }

    /// <inheritdoc />
    public bool DiscardBuffersBeforeWrite
    {
        get => _connection.DiscardBuffersBeforeWrite;
        set => _connection.DiscardBuffersBeforeWrite = value;
    }

    /// <inheritdoc />
    public Task Open() => _connection.Open();

    /// <inheritdoc />
    public Task Close() => _connection.Close();

    /// <inheritdoc />
    public async Task WriteAsync(byte[] buffer)
    {
        await _connection.WriteAsync(buffer);

        Report(TraceDirection.Output, ref _lastTransmitTick);
    }

    /// <inheritdoc />
    public async Task<int> ReadAsync(byte[] buffer, CancellationToken token)
    {
        int count = await _connection.ReadAsync(buffer, token);

        // Only bytes that actually arrived count. A read returns zero when it times out, and an
        // indicator that kept flashing through a line with nothing on the far end would be telling
        // the technician the opposite of what the test is about to report.
        if (count > 0)
        {
            Report(TraceDirection.Input, ref _lastReceiveTick);
        }

        return count;
    }

    /// <inheritdoc />
    public void SetBaudRate(int baudRate) => _connection.SetBaudRate(baudRate);

    /// <inheritdoc />
    public Task WaitForTransmitCompleteAsync(int frameByteCount, CancellationToken token) =>
        _connection.WaitForTransmitCompleteAsync(frameByteCount, token);

    /// <inheritdoc />
    public void Dispose() => _connection.Dispose();

    /// <summary>
    /// Reports activity in one direction unless that direction has reported recently.
    /// </summary>
    /// <remarks>
    /// Two loops never share a direction, so the read of the last tick needs no interlock: the
    /// worst a race could cost is one extra report, which the indicator cannot show anyway.
    /// </remarks>
    private void Report(TraceDirection direction, ref long lastTick)
    {
        long now = Environment.TickCount64;
        if (now - lastTick < ReportInterval.TotalMilliseconds) return;

        lastTick = now;
        ActivityObserved?.Invoke(this, direction);
    }
}
