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
    public async Task<string?> SaveFilesWithDataAsync(string title,
        IEnumerable<(string FileName, byte[] Data)> files, string? initialDirectory = null)
    {
        var fileList = files.ToList();
        if (fileList.Count == 0)
        {
            return null;
        }

        // On Windows the user picks a destination folder and every file is written into it
        var dialog = new OpenFolderDialog
        {
            Title = title,
            Multiselect = false
        };

        // A remembered folder that has since been deleted or unmounted, which is what a job folder
        // on a removable drive looks like on the next visit, is left to the dialog's own default
        // rather than opened on a path that no longer resolves.
        if (!string.IsNullOrWhiteSpace(initialDirectory) && Directory.Exists(initialDirectory))
        {
            dialog.InitialDirectory = initialDirectory;
        }

        if (dialog.ShowDialog() != true)
        {
            return null;
        }

        foreach (var (fileName, data) in fileList)
        {
            await File.WriteAllBytesAsync(Path.Combine(dialog.FolderName, fileName), data);
        }

        return dialog.FolderName;
    }

    private static string FormatExceptionMessage(Exception exception)
    {
        return $"{exception.Message}\n\nDetails: {exception.GetType().Name}";
    }
}