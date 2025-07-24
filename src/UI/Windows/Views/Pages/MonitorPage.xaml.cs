using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;
using OSDPBench.Core.ViewModels.Pages;
using Wpf.Ui.Abstractions.Controls;
using Button = Wpf.Ui.Controls.Button;
using Wpf.Ui.Controls;

namespace OSDPBench.Windows.Views.Pages;

public partial class MonitorPage : INavigableView<MonitorViewModel>
{
    public MonitorPage(MonitorViewModel viewModel)
    {
        ViewModel = viewModel;
        DataContext = this;
        
        InitializeComponent();
    }

    public MonitorViewModel ViewModel { get; }

    private void ToggleRowDetails(object sender, RoutedEventArgs e)
    {
        if (sender is Button { DataContext: not null } button)
        {
            // Get the DataGridRow for the clicked button
            var row = FindParent<DataGridRow>(button);

            if (row != null)
            {
                // Toggle RowDetails visibility
                row.DetailsVisibility = row.DetailsVisibility == Visibility.Visible
                    ? Visibility.Collapsed
                    : Visibility.Visible;
                
                button.Icon = new SymbolIcon { Symbol = row.DetailsVisibility == Visibility.Visible ? SymbolRegular.ChevronUp24 : SymbolRegular.ChevronDown24 };
                button.ToolTip = row.DetailsVisibility == Visibility.Visible ? Core.Resources.Resources.GetString("Monitor_Collapse") : Core.Resources.Resources.GetString("Monitor_Expand");
            }
        }
    }

    /// <summary>
    /// Helper method to find parent of a specific type.
    /// </summary>
    private T? FindParent<T>(DependencyObject child) where T : DependencyObject
    {
        DependencyObject? parent = VisualTreeHelper.GetParent(child);

        while (parent != null && !(parent is T))
        {
            parent = VisualTreeHelper.GetParent(parent);
        }

        return parent as T;
    }
}