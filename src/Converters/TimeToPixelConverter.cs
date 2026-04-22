using CreanexDataVis.Services;
using System.Globalization;
using System.Windows.Data;

namespace CreanexDataVis.Converters;

internal class TimeToPixelConverter : IValueConverter
{
    public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
    {
        return (double)value * 1000 * TimelineRenderer.MsToPixel;
    }

    public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
    {
        return (double)value / 1000 / TimelineRenderer.MsToPixel;
    }
}