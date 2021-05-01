using System;
using Windows.UI;
using Windows.UI.Xaml.Data;
using Windows.UI.Xaml.Media;
using OSDPBench.Core.ViewModels;

namespace OSDPBenchUWP.Converters
{
    public class StatusLevelConverter : IValueConverter
    {
        public object Convert(object value, Type targetType, object parameter, string language)
        {
            var statusLevel = (StatusLevel) (value ?? StatusLevel.None);

            switch (statusLevel)
            {
                case StatusLevel.None:
                    return new SolidColorBrush(Colors.Green);
                case StatusLevel.Processing:
                    return new SolidColorBrush(Colors.Orange);
                case StatusLevel.Error:
                    return new SolidColorBrush(Colors.Red);
                default:
                    throw new ArgumentOutOfRangeException();
            }
        }

        public object ConvertBack(object value, Type targetType, object parameter, string language)
        {
            throw new NotImplementedException();
        }
    }
}