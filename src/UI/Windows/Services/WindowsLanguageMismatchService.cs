using System.Globalization;
using System.Windows;
using OSDPBench.Core.Services;
using OSDPBench.Core.ViewModels.Dialogs;
using OSDPBench.Windows.Views.Dialogs;

namespace OSDPBench.Windows.Services;

/// <summary>
/// Windows-specific implementation of the language mismatch service with custom dialog support
/// </summary>
public class WindowsLanguageMismatchService : ILanguageMismatchService
{
    private readonly ILocalizationService _localizationService;
    private readonly IUserSettingsService _userSettingsService;

    /// <summary>
    /// Initializes a new instance of the WindowsLanguageMismatchService
    /// </summary>
    /// <param name="localizationService">The localization service</param>
    /// <param name="userSettingsService">The user settings service</param>
    public WindowsLanguageMismatchService(
        ILocalizationService localizationService,
        IUserSettingsService userSettingsService)
    {
        _localizationService = localizationService ?? throw new ArgumentNullException(nameof(localizationService));
        _userSettingsService = userSettingsService ?? throw new ArgumentNullException(nameof(userSettingsService));
    }

    /// <inheritdoc />
    public async Task CheckAndPromptForLanguageMismatchAsync()
    {
        System.Diagnostics.Debug.WriteLine("LanguageMismatchService: Starting check...");
        
        // Only check if this is not the user's first time setting a language
        if (string.IsNullOrEmpty(_userSettingsService.Settings.PreferredCulture))
        {
            System.Diagnostics.Debug.WriteLine("LanguageMismatchService: First time user - skipping");
            return;
        }

        // Check if the user has disabled language mismatch checking
        if (_userSettingsService.Settings.SkipLanguageMismatchCheck)
        {
            return;
        }

        // Check if there's a mismatch
        var systemCulture = _localizationService.GetSystemCulture();
        var isMatch = _localizationService.IsSystemLanguageMatch();
        
        if (isMatch)
        {
            return;
        }

        var systemLanguageName = systemCulture.NativeName;
        
        // Show custom dialog
        var (userWantsToSwitch, dontAskAgain) = await ShowLanguageMismatchDialogAsync(systemLanguageName);

        // Save the "don't ask again" preference
        if (dontAskAgain)
        {
            await _userSettingsService.UpdateSettingsAsync(settings => 
                settings.SkipLanguageMismatchCheck = true);
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
        var tcs = new TaskCompletionSource<(bool, bool)>();

        await Application.Current.Dispatcher.InvokeAsync(() =>
        {
            // Create the dialog content
            var message = _localizationService.GetString("Language_SystemMismatchMessage", systemLanguageName);

            Window? dialogWindow = null;
            var viewModel = new LanguageMismatchDialogViewModel(message, (userChoice, dontAsk) =>
            {
                tcs.TrySetResult((userChoice, dontAsk));
                dialogWindow?.Close();
            });

            var dialogContent = new LanguageMismatchDialog
            {
                DataContext = viewModel
            };

            // Create and show the dialog window
            dialogWindow = new Window
            {
                Title = _localizationService.GetString("Language_SystemMismatchTitle"),
                Content = dialogContent,
                SizeToContent = SizeToContent.WidthAndHeight,
                ResizeMode = ResizeMode.NoResize,
                WindowStartupLocation = WindowStartupLocation.CenterOwner,
                Owner = Application.Current.MainWindow,
                ShowInTaskbar = false
            };

            dialogWindow.ShowDialog();
        });

        return await tcs.Task;
    }
    
    /// <summary>
    /// Finds the best matching supported culture for the given system culture
    /// </summary>
    /// <param name="systemCulture">The system culture to match</param>
    /// <returns>The best matching supported culture, or null if none found</returns>
    private CultureInfo? FindBestMatchingCulture(CultureInfo systemCulture)
    {
        var supportedCultures = _localizationService.SupportedCultures;
        
        // First try the exact match
        var exactMatch = supportedCultures.FirstOrDefault(c => c.Name == systemCulture.Name);
        if (exactMatch != null)
            return exactMatch;
            
        // Then try base language match
        var baseLanguageMatch = supportedCultures.FirstOrDefault(c => 
            c.TwoLetterISOLanguageName == systemCulture.TwoLetterISOLanguageName);
        
        return baseLanguageMatch;
    }
}