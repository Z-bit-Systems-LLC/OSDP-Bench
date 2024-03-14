using MvvmCore.Services;
using Wpf.Ui.Controls;

namespace OSDPBench.Windows.Services
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
