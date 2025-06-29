using System.Windows.Controls;
using System.ComponentModel;
using System.Windows;
using OSDPBench.Core.ViewModels.Pages;
using Wpf.Ui.Abstractions.Controls;
using Wpf.Ui.Controls;

namespace OSDPBench.Windows.Views.Pages;

/// <summary>
/// Interaction logic for ConnectPage.xaml
/// </summary>
public partial class ConnectPage : INavigableView<ConnectViewModel>, INotifyPropertyChanged
{
    public ConnectPage(ConnectViewModel viewModel)
    {
        ViewModel = viewModel;
        DataContext = this;

        // Subscribe to culture changes
        Core.Resources.Resources.PropertyChanged += OnResourcesPropertyChanged;

        // Subscribe to StatusLevel changes to update button visibility
        ViewModel.PropertyChanged += OnViewModelPropertyChanged;

        InitializeComponent();

        // Set up loaded event to ensure proper initialization
        this.Loaded += OnPageLoaded;
    }

    public ConnectViewModel ViewModel { get; }

    public IEnumerable<string> ConnectionTypes =>
    [
        Core.Resources.Resources.GetString("ConnectionType_Discover"),
        Core.Resources.Resources.GetString("ConnectionType_Manual")
    ];

    private int _selectedConnectionTypeIndex = -1; // Start with -1 to ensure property change fires

    public int SelectedConnectionTypeIndex
    {
        get => _selectedConnectionTypeIndex;
        set
        {
            if (_selectedConnectionTypeIndex != value)
            {
                _selectedConnectionTypeIndex = value;
                PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(nameof(SelectedConnectionTypeIndex)));
                UpdateButtonVisibility();
            }
        }
    }

    // Button visibility properties
    public Visibility ConnectVisibility => CalculateConnectVisibility();
    public Visibility DisconnectVisibility => CalculateDisconnectVisibility();
    public Visibility StartDiscoveryVisibility => CalculateStartDiscoveryVisibility();
    public Visibility CancelDiscoveryVisibility => CalculateCancelDiscoveryVisibility();

    private Visibility CalculateConnectVisibility()
    {
        // Show the Connect button when Manual mode is selected and not connected
        return SelectedConnectionTypeIndex == 1 && ViewModel.StatusLevel != StatusLevel.Connected
            ? Visibility.Visible
            : Visibility.Collapsed;
    }

    private Visibility CalculateDisconnectVisibility()
    {
        // Show the Disconnect button when connected
        return ViewModel.StatusLevel == StatusLevel.Connected
            ? Visibility.Visible
            : Visibility.Collapsed;
    }

    private Visibility CalculateStartDiscoveryVisibility()
    {
        return SelectedConnectionTypeIndex == 0 && ViewModel.StatusLevel is not StatusLevel.Discovering
            and not StatusLevel.Discovered and not StatusLevel.Connected
            ? Visibility.Visible
            : Visibility.Collapsed;
    }

    private Visibility CalculateCancelDiscoveryVisibility()
    {
        return SelectedConnectionTypeIndex == 0 && ViewModel.StatusLevel == StatusLevel.Discovering
            ? Visibility.Visible
            : Visibility.Collapsed;
    }

    private void OnResourcesPropertyChanged(object? sender, PropertyChangedEventArgs e)
    {
        // When culture changes, notify that ConnectionTypes has changed
        PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(nameof(ConnectionTypes)));

        // Force the ComboBox to refresh its selection
        var currentIndex = SelectedConnectionTypeIndex;
        SelectedConnectionTypeIndex = -1;
        SelectedConnectionTypeIndex = currentIndex;
    }

    private void OnViewModelPropertyChanged(object? sender, PropertyChangedEventArgs e)
    {
        if (e.PropertyName == nameof(ViewModel.StatusLevel))
        {
            UpdateButtonVisibility();
        }
    }

    private void UpdateButtonVisibility()
    {
        PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(nameof(ConnectVisibility)));
        PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(nameof(DisconnectVisibility)));
        PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(nameof(StartDiscoveryVisibility)));
        PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(nameof(CancelDiscoveryVisibility)));
    }

    public event PropertyChangedEventHandler? PropertyChanged;

    private void OnPageLoaded(object sender, RoutedEventArgs e)
    {
        // Ensure a default selection is set when a page is loaded
        if (SelectedConnectionTypeIndex == -1)
        {
            SelectedConnectionTypeIndex = 0;
        }
    }

    private void AddressNumberBox_OnTextChanged(object sender, TextChangedEventArgs e)
    {
        ViewModel.SelectedAddress = (byte)(AddressNumberBox.Value ?? 0);
    }

    private void AddressNumberBox_OnValueChanged(object sender, NumberBoxValueChangedEventArgs args)
    {
        ViewModel.SelectedAddress = (byte)(AddressNumberBox.Value ?? 0);
    }
}