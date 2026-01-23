using System.IO;
using System.Runtime.InteropServices;
using System.Text.Json;
using OSDPBench.Core.Models;
using OSDPBench.Core.Services;

namespace OSDPBench.Windows.Services;

/// <summary>
/// Windows implementation of a user settings service using JSON file storage
/// </summary>
public class WindowsUserSettingsService : IUserSettingsService
{
    private const string SettingsFileName = "settings.json";
    private readonly string _settingsFilePath;
    private UserSettings _settings;

    /// <summary>
    /// Initializes a new instance of the WindowsUserSettingsService
    /// </summary>
    public WindowsUserSettingsService()
    {
        var appFolderPath = GetSettingsFolderPath();
        Directory.CreateDirectory(appFolderPath);
        _settingsFilePath = Path.Combine(appFolderPath, SettingsFileName);
        _settings = LoadSettingsFromFile();
    }

    private UserSettings LoadSettingsFromFile()
    {
        try
        {
            if (File.Exists(_settingsFilePath))
            {
                var json = File.ReadAllText(_settingsFilePath);
                var loadedSettings = JsonSerializer.Deserialize<UserSettings>(json);
                if (loadedSettings != null)
                {
                    return loadedSettings;
                }
            }
        }
        catch
        {
            // If loading fails, use default settings
        }

        return new UserSettings();
    }

    private static string GetSettingsFolderPath()
    {
        if (IsPackagedApp())
        {
            // For Microsoft Store packaged apps, use the app's local folder
            // which is automatically managed and cleaned up with the app
            var localAppData = Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData);
            return localAppData;
        }

        // For unpackaged apps, use traditional AppData location with app subfolder
        var appDataPath = Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData);
        return Path.Combine(appDataPath, "OSDPBench");
    }

    private static bool IsPackagedApp()
    {
        // Check if running as a packaged app using the kernel32 API
        var length = 0u;
        var result = GetCurrentPackageFullName(ref length, null);
        return result != AppmodelErrorNoPackage;
    }

    private const int AppmodelErrorNoPackage = 15700;

    [DllImport("kernel32.dll", CharSet = CharSet.Unicode, SetLastError = false)]
    private static extern int GetCurrentPackageFullName(ref uint packageFullNameLength, char[]? packageFullName);
    
    /// <inheritdoc />
    public UserSettings Settings => _settings;

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
    /// Updates a setting and saves
    /// </summary>
    /// <param name="updateAction">Action to update the settings</param>
    public async Task UpdateSettingsAsync(Action<UserSettings> updateAction)
    {
        updateAction(_settings);
        await SaveAsync();
    }
}   