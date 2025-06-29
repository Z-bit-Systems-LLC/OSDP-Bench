namespace OSDPBench.Core.Services;

/// <summary>
/// Provides helper methods for standardized exception handling.
/// </summary>
public static class ExceptionHelper
{
    /// <summary>
    /// Handle an exception in a standardized way using the dialog service.
    /// </summary>
    /// <param name="dialogService">The dialog service to use for displaying errors.</param>
    /// <param name="title">Title for the error dialog.</param>
    /// <param name="exception">The exception to handle.</param>
    /// <returns>A task representing the asynchronous operation.</returns>
    public static Task HandleException(IDialogService dialogService, string title, Exception exception)
    {
        return dialogService.ShowExceptionDialog(title, exception);
    }

    /// <summary>
    /// Execute an action safely, handling any exceptions that occur.
    /// </summary>
    /// <param name="dialogService">The dialog service to use for displaying errors.</param>
    /// <param name="title">Title for the error dialog.</param>
    /// <param name="action">The action to execute.</param>
    /// <returns>True if the action executed successfully, false otherwise.</returns>
    public static bool ExecuteSafely(IDialogService dialogService, string title, Action action)
    {
        try
        {
            action();
            return true;
        }
        catch (Exception ex)
        {
            dialogService.ShowExceptionDialog(title, ex).ConfigureAwait(false);
            return false;
        }
    }

    /// <summary>
    /// Execute an action safely, handling any exceptions that occur asynchronously.
    /// </summary>
    /// <param name="dialogService">The dialog service to use for displaying errors.</param>
    /// <param name="title">Title for the error dialog.</param>
    /// <param name="action">The asynchronous action to execute.</param>
    /// <returns>A task representing the asynchronous operation with a boolean result indicating success.</returns>
    public static async Task<bool> ExecuteSafelyAsync(IDialogService dialogService, string title, Func<Task> action)
    {
        try
        {
            await action();
            return true;
        }
        catch (OperationCanceledException)
        {
            // Silently handle operation canceled exceptions 
            return false;
        }
        catch (Exception ex)
        {
            await dialogService.ShowExceptionDialog(title, ex);
            return false;
        }
    }

    /// <summary>
    /// Execute a function safely, handling any exceptions that occur.
    /// </summary>
    /// <typeparam name="T">The return type of the function.</typeparam>
    /// <param name="dialogService">The dialog service to use for displaying errors.</param>
    /// <param name="title">Title for the error dialog.</param>
    /// <param name="func">The function to execute.</param>
    /// <param name="defaultValue">The default value to return if an exception occurs.</param>
    /// <returns>The result of the function or the default value if an exception occurred.</returns>
    public static T ExecuteSafely<T>(IDialogService dialogService, string title, Func<T> func, T defaultValue)
    {
        try
        {
            return func();
        }
        catch (Exception ex)
        {
            dialogService.ShowExceptionDialog(title, ex).ConfigureAwait(false);
            return defaultValue;
        }
    }

    /// <summary>
    /// Execute a function safely, handling any exceptions that occur asynchronously.
    /// </summary>
    /// <typeparam name="T">The return type of the function.</typeparam>
    /// <param name="dialogService">The dialog service to use for displaying errors.</param>
    /// <param name="title">Title for the error dialog.</param>
    /// <param name="func">The asynchronous function to execute.</param>
    /// <param name="defaultValue">The default value to return if an exception occurs.</param>
    /// <returns>A task representing the asynchronous operation with the result or default value.</returns>
    public static async Task<T> ExecuteSafelyAsync<T>(IDialogService dialogService, string title, Func<Task<T>> func, T defaultValue)
    {
        try
        {
            return await func();
        }
        catch (OperationCanceledException)
        {
            // Silently handle operation canceled exceptions
            return defaultValue;
        }
        catch (Exception ex)
        {
            await dialogService.ShowExceptionDialog(title, ex);
            return defaultValue;
        }
    }
}