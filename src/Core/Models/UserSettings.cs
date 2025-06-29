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
    public double WindowWidth { get; set; } = 800;
    
    /// <summary>
    /// Gets or sets the window height
    /// </summary>
    public double WindowHeight { get; set; } = 600;
    
    /// <summary>
    /// Gets or sets whether the window is maximized
    /// </summary>
    public bool IsMaximized { get; set; } = false;
}