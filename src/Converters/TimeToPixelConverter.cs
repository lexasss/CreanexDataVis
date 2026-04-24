using CreanexDataVis.Services;
using System.Globalization;
using System.Windows.Data;

namespace CreanexDataVis.Converters;

internal class TimeToPixelConverter : IValueConverter
{
    public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
    {
        return TimelineRenderer.SecondsToPixels((double)value);
    }

    public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
    {
        return TimelineRenderer.PixelsToSeconds((double)value);
    }
}