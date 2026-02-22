using System.Net.Http;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Documents;
using OSDP.Net.Model.ReplyData;
using OSDPBench.Core.Actions;
using OSDPBench.Core.Models;
using OSDPBench.Core.ViewModels.Pages;
using OSDPBench.Windows.Views.Controls;
using Wpf.Ui.Abstractions.Controls;

namespace OSDPBench.Windows.Views.Pages;

/// <summary>
/// Interaction logic for ManagePage.xaml
/// </summary>
public partial class ManagePage : INavigableView<ManageViewModel>
{
    private static readonly FileTransferParameters FileTransferParameters = new();
        
    public ManagePage(ManageViewModel viewModel)
    {
        ViewModel = viewModel;
        DataContext = this;
            
        InitializeComponent();
            
        viewModel.SelectedDeviceAction = viewModel.AvailableDeviceActions.FirstOrDefault();
    }

    public ManageViewModel ViewModel { get; }

    private void DeviceActionsComboBox_OnSelectionChanged(object sender, SelectionChangedEventArgs eventArgs)
    {
        DeviceActionControl.Children.Clear();
        PerformActionButton.IsEnabled = true;

        if (ViewModel.ConnectedPortName == null)
        {
            return;
        }

        var selectedAction = DeviceActionsComboBox.SelectedValue as IDeviceAction;

        switch (selectedAction)
        {
            case ControlBuzzerAction:
                ControlBuzzerControl();
                break;

            case FileTransferAction:
                FileTransferControl();
                break;

            case MonitoringAction monitoringAction:
                switch (monitoringAction.MonitoringType)
                {
                    case MonitoringType.CardReads:
                        MonitorCardReadsControl();
                        break;

                    case MonitoringType.KeypadReads:
                        MonitorKeypadReadsControl();
                        break;
                }
                break;

            case ResetCypressDeviceAction:
                ResetControl();
                break;

            case SetCommunicationAction:
                SetCommunicationActionControl();
                break;

            case SetReaderLedAction:
                SetReaderLedActionControl();
                break;

            case SupervisionAction:
                SupervisionControl();
                break;
        }

        CheckCapabilitySupport(selectedAction);
    }

    private void CheckCapabilitySupport(IDeviceAction? selectedAction)
    {
        if (selectedAction?.RequiredCapability == null || ViewModel.CapabilitiesLookup == null)
        {
            return;
        }

        bool isSupported = selectedAction.RequiredCapability switch
        {
            CapabilityFunction.ReaderAudibleOutput => ViewModel.CapabilitiesLookup.AudioOutput,
            CapabilityFunction.ReaderLEDControl => ViewModel.CapabilitiesLookup.LedControl,
            _ => true
        };

        if (isSupported) return;

        string capabilityName = selectedAction.RequiredCapability switch
        {
            CapabilityFunction.ReaderAudibleOutput =>
                Core.Resources.Resources.GetString("Manage_CapabilityAudioOutput"),
            CapabilityFunction.ReaderLEDControl =>
                Core.Resources.Resources.GetString("Manage_CapabilityLedControl"),
            _ => selectedAction.RequiredCapability.ToString()!
        };

        string message = Core.Resources.Resources.GetString("Manage_CapabilityNotSupported")
            .Replace("{0}", capabilityName);

        var warningText = new TextBlock
        {
            Text = message,
            TextWrapping = TextWrapping.Wrap,
            Foreground = (System.Windows.Media.Brush)FindResource("SemanticWarningBrush"),
            Margin = new Thickness(0, 4, 0, 0)
        };

        DeviceActionControl.Children.Add(warningText);
        PerformActionButton.IsEnabled = false;
    }

    private void ControlBuzzerControl()
    {
        PerformActionButton.Visibility = Visibility.Visible;

        var actionControl = new ControlBuzzerControl();
        ViewModel.DeviceActionParameter = actionControl.GetBuzzerParameters();
        actionControl.PropertyChanged += (_, _) =>
        {
            ViewModel.DeviceActionParameter = actionControl.GetBuzzerParameters();
            PerformActionButton.IsEnabled = !actionControl.HasErrors;
        };

        DeviceActionControl.Children.Add(actionControl);
    }

    private void FileTransferControl()
    {
        PerformActionButton.Visibility = Visibility.Visible;
            
        ViewModel.DeviceActionParameter = FileTransferParameters;
            
        var actionControl = new FileTransferControl(FileTransferParameters);

        DeviceActionControl.Children.Add(actionControl);
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
                (byte)actionControl.SelectedAddress);
        };
        DeviceActionControl.Children.Add(actionControl);
    }

    private void MonitorCardReadsControl()
    {
        PerformActionButton.Visibility = Visibility.Collapsed;
            
        var actionControl = new MonitorCardReadsControl();
        actionControl.Initialize(ViewModel);

        DeviceActionControl.Children.Add(actionControl);
    }
        
    private void MonitorKeypadReadsControl()
    {
        PerformActionButton.Visibility = Visibility.Collapsed;
            
        var actionControl = new MonitorKeypadReadsControl();
        actionControl.KeypadTextBox.SetBinding(TextBox.TextProperty, new Binding("KeypadReadData")
        {
            Source = ViewModel,
            Mode = BindingMode.TwoWay
        });

        DeviceActionControl.Children.Add(actionControl);
    }

    private void ResetControl()
    {
        PerformActionButton.Visibility = Visibility.Visible;
            
        var actionControl = new ResetControl();

        DeviceActionControl.Children.Add(actionControl);
    }
        
    private void SetReaderLedActionControl()
    {
        PerformActionButton.Visibility = Visibility.Visible;

        var actionControl = new SetReaderLedControl();
        ViewModel.DeviceActionParameter = actionControl.GetLedParameters();
        actionControl.PropertyChanged += (_, _) =>
        {
            ViewModel.DeviceActionParameter = actionControl.GetLedParameters();
            PerformActionButton.IsEnabled = !actionControl.HasErrors;
        };

        DeviceActionControl.Children.Add(actionControl);
    }

    private void SupervisionControl()
    {
        PerformActionButton.Visibility = Visibility.Visible;

        var actionControl = new SupervisionControl();
        actionControl.Initialize(ViewModel);

        DeviceActionControl.Children.Add(actionControl);
    }

    private async void Hyperlink_OnClick(object sender, RoutedEventArgs eventArgs)
    {
        string vendorCode = ((Run)((Hyperlink)sender).Inlines.FirstInline).Text;
        string url = $"https://macvendors.com/query/{vendorCode}";

        using var client = new HttpClient();
        
        try
        {
            string result = await client.GetStringAsync(url);

            MessageBox.Show(result, Core.Resources.Resources.GetString("Dialog_VendorInformation_Title"));
        }
        catch (Exception exception)
        {
            MessageBox.Show(Core.Resources.Resources.GetString("Error_OUILookupFailed").Replace("{0}", exception.Message));
        }
    }
}