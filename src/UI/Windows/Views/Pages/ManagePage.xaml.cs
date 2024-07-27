using System.Windows.Controls;
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

            if (eventArgs.AddedItems.Count > 0)
            {
                var selectedItem = eventArgs.AddedItems[0];
                switch (selectedItem)
                {
                    case SetCommunicationAction when ViewModel.ConnectedPortName != null:
                    {
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
                        break;
                    }
                }
            }
        }
    }
}
