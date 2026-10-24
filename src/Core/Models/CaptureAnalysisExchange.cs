using CommunityToolkit.Mvvm.ComponentModel;

namespace OSDPBench.Core.Models;

/// <summary>
/// One question and its streamed answer in a capture analysis. The first exchange is the report itself.
/// </summary>
public partial class CaptureAnalysisExchange : ObservableObject
{
    /// <summary>
    /// Initializes a new instance of the <see cref="CaptureAnalysisExchange"/> class.
    /// </summary>
    /// <param name="question">The question asked, or <see langword="null"/> for a report with no question.</param>
    public CaptureAnalysisExchange(string? question)
    {
        Question = question;
    }

    /// <summary>
    /// Gets the question asked, or <see langword="null"/> for a report with no question.
    /// </summary>
    public string? Question { get; }

    /// <summary>
    /// Gets a value indicating whether a question was asked.
    /// </summary>
    public bool HasQuestion => !string.IsNullOrEmpty(Question);

    /// <summary>
    /// Gets the Markdown answer received so far.
    /// </summary>
    [ObservableProperty] private string _answer = string.Empty;

    /// <summary>
    /// Gets or sets a value indicating whether the answer is still being written.
    /// </summary>
    [ObservableProperty] private bool _isInProgress = true;

    /// <summary>
    /// Gets or sets a value indicating whether the answer was cut off at the analyzer's length limit.
    /// </summary>
    [ObservableProperty] private bool _isTruncated;

    /// <summary>
    /// Gets or sets a value indicating whether the user stopped the answer before it finished.
    /// </summary>
    [ObservableProperty] private bool _wasStopped;

    /// <summary>
    /// Appends the next piece of the answer.
    /// </summary>
    /// <param name="text">The text to append.</param>
    public void Append(string text)
    {
        Answer += text;
    }
}
