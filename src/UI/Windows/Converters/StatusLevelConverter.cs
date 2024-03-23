using System.Globalization;
using System.Windows.Data;
using System.Windows.Media;
using OSDPBench.Core.ViewModels.Pages;

namespace OSDPBench.Windows.Converters;

internal class StatusLevelConverter : IValueConverter
{
    public object Convert(object? value, Type targetType, object? parameter, CultureInfo culture)
    {
        var statusLevel = (StatusLevel) (value ?? StatusLevel.None);

        switch (statusLevel)
        {
            case StatusLevel.Discovering:
            case StatusLevel.NotReady:
            case StatusLevel.Connecting:
                return new SolidColorBrush(Colors.Orange);
            case StatusLevel.Error:
                return new SolidColorBrush(Colors.Red);
            default:
                return new SolidColorBrush(Colors.Green);
        }
    }

    public object ConvertBack(object? value, Type targetType, object? parameter, CultureInfo culture)
    {
        throw new NotImplementedException();
    }
}