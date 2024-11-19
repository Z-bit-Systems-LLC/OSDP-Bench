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
}