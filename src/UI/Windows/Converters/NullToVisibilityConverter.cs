using System.Globalization;
using System.Windows;
using System.Windows.Data;

namespace OSDPBench.Windows.Converters;

public class NullToVisibilityConverter : IValueConverter
{
    public object Convert(object? value, Type targetType, object? parameter, CultureInfo culture)
    {
        bool isVisible = value != null;
        if (parameter != null && bool.Parse(parameter.ToString() ?? "False")) isVisible = !isVisible;
        return isVisible ? Visibility.Visible : Visibility.Collapsed;
    }

    public object ConvertBack(object? value, Type targetType, object? parameter, CultureInfo culture)
    {
        throw new NotImplementedException();
    }
}