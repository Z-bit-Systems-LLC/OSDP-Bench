namespace OSDPBench.Core.Services;

/// <summary>
/// Service for managing user settings persistence
/// </summary>
public interface IUserSettingsService
{
    /// <summary>
    /// Gets the user's preferred culture/language
    /// </summary>
    string PreferredCulture { get; }

    /// <summary>
    /// Gets whether to skip language mismatch checking
    /// </summary>
    bool SkipLanguageMismatchCheck { get; }

    /// <summary>
    /// Updates the preferred culture and saves settings
    /// </summary>
    /// <param name="cultureName">The culture name to save</param>
    /// <returns>Task representing the async operation</returns>
    Task UpdatePreferredCultureAsync(string cultureName);

    /// <summary>
    /// Updates the skip language mismatch check preference and saves settings
    /// </summary>
    /// <param name="skip">Whether to skip language mismatch checking</param>
    /// <returns>Task representing the async operation</returns>
    Task UpdateSkipLanguageMismatchCheckAsync(bool skip);

    /// <summary>
    /// Gets the last selected serial port name, or null if none has been saved.
    /// </summary>
    string? LastSerialPortName => null;

    /// <summary>
    /// Gets the last selected baud rate.
    /// </summary>
    int LastBaudRate => 9600;

    /// <summary>
    /// Gets the last selected device address.
    /// </summary>
    byte LastAddress => 0;

    /// <summary>
    /// Updates the last-used connection settings (serial port, baud rate, address) and saves.
    /// </summary>
    /// <param name="serialPortName">The selected serial port name, or null if none is selected.</param>
    /// <param name="baudRate">The selected baud rate.</param>
    /// <param name="address">The selected device address.</param>
    /// <returns>Task representing the async operation</returns>
    Task UpdateConnectionSettingsAsync(string? serialPortName, int baudRate, byte address) => Task.CompletedTask;
}
