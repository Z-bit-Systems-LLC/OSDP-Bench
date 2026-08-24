using System.Globalization;
using System.Windows;
using System.Windows.Data;
using System.Windows.Media;
using OSDP.Net.LineQuality;

namespace OSDPBench.Windows.Converters;

/// <summary>
/// Maps a <see cref="LineQualityVerdict"/> onto a theme-aware semantic brush.
/// </summary>
/// <remarks>
/// Marginal deliberately reads as a warning rather than a success. It means the line is already
/// carrying errors, which is not a basis for choosing an operating rate.
/// </remarks>
internal class LineQualityVerdictConverter : IValueConverter
{
    public object Convert(object? value, Type targetType, object? parameter, CultureInfo culture)
    {
        string resourceKey = (LineQualityVerdict)(value ?? LineQualityVerdict.Untested) switch
        {
            LineQualityVerdict.Pass => "SemanticSuccessBrush",
            LineQualityVerdict.Marginal => "SemanticWarningBrush",
            LineQualityVerdict.Fail => "SemanticErrorBrush",
            _ => "SemanticInfoBrush"
        };

        return Application.Current?.TryFindResource(resourceKey) as Brush ??
               new SolidColorBrush(Colors.Gray);
    }

    public object ConvertBack(object? value, Type targetType, object? parameter, CultureInfo culture)
    {
        throw new NotSupportedException();
    }
}
