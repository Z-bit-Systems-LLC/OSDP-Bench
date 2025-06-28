using System.Windows;
using System.Windows.Controls;
using Microsoft.Win32;
using OSDPBench.Core.Models;

namespace OSDPBench.Windows.Views.Controls;

public partial class FileTransferControl
{
    public FileTransferControl(FileTransferParameters parameters)
    {        
        DataContext = parameters;
        
        InitializeComponent();
    }

    private void BrowseFileButton_OnClick(object sender, RoutedEventArgs e)
    {
        var openFileDialog = new OpenFileDialog
        {
            Filter = "All Files (*.*)|*.*",
            Title = "Select File to Transfer"
        };

        if (openFileDialog.ShowDialog() != true) return;

        if (DataContext is FileTransferParameters fileTransferParameters)
        {
            fileTransferParameters.FilePath = openFileDialog.FileName;
        }
    }

}