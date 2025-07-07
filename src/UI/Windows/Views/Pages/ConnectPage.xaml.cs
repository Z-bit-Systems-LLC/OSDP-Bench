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

        // Initialize connection types
        _connectionTypes = new System.Collections.ObjectModel.ObservableCollection<string>();
        UpdateConnectionTypes();

        // Subscribe to culture changes
        Core.Resources.Resources.PropertyChanged += OnResourcesPropertyChanged;

        // Subscribe to StatusLevel changes to update button visibility
        ViewModel.PropertyChanged += OnViewModelPropertyChanged;

        InitializeComponent();

        // Set up loaded event to ensure proper initialization
        Loaded += OnPageLoaded;
    }

    public ConnectViewModel ViewModel { get; }

    private readonly System.Collections.ObjectModel.ObservableCollection<string> _connectionTypes;
    public System.Collections.ObjectModel.ObservableCollection<string> ConnectionTypes => _connectionTypes;

    private void UpdateConnectionTypes()
    {
        int previousSelectedConnectionTypeIndex = SelectedConnectionTypeIndex;
        
        _connectionTypes.Clear();
        
        _connectionTypes.Add(Core.Resources.Resources.GetString("ConnectionType_Discover"));
        _connectionTypes.Add(Core.Resources.Resources.GetString("ConnectionType_Manual"));

        SelectedConnectionTypeIndex = previousSelectedConnectionTypeIndex;
    }

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
        // When culture changes, update the connection types with new localized strings
        // Since we're updating in place. The selection should be maintained
        UpdateConnectionTypes();
        
        // Ensure the UI updates properly
        UpdateButtonVisibility();
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
    
    private void OnConnectionGridSizeChanged(object sender, SizeChangedEventArgs e)
    {
        // Check if we have enough width for side-by-side layout
        // Threshold of 500 pixels seems reasonable for this layout
        const double widthThreshold = 500;
        
        if (ButtonPanel != null)
        {
            if (e.NewSize.Width < widthThreshold)
            {
                // Narrow: Move buttons to the second row, left aligned
                Grid.SetRow(ButtonPanel, 1);
                Grid.SetColumn(ButtonPanel, 0);
                Grid.SetColumnSpan(ButtonPanel, 2);
                ButtonPanel.HorizontalAlignment = HorizontalAlignment.Left;
            }
            else
            {
                // Wide: Buttons on same row as title, right aligned
                Grid.SetRow(ButtonPanel, 0);
                Grid.SetColumn(ButtonPanel, 1);
                Grid.SetColumnSpan(ButtonPanel, 1);
                ButtonPanel.HorizontalAlignment = HorizontalAlignment.Right;
            }
        }
    }
}