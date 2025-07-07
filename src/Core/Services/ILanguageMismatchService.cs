namespace OSDPBench.Core.Services;

/// <summary>
/// Service for handling system language mismatch detection and user prompts
/// </summary>
public interface ILanguageMismatchService
{
    /// <summary>
    /// Checks for system language mismatch and prompts the user if needed
    /// </summary>
    Task CheckAndPromptForLanguageMismatchAsync();
    
    /// <summary>
    /// Shows the language mismatch dialog with custom UI
    /// </summary>
    /// <param name="systemLanguageName">The name of the system language</param>
    /// <returns>A tuple containing (userWantsToSwitch, dontAskAgain)</returns>
    Task<(bool userWantsToSwitch, bool dontAskAgain)> ShowLanguageMismatchDialogAsync(string systemLanguageName);
}