namespace OSDPBench.Core.Models;

/// <summary>
/// The Line Quality page settings that survive a restart, so a technician working through a run of
/// wiring drops sets up the page once rather than once per line.
/// </summary>
/// <remarks>
/// Deliberately split from the per-line details that must not be carried silently: the notes belong
/// to the measurement that was just taken and are cleared when the next run starts. The location and
/// cable are kept because the next drop is usually described the same way as the last one, and the
/// page offers an explicit way to clear them.
/// </remarks>
public class LineQualityUserSettings
{
    /// <summary>
    /// Gets or sets the name of the last selected test profile.
    /// </summary>
    /// <remarks>
    /// Stored by name rather than by its numeric value so that adding or reordering a profile in the
    /// library cannot silently turn a saved Extended run into a Screening one.
    /// </remarks>
    public string? Profile { get; set; }

    /// <summary>
    /// Gets or sets the baud rates that were included in the last sweep, or null when none has been saved.
    /// </summary>
    public int[]? BaudRates { get; set; }

    /// <summary>
    /// Gets or sets the last responder address, or null when none has been saved.
    /// </summary>
    /// <remarks>
    /// Nullable because address zero is a legal address, so a plain default cannot be told apart
    /// from an address the technician actually chose.
    /// </remarks>
    public byte? Address { get; set; }

    /// <summary>
    /// Gets or sets whether the page was last used in controller mode rather than responder mode.
    /// </summary>
    public bool IsControllerMode { get; set; } = true;

    /// <summary>Gets or sets the last tester name.</summary>
    public string? TesterName { get; set; }

    /// <summary>Gets or sets the last installation location.</summary>
    public string? InstallationLocation { get; set; }

    /// <summary>Gets or sets the last cable type and length description.</summary>
    public string? CableDescription { get; set; }

    /// <summary>
    /// Gets or sets where the last report was saved, so the next one opens in the same place.
    /// </summary>
    /// <remarks>
    /// A job produces one report per drop, all belonging in one folder. Opening the destination
    /// dialog at its default each time makes the technician walk the same path back on every save.
    /// The value is whatever the dialog handed back and is passed straight to it again, so it is
    /// not necessarily a file system path.
    /// </remarks>
    public string? ReportDestination { get; set; }
}
