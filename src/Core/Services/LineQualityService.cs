using OSDP.Net.Connections;
using OSDP.Net.LineQuality;

namespace OSDPBench.Core.Services;

/// <summary>
/// Default <see cref="ILineQualityService"/>, driving the OSDP.Net line quality controller and
/// responder over a serial port the caller nominates.
/// </summary>
public sealed class LineQualityService : ILineQualityService
{
    /// <summary>
    /// The rate both roles start at. The procedure requires a responder to power up at 9600, and
    /// the controller searches from there, so opening anywhere else only delays first contact.
    /// </summary>
    private const int BaselineBaudRate = 9600;

    private readonly ISerialPortConnectionService _serialPortConnectionService;
    private readonly SynchronizationContext? _synchronizationContext;

    private CancellationTokenSource? _responderCancellation;
    private Task? _responderTask;
    private IRetunableOsdpConnection? _responderConnection;
    private LineQualityResponder? _responder;

    /// <summary>
    /// Initializes the service.
    /// </summary>
    /// <param name="serialPortConnectionService">Provides the serial connections both roles use.</param>
    /// <exception cref="ArgumentNullException">The connection service is null.</exception>
    public LineQualityService(ISerialPortConnectionService serialPortConnectionService)
    {
        _serialPortConnectionService = serialPortConnectionService ??
                                       throw new ArgumentNullException(nameof(serialPortConnectionService));

        // Captured so responder events, which are raised on its own receive loop, reach subscribers
        // on the thread that created the service.
        _synchronizationContext = SynchronizationContext.Current;
    }

    /// <inheritdoc />
    public bool IsTestRunning { get; private set; }

    /// <inheritdoc />
    public bool IsResponderRunning => _responderTask is { IsCompleted: false };

    /// <inheritdoc />
    public int ResponderBaudRate => _responder?.CurrentBaudRate ?? 0;

    /// <inheritdoc />
    public event EventHandler<LineQualityExchangeEventArgs>? ResponderExchangeCompleted;

    /// <inheritdoc />
    public event EventHandler<LineQualityBaudRateChangedEventArgs>? ResponderBaudRateChanged;

    /// <inheritdoc />
    public event EventHandler<Exception?>? ResponderStopped;

    /// <inheritdoc />
    public bool IsSupported(string portName)
    {
        if (string.IsNullOrWhiteSpace(portName)) return false;

        var connection = _serialPortConnectionService.GetRetunableConnection(portName, BaselineBaudRate);
        if (connection == null) return false;

        connection.Dispose();
        return true;
    }

    /// <inheritdoc />
    public async Task<LineQualityReport> RunTestAsync(string portName, LineQualityOptions options,
        CancellationToken token)
    {
        ArgumentNullException.ThrowIfNull(options);
        EnsurePortIsFree();

        var connection = CreateConnection(portName);
        IsTestRunning = true;
        try
        {
            var test = new LineQualityTest(connection);
            return await test.RunAsync(options, token);
        }
        finally
        {
            IsTestRunning = false;
            await CloseQuietly(connection);
        }
    }

    /// <inheritdoc />
    public Task StartResponderAsync(string portName, byte address)
    {
        EnsurePortIsFree();

        var connection = CreateConnection(portName);
        var responder = new LineQualityResponder(connection, address);
        responder.ExchangeCompleted += OnResponderExchangeCompleted;
        responder.BaudRateChanged += OnResponderBaudRateChanged;

        var cancellation = new CancellationTokenSource();

        _responderConnection = connection;
        _responder = responder;
        _responderCancellation = cancellation;
        _responderTask = RunResponder(responder, cancellation.Token);

        return Task.CompletedTask;
    }

    /// <inheritdoc />
    public async Task StopResponderAsync()
    {
        var cancellation = _responderCancellation;
        var task = _responderTask;

        if (cancellation == null || task == null) return;

        // Claim the stop before awaiting so a second caller cannot cancel or dispose it twice.
        // IsResponderRunning stays true until the loop has actually released the port.
        _responderCancellation = null;

        await cancellation.CancelAsync();

        try
        {
            await task;
        }
        catch (OperationCanceledException)
        {
            // Expected: this is how the responder loop ends.
        }
        finally
        {
            _responderTask = null;

            // Disposed here rather than on the responder's own thread, so it cannot be torn down
            // while the cancellation that stopped it is still propagating.
            cancellation.Dispose();
        }
    }

    private async Task RunResponder(LineQualityResponder responder, CancellationToken token)
    {
        Exception? failure = null;
        try
        {
            await responder.RunAsync(token);
        }
        catch (OperationCanceledException)
        {
            // Expected: this is how the responder loop ends.
        }
        catch (Exception exception)
        {
            failure = exception;
        }
        finally
        {
            responder.ExchangeCompleted -= OnResponderExchangeCompleted;
            responder.BaudRateChanged -= OnResponderBaudRateChanged;

            var connection = _responderConnection;
            _responderConnection = null;
            _responder = null;

            if (connection != null)
            {
                await CloseQuietly(connection);
            }
        }

        RaiseEvent(ResponderStopped, failure);
    }

    private void OnResponderExchangeCompleted(object? sender, LineQualityExchangeEventArgs args) =>
        RaiseEvent(ResponderExchangeCompleted, args);

    private void OnResponderBaudRateChanged(object? sender, LineQualityBaudRateChangedEventArgs args) =>
        RaiseEvent(ResponderBaudRateChanged, args);

    private IRetunableOsdpConnection CreateConnection(string portName)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(portName);

        return _serialPortConnectionService.GetRetunableConnection(portName, BaselineBaudRate) ??
               throw new PlatformNotSupportedException(
                   "This platform's serial connection cannot change baud rate while it stays open, " +
                   "which the line quality test requires.");
    }

    private void EnsurePortIsFree()
    {
        if (IsTestRunning || IsResponderRunning)
        {
            throw new InvalidOperationException(
                "A line quality test or responder is already using the serial port.");
        }
    }

    private static async Task CloseQuietly(IOsdpConnection connection)
    {
        try
        {
            await connection.Close();
        }
        catch (Exception)
        {
            // The port is being torn down; a failure to close it cleanly tells the caller nothing
            // useful and must not mask the result of the run.
        }
        finally
        {
            connection.Dispose();
        }
    }

    private void RaiseEvent<T>(EventHandler<T>? eventHandler, T arg)
    {
        if (eventHandler == null) return;

        if (_synchronizationContext != null)
        {
            _synchronizationContext.Post(_ => eventHandler.Invoke(this, arg), null);
        }
        else
        {
            eventHandler.Invoke(this, arg);
        }
    }
}
