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

    private void ExportButton_Click(object sender, RoutedEventArgs e)
    {
        if (sender is Button button && button.ContextMenu != null)
        {
            button.ContextMenu.PlacementTarget = button;
            button.ContextMenu.IsOpen = true;
        }
    }

    private void ToggleRowDetails(object sender, RoutedEventArgs e)
    {
        if (sender is Button { DataContext: not null } button)
        {
            var row = FindParent<DataGridRow>(button);
            if (row != null)
            {
                ToggleRowDetailsVisibility(row);
            }
        }
    }

    private void DataGrid_PreviewMouseLeftButtonUp(object sender, System.Windows.Input.MouseButtonEventArgs e)
    {
        if (e.OriginalSource is DependencyObject source)
        {
            var row = FindParent<DataGridRow>(source);
            if (row != null)
            {
                ToggleRowDetailsVisibility(row);
            }
        }
    }

    private void ToggleRowDetailsVisibility(DataGridRow row)
    {
        row.DetailsVisibility = row.DetailsVisibility == Visibility.Visible
            ? Visibility.Collapsed
            : Visibility.Visible;

        var button = FindChild<Button>(row);
        if (button != null)
        {
            button.Icon = new SymbolIcon
            {
                Symbol = row.DetailsVisibility == Visibility.Visible
                    ? SymbolRegular.ChevronUp24
                    : SymbolRegular.ChevronDown24
            };
            button.ToolTip = row.DetailsVisibility == Visibility.Visible
                ? Core.Resources.Resources.GetString("Monitor_Collapse")
                : Core.Resources.Resources.GetString("Monitor_Expand");
        }
    }

    private static T? FindParent<T>(DependencyObject child) where T : DependencyObject
    {
        var parent = VisualTreeHelper.GetParent(child);

        while (parent != null && parent is not T)
        {
            parent = VisualTreeHelper.GetParent(parent);
        }

        return parent as T;
    }

    private static T? FindChild<T>(DependencyObject parent) where T : DependencyObject
    {
        var childCount = VisualTreeHelper.GetChildrenCount(parent);
        for (var i = 0; i < childCount; i++)
        {
            var child = VisualTreeHelper.GetChild(parent, i);
            if (child is T typedChild)
            {
                return typedChild;
            }

            var result = FindChild<T>(child);
            if (result != null)
            {
                return result;
            }
        }

        return null;
    }
}