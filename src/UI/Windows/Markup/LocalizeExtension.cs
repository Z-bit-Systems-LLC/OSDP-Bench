using System.Globalization;
using System.Windows.Markup;
using OSDPBench.Core.Resources;

namespace OSDPBench.Windows.Markup;

/// <summary>
/// Markup extension for accessing localized resources in XAML
/// </summary>
public class LocalizeExtension : MarkupExtension
{
    /// <summary>
    /// Gets or sets the resource key
    /// </summary>
    public string Key { get; set; } = string.Empty;

    /// <summary>
    /// Initializes a new instance of the LocalizeExtension
    /// </summary>
    public LocalizeExtension()
    {
    }

    /// <summary>
    /// Initializes a new instance of the LocalizeExtension with a key
    /// </summary>
    /// <param name="key">The resource key</param>
    public LocalizeExtension(string key)
    {
        Key = key;
    }

    /// <summary>
    /// Provides the localized value
    /// </summary>
    /// <param name="serviceProvider">The service provider</param>
    /// <returns>The localized string value</returns>
    public override object ProvideValue(IServiceProvider serviceProvider)
    {
        if (string.IsNullOrEmpty(Key))
            return "[MISSING_KEY]";

        try
        {
            // Use the Resources class to get the localized string
            return Resources.GetString(Key);
        }
        catch
        {
            // Return the key in brackets if there's an error
            return $"[{Key}]";
        }
    }
}