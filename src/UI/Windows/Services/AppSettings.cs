using ZBitSystems.Wpf.UI.Settings;

namespace OSDPBench.Windows.Services;

/// <summary>
/// Application-specific settings extending Guidelines' UserSettings base class.
/// Inherits window state properties (position, size, maximized) and IWindowStateStorage implementation.
/// </summary>
public class AppSettings : UserSettings
{
    /// <summary>
    /// Gets or sets the user's preferred culture/language
    /// </summary>
    public string PreferredCulture { get; set; } = "en-US";

    /// <summary>
    /// Gets or sets whether to skip language mismatch checking
    /// </summary>
    public bool SkipLanguageMismatchCheck { get; set; }
}
