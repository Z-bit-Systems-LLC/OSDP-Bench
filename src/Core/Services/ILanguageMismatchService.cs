using System.Globalization;

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
}