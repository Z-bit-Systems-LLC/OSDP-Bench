namespace OSDPBench.Core.Services;

/// <summary>
/// Service for managing user settings persistence
/// </summary>
public interface IUserSettingsService
{
    /// <summary>
    /// Gets the user's preferred culture/language
    /// </summary>
    string PreferredCulture { get; }

    /// <summary>
    /// Gets whether to skip language mismatch checking
    /// </summary>
    bool SkipLanguageMismatchCheck { get; }

    /// <summary>
    /// Updates the preferred culture and saves settings
    /// </summary>
    /// <param name="cultureName">The culture name to save</param>
    /// <returns>Task representing the async operation</returns>
    Task UpdatePreferredCultureAsync(string cultureName);

    /// <summary>
    /// Updates the skip language mismatch check preference and saves settings
    /// </summary>
    /// <param name="skip">Whether to skip language mismatch checking</param>
    /// <returns>Task representing the async operation</returns>
    Task UpdateSkipLanguageMismatchCheckAsync(bool skip);
}
