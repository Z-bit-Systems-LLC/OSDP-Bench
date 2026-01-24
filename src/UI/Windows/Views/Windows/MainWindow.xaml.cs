using System.Windows;
using OSDPBench.Core.Services;
using OSDPBench.Core.ViewModels.Windows;
using Wpf.Ui;
using Wpf.Ui.Abstractions;
using Wpf.Ui.Appearance;
using Wpf.Ui.Controls;

namespace OSDPBench.Windows.Views.Windows;

/// <summary>
/// Interaction logic for MainWindow.xaml
/// </summary>
public partial class MainWindow : INavigationWindow
{
    private readonly IUserSettingsService _userSettingsService;

    // ReSharper disable once UnusedAutoPropertyAccessor.Global
    public MainWindowViewModel ViewModel { get; }

    public MainWindow(MainWindowViewModel viewModel,
        INavigationViewPageProvider pageService,
        INavigationService navigationService,
        IUserSettingsService userSettingsService)
    {
        _userSettingsService = userSettingsService;
        ViewModel = viewModel;
        DataContext = this;

        // Register for system theme changes after the window is fully loaded to ensure
        // the window handle is available. See: https://github.com/lepoco/wpfui/issues/1193
        SystemThemeWatcher.Watch(this);

        InitializeComponent();

        RestoreWindowPosition();

        SetPageService(pageService);

        navigationService.SetNavigationControl(RootNavigation);

        Closing += MainWindow_Closing;
    }

    private void RestoreWindowPosition()
    {
        var settings = _userSettingsService.Settings;
        var workArea = SystemParameters.WorkArea;

        // Restore size, clamped to fit within the primary monitor's work area
        Width = Math.Clamp(settings.WindowWidth, 400, workArea.Width);
        Height = Math.Clamp(settings.WindowHeight, 300, workArea.Height);

        // Restore position or center on screen
        if (settings.WindowLeft.HasValue && settings.WindowTop.HasValue)
        {
            var left = settings.WindowLeft.Value;
            var top = settings.WindowTop.Value;

            // Check if the saved position is within the virtual screen (all monitors)
            var virtualLeft = SystemParameters.VirtualScreenLeft;
            var virtualTop = SystemParameters.VirtualScreenTop;
            var virtualRight = virtualLeft + SystemParameters.VirtualScreenWidth;
            var virtualBottom = virtualTop + SystemParameters.VirtualScreenHeight;

            // Clamp position to ensure entire window is visible within virtual screen bounds
            left = Math.Clamp(left, virtualLeft, virtualRight - Width);
            top = Math.Clamp(top, virtualTop, virtualBottom - Height);

            // Additional check: ensure the window is reasonably accessible
            // (not hidden behind taskbar on primary monitor if that's where it ends up)
            if (left >= workArea.Left && left + Width <= workArea.Right &&
                top >= workArea.Top && top + Height <= workArea.Bottom)
            {
                // Window fits entirely within primary work area
                Left = left;
                Top = top;
            }
            else if (left >= virtualLeft && left + Width <= virtualRight &&
                     top >= virtualTop && top + Height <= virtualBottom)
            {
                // Window is on another monitor or partially outside work area but within virtual screen
                Left = left;
                Top = top;
            }
            else
            {
                // Window would be off-screen, center on primary monitor
                CenterOnWorkArea(workArea);
            }
        }
        else
        {
            CenterOnWorkArea(workArea);
        }

        // Restore maximized state after setting position
        if (settings.IsMaximized)
        {
            WindowState = WindowState.Maximized;
        }
    }

    private void CenterOnWorkArea(Rect workArea)
    {
        Left = workArea.Left + (workArea.Width - Width) / 2;
        Top = workArea.Top + (workArea.Height - Height) / 2;
    }

    private async void MainWindow_Closing(object? sender, System.ComponentModel.CancelEventArgs e)
    {
        try
        {
            await SaveWindowPosition();
        }
        catch
        {
            // ignored
        }
    }

    private async Task SaveWindowPosition()
    {
        _userSettingsService.Settings.IsMaximized = WindowState == WindowState.Maximized;

        // Save normal bounds (not maximized bounds)
        if (WindowState == WindowState.Normal)
        {
            _userSettingsService.Settings.WindowWidth = Width;
            _userSettingsService.Settings.WindowHeight = Height;
            _userSettingsService.Settings.WindowLeft = Left;
            _userSettingsService.Settings.WindowTop = Top;
        }

        // Wait for save to complete before app exits
        await _userSettingsService.SaveAsync();
    }
        
    /// <inheritdoc />
    public INavigationView GetNavigation() => RootNavigation;


    /// <inheritdoc />
    public bool Navigate(Type pageType) => RootNavigation.Navigate(pageType);
        
    /// <inheritdoc />
    public void SetServiceProvider(IServiceProvider serviceProvider)
    {
    }

    /// <inheritdoc />
    public void SetPageService(INavigationViewPageProvider navigationViewPageProvider)
    {
        RootNavigation.SetPageProviderService(navigationViewPageProvider);
    }

    public void ShowWindow() => Show();

    public void CloseWindow() => Close();

    /// <summary>
    /// Forces visual refresh when moving between monitors with different DPI settings.
    /// Fixes rendering glitches that occur during DPI transitions.
    /// </summary>
    protected override void OnDpiChanged(DpiScale oldDpi, DpiScale newDpi)
    {
        base.OnDpiChanged(oldDpi, newDpi);

        // Force the window and its content to re-render
        InvalidateVisual();

        if (Content is FrameworkElement content)
        {
            content.InvalidateMeasure();
            content.InvalidateArrange();
        }
    }
}