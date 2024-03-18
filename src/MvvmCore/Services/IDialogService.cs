namespace MvvmCore.Services;

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
}

public enum MessageIcon
{
    Information,
    Error,
    Warning
}