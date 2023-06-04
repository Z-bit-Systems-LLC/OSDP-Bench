using System;
using Microsoft.UI.Xaml.Data;

namespace WinUI.Converters;

public class CardDataSizeConverter : IValueConverter
{
    public object Convert(object value, Type targetType, object parameter, string language)
    {
        var cardData = (string)(value ?? string.Empty);

        return cardData.Length > 0 ? $"{cardData.Length}-bits" : string.Empty;
    }

    public object ConvertBack(object value, Type targetType, object parameter, string language)
    {
        throw new NotImplementedException();
    }
}