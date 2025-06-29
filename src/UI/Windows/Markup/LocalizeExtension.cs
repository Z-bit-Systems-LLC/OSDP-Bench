using System.Globalization;
using System.Windows.Markup;
using System.Windows.Data;
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
    /// Provides the localized value or a binding if possible
    /// </summary>
    /// <param name="serviceProvider">The service provider</param>
    /// <returns>The localized string value or binding</returns>
    public override object ProvideValue(IServiceProvider serviceProvider)
    {
        if (string.IsNullOrEmpty(Key))
            return "[MISSING_KEY]";

        try
        {
            // Create a binding to the LocalizedStringBinding for dynamic updates
            var localizedBinding = new LocalizedStringBinding(Key);
            var binding = new Binding(nameof(LocalizedStringBinding.Value))
            {
                Source = localizedBinding,
                Mode = BindingMode.OneWay
            };
            
            return binding.ProvideValue(serviceProvider);
        }
        catch
        {
            // Fallback to static string if binding fails
            try
            {
                return Resources.GetString(Key);
            }
            catch
            {
                return $"[{Key}]";
            }
        }
    }
}