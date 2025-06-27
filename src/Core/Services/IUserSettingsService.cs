using OSDPBench.Core.Models;

namespace OSDPBench.Core.Services;

/// <summary>
/// Service for managing user settings persistence
/// </summary>
public interface IUserSettingsService
{
    /// <summary>
    /// Gets the current user settings
    /// </summary>
    UserSettings Settings { get; }
    
    /// <summary>
    /// Loads user settings from storage
    /// </summary>
    /// <returns>Task representing the async operation</returns>
    Task LoadAsync();
    
    /// <summary>
    /// Saves user settings to storage
    /// </summary>
    /// <returns>Task representing the async operation</returns>
    Task SaveAsync();
    
    /// <summary>
    /// Event raised when settings are changed
    /// </summary>
    event EventHandler<UserSettings>? SettingsChanged;
    
    /// <summary>
    /// Updates settings using an action and saves them
    /// </summary>
    /// <param name="updateAction">Action to update the settings</param>
    /// <returns>Task representing the async operation</returns>
    Task UpdateSettingsAsync(Action<UserSettings> updateAction);
}