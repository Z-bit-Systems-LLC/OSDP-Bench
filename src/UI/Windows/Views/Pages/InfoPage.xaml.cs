using OSDPBench.Core.ViewModels.Windows;
using System.ComponentModel;
using Wpf.Ui.Abstractions.Controls;
using ZBitSystems.Wpf.UI.Helpers;
using ZBitSystems.Wpf.UI.Services;

namespace OSDPBench.Windows.Views.Pages;

/// <summary>
/// Interaction logic for InfoPage.xaml
/// </summary>
public sealed partial class InfoPage : INavigableView<MainWindowViewModel>, INotifyPropertyChanged, IDisposable
{
    private readonly ThemeManager _themeManager;

    public InfoPage(MainWindowViewModel viewModel)
    {
        ViewModel = viewModel;
        DataContext = this;
        _themeManager = new ThemeManager();
        _themeManager.PropertyChanged += OnThemePropertyChanged;

        InitializeComponent();
    }

    public MainWindowViewModel ViewModel { get; }

    public string AppVersion => ApplicationInfoHelper.GetVersion();

    public string CopyWriteNotice => ApplicationInfoHelper.GetCopyright();

    public bool IsDarkMode => _themeManager.IsDarkMode;

    public event PropertyChangedEventHandler? PropertyChanged;

    private void OnPropertyChanged([System.Runtime.CompilerServices.CallerMemberName] string? propertyName = null)
    {
        PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
    }

    private void OnThemePropertyChanged(object? sender, PropertyChangedEventArgs e)
    {
        if (e.PropertyName == nameof(ThemeManager.IsDarkMode))
        {
            OnPropertyChanged(nameof(IsDarkMode));
        }
    }

    public void Dispose()
    {
        _themeManager.PropertyChanged -= OnThemePropertyChanged;
        _themeManager.Dispose();
    }
}
