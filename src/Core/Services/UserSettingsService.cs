using System.Text.Json;
using OSDPBench.Core.Models;

namespace OSDPBench.Core.Services;

/// <summary>
/// Implementation of user settings service using JSON file storage
/// </summary>
public class UserSettingsService : IUserSettingsService
{
    private readonly string _settingsFilePath;
    private UserSettings _settings;
    
    /// <summary>
    /// Initializes a new instance of the UserSettingsService
    /// </summary>
    public UserSettingsService()
    {
        var appDataPath = Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData);
        var appFolderPath = Path.Combine(appDataPath, "OSDPBench");
        Directory.CreateDirectory(appFolderPath);
        _settingsFilePath = Path.Combine(appFolderPath, "settings.json");
        _settings = new UserSettings();
    }
    
    /// <inheritdoc />
    public UserSettings Settings => _settings;
    
    /// <inheritdoc />
    public event EventHandler<UserSettings>? SettingsChanged;
    
    /// <inheritdoc />
    public async Task LoadAsync()
    {
        try
        {
            if (File.Exists(_settingsFilePath))
            {
                var json = await File.ReadAllTextAsync(_settingsFilePath);
                var loadedSettings = JsonSerializer.Deserialize<UserSettings>(json);
                if (loadedSettings != null)
                {
                    _settings = loadedSettings;
                    SettingsChanged?.Invoke(this, _settings);
                }
            }
        }
        catch (Exception)
        {
            // If loading fails, use default settings
            _settings = new UserSettings();
        }
    }
    
    /// <inheritdoc />
    public async Task SaveAsync()
    {
        try
        {
            var json = JsonSerializer.Serialize(_settings, new JsonSerializerOptions 
            { 
                WriteIndented = true 
            });
            await File.WriteAllTextAsync(_settingsFilePath, json);
        }
        catch (Exception)
        {
            // Silently fail - settings will revert to defaults next time
        }
    }
    
    /// <summary>
    /// Updates a setting and notifies listeners
    /// </summary>
    /// <param name="updateAction">Action to update the settings</param>
    public async Task UpdateSettingsAsync(Action<UserSettings> updateAction)
    {
        updateAction(_settings);
        SettingsChanged?.Invoke(this, _settings);
        await SaveAsync();
    }
}