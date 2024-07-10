using System.Globalization;
using System.Windows;
using System.Windows.Data;

namespace OSDPBench.Windows.Converters
{
    internal class NullToVisibilityConverter : IValueConverter
    {
        public object Convert(object? value, Type targetType, object? parameter, CultureInfo culture)
        {
            bool inverted = bool.Parse(parameter?.ToString() ?? "False");
            return !inverted ? value != null ? Visibility.Visible : Visibility.Collapsed :
                value == null ? Visibility.Visible : Visibility.Collapsed;
        }

        public object ConvertBack(object? value, Type targetType, object? parameter, CultureInfo culture)
        {
            throw new NotImplementedException();
        }
    }
}
