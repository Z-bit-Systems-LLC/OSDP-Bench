# System Language Matching

This document describes the system language matching functionality added to OSDP-Bench.

## Overview

The localization service now provides methods to check if the application's current language matches the system language. This can be useful for:
- Notifying users when their app language differs from the system
- Providing an option to sync with system language
- Analytics to understand user language preferences

## New API Methods

### `IsSystemLanguageMatch()`
Checks if the current application language matches the system language.

```csharp
bool isMatch = _localizationService.IsSystemLanguageMatch();
```

Returns `true` if:
- The system language is not supported by the application (no mismatch to report)
- The culture names are exactly the same (e.g., "en-US" == "en-US")
- The base languages match (e.g., "en-US" matches "en-GB")

### `GetSystemCulture()`
Gets the current system UI culture.

```csharp
CultureInfo systemCulture = _localizationService.GetSystemCulture();
```

### `IsCultureSupported(CultureInfo culture)`
Checks if a specific culture is supported by the application.

```csharp
bool isSupported = _localizationService.IsCultureSupported(systemCulture);
```

Returns `true` if the culture or its base language is supported by the application.

## Usage Examples

### Example 1: Display System Language Mismatch Indicator
```csharp
public class LanguageStatusViewModel : ObservableObject
{
    private readonly ILocalizationService _localizationService;
    
    public bool IsSystemLanguageMismatch => !_localizationService.IsSystemLanguageMatch();
    
    public string SystemLanguageName => _localizationService.GetSystemCulture().NativeName;
    
    public LanguageStatusViewModel(ILocalizationService localizationService)
    {
        _localizationService = localizationService;
        _localizationService.CultureChanged += (s, e) => OnPropertyChanged(nameof(IsSystemLanguageMismatch));
    }
}
```

### Example 2: Prompt User on Language Mismatch
```csharp
public async Task CheckLanguageOnStartupAsync()
{
    if (!_localizationService.IsSystemLanguageMatch())
    {
        var systemCulture = _localizationService.GetSystemCulture();
        var result = await _dialogService.ShowMessageAsync(
            $"Your system language is {systemCulture.NativeName}. " +
            "Would you like to switch the application to match?",
            "Language Mismatch",
            DialogButtons.YesNo);
            
        if (result == DialogResult.Yes)
        {
            _localizationService.ChangeCulture(systemCulture);
        }
    }
}
```

### Example 3: Add "Use System Language" Option
```csharp
public class LanguageSelectionViewModel : ObservableObject
{
    [RelayCommand]
    private void UseSystemLanguage()
    {
        var systemCulture = _localizationService.GetSystemCulture();
        _localizationService.ChangeCulture(systemCulture);
    }
    
    public bool IsUsingSystemLanguage => _localizationService.IsSystemLanguageMatch();
}
```

## Implementation Notes

1. **System Culture Detection**: Uses `CultureInfo.InstalledUICulture` which represents the OS display language
2. **Flexible Matching**: Considers both exact matches and base language matches
3. **Real-time Updates**: The `IsSystemLanguageMatch()` method always checks against the current system state

## UI Integration

The system language prompt has been integrated into the application startup process. Here's how it works:

### Automatic Detection
- When the application starts, it automatically checks if the system language matches the current app language
- The check only runs for users who have previously set a language preference (not first-time users)
- If the system language is not supported by the app, no prompt is shown

### User Experience
1. **Language Mismatch Detected**: A dialog appears with the title "System Language Detected"
2. **Informative Message**: Shows "Your system language is [Language Name]. Would you like to switch the application to match your system language?"
3. **User Choice**: User can choose "OK" to switch or "Cancel" to keep current language
4. **Automatic Switching**: If user chooses "OK", the app immediately switches to the best matching supported language

### Implementation Details
- **Service**: `LanguageMismatchService` handles all the logic
- **Timing**: Check runs 500ms after startup to ensure UI is fully loaded
- **Language Matching**: Uses exact match first, then falls back to base language match (e.g., "en-GB" → "en-US")
- **Persistence**: Language choice is automatically saved to user settings

## Testing

To test the system language prompt:

### Test Case 1: Language Mismatch
1. Set Windows display language to Spanish (Settings > Time & Language > Language)
2. Set OSDP-Bench to English in the Info page
3. Close and relaunch the application
4. You should see the system language prompt asking to switch to Spanish

### Test Case 2: No Prompt for Unsupported Language
1. Set Windows display language to Italian (not supported by the app)
2. Set OSDP-Bench to English
3. Close and relaunch the application
4. No prompt should appear (Italian is not supported)

### Test Case 3: First-Time User
1. Delete the settings file (`%APPDATA%\OSDPBench\settings.json`)
2. Launch the application
3. No prompt should appear (first-time users get system language by default)

### Test Case 4: Languages Already Match
1. Set both Windows and OSDP-Bench to the same language
2. Relaunch the application
3. No prompt should appear (languages already match)