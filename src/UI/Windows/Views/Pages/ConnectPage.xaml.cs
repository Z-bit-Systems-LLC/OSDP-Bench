﻿using OSDPBench.Core.ViewModels.Pages;
using Wpf.Ui.Abstractions.Controls;

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
}