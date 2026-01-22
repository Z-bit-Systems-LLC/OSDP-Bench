using System.ComponentModel;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using OSDPBench.Core.ViewModels.Pages;

namespace OSDPBench.Windows.Views.Controls;

/// <summary>
/// Interaction logic for SupervisionControl.xaml
/// </summary>
public sealed partial class SupervisionControl
{
    private ManageViewModel? _viewModel;

    public SupervisionControl()
    {
        InitializeComponent();
        DataContext = this;
    }

    /// <summary>
    /// Initializes the control with the ManageViewModel and sets up bindings.
    /// </summary>
    /// <param name="viewModel">The ManageViewModel providing supervision data.</param>
    public void Initialize(ManageViewModel viewModel)
    {
        _viewModel = viewModel;

        // Bind the DataGrid to the SupervisionEntries
        HistoryDataGrid.SetBinding(ItemsControl.ItemsSourceProperty, new Binding("SupervisionEntries")
        {
            Source = viewModel
        });

        // Update badge displays based on current values
        UpdateTamperBadge(viewModel.TamperStatus, viewModel.TamperTimestamp);
        UpdatePowerBadge(viewModel.PowerStatus, viewModel.PowerTimestamp);
        UpdateOnlineBadge(viewModel.OnlineStatus, viewModel.OnlineTimestamp);

        // Subscribe to property changes to update badges
        viewModel.PropertyChanged += ViewModel_PropertyChanged;
    }

    private void ViewModel_PropertyChanged(object? sender, PropertyChangedEventArgs e)
    {
        if (_viewModel == null) return;

        switch (e.PropertyName)
        {
            case nameof(ManageViewModel.TamperStatus):
            case nameof(ManageViewModel.TamperTimestamp):
                UpdateTamperBadge(_viewModel.TamperStatus, _viewModel.TamperTimestamp);
                break;
            case nameof(ManageViewModel.PowerStatus):
            case nameof(ManageViewModel.PowerTimestamp):
                UpdatePowerBadge(_viewModel.PowerStatus, _viewModel.PowerTimestamp);
                break;
            case nameof(ManageViewModel.OnlineStatus):
            case nameof(ManageViewModel.OnlineTimestamp):
                UpdateOnlineBadge(_viewModel.OnlineStatus, _viewModel.OnlineTimestamp);
                break;
        }
    }

    private void UpdateTamperBadge(bool? status, DateTime? timestamp)
    {
        if (status == null)
        {
            TamperBadge.Style = (Style)FindResource("Badge.Info.Small");
            TamperBadgeText.Text = Core.Resources.Resources.GetString("Supervision_Unknown");
            TamperBadgeText.Style = (Style)FindResource("Badge.Text.Small");
            TamperTimestampText.Text = string.Empty;
        }
        else if (status == true)
        {
            TamperBadge.Style = (Style)FindResource("Badge.Error.Outlined.Small");
            TamperBadgeText.Text = Core.Resources.Resources.GetString("Supervision_Tampered");
            TamperBadgeText.Style = (Style)FindResource("Badge.Text.Error.Small");
            TamperTimestampText.Text = timestamp?.ToString("MM/dd/yyyy HH:mm:ss");
        }
        else
        {
            TamperBadge.Style = (Style)FindResource("Badge.Success.Small");
            TamperBadgeText.Text = Core.Resources.Resources.GetString("Supervision_Normal");
            TamperBadgeText.Style = (Style)FindResource("Badge.Text.Small");
            TamperTimestampText.Text = timestamp?.ToString("MM/dd/yyyy HH:mm:ss");
        }
    }

    private void UpdatePowerBadge(bool? status, DateTime? timestamp)
    {
        if (status == null)
        {
            PowerBadge.Style = (Style)FindResource("Badge.Info.Small");
            PowerBadgeText.Text = Core.Resources.Resources.GetString("Supervision_Unknown");
            PowerBadgeText.Style = (Style)FindResource("Badge.Text.Small");
            PowerTimestampText.Text = string.Empty;
        }
        else if (status == true)
        {
            PowerBadge.Style = (Style)FindResource("Badge.Error.Outlined.Small");
            PowerBadgeText.Text = Core.Resources.Resources.GetString("Supervision_PowerFailure");
            PowerBadgeText.Style = (Style)FindResource("Badge.Text.Error.Small");
            PowerTimestampText.Text = timestamp?.ToString("MM/dd/yyyy HH:mm:ss");
        }
        else
        {
            PowerBadge.Style = (Style)FindResource("Badge.Success.Small");
            PowerBadgeText.Text = Core.Resources.Resources.GetString("Supervision_Normal");
            PowerBadgeText.Style = (Style)FindResource("Badge.Text.Small");
            PowerTimestampText.Text = timestamp?.ToString("MM/dd/yyyy HH:mm:ss");
        }
    }

    private void UpdateOnlineBadge(bool? status, DateTime? timestamp)
    {
        if (status == null)
        {
            OnlineBadge.Style = (Style)FindResource("Badge.Info.Small");
            OnlineBadgeText.Text = Core.Resources.Resources.GetString("Supervision_Unknown");
            OnlineBadgeText.Style = (Style)FindResource("Badge.Text.Small");
            OnlineTimestampText.Text = string.Empty;
        }
        else if (status == true)
        {
            OnlineBadge.Style = (Style)FindResource("Badge.Success.Small");
            OnlineBadgeText.Text = Core.Resources.Resources.GetString("Supervision_Online");
            OnlineBadgeText.Style = (Style)FindResource("Badge.Text.Small");
            OnlineTimestampText.Text = timestamp?.ToString("MM/dd/yyyy HH:mm:ss");
        }
        else
        {
            OnlineBadge.Style = (Style)FindResource("Badge.Error.Outlined.Small");
            OnlineBadgeText.Text = Core.Resources.Resources.GetString("Supervision_Offline");
            OnlineBadgeText.Style = (Style)FindResource("Badge.Text.Error.Small");
            OnlineTimestampText.Text = timestamp?.ToString("MM/dd/yyyy HH:mm:ss");
        }
    }

    private void ClearHistory_Click(object sender, RoutedEventArgs e)
    {
        _viewModel?.ClearSupervisionHistory();
    }
}
