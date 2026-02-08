using System.Windows;
using OSDPBench.Core.ViewModels.Windows;
using OSDPBench.Windows.Services;
using Wpf.Ui;
using Wpf.Ui.Abstractions;
using Wpf.Ui.Appearance;
using Wpf.Ui.Controls;
using ZBitSystems.Wpf.UI.Services;

namespace OSDPBench.Windows.Views.Windows;

/// <summary>
/// Interaction logic for MainWindow.xaml
/// </summary>
public partial class MainWindow : INavigationWindow
{
    private readonly AppUserSettingsService _settingsService;
    private readonly WindowStateManager _windowStateManager;

    // ReSharper disable once UnusedAutoPropertyAccessor.Global
    public MainWindowViewModel ViewModel { get; }

    public MainWindow(MainWindowViewModel viewModel,
        INavigationViewPageProvider pageService,
        INavigationService navigationService,
        AppUserSettingsService settingsService)
    {
        _settingsService = settingsService;
        _windowStateManager = new WindowStateManager(this, settingsService.Settings);
        ViewModel = viewModel;
        DataContext = this;

        // Register for system theme changes after the window is fully loaded to ensure
        // the window handle is available. See: https://github.com/lepoco/wpfui/issues/1193
        SystemThemeWatcher.Watch(this);

        InitializeComponent();

        _windowStateManager.RestoreWindowState();

        SetPageService(pageService);

        navigationService.SetNavigationControl(RootNavigation);

        Closing += MainWindow_Closing;
    }

    private async void MainWindow_Closing(object? sender, System.ComponentModel.CancelEventArgs e)
    {
        try
        {
            _windowStateManager.SaveWindowState();
            await _settingsService.SaveAsync();
        }
        catch
        {
            // ignored
        }
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

    /// <inheritdoc />
    protected override void OnDpiChanged(DpiScale oldDpi, DpiScale newDpi)
    {
        base.OnDpiChanged(oldDpi, newDpi);
        _windowStateManager.HandleDpiChanged();
    }
}
