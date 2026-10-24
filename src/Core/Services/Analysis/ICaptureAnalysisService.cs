namespace OSDPBench.Core.Services.Analysis;

/// <summary>
/// A service that analyzes OSDP captures and reports on them. Implement this interface to plug a different analyzer
/// into OSDP Bench; the capture analysis view model only talks to this interface.
/// </summary>
/// <remarks>
/// Failures are reported by throwing <see cref="CaptureAnalysisException"/>, both before and during a streamed answer,
/// with a message that can be shown to the user.
/// </remarks>
public interface ICaptureAnalysisService
{
    /// <summary>
    /// Gets the name of the analyzer to show to the user, e.g. "Z-bit Capture Analyzer".
    /// </summary>
    string DisplayName { get; }

    /// <summary>
    /// Gets a value indicating whether the user has to sign in before analyzing a capture.
    /// </summary>
    bool RequiresSignIn { get; }

    /// <summary>
    /// Gets a value indicating whether follow-up questions can be asked about an analysis with
    /// <see cref="AskAsync"/>.
    /// </summary>
    bool SupportsFollowUpQuestions { get; }

    /// <summary>
    /// Gets the account the user is signed in with, restoring a saved sign-in if there is one.
    /// </summary>
    /// <param name="cancellationToken">A token to cancel the operation.</param>
    /// <returns>The signed-in account, or <see langword="null"/> when the user isn't signed in or the service has no
    /// accounts.</returns>
    Task<CaptureAnalysisAccount?> GetAccountAsync(CancellationToken cancellationToken = default);

    /// <summary>
    /// Signs the user in, typically through the system browser.
    /// </summary>
    /// <param name="cancellationToken">A token to cancel the sign-in.</param>
    /// <returns>The account the user signed in with.</returns>
    /// <exception cref="CaptureAnalysisException">The sign-in failed or was refused.</exception>
    Task<CaptureAnalysisAccount> SignInAsync(CancellationToken cancellationToken = default);

    /// <summary>
    /// Signs the user out and forgets any saved sign-in.
    /// </summary>
    /// <param name="cancellationToken">A token to cancel the operation.</param>
    Task SignOutAsync(CancellationToken cancellationToken = default);

    /// <summary>
    /// Gets how many analyses the user has left.
    /// </summary>
    /// <param name="cancellationToken">A token to cancel the operation.</param>
    /// <returns>The quota, or <see langword="null"/> when the service has no limit.</returns>
    /// <exception cref="CaptureAnalysisException">The quota could not be read.</exception>
    Task<CaptureAnalysisQuota?> GetQuotaAsync(CancellationToken cancellationToken = default);

    /// <summary>
    /// Analyzes a capture and streams the report as it is written.
    /// </summary>
    /// <param name="request">The capture and options to analyze it with.</param>
    /// <param name="cancellationToken">A token to stop the analysis.</param>
    /// <returns>A <see cref="CaptureAnalysisEvent.Started"/> event, then <see cref="CaptureAnalysisEvent.TextReceived"/>
    /// events with pieces of the Markdown report, then a <see cref="CaptureAnalysisEvent.Completed"/> event.</returns>
    /// <exception cref="CaptureAnalysisException">The analysis failed.</exception>
    IAsyncEnumerable<CaptureAnalysisEvent> AnalyzeAsync(CaptureAnalysisRequest request,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Asks a follow-up question about an earlier analysis and streams the answer as it is written.
    /// </summary>
    /// <param name="analysisId">The identifier from the analysis's <see cref="CaptureAnalysisEvent.Started"/> event.</param>
    /// <param name="question">The question to ask.</param>
    /// <param name="cancellationToken">A token to stop the answer.</param>
    /// <returns>The same sequence of events as <see cref="AnalyzeAsync"/>.</returns>
    /// <exception cref="CaptureAnalysisException">The question could not be answered.</exception>
    /// <exception cref="NotSupportedException"><see cref="SupportsFollowUpQuestions"/> is false.</exception>
    IAsyncEnumerable<CaptureAnalysisEvent> AskAsync(string analysisId, string question,
        CancellationToken cancellationToken = default);
}
