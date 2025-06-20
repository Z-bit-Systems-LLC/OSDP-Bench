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
    }

    public ConnectViewModel ViewModel { get; }
        
    public IEnumerable<string> ConnectionTypes => [OSDPBench.Core.Resources.Resources.GetString("ConnectionType_Discover"), OSDPBench.Core.Resources.Resources.GetString("ConnectionType_Manual")];

    private void OnResourcesPropertyChanged(object? sender, PropertyChangedEventArgs e)
    {
        // When culture changes, notify that ConnectionTypes has changed
        PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(nameof(ConnectionTypes)));
    }

    public event PropertyChangedEventHandler? PropertyChanged;

    private void AddressNumberBox_OnTextChanged(object sender, TextChangedEventArgs e)
    {
        ViewModel.SelectedAddress = (byte)(AddressNumberBox.Value ?? 0);
    }

    private void AddressNumberBox_OnValueChanged(object sender, NumberBoxValueChangedEventArgs args)
    {
        ViewModel.SelectedAddress = (byte)(AddressNumberBox.Value ?? 0);
    }
}