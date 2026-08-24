using CommunityToolkit.Mvvm.ComponentModel;
using OSDPBench.Core.Models;
using OSDPBench.Core.Services;

namespace OSDPBench.Core.ViewModels.Windows;

/// <summary>
/// Represents the view model for the main window of the application.
/// </summary>
/// <remarks>
/// Owns the rules about which pages can be reached. Only one thing at a time may hold the serial
/// port: the polling bus behind a normal connection, or the line quality test, which drives the
/// port directly. Rather than letting the user walk into that collision and meet an error, the
/// shell simply closes the door that is not available.
/// </remarks>
public partial class MainWindowViewModel : ObservableObject, IDisposable
{
    private readonly IDeviceManagementService _deviceManagementService;
    private readonly ILineQualityService _lineQualityService;
    private bool _isDisposed;

    /// <summary>
    /// Gets the language selection view model
    /// </summary>
    public LanguageSelectionViewModel LanguageViewModel { get; }

    /// <summary>
    /// Initializes a new instance of the MainWindowViewModel
    /// </summary>
    /// <param name="localizationService">The localization service</param>
    /// <param name="deviceManagementService">Reports whether a device connection holds the port</param>
    /// <param name="lineQualityService">Reports whether a line quality run holds the port</param>
    public MainWindowViewModel(ILocalizationService localizationService,
        IDeviceManagementService deviceManagementService,
        ILineQualityService lineQualityService)
    {
        _deviceManagementService = deviceManagementService ??
                                   throw new ArgumentNullException(nameof(deviceManagementService));
        _lineQualityService = lineQualityService ??
                              throw new ArgumentNullException(nameof(lineQualityService));

        LanguageViewModel = new LanguageSelectionViewModel(localizationService);

        _deviceManagementService.ConnectionStatusChange += OnConnectionStatusChange;
        _lineQualityService.BusyChanged += OnLineQualityBusyChanged;

        UpdateNavigationState();
    }

    /// <summary>
    /// Gets a value indicating whether the pages other than Line Quality can be reached.
    /// </summary>
    /// <remarks>
    /// False while a line quality run holds the port. A run cannot be paused, and navigating away
    /// mid-sweep would leave a responder stranded at whatever rate it was last moved to, reachable
    /// only through its idle revert.
    /// </remarks>
    [ObservableProperty] private bool _isNavigationEnabled = true;

    /// <summary>
    /// Gets a value indicating whether the Line Quality page can be reached.
    /// </summary>
    /// <remarks>
    /// False while a device connection or passive monitoring session holds the port, because the
    /// line quality test needs it to itself.
    /// </remarks>
    [ObservableProperty] private bool _isLineQualityEnabled = true;

    /// <summary>
    /// Gets an explanation of why the other pages are unavailable, or null when they are not.
    /// </summary>
    public string? NavigationDisabledReason => IsNavigationEnabled
        ? null
        : Resources.Resources.GetString("Navigation_BusyWithLineQuality");

    /// <summary>
    /// Gets an explanation of why the Line Quality page is unavailable, or null when it is not.
    /// </summary>
    public string? LineQualityDisabledReason => IsLineQualityEnabled
        ? null
        : Resources.Resources.GetString("Navigation_LineQualityNeedsPort");

    private void OnConnectionStatusChange(object? sender, ConnectionStatus status)
    {
        // The status enum is not consulted: the service is the authority on whether it still holds
        // the port, and re-reading it avoids having to keep a second copy of that state in step.
        _ = status;
        UpdateNavigationState();
    }

    private void OnLineQualityBusyChanged(object? sender, EventArgs args) => UpdateNavigationState();

    private void UpdateNavigationState()
    {
        IsNavigationEnabled = !_lineQualityService.IsBusy;
        IsLineQualityEnabled = !_deviceManagementService.IsConnected &&
                               !_deviceManagementService.IsPassiveMonitoring;
    }

    partial void OnIsNavigationEnabledChanged(bool value)
    {
        _ = value;
        OnPropertyChanged(nameof(NavigationDisabledReason));
    }

    partial void OnIsLineQualityEnabledChanged(bool value)
    {
        _ = value;
        OnPropertyChanged(nameof(LineQualityDisabledReason));
    }

    /// <inheritdoc />
    public void Dispose()
    {
        if (_isDisposed) return;
        _isDisposed = true;

        _deviceManagementService.ConnectionStatusChange -= OnConnectionStatusChange;
        _lineQualityService.BusyChanged -= OnLineQualityBusyChanged;

        GC.SuppressFinalize(this);
    }
}
