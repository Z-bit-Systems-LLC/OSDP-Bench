using System.Windows.Controls;
using OSDPBench.Core.ViewModels.Pages;
using Wpf.Ui.Abstractions.Controls;
using Wpf.Ui.Controls;

namespace OSDPBench.Windows.Views.Pages;

/// <summary>
/// Interaction logic for ConnectPage.xaml
/// </summary>
public partial class ConnectPage : INavigableView<ConnectViewModel>
{
    public ConnectPage(ConnectViewModel viewModel)
    {
        ViewModel = viewModel;
        DataContext = this;

        InitializeComponent();

        Loaded += async (_, _) =>
        {
            if (!ViewModel.AvailableSerialPorts.Any())
            {
                await ViewModel.ScanSerialPortsCommand.ExecuteAsync(null);
            }
        };
    }

    public ConnectViewModel ViewModel { get; }
        
    public IEnumerable<string> ConnectionTypes => ["Discover", "Manual"];

    private void AddressNumberBox_OnTextChanged(object sender, TextChangedEventArgs e)
    {
        ViewModel.SelectedAddress = AddressNumberBox.Value ?? 0;
    }

    private void AddressNumberBox_OnValueChanged(object sender, NumberBoxValueChangedEventArgs args)
    {
        ViewModel.SelectedAddress = AddressNumberBox.Value ?? 0;
    }
}