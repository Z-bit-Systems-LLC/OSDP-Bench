using System.IO;
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
    public async Task<bool> SaveFilesWithDataAsync(string title, IEnumerable<(string FileName, byte[] Data)> files)
    {
        var fileList = files.ToList();
        if (fileList.Count == 0)
        {
            return false;
        }

        // On Windows the user picks a destination folder and every file is written into it
        var dialog = new OpenFolderDialog
        {
            Title = title,
            Multiselect = false
        };

        if (dialog.ShowDialog() != true)
        {
            return false;
        }

        foreach (var (fileName, data) in fileList)
        {
            await File.WriteAllBytesAsync(Path.Combine(dialog.FolderName, fileName), data);
        }

        return true;
    }

    private static string FormatExceptionMessage(Exception exception)
    {
        return $"{exception.Message}\n\nDetails: {exception.GetType().Name}";
    }
}