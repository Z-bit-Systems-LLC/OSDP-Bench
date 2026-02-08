using System.ComponentModel;
using System.Globalization;
using ZBitSystems.Wpf.UI.Localization;

namespace OSDPBench.Windows.Services;

/// <summary>
/// Adapts Core's static Resources class to the Guidelines ILocalizationProvider interface
/// </summary>
public class ResourceLocalizationProvider : ILocalizationProvider
{
    public ResourceLocalizationProvider()
    {
        Core.Resources.Resources.PropertyChanged += OnResourcesPropertyChanged;
    }

    /// <inheritdoc />
    public string GetString(string key)
    {
        return Core.Resources.Resources.GetString(key);
    }

    /// <inheritdoc />
    public string CurrentCulture =>
        Core.Resources.Resources.Culture?.Name ?? CultureInfo.CurrentUICulture.Name;

    private void OnResourcesPropertyChanged(object? sender, PropertyChangedEventArgs e)
    {
        PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(string.Empty));
    }

    /// <inheritdoc />
    public event PropertyChangedEventHandler? PropertyChanged;
}
