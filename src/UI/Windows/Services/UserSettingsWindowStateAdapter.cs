using OSDPBench.Core.Models;
using ZBitSystems.Wpf.UI.Services;

namespace OSDPBench.Windows.Services;

/// <summary>
/// Adapter that allows UserSettings to work with WindowStateManager.
/// Keeps the Core project UI-independent by providing the interface implementation in the Windows project.
/// </summary>
public class UserSettingsWindowStateAdapter : IWindowStateStorage
{
    private readonly UserSettings _settings;

    /// <summary>
    /// Initializes a new instance of the UserSettingsWindowStateAdapter.
    /// </summary>
    /// <param name="settings">The user settings to adapt.</param>
    public UserSettingsWindowStateAdapter(UserSettings settings)
    {
        _settings = settings ?? throw new ArgumentNullException(nameof(settings));
    }

    /// <inheritdoc />
    public double WindowWidth
    {
        get => _settings.WindowWidth;
        set => _settings.WindowWidth = value;
    }

    /// <inheritdoc />
    public double WindowHeight
    {
        get => _settings.WindowHeight;
        set => _settings.WindowHeight = value;
    }

    /// <inheritdoc />
    public double? WindowLeft
    {
        get => _settings.WindowLeft;
        set => _settings.WindowLeft = value;
    }

    /// <inheritdoc />
    public double? WindowTop
    {
        get => _settings.WindowTop;
        set => _settings.WindowTop = value;
    }

    /// <inheritdoc />
    public bool IsMaximized
    {
        get => _settings.IsMaximized;
        set => _settings.IsMaximized = value;
    }
}
