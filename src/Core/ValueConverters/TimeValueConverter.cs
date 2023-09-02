using MvvmCross.Converters;
using System;
using System.Globalization;

namespace OSDPBench.Core.ValueConverters
{
    public class TimeValueConverter : MvxValueConverter<DateTime, string>
    {
        protected override string Convert(DateTime value, Type targetType, object parameter, CultureInfo cultureInfo)
        {
            return value != DateTime.MinValue ? value.ToString("G") : "None";
        }
    }
}
