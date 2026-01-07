using System.Windows;
using Microsoft.Win32;
using OSDPBench.Core.Services;

namespace OSDPBench.Windows.Services;

/// <summary>
/// Provides an implementation of the <see cref="Core.Services.IDialogService"/> interface for Windows.
/// This service is used to display message and confirmation dialogs in a Windows environment.
/// </summary>
/// <remarks>
/// This class is part of the OSDPBench.Windows.Services namespace and is used throughout the application
/// to show dialogs to the user. It uses the standard MessageBox class to show the dialogs.
/// </remarks>
internal class WindowsDialogService : IDialogService
{
    private readonly Dictionary<MessageIcon, MessageBoxImage> _icons = new()
    {
        { MessageIcon.Information, MessageBoxImage.Information},
        { MessageIcon.Error, MessageBoxImage.Error},
        { MessageIcon.Warning, MessageBoxImage.Warning}
    };
        
    /// <inheritdoc/>
    public Task ShowMessageDialog(string title, string message, MessageIcon messageIcon)
    {
        MessageBox.Show(message, title, MessageBoxButton.OK, _icons[messageIcon]);
            
        return Task.CompletedTask;
    }

    /// <inheritdoc/>
    public Task<bool> ShowConfirmationDialog(string title, string message, MessageIcon messageIcon)
    {
        var result = MessageBox.Show(message, title, MessageBoxButton.OKCancel, _icons[messageIcon]);

        return Task.FromResult(result == MessageBoxResult.OK);
    }
    
    /// <inheritdoc/>
    public Task ShowExceptionDialog(string title, Exception exception)
    {
        string message = FormatExceptionMessage(exception);
        return ShowMessageDialog(title, message, MessageIcon.Error);
    }

    /// <inheritdoc/>
    public Task<string?> ShowSaveFileDialogAsync(string title, string defaultFileName,
        IEnumerable<(string DisplayName, string Extension)> filters)
    {
        var filterList = filters.ToList();
        var filterString = string.Join("|", filterList.Select(f => $"{f.DisplayName}|*{f.Extension}"));

        var dialog = new SaveFileDialog
        {
            Title = title,
            FileName = defaultFileName,
            Filter = filterString,
            DefaultExt = filterList.FirstOrDefault().Extension ?? ".txt"
        };

        return Task.FromResult(dialog.ShowDialog() == true ? dialog.FileName : null);
    }

    private static string FormatExceptionMessage(Exception exception)
    {
        return $"{exception.Message}\n\nDetails: {exception.GetType().Name}";
    }
}