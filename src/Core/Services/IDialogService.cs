namespace OSDPBench.Core.Services;

/// <summary>
/// The default <see langword="interface"/> for a service that shows dialogs
/// </summary>
public interface IDialogService
{
    /// <summary>
    /// Shows a message dialog with a title and custom content, and an icon.
    /// </summary>
    /// <param name="title">The title of the message dialog.</param>
    /// <param name="message">The content of the message dialog.</param>
    /// <param name="messageIcon">The icon to display in the message dialog.</param>
    Task ShowMessageDialog(string title, string message, MessageIcon messageIcon);

    /// <summary>
    /// Shows a confirmation dialog with a title, custom content, and an icon.
    /// </summary>
    /// <param name="title">The title of the confirmation dialog.</param>
    /// <param name="message">The content of the confirmation dialog.</param>
    /// <param name="messageIcon">The icon to display in the confirmation dialog.</param>
    /// <returns>A task that represents the asynchronous operation. The task result contains a boolean value that is true if the user clicks OK, and false otherwise.</returns>
    Task<bool> ShowConfirmationDialog(string title, string message, MessageIcon messageIcon);
    
    /// <summary>
    /// Shows an error dialog for an exception.
    /// </summary>
    /// <param name="title">The title of the dialog.</param>
    /// <param name="exception">The exception to display information about.</param>
    /// <returns>A task representing the asynchronous operation.</returns>
    Task ShowExceptionDialog(string title, Exception exception);

    /// <summary>
    /// Shows a save file dialog to allow the user to select a file path for saving.
    /// </summary>
    /// <param name="title">The title of the save file dialog.</param>
    /// <param name="defaultFileName">The default file name to suggest.</param>
    /// <param name="filters">The file type filters to display (DisplayName and Extension pairs).</param>
    /// <returns>The selected file path, or null if the user cancelled.</returns>
    Task<string?> ShowSaveFileDialogAsync(string title, string defaultFileName,
        IEnumerable<(string DisplayName, string Extension)> filters);
}

/// <summary>
/// Icon used in dialog
/// </summary>
public enum MessageIcon
{
    /// <summary>
    /// Information icon.
    /// </summary>
    Information,
    
    /// <summary>
    /// Error icon.
    /// </summary>
    Error,
    
    /// <summary>
    /// Warning icon.
    /// </summary>
    Warning
}