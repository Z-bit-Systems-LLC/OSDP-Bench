using System;
using Microsoft.UI.Xaml.Data;

namespace WinUI.Converters;

public class TimeConverter : IValueConverter
{
    public object Convert(object value, Type targetType, object parameter, string language)
    {
        var time = (DateTime)(value ?? DateTime.MinValue);

        return time != DateTime.MinValue ? time.ToString("g") : string.Empty;
    }

    public object ConvertBack(object value, Type targetType, object parameter, string language)
    {
        throw new NotImplementedException();
    }
}