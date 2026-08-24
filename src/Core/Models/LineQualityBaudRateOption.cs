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
    /// Gets or sets a value indicating whether the rate is included in the sweep.
    /// </summary>
    [ObservableProperty] private bool _isSelected;
}
