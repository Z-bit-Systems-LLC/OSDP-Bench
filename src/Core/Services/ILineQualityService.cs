using OSDP.Net.LineQuality;
using OSDP.Net.Tracing;

namespace OSDPBench.Core.Services;

/// <summary>
/// Runs the two halves of the OSDP Line Quality Test Procedure: the controller that measures a
/// line, and the responder that answers on the far end of it.
/// </summary>
/// <remarks>
/// The test deliberately bypasses <c>ControlPanel</c>. The polling bus queues commands, retries on
/// timeout, and folds integrity failures in with timeouts, none of which a measurement can
/// tolerate. Both roles therefore take exclusive ownership of the serial port for as long as they
/// run, which is why nothing else may be connected to it at the time.
/// </remarks>
public interface ILineQualityService
{
    /// <summary>
    /// Gets a value indicating whether a line quality test is currently running.
    /// </summary>
    bool IsTestRunning { get; }

    /// <summary>
    /// Gets a value indicating whether the responder is currently answering on a port.
    /// </summary>
    bool IsResponderRunning { get; }

    /// <summary>
    /// Gets a value indicating whether either role currently owns a serial port.
    /// </summary>
    bool IsBusy { get; }

    /// <summary>
    /// Gets the baud rate the responder is currently answering at, or zero when it is not running.
    /// </summary>
    int ResponderBaudRate { get; }

    /// <summary>
    /// Occurs when <see cref="IsBusy"/> changes.
    /// </summary>
    /// <remarks>
    /// The shell listens to this to lock navigation for as long as a port is held. Neither role
    /// can be interrupted halfway without leaving the responder stranded at whatever rate it was
    /// last moved to.
    /// </remarks>
    event EventHandler? BusyChanged;

    /// <summary>
    /// Occurs after the responder completes an exchange.
    /// </summary>
    event EventHandler<LineQualityExchangeEventArgs>? ResponderExchangeCompleted;

    /// <summary>
    /// Occurs when the responder retunes, either because the controller asked it to or because
    /// its idle timeout returned it to the baseline rate.
    /// </summary>
    event EventHandler<LineQualityBaudRateChangedEventArgs>? ResponderBaudRateChanged;

    /// <summary>
    /// Occurs when the responder stops on its own, which normally means the port failed.
    /// </summary>
    event EventHandler<Exception?>? ResponderStopped;

    /// <summary>
    /// Occurs when traffic passes on the line, in whichever role currently owns the port.
    /// </summary>
    /// <remarks>
    /// Reports that the line is busy rather than what was sent: it is raised a few times a second
    /// in each direction at most, not once per packet, and is meant for activity indicators rather
    /// than for anything that needs to account for traffic.
    /// </remarks>
    event EventHandler<TraceDirection>? TrafficObserved;

    /// <summary>
    /// Determines whether the platform's serial connection can be retuned in place, which both
    /// roles require.
    /// </summary>
    /// <param name="portName">The name of the serial port to check.</param>
    /// <returns>True when the line quality test can run on this platform.</returns>
    bool IsSupported(string portName);

    /// <summary>
    /// Runs a line quality test against a responder on the given port.
    /// </summary>
    /// <param name="portName">The name of the serial port to test on.</param>
    /// <param name="options">Options controlling the run.</param>
    /// <param name="token">Token used to abandon the run.</param>
    /// <returns>The results of the run.</returns>
    /// <exception cref="PlatformNotSupportedException">The connection cannot be retuned in place.</exception>
    /// <exception cref="InvalidOperationException">A test or the responder is already running.</exception>
    Task<LineQualityReport> RunTestAsync(string portName, LineQualityOptions options,
        CancellationToken token);

    /// <summary>
    /// Starts answering line quality test traffic on the given port, until
    /// <see cref="StopResponderAsync"/> is called.
    /// </summary>
    /// <param name="portName">The name of the serial port to answer on.</param>
    /// <param name="address">The address to answer at.</param>
    /// <exception cref="PlatformNotSupportedException">The connection cannot be retuned in place.</exception>
    /// <exception cref="InvalidOperationException">A test or the responder is already running.</exception>
    Task StartResponderAsync(string portName, byte address);

    /// <summary>
    /// Stops the responder and releases the port. Safe to call when it is not running.
    /// </summary>
    Task StopResponderAsync();
}
