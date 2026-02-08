using OSDPBench.Core.Services;
using ZBitSystems.Wpf.UI.Settings;

namespace OSDPBench.Windows.Services;

/// <summary>
/// Windows implementation of user settings using Guidelines' JsonUserSettingsService.
/// Wraps JsonUserSettingsService&lt;AppSettings&gt; and implements Core's IUserSettingsService.
/// </summary>
public class AppUserSettingsService : IUserSettingsService
{
    private readonly JsonUserSettingsService<AppSettings> _inner;

    /// <summary>
    /// Initializes a new instance of the AppUserSettingsService
    /// </summary>
    public AppUserSettingsService()
    {
        _inner = new JsonUserSettingsService<AppSettings>("OSDPBench");
    }

    /// <summary>
    /// Gets the current application settings (includes window state and app-specific properties)
    /// </summary>
    public AppSettings Settings => _inner.Settings;

    /// <inheritdoc />
    public string PreferredCulture => _inner.Settings.PreferredCulture;

    /// <inheritdoc />
    public bool SkipLanguageMismatchCheck => _inner.Settings.SkipLanguageMismatchCheck;

    /// <inheritdoc />
    public async Task UpdatePreferredCultureAsync(string cultureName)
    {
        _inner.Settings.PreferredCulture = cultureName;
        await _inner.SaveAsync();
    }

    /// <inheritdoc />
    public async Task UpdateSkipLanguageMismatchCheckAsync(bool skip)
    {
        _inner.Settings.SkipLanguageMismatchCheck = skip;
        await _inner.SaveAsync();
    }

    /// <summary>
    /// Saves the current settings to storage
    /// </summary>
    /// <returns>Task representing the async operation</returns>
    public async Task SaveAsync()
    {
        await _inner.SaveAsync();
    }
}
