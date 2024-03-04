using MvvmCross.Converters;
using System;
using System.Globalization;

namespace OSDPBench.Core.ValueConverters
{
    public class CardDataSizeValueConverter : MvxValueConverter<string, string>
    {
        protected override string Convert(string value, Type targetType, object parameter, CultureInfo cultureInfo)
        {
            return value?.Length > 0 ? $"{value.Length}-bits" : string.Empty;
        }
    }
}
