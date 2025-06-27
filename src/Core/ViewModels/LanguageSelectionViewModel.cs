using System.Collections.ObjectModel;
using System.Globalization;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using OSDPBench.Core.Services;
using OSDPBench.Core.Resources;

namespace OSDPBench.Core.ViewModels;

/// <summary>
/// ViewModel for language selection functionality
/// </summary>
public partial class LanguageSelectionViewModel : ObservableObject
{
    private readonly ILocalizationService _localizationService;
    
    [ObservableProperty]
    private LanguageItem? _selectedLanguage;
    
    partial void OnSelectedLanguageChanged(LanguageItem? value)
    {
        if (value != null && value.CultureCode != _localizationService.CurrentCulture.Name)
        {
            try
            {
                _localizationService.ChangeCulture(value.CultureCode);
            }
            catch (Exception)
            {
                // If culture change fails, revert selection
                var currentCulture = _localizationService.CurrentCulture.Name;
                SelectedLanguage = AvailableLanguages.FirstOrDefault(l => l.CultureCode == currentCulture) 
                                  ?? AvailableLanguages.First();
            }
        }
    }
    
    /// <summary>
    /// Gets the collection of available languages
    /// </summary>
    public ObservableCollection<LanguageItem> AvailableLanguages { get; }
    
    /// <summary>
    /// Initializes a new instance of the LanguageSelectionViewModel
    /// </summary>
    /// <param name="localizationService">The localization service</param>
    public LanguageSelectionViewModel(ILocalizationService localizationService)
    {
        _localizationService = localizationService ?? throw new ArgumentNullException(nameof(localizationService));
        
        // Initialize available languages with native names that don't change
        AvailableLanguages = new ObservableCollection<LanguageItem>
        {
            new("en-US", "English"),
            new("es-ES", "Español"),
            new("fr-FR", "Français"), 
            new("de-DE", "Deutsch"),
            new("ja-JP", "日本語"),
            new("zh-CN", "中文")
        };
        
        // Set current language as selected
        var currentCulture = _localizationService.CurrentCulture.Name;
        SelectedLanguage = AvailableLanguages.FirstOrDefault(l => l.CultureCode == currentCulture) 
                          ?? AvailableLanguages.First();
        
        // Subscribe to culture changes to update selection
        _localizationService.CultureChanged += OnCultureChanged;
    }
    
    [RelayCommand]
    private void ChangeLanguage(LanguageItem? languageItem)
    {
        if (languageItem == null || languageItem == SelectedLanguage) return;
        
        try
        {
            _localizationService.ChangeCulture(languageItem.CultureCode);
            SelectedLanguage = languageItem;
        }
        catch (Exception)
        {
            // If culture change fails, revert selection
            var currentCulture = _localizationService.CurrentCulture.Name;
            SelectedLanguage = AvailableLanguages.FirstOrDefault(l => l.CultureCode == currentCulture) 
                              ?? AvailableLanguages.First();
        }
    }
    
    private void OnCultureChanged(object? sender, CultureInfo culture)
    {
        // Update selected language when culture changes externally
        var languageItem = AvailableLanguages.FirstOrDefault(l => l.CultureCode == culture.Name);
        if (languageItem != null && languageItem != SelectedLanguage)
        {
            SelectedLanguage = languageItem;
        }
    }
    
}

/// <summary>
/// Represents a language option in the UI
/// </summary>
public record LanguageItem(string CultureCode, string DisplayName)
{
    /// <summary>
    /// Returns the display name of the language
    /// </summary>
    /// <returns>The display name</returns>
    public override string ToString() => DisplayName;
}