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
            (int)((endTime - startTime) * MsToPixel) + Margin,
            trackCount * (TrackHeight + TrackSpacing) + Margin + TrackHeight, // add extra space for timeline
            96, 96,
            PixelFormats.Pbgra32);

        var tracks = DrawTracks(records, out var eventsRange);
        bitmap.Render(tracks);

        var timeline = DrawTimeline(records, trackCount * (TrackHeight + TrackSpacing));
        bitmap.Render(timeline);

        // Clip the bitmap to the range of events, with some padding (1000 ms) on each side

        eventsRange = eventsRange with {
            Start = Math.Max(startTime, eventsRange.Start - 1000),
            End = Math.Min(endTime, eventsRange.End + 1000)
        };

        return Clip(bitmap, startTime, eventsRange);
    }

    // Internal

    record class Range(long Start, long End);

    const int TrackHeight = 20;     // pixels
    const int TrackSpacing = 10;    // pixels
    const int DotSize = 15;         // pixels
    const int Margin = 5;           // pixels
    const double MsToPixel = 0.1;   // scale
    const int TimeInterval = 1000;  // ms

    readonly double TreeIdFontSize = 9; // avoid sizes 10-11, as these are not printed at distances x >= 11000 (.NET bug reported at 2012 already!)
    readonly double TimelineFontSize = 9.5; // avoid sizes 10-11, as these are not printed at distances x >= 11000 (.NET bug reported at 2012 already!)
    readonly Typeface FontFamily = new("Segoe UI");
    readonly Brush FontBrush = Brushes.Black;
    readonly Brush TrackBackgroundBrush = new SolidColorBrush(Color.FromRgb(220, 220, 220));
    readonly Brush[] TrackBrushes =
    [
        new SolidColorBrush(Colors.Gold),
        new SolidColorBrush(Colors.DarkRed),
        new SolidColorBrush(Colors.Blue),
        new SolidColorBrush(Colors.Orange),
        new SolidColorBrush(Colors.Purple),
        new SolidColorBrush(Colors.Olive),
        new SolidColorBrush(Colors.Turquoise),
    ];

    DrawingVisual DrawTracks(MappingRecord[] records, out Range timeRange)
    {
        double dpi = VisualTreeHelper.GetDpi(Application.Current.MainWindow).PixelsPerDip;
        int minTreeId = records.Min(r => r.GazeTargetTreeId == 0 ? int.MaxValue : r.GazeTargetTreeId);

        long startTime = records[0].TimeStamp;
        long endTime = records[^1].TimeStamp;

        long earliestEventTime = -1;
        long latestEventTime = startTime;

        int trackCount = TrackBrushes.Length;
        double dotRadius = 0.5 * DotSize;

        int width = (int)((endTime - startTime) * MsToPixel);

        int[] trackStarts = new int[trackCount];
        int[] trackStatus = new int[trackCount];

        var dv = new DrawingVisual();
        using (var dc = dv.RenderOpen())
        {
            for (int i = 0; i < trackCount; i++)
            {
                int y = Margin + i * (TrackHeight + TrackSpacing);
                dc.DrawRectangle(TrackBackgroundBrush, null, new Rect(0, y, width + Margin, TrackHeight));
            }

            for (int i = 0; i < records.Length; i++)
            {
                var r = records[i];

                int x = (int)((r.TimeStamp - startTime) * MsToPixel);

                int prevGazeTargetTreeId = trackStatus[6];

                trackStatus[0] = r.DrivingStart ? 1 : (r.DrivingEnd ? 0 : trackStatus[0]);
                trackStatus[1] = r.GazeLeftWindow ? 1 : 0;
                trackStatus[2] = r.GazeFrontWindow ? 1 : 0;
                trackStatus[3] = r.GazeRightWindow ? 1 : 0;
                trackStatus[4] = r.GazeTDAScreen ? 1 : 0;
                trackStatus[5] = r.GazeHarvesterHead ? 1 : 0;
                trackStatus[6] = r.GazeTargetTreeId > 0 ? r.GazeTargetTreeId : 0;

                latestEventTime = trackStatus.Any(s => s > 0) ? r.TimeStamp : latestEventTime;
                if (earliestEventTime < 0)
                {
                    earliestEventTime = trackStatus.Any(s => s > 0) ? r.TimeStamp : earliestEventTime;
                }

                for (int j = 0; j < trackStarts.Length; j++)
                {
                    if (trackStatus[j] > 0 && trackStarts[j] == 0)
                    {
                        trackStarts[j] = x;
                    }
                    else 
                    {
                        bool finalizeTrack = trackStatus[j] == 0 && trackStarts[j] > 0;

                        int y = Margin + j * (TrackHeight + TrackSpacing);

                        if (finalizeTrack)
                            dc.DrawRectangle(TrackBrushes[j], null, new Rect(trackStarts[j], y, x - trackStarts[j], TrackHeight));

                        if (j == 6 && prevGazeTargetTreeId != trackStatus[j] && prevGazeTargetTreeId > 0)
                        {
                            dc.DrawText(
                                new FormattedText(
                                    (prevGazeTargetTreeId - minTreeId).ToString(),
                                    System.Globalization.CultureInfo.InvariantCulture,
                                    FlowDirection.LeftToRight,
                                    FontFamily,
                                    TreeIdFontSize,
                                    FontBrush,
                                    dpi),
                                new Point(trackStarts[j], y + 3));
                        }

                        if (finalizeTrack)
                            trackStarts[j] = 0;
                    }
                }

                // Grab events (above tracks)
                int dotY = Margin + TrackHeight / 2;

                if (r.GrabTargetTreeId > 0)
                    dc.DrawEllipse(new SolidColorBrush(Colors.Green), null, new Point(x, dotY), dotRadius, dotRadius);
                if (r.GrabNonTargetTreeId > 0)
                    dc.DrawEllipse(new SolidColorBrush(Colors.Red), null, new Point(x, dotY), dotRadius, dotRadius);
            }

            // Finalize ongoing tracks
            for (int j = 0; j < trackStarts.Length; j++)
            {
                if (trackStarts[j] > 0)
                {
                    int y = Margin + j * (TrackHeight + TrackSpacing);
                    dc.DrawRectangle(TrackBrushes[j], null, new Rect(trackStarts[j], y, width - trackStarts[j], TrackHeight));
                }
            }
        }

        timeRange = new Range(earliestEventTime, latestEventTime);
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
                        TimeSpan.FromMilliseconds(t).ToString("g"),
                        System.Globalization.CultureInfo.InvariantCulture,
                        FlowDirection.LeftToRight,
                        FontFamily,
                        TimelineFontSize,
                        FontBrush,
                        dpi),
                    new Point(x, y));
            }
        }

        return dv;
    }

    private static BitmapSource Clip(RenderTargetBitmap bitmap, long startTime, Range timeRange)
    {
        var clippedStartX = (int)((timeRange.Start - startTime) * MsToPixel);
        var clippedEndX = (int)((timeRange.End - startTime) * MsToPixel);
        var clippedWidth = clippedEndX - clippedStartX + Margin;
        var stride = clippedWidth * 4;
        var pixels = new byte[stride * bitmap.PixelHeight];

        bitmap.CopyPixels(new Int32Rect(clippedStartX, 0, clippedWidth, bitmap.PixelHeight),
            pixels, stride, 0);

        return BitmapSource.Create(clippedWidth, bitmap.PixelHeight,
            96, 96,
            PixelFormats.Pbgra32,
            null, pixels, stride);
    }
}
