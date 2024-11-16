using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using OSDPBench.Core.Actions;
using OSDPBench.Core.Models;
using OSDPBench.Core.ViewModels.Pages;
using OSDPBench.Windows.Views.Controls;
using Wpf.Ui.Controls;

namespace OSDPBench.Windows.Views.Pages
{
    /// <summary>
    /// Interaction logic for ManagePage.xaml
    /// </summary>
    public partial class ManagePage : INavigableView<ManageViewModel>
    {
        public ManagePage(ManageViewModel viewModel)
        {
            ViewModel = viewModel;
            DataContext = this;

            InitializeComponent();
        }

        public ManageViewModel ViewModel { get; }

        private void DeviceActionsComboBox_OnSelectionChanged(object sender, SelectionChangedEventArgs eventArgs)
        {
            DeviceActionControl.Children.Clear();
            
            switch (DeviceActionsComboBox.SelectedValue)
            {
                case SetCommunicationAction when ViewModel.ConnectedPortName != null:
                {
                    SetCommunicationActionControl();
                    break;
                }
                case MonitorCardReads when ViewModel.ConnectedPortName != null:
                {
                    MonitorCardReadsControl();
                    break;
                }
            }
        }

        private void SetCommunicationActionControl()
        {
            if (ViewModel.ConnectedPortName == null) return;
            
            PerformActionButton.Visibility = Visibility.Visible;

            ViewModel.DeviceActionParameter = new CommunicationParameters(ViewModel.ConnectedPortName,
                ViewModel.ConnectedBaudRate, ViewModel.ConnectedAddress);
            var actionControl = new SetCommunicationControl(ViewModel.AvailableBaudRates.ToArray(),
                ViewModel.ConnectedBaudRate, ViewModel.ConnectedAddress);
            actionControl.PropertyChanged += (_, _) =>
            {
                ViewModel.DeviceActionParameter = new CommunicationParameters(
                    ViewModel.ConnectedPortName, (uint)actionControl.SelectedBaudRate,
                    actionControl.SelectedAddress);
            };
            DeviceActionControl.Children.Add(actionControl);
        }

        private void MonitorCardReadsControl()
        {
            PerformActionButton.Visibility = Visibility.Collapsed;

            var actionControl = new MonitorCardReadsControl();
            actionControl.CardNumberTextBox.SetBinding(System.Windows.Controls.TextBox.TextProperty, new Binding("LastCardNumberRead")
            {
                Source = ViewModel
            });

            DeviceActionControl.Children.Add(actionControl);
        }

        private void DeviceInformationStackPanel_OnIsVisibleChanged(object sender, DependencyPropertyChangedEventArgs e)
        {
            if (DeviceInformationStackPanel.Visibility != Visibility.Visible || ViewModel.StatusLevel != StatusLevel.Connected) return;

            DeviceActionsComboBox.SelectedIndex = 0;
        }
    }
}
