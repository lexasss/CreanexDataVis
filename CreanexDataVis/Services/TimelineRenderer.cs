using CreanexDataVis.Models;
using System.Windows;
using System.Windows.Media;
using System.Windows.Media.Imaging;

namespace CreanexDataVis.Services;

internal class TimelineRenderer
{
    public ImageSource? Render(MappingRecord[] records)
    {
        if (records.Length == 0)
            return null;

        long startTime = records[0].TimeStamp;
        long endTime = records[^1].TimeStamp;

        int trackCount = TrackBrushes.Length;

        var bitmap = new RenderTargetBitmap(
            (int)((endTime - startTime) * MsToPixel) + 20,
            trackCount * (TrackHeight + TrackSpacing) + Margin + TrackHeight, // add extra space for timeline
            96, 96,
            PixelFormats.Pbgra32);

        var image = DrawTracks(records);
        bitmap.Render(image);

        var timeline = DrawTimeline(records, trackCount * (TrackHeight + TrackSpacing));
        bitmap.Render(timeline);

        return bitmap;
    }

    // Internal

    private const int TrackHeight = 20;     // pixels
    private const int TrackSpacing = 10;    // pixels
    private const int DotSize = 15;         // pixels
    private const int Margin = 5;           // pixels
    private const double MsToPixel = 0.1;   // scale
    private const int TimeInterval = 1000;  // ms

    private readonly double TimelineFontSize = 9.5; // avoid sizes 10-11, as these are not printed at distances x >= 11000 (.NET bug reported at 2012 already!)
    private readonly Typeface TimelineFontFamily = new Typeface("Segoe UI");
    private readonly Brush TimelineBrush = Brushes.Black;
    private readonly Brush TrackBackgroundBrush = new SolidColorBrush(Color.FromRgb(220, 220, 220));
    private readonly Brush[] TrackBrushes =
    [
        new SolidColorBrush(Colors.Gold),
        new SolidColorBrush(Colors.DarkRed),
        new SolidColorBrush(Colors.Blue),
        new SolidColorBrush(Colors.Orange),
        new SolidColorBrush(Colors.Purple),
        new SolidColorBrush(Colors.Olive),
        new SolidColorBrush(Colors.Green),
    ];

    DrawingVisual DrawTracks(MappingRecord[] records)
    {
        long startTime = records[0].TimeStamp;
        long endTime = records[^1].TimeStamp;

        int trackCount = TrackBrushes.Length;

        int width = (int)((endTime - startTime) * MsToPixel) + 20;
        int[] trackStarts = new int[trackCount];

        var dv = new DrawingVisual();
        using (var dc = dv.RenderOpen())
        {
            int[] trackOn = new int[trackCount];

            for (int i = 0; i < trackCount; i++)
            {
                int y = Margin + i * (TrackHeight + TrackSpacing);
                dc.DrawRectangle(TrackBackgroundBrush, null, new Rect(0, y, width, TrackHeight));
            }

            for (int i = 0; i < records.Length - 1; i++)
            {
                var r = records[i];

                int x = (int)((r.TimeStamp - startTime) * MsToPixel);

                trackOn[0] = r.DrivingStart ? 1 : (r.DrivingEnd ? 0 : trackOn[0]);
                trackOn[1] = r.GazeLeftWindow ? 1 : 0;
                trackOn[2] = r.GazeFrontWindow ? 1 : 0;
                trackOn[3] = r.GazeRightWindow ? 1 : 0;
                trackOn[4] = r.GazeTDAScreen ? 1 : 0;
                trackOn[5] = r.GazeHarvesterHead ? 1 : 0;
                trackOn[6] = r.GazeTargetTreeId > 0 ? 1 : 0;

                for (int j = 0; j < trackOn.Length; j++)
                {
                    if (trackOn[j] > 0 && trackStarts[j] == 0)
                    {
                        trackStarts[j] = x;
                    }
                    else if (trackOn[j] == 0 && trackStarts[j] > 0)
                    {
                        int y = Margin + j * (TrackHeight + TrackSpacing);

                        dc.DrawRectangle(TrackBrushes[j], null, new Rect(trackStarts[j], y, x - trackStarts[j], TrackHeight));

                        trackStarts[j] = 0;
                    }
                }

                // Grab events (above tracks)
                int dotY = Margin + TrackHeight / 2;

                if (r.GrabTargetTreeId > 0)
                    dc.DrawEllipse(new SolidColorBrush(Colors.Green), null, new Point(x, dotY), DotSize/2, DotSize/2);
                if (r.GrabNonTargetTreeId > 0)
                    dc.DrawEllipse(new SolidColorBrush(Colors.Red), null, new Point(x, dotY), DotSize / 2, DotSize / 2);
            }

            // Finalize ongoing tracks
            int xEnd = (int)((endTime - startTime) * MsToPixel);
            for (int j = 0; j < trackOn.Length; j++)
            {
                if (trackStarts[j] > 0)
                {
                    int y = Margin + j * (TrackHeight + TrackSpacing);
                    dc.DrawRectangle(TrackBrushes[j], null, new Rect(trackStarts[j], y, xEnd - trackStarts[j], TrackHeight));
                }
            }
        }

        return dv;
    }

    DrawingVisual DrawTimeline(MappingRecord[] records, int y)
    {
        double dpi = VisualTreeHelper.GetDpi(Application.Current.MainWindow).PixelsPerDip;
        
        var dv = new DrawingVisual();

        using (var dc = dv.RenderOpen())
        {
            long startTime = records[0].TimeStamp;
            long endTime = records[^1].TimeStamp;

            long time = startTime;

            while (time < endTime)
            {
                var t = time - startTime;
                time += TimeInterval;

                int x = (int)(t * MsToPixel);

                dc.DrawText(
                    new FormattedText(
                        (t / 1000).ToString("N1"),
                        System.Globalization.CultureInfo.InvariantCulture,
                        FlowDirection.LeftToRight,
                        TimelineFontFamily,
                        TimelineFontSize,
                        TimelineBrush,
                        dpi),
                    new Point(x, y));
            }
        }

        return dv;
    }
}
