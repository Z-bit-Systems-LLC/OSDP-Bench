using OSDPBench.Core.ViewModels.Windows;
using System.ComponentModel;
using System.IO;
using System.Windows;
using Wpf.Ui.Abstractions.Controls;
using ZBitSystems.Wpf.UI.Services;

namespace OSDPBench.Windows.Views.Pages;

/// <summary>
/// Interaction logic for InfoPage.xaml
/// </summary>
public sealed partial class InfoPage : INavigableView<MainWindowViewModel>, INotifyPropertyChanged, IDisposable
{
    const string EplFilePath = "pack://application:,,,/Assets/EPL.txt";
    const string ApacheFilePath = "pack://application:,,,/Assets/Apache.txt";
    const string MitFilePath = "pack://application:,,,/Assets/MIT.txt";

    private readonly ThemeManager _themeManager;

    public InfoPage(MainWindowViewModel viewModel)
    {
        ViewModel = viewModel;
        DataContext = this;
        _themeManager = new ThemeManager();
        _themeManager.PropertyChanged += OnThemePropertyChanged;

        InitializeComponent();
            
        Loaded += (_, _) =>
        {
            var info = Application.GetResourceStream(new Uri(EplFilePath));
            if (info != null)
            {
                using StreamReader reader = new(info.Stream);
                EplLicenseTextBlock.Text = reader.ReadToEnd();
            }
                
            info = Application.GetResourceStream(new Uri(ApacheFilePath));
            if (info != null)
            {
                using StreamReader reader = new(info.Stream);
                ApacheLicenseTextBlock.Text = reader.ReadToEnd();
            }

            info = Application.GetResourceStream(new Uri(MitFilePath));
            if (info != null)
            {
                using StreamReader reader = new(info.Stream);
                MitLicenseTextBlock.Text = reader.ReadToEnd();
            }
        };
    }

    public MainWindowViewModel ViewModel { get; }

    public string AppVersion
    {
        get
        {
            var version = System.Reflection.Assembly.GetExecutingAssembly().GetName().Version;
            return version != null ? $"Version {version.Major}.{version.Minor}.{version.Build}" : string.Empty;
        }
    }

    public string CopyWriteNotice => $"\u00a9 {DateTime.Now.Year} Z-bit Systems, LLC";

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