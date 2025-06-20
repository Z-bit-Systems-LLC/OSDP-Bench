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
        
        // Initialize available languages
        AvailableLanguages = new ObservableCollection<LanguageItem>
        {
            new("en-US", OSDPBench.Core.Resources.Resources.GetString("Language_English")),
            new("es-ES", OSDPBench.Core.Resources.Resources.GetString("Language_Spanish")),
            new("fr-FR", OSDPBench.Core.Resources.Resources.GetString("Language_French")),
            new("de-DE", OSDPBench.Core.Resources.Resources.GetString("Language_German")),
            new("ja-JP", OSDPBench.Core.Resources.Resources.GetString("Language_Japanese"))
        };
        
        // Set current language as selected
        var currentCulture = _localizationService.CurrentCulture.Name;
        SelectedLanguage = AvailableLanguages.FirstOrDefault(l => l.CultureCode == currentCulture) 
                          ?? AvailableLanguages.First();
        
        // Subscribe to culture changes to update selection
        _localizationService.CultureChanged += OnCultureChanged;
        
        // Subscribe to resource changes to update language names
        OSDPBench.Core.Resources.Resources.PropertyChanged += OnResourcesPropertyChanged;
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
    
    private void OnResourcesPropertyChanged(object? sender, System.ComponentModel.PropertyChangedEventArgs e)
    {
        // Update language display names when culture changes
        UpdateLanguageDisplayNames();
    }
    
    private void UpdateLanguageDisplayNames()
    {
        // Update the display names while preserving culture codes
        var languages = new[]
        {
            new LanguageItem("en-US", OSDPBench.Core.Resources.Resources.GetString("Language_English")),
            new LanguageItem("es-ES", OSDPBench.Core.Resources.Resources.GetString("Language_Spanish")),
            new LanguageItem("fr-FR", OSDPBench.Core.Resources.Resources.GetString("Language_French")),
            new LanguageItem("de-DE", OSDPBench.Core.Resources.Resources.GetString("Language_German")),
            new LanguageItem("ja-JP", OSDPBench.Core.Resources.Resources.GetString("Language_Japanese"))
        };
        
        var selectedCode = SelectedLanguage?.CultureCode;
        
        AvailableLanguages.Clear();
        foreach (var language in languages)
        {
            AvailableLanguages.Add(language);
        }
        
        // Restore selection
        SelectedLanguage = AvailableLanguages.FirstOrDefault(l => l.CultureCode == selectedCode) 
                          ?? AvailableLanguages.First();
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