using CommunityToolkit.Mvvm.ComponentModel;

namespace OSDPBench.Core.Models;

public partial class FileTransferParameters : ObservableObject
{
    [ObservableProperty]
    private string? _filePath;

    /// <summary>
    /// Gets or sets the file's contents, for a platform that has no file paths to read from, such as a browser.
    /// When set, these bytes are sent and <see cref="FilePath"/> only names the file.
    /// </summary>
    [ObservableProperty]
    private byte[]? _fileData;

    /// <summary>
    /// Gets the bytes to transfer: <see cref="FileData"/> when set, or else the contents of the file at
    /// <see cref="FilePath"/>.
    /// </summary>
    /// <returns>The file's contents.</returns>
    /// <exception cref="InvalidOperationException">No file has been selected.</exception>
    public async Task<byte[]> ReadFileDataAsync()
    {
        if (FileData != null) return FileData;

        if (string.IsNullOrEmpty(FilePath))
        {
            throw new InvalidOperationException("No file selected");
        }

        return await File.ReadAllBytesAsync(FilePath);
    }

    private int _progressPercentage;

    /// <summary>
    /// Gets the progress of a file transfer operation as a percentage.
    /// </summary>
    /// <remarks>
    /// This property provides a read-only view of the progress, calculated based on the ratio of transferred bytes to the total file size.
    /// It dynamically updates during the file transfer to reflect the ongoing progress.
    /// </remarks>
    public int ProgressPercentage
    {
        get => _progressPercentage;
        private set => SetProperty(ref _progressPercentage, value);
    }

    [ObservableProperty]
    private string _statusMessage = string.Empty;

    [ObservableProperty]
    private bool _isBusy;

    [ObservableProperty] 
    private int _totalBytes;

    private int _transferredBytes;

    /// <summary>
    /// Gets or sets the number of bytes that have been transferred during a file transfer operation.
    /// </summary>
    /// <remarks>
    /// This property tracks the progress of the file transfer in terms of the amount of data successfully transferred.
    /// Updating this property also updates the progress percentage to reflect the current transfer status relative to the total file size.
    /// </remarks>
    public int TransferredBytes
    {
        get => _transferredBytes;
        set
        {
            if (SetProperty(ref _transferredBytes, value))
            {
                UpdateProgressPercentage();
            }
        }
    }

    private void UpdateProgressPercentage()
    {
        if (TotalBytes <= 0)
        {
            ProgressPercentage = 0;
            return;
        }

        ProgressPercentage = (int)((TransferredBytes / (double)TotalBytes) * 100);
    }
}