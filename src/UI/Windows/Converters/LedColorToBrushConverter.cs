using System.Globalization;
using System.Windows.Data;
using System.Windows.Media;
using OSDP.Net.Model.CommandData;

namespace OSDPBench.Windows.Converters;

internal class LedColorToBrushConverter : IValueConverter
{
    public object Convert(object? value, Type targetType, object? parameter, CultureInfo culture)
    {
        var ledColor = value is LedColor color ? color : LedColor.Black;

        return ledColor switch
        {
            LedColor.Black => new SolidColorBrush(Colors.Black),
            LedColor.Red => new SolidColorBrush(Colors.Red),
            LedColor.Green => new SolidColorBrush(Colors.Green),
            LedColor.Amber => new SolidColorBrush(Color.FromRgb(255, 191, 0)),
            LedColor.Blue => new SolidColorBrush(Colors.Blue),
            LedColor.Magenta => new SolidColorBrush(Colors.Magenta),
            LedColor.Cyan => new SolidColorBrush(Colors.Cyan),
            LedColor.White => new SolidColorBrush(Colors.White),
            _ => new SolidColorBrush(Colors.Transparent)
        };
    }

    public object ConvertBack(object? value, Type targetType, object? parameter, CultureInfo culture)
    {
        throw new NotImplementedException();
    }
}
