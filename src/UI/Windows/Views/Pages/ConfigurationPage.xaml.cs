using System.Text.RegularExpressions;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using OSDPBench.Core.Services;
using OSDPBench.Core.ViewModels.Pages;
using Wpf.Ui.Abstractions.Controls;
using Wpf.Ui.Controls;

namespace OSDPBench.Windows.Views.Pages;

/// <summary>
/// Interaction logic for ConfigurationPage.xaml
/// </summary>
public partial class ConfigurationPage : INavigableView<ConfigurationViewModel>
{
    public ConfigurationPage(ConfigurationViewModel viewModel)
    {
        ViewModel = viewModel;
        DataContext = this;

        InitializeComponent();
    }

    public ConfigurationViewModel ViewModel { get; }

    private void AddressNumberBox_OnTextChanged(object sender, TextChangedEventArgs e)
    {
        ViewModel.SelectedAddress = (byte)(AddressNumberBox.Value ?? 0);
    }

    private void AddressNumberBox_OnValueChanged(object sender, NumberBoxValueChangedEventArgs args)
    {
        ViewModel.SelectedAddress = (byte)(AddressNumberBox.Value ?? 0);
    }
    
    [GeneratedRegex("[^0-9a-fA-F]")]
    private static partial Regex NonHexCharacterRegex();

    private void SecurityKeyTextBox_OnPreviewTextInput(object sender, TextCompositionEventArgs e)
    {
        e.Handled = NonHexCharacterRegex().IsMatch(e.Text);
    }

    /// <summary>
    /// Normalizes pasted text so a key copied in a delimited format (for example "00-11-22" or
    /// "00:11:22") is accepted, with the delimiters removed. Stripping happens here rather than
    /// after insertion because MaxLength is applied to the inserted text - a delimited 16-byte key
    /// is 47 characters, so a later cleanup would have already lost the truncated tail.
    /// </summary>
    private void SecurityKeyTextBox_OnPasting(object sender, DataObjectPastingEventArgs e)
    {
        if (!e.DataObject.GetDataPresent(typeof(string)))
        {
            e.CancelCommand();
            return;
        }

        var original = (string)e.DataObject.GetData(typeof(string))!;
        var hexOnly = HexConverter.NormalizeHexInput(original);
        if (hexOnly.Length == 0)
        {
            e.CancelCommand();
            return;
        }

        // Nothing was removed, so let the text box handle the paste unchanged
        if (hexOnly == original) return;

        var dataObject = new DataObject();
        dataObject.SetData(DataFormats.UnicodeText, hexOnly);
        e.DataObject = dataObject;
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
                // Narrow: Move buttons below the radio buttons (row 2), left aligned
                Grid.SetRow(ButtonPanel, 2);
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