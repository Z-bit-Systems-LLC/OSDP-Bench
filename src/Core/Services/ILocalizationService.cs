using System.Globalization;

namespace OSDPBench.Core.Services;

/// <summary>
/// Service for managing localization and culture-specific formatting
/// </summary>
public interface ILocalizationService
{
    /// <summary>
    /// Gets or sets the current culture
    /// </summary>
    CultureInfo CurrentCulture { get; set; }
    
    /// <summary>
    /// Gets the list of supported cultures
    /// </summary>
    IReadOnlyList<CultureInfo> SupportedCultures { get; }
    
    /// <summary>
    /// Event raised when the current culture changes
    /// </summary>
    event EventHandler<CultureInfo>? CultureChanged;
    
    /// <summary>
    /// Gets a localized string by key
    /// </summary>
    /// <param name="key">The resource key</param>
    /// <returns>The localized string</returns>
    string GetString(string key);
    
    /// <summary>
    /// Gets a localized string by key with format arguments
    /// </summary>
    /// <param name="key">The resource key</param>
    /// <param name="args">Format arguments</param>
    /// <returns>The formatted localized string</returns>
    string GetString(string key, params object[] args);
    
    /// <summary>
    /// Changes the current culture and notifies all components
    /// </summary>
    /// <param name="culture">The new culture to set</param>
    void ChangeCulture(CultureInfo culture);
    
    /// <summary>
    /// Changes the current culture by culture name
    /// </summary>
    /// <param name="cultureName">The culture name (e.g., "en-US", "es-ES")</param>
    void ChangeCulture(string cultureName);
    
    /// <summary>
    /// Gets the display name for a culture in the current language
    /// </summary>
    /// <param name="culture">The culture to get the display name for</param>
    /// <returns>The localized display name</returns>
    string GetCultureDisplayName(CultureInfo culture);
}