using System.Globalization;

namespace OSDPBench.Core.Services;

/// <summary>
/// Default implementation of the localization service
/// </summary>
public class LocalizationService : ILocalizationService
{
    private readonly IUserSettingsService? _userSettingsService;
    private CultureInfo _currentCulture;
    
    /// <summary>
    /// Initializes a new instance of the LocalizationService
    /// </summary>
    /// <param name="userSettingsService">Optional user settings service for persistence</param>
    public LocalizationService(IUserSettingsService? userSettingsService)
    {
        _userSettingsService = userSettingsService;
        
        // Initialize culture from settings or system default
        if (_userSettingsService?.PreferredCulture != null)
        {
            try
            {
                _currentCulture = new CultureInfo(_userSettingsService.PreferredCulture);
            }
            catch
            {
                _currentCulture = CultureInfo.CurrentUICulture;
            }
        }
        else
        {
            _currentCulture = CultureInfo.CurrentUICulture;
        }
        
        // Initialize supported cultures - start with English, more can be added later
        SupportedCultures = new List<CultureInfo>
        {
            new("en-US"), // English (United States)
            new("es-ES"), // Spanish (Spain)
            new("fr-FR"), // French (France)
            new("de-DE"), // German (Germany)
            new("ja-JP"), // Japanese (Japan)
            new("zh-CN")  // Chinese (Simplified)
        }.AsReadOnly();
    }
    
    /// <inheritdoc />
    public CultureInfo CurrentCulture
    {
        get => _currentCulture;
        set
        {
            if (_currentCulture.Equals(value)) return;
            
            ChangeCulture(value);
        }
    }
    
    /// <inheritdoc />
    public IReadOnlyList<CultureInfo> SupportedCultures { get; }
    
    /// <inheritdoc />
    public event EventHandler<CultureInfo>? CultureChanged;
    
    /// <inheritdoc />
    public string GetString(string key)
    {
        return Resources.Resources.GetString(key);
    }
    
    /// <inheritdoc />
    public string GetString(string key, params object[] args)
    {
        try
        {
            var format = GetString(key);
            return string.Format(_currentCulture, format, args);
        }
        catch
        {
            return $"[{key}]"; // Return key in brackets on error
        }
    }
    
    /// <inheritdoc />
    public void ChangeCulture(CultureInfo culture)
    {
        if (_currentCulture.Equals(culture)) return;
        
        _currentCulture = culture;
        
        // Update system culture
        CultureInfo.CurrentUICulture = culture;
        CultureInfo.CurrentCulture = culture;
        
        // Update the Resources class culture (this will trigger PropertyChanged)
        Resources.Resources.ChangeCulture(culture);
        
        // Save preference to settings
        if (_userSettingsService != null)
        {
            _ = Task.Run(async () =>
            {
                await _userSettingsService.UpdatePreferredCultureAsync(culture.Name);
            });
        }
        
        // Notify our own listeners
        CultureChanged?.Invoke(this, culture);
    }
    
    /// <inheritdoc />
    public void ChangeCulture(string cultureName)
    {
        try
        {
            var culture = new CultureInfo(cultureName);
            ChangeCulture(culture);
        }
        catch (CultureNotFoundException)
        {
            // If culture not found, fall back to English
            ChangeCulture(new CultureInfo("en-US"));
        }
    }
    
    /// <inheritdoc />
    public string GetCultureDisplayName(CultureInfo culture)
    {
        // Return the native name of the culture
        return culture.NativeName;
    }
    
    /// <inheritdoc />
    public bool IsSystemLanguageMatch()
    {
        var systemCulture = GetSystemCulture();
        
        // If system language is not supported, consider it a match (no need to prompt)
        if (!IsCultureSupported(systemCulture))
            return true;
        
        // Check if cultures are exactly the same
        if (_currentCulture.Name == systemCulture.Name)
            return true;
            
        // Check if the base languages match (e.g., "en-US" matches "en-GB")
        if (_currentCulture.TwoLetterISOLanguageName == systemCulture.TwoLetterISOLanguageName)
            return true;
            
        return false;
    }
    
    /// <inheritdoc />
    public CultureInfo GetSystemCulture()
    {
        // Get the current system UI culture
        // This represents the OS display language
        return CultureInfo.InstalledUICulture;
    }
    
    /// <inheritdoc />
    public bool IsCultureSupported(CultureInfo culture)
    {
        return SupportedCultures.Any(c => 
            c.Name == culture.Name || 
            c.TwoLetterISOLanguageName == culture.TwoLetterISOLanguageName);
    }
}