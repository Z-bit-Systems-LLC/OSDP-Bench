using OSDP.Net.LineQuality;

namespace OSDPBench.Core.Models;

/// <summary>
/// A line quality test profile offered to the user, with the detection limit it buys.
/// </summary>
/// <remarks>
/// The profile sets how many packets each pattern and payload combination sends, and therefore the
/// smallest packet loss rate the run can rule out. A pass under Screening says only that loss is
/// below roughly 1.9%, which is a very different claim from a commissioning result, so the limit
/// is carried alongside the name wherever the profile is shown.
/// </remarks>
public class LineQualityProfileOption
{
    /// <summary>
    /// Initializes an option for a profile.
    /// </summary>
    /// <param name="profile">The profile this option selects.</param>
    public LineQualityProfileOption(TestProfile profile)
    {
        Profile = profile;
    }

    /// <summary>
    /// Gets the profile this option selects.
    /// </summary>
    public TestProfile Profile { get; }

    /// <summary>
    /// The number of pattern and payload size combinations the test matrix exercises at each
    /// baud rate.
    /// </summary>
    /// <remarks>
    /// Not the full cross product of six patterns and three lengths: the zero-length case is only
    /// meaningful for the four constant patterns, because sequential and walking-one produce no
    /// bytes at all at that length. That gives 4 x 3 + 2 x 2 = 16, matching section 3.10. The
    /// library builds the matrix privately, so the count is restated here rather than derived.
    /// </remarks>
    private const int CombinationsPerBaudRate = 16;

    /// <summary>
    /// Gets the number of packets the profile sends at each baud rate.
    /// </summary>
    public int PacketsPerBaudRate =>
        LineQualityProtocol.IterationsPerCombination(Profile) * CombinationsPerBaudRate;

    /// <summary>
    /// Gets the smallest packet loss rate the profile can rule out, as a percentage.
    /// </summary>
    public double DetectionLimitPercent => LineQualityProtocol.DetectionLimitPercent(PacketsPerBaudRate);

    /// <summary>
    /// Gets the localized name of the profile.
    /// </summary>
    public string Name => Resources.Resources.GetString($"LineQuality_Profile_{Profile}");

    /// <summary>
    /// Gets a localized summary of what the profile costs and what it proves.
    /// </summary>
    public string Description => Resources.Resources.GetString("LineQuality_ProfileDetail")
        .Replace("{0}", PacketsPerBaudRate.ToString())
        .Replace("{1}", DetectionLimitPercent.ToString("0.###"));
}
