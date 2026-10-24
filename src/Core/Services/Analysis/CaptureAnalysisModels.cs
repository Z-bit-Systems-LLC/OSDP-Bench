namespace OSDPBench.Core.Services.Analysis;

/// <summary>
/// A capture to analyze, with the options to analyze it with.
/// </summary>
/// <param name="FileName">The capture's file name, e.g. "osdp-trace-20261023-101500.osdpcap".</param>
/// <param name="Capture">The capture in .osdpcap format.</param>
public sealed record CaptureAnalysisRequest(string FileName, ReadOnlyMemory<byte> Capture)
{
    /// <summary>
    /// Gets an optional question to focus the analysis on.
    /// </summary>
    public string? Question { get; init; }

    /// <summary>
    /// Gets the Secure Channel base key (16 bytes) to decrypt secure traffic with, or <see langword="null"/> to leave
    /// it encrypted.
    /// </summary>
    public byte[]? SecureChannelKey { get; init; }

    /// <summary>
    /// Gets a value indicating whether card numbers and keypad entries may be included in the analysis.
    /// </summary>
    public bool IncludeCredentials { get; init; }
}

/// <summary>
/// A capture handed to the analysis window, with what OSDP Bench knows about it.
/// </summary>
/// <param name="FileName">The capture's file name.</param>
/// <param name="Capture">The capture in .osdpcap format.</param>
/// <param name="SecureChannelKey">The Secure Channel base key the capture was made with, if any. It is only sent to
/// the analyzer when the user agrees.</param>
public sealed record CaptureForAnalysis(string FileName, byte[] Capture, byte[]? SecureChannelKey);

/// <summary>
/// The account a user is signed in to an analysis service with.
/// </summary>
/// <param name="Name">The user's name, if the service provides it.</param>
/// <param name="Email">The user's email address, if the service provides it.</param>
public sealed record CaptureAnalysisAccount(string? Name, string? Email)
{
    /// <summary>
    /// Gets the best text to identify the account with: the email address, else the name.
    /// </summary>
    public string DisplayName => !string.IsNullOrWhiteSpace(Email) ? Email : Name ?? string.Empty;
}

/// <summary>
/// How many analyses a user has left in the current period.
/// </summary>
/// <param name="Remaining">The analyses left.</param>
/// <param name="Limit">The analyses allowed per period.</param>
public sealed record CaptureAnalysisQuota(int Remaining, int Limit);

/// <summary>
/// The result of one automatic check the analyzer ran on the capture.
/// </summary>
/// <param name="Name">What was checked, e.g. "NAK replies".</param>
/// <param name="Count">How many times the problem was found; zero means the check passed.</param>
public sealed record CaptureAnalysisCheck(string Name, int Count);

/// <summary>
/// What the analyzer read from the capture before writing its report.
/// </summary>
/// <param name="FileName">The capture's file name.</param>
/// <param name="Records">The number of packets in the capture.</param>
/// <param name="Truncated">True when the capture was too long and only part of it was analyzed.</param>
/// <param name="Checks">The automatic checks that were run.</param>
public sealed record CaptureAnalysisSummary(
    string? FileName,
    int Records,
    bool Truncated,
    IReadOnlyList<CaptureAnalysisCheck> Checks);

/// <summary>
/// An event in a streamed analysis or answer.
/// </summary>
public abstract record CaptureAnalysisEvent
{
    private CaptureAnalysisEvent()
    {
    }

    /// <summary>
    /// The analyzer accepted the request and is about to write.
    /// </summary>
    /// <param name="AnalysisId">The identifier to ask follow-up questions with, if the service supports them.</param>
    /// <param name="Summary">What the analyzer read from the capture; only sent for a new analysis.</param>
    public sealed record Started(string? AnalysisId, CaptureAnalysisSummary? Summary) : CaptureAnalysisEvent;

    /// <summary>
    /// The next piece of the Markdown answer.
    /// </summary>
    /// <param name="Text">The text to append.</param>
    public sealed record TextReceived(string Text) : CaptureAnalysisEvent;

    /// <summary>
    /// The answer is complete.
    /// </summary>
    /// <param name="Truncated">True when the answer was cut off at the analyzer's length limit.</param>
    /// <param name="Quota">The user's quota after this answer, if the service has one.</param>
    public sealed record Completed(bool Truncated, CaptureAnalysisQuota? Quota) : CaptureAnalysisEvent;
}
