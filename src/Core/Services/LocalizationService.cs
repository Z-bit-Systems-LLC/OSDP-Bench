using System.Globalization;
using System.Resources;

namespace OSDPBench.Core.Services;

/// <summary>
/// Default implementation of the localization service
/// </summary>
public class LocalizationService : ILocalizationService
{
    private readonly ResourceManager _resourceManager;
    private CultureInfo _currentCulture;
    
    /// <summary>
    /// Initializes a new instance of the LocalizationService
    /// </summary>
    public LocalizationService()
    {
        _resourceManager = new ResourceManager("OSDPBench.Core.Resources.Resources", typeof(LocalizationService).Assembly);
        _currentCulture = CultureInfo.CurrentUICulture;
        
        // Initialize supported cultures - start with just English, more can be added later
        SupportedCultures = new List<CultureInfo>
        {
            new("en-US"), // English (United States)
            new("en-GB")  // English (United Kingdom)
        }.AsReadOnly();
    }
    
    /// <inheritdoc />
    public CultureInfo CurrentCulture
    {
        get => _currentCulture;
        set
        {
            if (_currentCulture.Equals(value)) return;
            
            _currentCulture = value;
            CultureInfo.CurrentUICulture = value;
            CultureInfo.CurrentCulture = value;
            
            CultureChanged?.Invoke(this, value);
        }
    }
    
    /// <inheritdoc />
    public IReadOnlyList<CultureInfo> SupportedCultures { get; }
    
    /// <inheritdoc />
    public event EventHandler<CultureInfo>? CultureChanged;
    
    /// <inheritdoc />
    public string GetString(string key)
    {
        try
        {
            var value = _resourceManager.GetString(key, _currentCulture);
            return value ?? $"[{key}]"; // Return key in brackets if not found
        }
        catch
        {
            return $"[{key}]"; // Return key in brackets on error
        }
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
}