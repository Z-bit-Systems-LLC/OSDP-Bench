namespace OSDPBench.Core.Services.Analysis;

/// <summary>
/// What went wrong with a capture analysis, so the app can react to it.
/// </summary>
public enum CaptureAnalysisErrorKind
{
    /// <summary>Anything not covered by a more specific kind.</summary>
    Failed,

    /// <summary>The user isn't signed in, or their sign-in has expired; they need to sign in again.</summary>
    NotSignedIn,

    /// <summary>The sign-in was cancelled, refused or didn't complete.</summary>
    SignInFailed,

    /// <summary>The service can't be reached or is switched off.</summary>
    Unavailable,

    /// <summary>The service is overloaded; trying again shortly may work.</summary>
    ServiceBusy,

    /// <summary>The request was malformed.</summary>
    InvalidRequest,

    /// <summary>The capture couldn't be read.</summary>
    InvalidCapture,

    /// <summary>The capture is larger than the service accepts.</summary>
    CaptureTooLarge,

    /// <summary>The Secure Channel key was rejected.</summary>
    InvalidSecurityKey,

    /// <summary>The question was empty or too long.</summary>
    InvalidQuestion,

    /// <summary>The user has no analyses left in this period.</summary>
    QuotaExceeded,

    /// <summary>No more follow-up questions can be asked about this analysis.</summary>
    FollowUpLimitReached,

    /// <summary>The analysis is too old to ask about; analyze the capture again.</summary>
    AnalysisExpired,

    /// <summary>An answer about this analysis is still being written.</summary>
    AnswerInProgress,

    /// <summary>The analyzer declined to answer.</summary>
    Refused,

    /// <summary>The answer stopped before it was finished.</summary>
    Incomplete
}

/// <summary>
/// Thrown by an <see cref="ICaptureAnalysisService"/> when an analysis, question or sign-in fails. The message is
/// meant to be shown to the user.
/// </summary>
public class CaptureAnalysisException : Exception
{
    /// <summary>
    /// Initializes a new instance of the <see cref="CaptureAnalysisException"/> class.
    /// </summary>
    /// <param name="kind">What went wrong.</param>
    /// <param name="message">A message to show the user.</param>
    /// <param name="quota">The user's quota, when the service reported it with the error.</param>
    /// <param name="innerException">The exception that caused this one, if any.</param>
    public CaptureAnalysisException(CaptureAnalysisErrorKind kind, string message,
        CaptureAnalysisQuota? quota = null, Exception? innerException = null)
        : base(message, innerException)
    {
        Kind = kind;
        Quota = quota;
    }

    /// <summary>
    /// Gets what went wrong.
    /// </summary>
    public CaptureAnalysisErrorKind Kind { get; }

    /// <summary>
    /// Gets the user's quota, when the service reported it with the error.
    /// </summary>
    public CaptureAnalysisQuota? Quota { get; }
}
