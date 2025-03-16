using System.Globalization;
using System.Windows;
using System.Windows.Data;

namespace OSDPBench.Windows.Converters;

public class StringToVisibilityConverter : IValueConverter
{
    public object Convert(object? value, Type targetType, object? parameter, CultureInfo culture)
    {
        if (parameter is string paramString && paramString.Contains(';'))
        {
            // Split the parameter string by comma
            var acceptableValues = paramString.Split(';').Select(p => p.Trim());
            
            // Check if the value matches any of the acceptable values
            return acceptableValues.Contains(value?.ToString()) ? Visibility.Visible : Visibility.Collapsed;
        }
        
        // Original behavior for backward compatibility
        return value?.ToString() == parameter?.ToString() ? Visibility.Visible : Visibility.Collapsed;
    }

    public object ConvertBack(object? value, Type targetType, object? parameter, CultureInfo culture)
    {
        throw new NotImplementedException();
    }
}