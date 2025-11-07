using System.Windows;
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

        // Set up loaded event to ensure proper initialization
        Loaded += OnPageLoaded;
    }

    public ConnectViewModel ViewModel { get; }

    private void OnPageLoaded(object sender, RoutedEventArgs e)
    {
        // Ensure a default selection is set when a page is loaded
        if (ViewModel.SelectedConnectionTypeIndex == -1)
        {
            ViewModel.SelectedConnectionTypeIndex = 0;
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