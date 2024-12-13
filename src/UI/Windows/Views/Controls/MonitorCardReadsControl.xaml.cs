using System.Collections.ObjectModel;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using OSDPBench.Core.Models;
using OSDPBench.Core.ViewModels.Pages;

namespace OSDPBench.Windows.Views.Controls;

public sealed partial class MonitorCardReadsControl
{
    public MonitorCardReadsControl()
    {
        InitializeComponent();
        
        DataContext = this;
    }

    /// <summary>
    /// Initializes the ViewModel and sets up bindings and event handling for
    /// card read operations in the MonitorCardReadsControl.
    /// </summary>
    /// <param name="viewModel">The ViewModel of type ManageViewModel
    /// providing the data context for the control.</param>
    public void Initialize(ManageViewModel viewModel)
    {
        // Bind the TextBox
        CardNumberTextBox.SetBinding(TextBox.TextProperty, new Binding("LastCardNumberRead")
        {
            Source = viewModel
        });

        // Bind the DataGrid to the CardReadEntries
        HistoryDataGrid.SetBinding(ItemsControl.ItemsSourceProperty, new Binding("CardReadEntries")
        {
            Source = viewModel
        });
    }

    private void ClearHistory_Click(object sender, RoutedEventArgs e)
    {
        if (HistoryDataGrid.ItemsSource is ObservableCollection<CardReadEntry> entries)
        {
            entries.Clear();
        }
    }
}