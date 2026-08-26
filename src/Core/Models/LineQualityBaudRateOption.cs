using CommunityToolkit.Mvvm.ComponentModel;

namespace OSDPBench.Core.Models;

/// <summary>
/// A baud rate offered for a line quality sweep, and whether the user has chosen to include it.
/// </summary>
public partial class LineQualityBaudRateOption : ObservableObject
{
    /// <summary>
    /// Initializes an option for a baud rate.
    /// </summary>
    /// <param name="baudRate">The baud rate this option selects.</param>
    /// <param name="isSelected">Whether the rate starts out included in the sweep.</param>
    public LineQualityBaudRateOption(int baudRate, bool isSelected = true)
    {
        BaudRate = baudRate;
        IsSelected = isSelected;
    }

    /// <summary>
    /// Gets the baud rate this option selects.
    /// </summary>
    public int BaudRate { get; }

    /// <summary>
    /// Gets the localized name a screen reader announces for the option, because the visible
    /// label is a bare number that does not say what it sets.
    /// </summary>
    public string AccessibleName => Resources.Resources.GetString("LineQuality_BaudRateAccessibleName")
        .Replace("{0}", BaudRate.ToString());

    /// <summary>
    /// Gets the localized name a screen reader announces for the affordance that drops this rate
    /// from the sweep, because every tag carries the same bare remove icon.
    /// </summary>
    public string RemoveAccessibleName => Resources.Resources.GetString("LineQuality_RemoveBaudRate")
        .Replace("{0}", BaudRate.ToString());

    /// <summary>
    /// Gets or sets a value indicating whether the rate is included in the sweep.
    /// </summary>
    [ObservableProperty] private bool _isSelected;
}
