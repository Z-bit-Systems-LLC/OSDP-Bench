using System;
using System.Globalization;
using System.Windows;
using System.Windows.Data;

namespace OSDPBench.Windows.Converters
{
    public class BooleanToVisibilityConverter : IValueConverter
    {
        public object Convert(object? value, Type targetType, object? parameter, CultureInfo culture)
        {
            if (value is bool booleanValue)
            {
                // Handle the "Invert" parameter
                var isInverted = parameter != null && parameter.ToString()!.Equals("Invert", StringComparison.OrdinalIgnoreCase);

                // Return the inverted or non-inverted result
                return isInverted ? booleanValue ? Visibility.Collapsed : Visibility.Visible :
                    booleanValue ? Visibility.Visible : Visibility.Collapsed;
            }

            return Visibility.Collapsed; // Default
        }

        public object ConvertBack(object? value, Type targetType, object? parameter, CultureInfo culture)
        {
            if (value is Visibility visibilityValue)
            {
                var isInverted = parameter != null && parameter.ToString()!.Equals("Invert", StringComparison.OrdinalIgnoreCase);

                // Convert visibility back to boolean
                return isInverted ? visibilityValue == Visibility.Collapsed : visibilityValue == Visibility.Visible;
            }

            return false; // Default
        }
    }
}