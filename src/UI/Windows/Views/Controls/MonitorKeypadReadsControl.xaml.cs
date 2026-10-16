using System.Windows;
using System.Windows.Controls;

namespace OSDPBench.Windows.Views.Controls;

public partial class MonitorKeypadReadsControl
{
    public MonitorKeypadReadsControl()
    {
        InitializeComponent();
    }

    private void ClearButton_OnClick(object sender, RoutedEventArgs e)
    {
        KeypadTextBox.Clear();
        var bindingExpression = KeypadTextBox.GetBindingExpression(TextBox.TextProperty);
        bindingExpression?.UpdateSource();
    }

    private void KeypadTextBox_OnTextChanged(object sender, TextChangedEventArgs e)
    {
        // Keep the most recent entries in view once the text is wider than the box
        KeypadTextBox.ScrollToHorizontalOffset(double.MaxValue);
    }
}