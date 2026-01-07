using System.Globalization;
using System.Windows;
using System.Windows.Data;

namespace OSDPBench.Windows.Converters;

public class IndexToVisibilityConverter : IValueConverter
{
    public object Convert(object? value, Type targetType, object? parameter, CultureInfo culture)
    {
        if (value is int index && parameter is string paramString)
        {
            // Support pipe-separated indices like "1|2"
            var targetIndices = paramString.Split('|');
            foreach (var targetStr in targetIndices)
            {
                if (int.TryParse(targetStr.Trim(), out int targetIndex) && index == targetIndex)
                {
                    return Visibility.Visible;
                }
            }
        }

        return Visibility.Collapsed;
    }

    public object ConvertBack(object? value, Type targetType, object? parameter, CultureInfo culture)
    {
        throw new NotImplementedException();
    }
}