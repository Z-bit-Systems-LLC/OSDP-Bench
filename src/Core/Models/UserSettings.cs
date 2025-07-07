namespace OSDPBench.Core.Models;

/// <summary>
/// Represents user settings that persist between application sessions
/// </summary>
public class UserSettings
{
    /// <summary>
    /// Gets or sets the user's preferred culture/language
    /// </summary>
    public string PreferredCulture { get; set; } = "en-US";
    
    /// <summary>
    /// Gets or sets the window width
    /// </summary>
    public double WindowWidth { get; init; } = 800;
    
    /// <summary>
    /// Gets or sets the window height
    /// </summary>
    public double WindowHeight { get; init; } = 600;
    
    /// <summary>
    /// Gets or sets whether the window is maximized
    /// </summary>
    public bool IsMaximized { get; init; }
    
    /// <summary>
    /// Gets or sets whether to skip language mismatch checking
    /// </summary>
    public bool SkipLanguageMismatchCheck { get; set; }
}