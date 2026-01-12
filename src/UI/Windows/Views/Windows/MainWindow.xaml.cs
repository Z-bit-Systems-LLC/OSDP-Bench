using System.Windows;
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
    // ReSharper disable once UnusedAutoPropertyAccessor.Global
    public MainWindowViewModel ViewModel { get; }
        
    public MainWindow(MainWindowViewModel viewModel,
        INavigationViewPageProvider pageService,
        INavigationService navigationService)
    {
        ViewModel = viewModel;
        DataContext = this;

        SystemThemeWatcher.Watch(this);
            
        InitializeComponent();
        
        // Set window size based on screen dimensions
        var workArea = SystemParameters.WorkArea;
    
        // Use 80% of the screen size or 800px, whichever is smaller
        this.Width = Math.Min(800, workArea.Width * 0.8);
        this.Height = Math.Min(800, workArea.Height * 0.8);

        SetPageService(pageService);

        navigationService.SetNavigationControl(RootNavigation);
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
}