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
    /// Saves one or more files using the platform's preferred mechanism, prompting the user for a
    /// destination once. The platform owns where and how the data is persisted, so callers do not
    /// need a writable file system path.
    /// </summary>
    /// <param name="title">The title of the destination selection dialog.</param>
    /// <param name="files">The files to save, as file name and content pairs.</param>
    /// <returns>True if the files were saved, false if the user cancelled.</returns>
    Task<bool> SaveFilesWithDataAsync(string title, IEnumerable<(string FileName, byte[] Data)> files);
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