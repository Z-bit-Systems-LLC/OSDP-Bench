using MvvmCore.Platform;
using Wpf.Ui.Controls;

namespace OSDPBench.Windows.Platform
{
    internal class WindowsDialogService : IDialogService
    {
        /// <inheritdoc/>
        public async Task ShowMessageDialogAsync(string title, string message)
        {
            MessageBox dialog = new()
            {
                Title = title,
                CloseButtonText = "OK",
                Content = message
            };

            await dialog.ShowDialogAsync();
        }
    }
}
