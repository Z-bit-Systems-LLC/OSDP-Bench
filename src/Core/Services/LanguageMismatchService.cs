using System.Globalization;

namespace OSDPBench.Core.Services;

/// <summary>
/// Service for handling system language mismatch detection and user prompts
/// </summary>
public class LanguageMismatchService : ILanguageMismatchService
{
    private readonly ILocalizationService _localizationService;
    private readonly IDialogService _dialogService;
    private readonly IUserSettingsService _userSettingsService;

    /// <summary>
    /// Initializes a new instance of the LanguageMismatchService
    /// </summary>
    /// <param name="localizationService">The localization service</param>
    /// <param name="dialogService">The dialog service</param>
    /// <param name="userSettingsService">The user settings service</param>
    public LanguageMismatchService(
        ILocalizationService localizationService,
        IDialogService dialogService,
        IUserSettingsService userSettingsService)
    {
        _localizationService = localizationService ?? throw new ArgumentNullException(nameof(localizationService));
        _dialogService = dialogService ?? throw new ArgumentNullException(nameof(dialogService));
        _userSettingsService = userSettingsService ?? throw new ArgumentNullException(nameof(userSettingsService));
    }

    /// <inheritdoc />
    public async Task CheckAndPromptForLanguageMismatchAsync()
    {
        // Only check if this is not the user's first time setting a language
        // (i.e., they have a saved preference)
        if (string.IsNullOrEmpty(_userSettingsService.PreferredCulture))
        {
            // First time user - don't prompt, just use system language
            return;
        }

        // Check if user has disabled language mismatch checking
        if (_userSettingsService.SkipLanguageMismatchCheck)
        {
            // User has opted out of language mismatch checks
            return;
        }

        // Check if there's a mismatch
        if (_localizationService.IsSystemLanguageMatch())
        {
            // Languages match or system language not supported - no action needed
            return;
        }

        var systemCulture = _localizationService.GetSystemCulture();
        var systemLanguageName = systemCulture.NativeName;
        
        // Show custom dialog
        var (userWantsToSwitch, dontAskAgain) = await ShowLanguageMismatchDialogAsync(systemLanguageName);
        
        // Save the "don't ask again" preference
        if (dontAskAgain)
        {
            await _userSettingsService.UpdateSkipLanguageMismatchCheckAsync(true);
        }
        
        if (userWantsToSwitch)
        {
            // Find the best supported culture that matches the system language
            var supportedCulture = FindBestMatchingCulture(systemCulture);
            if (supportedCulture != null)
            {
                _localizationService.ChangeCulture(supportedCulture);
            }
        }
    }

    /// <inheritdoc />
    public async Task<(bool userWantsToSwitch, bool dontAskAgain)> ShowLanguageMismatchDialogAsync(string systemLanguageName)
    {
        // This method will be implemented in the Windows-specific service
        // For now, fall back to the simple dialog
        var title = _localizationService.GetString("Language_SystemMismatchTitle");
        var message = _localizationService.GetString("Language_SystemMismatchMessage", systemLanguageName);
        
        var userWantsToSwitch = await _dialogService.ShowConfirmationDialog(
            title, 
            message, 
            MessageIcon.Information);
            
        return (userWantsToSwitch, false); // No "don't ask again" option in fallback
    }
    
    /// <summary>
    /// Finds the best matching supported culture for the given system culture
    /// </summary>
    /// <param name="systemCulture">The system culture to match</param>
    /// <returns>The best matching supported culture, or null if none found</returns>
    private CultureInfo? FindBestMatchingCulture(CultureInfo systemCulture)
    {
        var supportedCultures = _localizationService.SupportedCultures;
        
        // First try exact match
        var exactMatch = supportedCultures.FirstOrDefault(c => c.Name == systemCulture.Name);
        if (exactMatch != null)
            return exactMatch;
            
        // Then try base language match
        var baseLanguageMatch = supportedCultures.FirstOrDefault(c => 
            c.TwoLetterISOLanguageName == systemCulture.TwoLetterISOLanguageName);
        
        return baseLanguageMatch;
    }
}