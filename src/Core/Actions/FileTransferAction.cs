using System.Text;
using OSDP.Net;
using OSDPBench.Core.Models;

namespace OSDPBench.Core.Actions;

/// <summary>
/// Represents an action for transferring files to a device through OSDP-based communication.
/// </summary>
public class FileTransferAction : IDeviceAction
{
    /// <inheritdoc />
    public string Name => "File Transfer";
    
    /// <inheritdoc />
    public string PerformActionName => "Transfer File";

    /// <inheritdoc />
    public async Task<object> PerformAction(ControlPanel panel, Guid connectionId, byte address, object? parameter)
    {
        if (parameter is not FileTransferParameters transferParams)
        {
            throw new ArgumentException(@"Invalid parameter type for file transfer", nameof(parameter));
        }

        if (string.IsNullOrEmpty(transferParams.FilePath))
        {
            throw new InvalidOperationException("No file selected");
        }

        byte[] fileData = await File.ReadAllBytesAsync(transferParams.FilePath);
        var cts = new CancellationTokenSource();

        transferParams.IsBusy = true;
        transferParams.StatusMessage = "Starting transfer...";
        transferParams.TotalBytes = fileData.Length;

        try
        {
            var status = await panel.FileTransfer(
                connectionId,
                address,
                1,
                fileData,
                128, // Default fragment size
                status =>
                {
                    transferParams.TransferredBytes = status.CurrentOffset;
                    transferParams.StatusMessage = $"Transferring... {FormatEnum(status.Status.ToString())}";

                    if (status.Nak != null)
                    {
                        transferParams.StatusMessage = $"Error: {status.Nak}";
                    }
                },
                cts.Token);

            transferParams.StatusMessage = $"Transfer complete: {FormatEnum(status.ToString())}";
            return status;
        }
        catch (Exception exception)
        {
            transferParams.StatusMessage = $"Error: {exception.Message}";
            throw;
        }
        finally
        {
            transferParams.IsBusy = false;
        }
    }

    private static string FormatEnum(string text)
    {
        if (string.IsNullOrEmpty(text))
            return text;

        var result = new StringBuilder(text.Length * 2);
        result.Append(char.ToUpper(text[0]));
    
        for (int index = 1; index < text.Length; index++)
        {
            if (char.IsUpper(text[index]) && index > 0 && !char.IsUpper(text[index - 1]))
            {
                result.Append(' ');
                result.Append(char.ToLower(text[index]));
            }
            else
            {
                result.Append(char.ToLower(text[index]));
            }
        }
    
        return result.ToString();
    }
}