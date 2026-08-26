using OSDPBench.Core.Models;
using ZBitSystems.Wpf.UI.Settings;

namespace OSDPBench.Windows.Services;

/// <summary>
/// Application-specific settings extending Guidelines' UserSettings base class.
/// Inherits window state properties (position, size, maximized) and IWindowStateStorage implementation.
/// </summary>
public class AppSettings : UserSettings
{
    /// <summary>
    /// Gets or sets the user's preferred culture/language
    /// </summary>
    public string PreferredCulture { get; set; } = "en-US";

    /// <summary>
    /// Gets or sets whether to skip language mismatch checking
    /// </summary>
    public bool SkipLanguageMismatchCheck { get; set; }

    /// <summary>
    /// Gets or sets the last selected serial port name, restored on the next launch when still present.
    /// </summary>
    public string? LastSerialPortName { get; set; }

    /// <summary>
    /// Gets or sets the last selected baud rate.
    /// </summary>
    public int LastBaudRate { get; set; } = 9600;

    /// <summary>
    /// Gets or sets the last selected device address.
    /// </summary>
    public byte LastAddress { get; set; }

    /// <summary>
    /// Gets or sets the Line Quality page settings carried over between launches.
    /// </summary>
    public LineQualityUserSettings LineQuality { get; set; } = new();
}
