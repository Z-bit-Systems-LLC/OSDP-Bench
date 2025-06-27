using System.Windows.Controls;
using System.ComponentModel;
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
        OSDPBench.Core.Resources.Resources.PropertyChanged += OnResourcesPropertyChanged;

        InitializeComponent();
        
        // Set up loaded event to ensure proper initialization
        this.Loaded += OnPageLoaded;
    }

    public ConnectViewModel ViewModel { get; }
        
    public IEnumerable<string> ConnectionTypes => [OSDPBench.Core.Resources.Resources.GetString("ConnectionType_Discover"), OSDPBench.Core.Resources.Resources.GetString("ConnectionType_Manual")];
    
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
            }
        }
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

    public event PropertyChangedEventHandler? PropertyChanged;
    
    private void OnPageLoaded(object sender, System.Windows.RoutedEventArgs e)
    {
        // Ensure default selection is set when page is loaded
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