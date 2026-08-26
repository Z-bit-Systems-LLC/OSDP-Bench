namespace OSDPBench.Core.Models;

/// <summary>
/// Builds the file name a line quality report is saved under.
/// </summary>
/// <remarks>
/// A technician working a job saves one report per drop into a single folder. A name that carries
/// only a timestamp makes that folder unreadable without opening every file, so the location the
/// report describes is folded into the name when one was given.
/// </remarks>
public static class LineQualityReportFileName
{
    /// <summary>
    /// The longest run of the location that is kept, past which the name stops helping and starts
    /// fighting the path length limit.
    /// </summary>
    private const int MaximumLabelLength = 40;

    /// <summary>
    /// Builds the file name for a report.
    /// </summary>
    /// <param name="installationLocation">Where the line runs, or null or blank when not given.</param>
    /// <param name="timestamp">When the report was saved.</param>
    /// <returns>A file name that is safe on every supported platform.</returns>
    /// <remarks>
    /// Everything in the location that is not a letter or a digit becomes a separator, which covers
    /// both the characters a file system rejects and the spaces and punctuation that make a name
    /// awkward to type. A location that survives as nothing, because it was punctuation alone,
    /// falls back to the timestamp on its own.
    /// </remarks>
    public static string Build(string? installationLocation, DateTime timestamp) =>
        $"line-quality-{Label(installationLocation)}{timestamp:yyyyMMdd-HHmmss}.md";

    private static string Label(string? installationLocation)
    {
        if (string.IsNullOrWhiteSpace(installationLocation)) return string.Empty;

        var words = new string(installationLocation
                .Select(character => char.IsLetterOrDigit(character) ? character : '-').ToArray())
            .Split('-', StringSplitOptions.RemoveEmptyEntries);

        string label = string.Join('-', words);
        if (label.Length == 0) return string.Empty;

        return label.Length > MaximumLabelLength
            ? $"{label[..MaximumLabelLength].TrimEnd('-')}-"
            : $"{label}-";
    }
}
