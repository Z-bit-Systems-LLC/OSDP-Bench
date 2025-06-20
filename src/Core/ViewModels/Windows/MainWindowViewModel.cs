using CommunityToolkit.Mvvm.ComponentModel;
using OSDPBench.Core.Services;

namespace OSDPBench.Core.ViewModels.Windows;

/// <summary>
/// Represents the view model for the main window of the application.
/// </summary>
public partial class MainWindowViewModel : ObservableObject
{
    /// <summary>
    /// Gets the language selection view model
    /// </summary>
    public LanguageSelectionViewModel LanguageViewModel { get; }

    /// <summary>
    /// Initializes a new instance of the MainWindowViewModel
    /// </summary>
    /// <param name="localizationService">The localization service</param>
    public MainWindowViewModel(ILocalizationService localizationService)
    {
        LanguageViewModel = new LanguageSelectionViewModel(localizationService);
    }
}