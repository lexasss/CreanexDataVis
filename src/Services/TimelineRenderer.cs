using CreanexDataVis.Models;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;
using System.Windows.Media.Imaging;

namespace CreanexDataVis.Services;

internal class TimelineRenderer
{
    public class VisualHost : FrameworkElement
    {
        public VisualHost(DrawingVisual[] visuals)
        {
            _children = new VisualCollection(this);
            foreach (var visual in visuals)
                _children.Add(visual);
        }

        protected override int VisualChildrenCount => _children.Count;

        protected override Visual GetVisualChild(int index)
        {
            return _children[index];
        }

        private readonly VisualCollection _children;
    }

    public bool CropBlankPeriods { get; set; } = true;

    public static double SecondsToPixels(double seconds) => seconds * 1000 * MsToPixel;
    public static double PixelsToSeconds(double pixels) => pixels / 1000 / MsToPixel;

    public ImageSource? Render(TimelineRecord[] records)
    {
        if (records.Length == 0)
            return null;

        long startTime = records[0].Timestamp;
        long endTime = records[^1].Timestamp;

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

        return CreateBitmapSource(bitmap, startTime, eventsRange);
    }

    public Canvas? Create(TimelineRecord[] records, out double secondsOffset)
    {
        if (records.Length == 0)
        {
            secondsOffset = 0;
            return null;
        }

        int trackCount = TrackBrushes.Length;

        var tracks = DrawTracks(records, out var eventsRange);

        var timeline = DrawTimeline(records, trackCount * (TrackHeight + TrackSpacing));

        // Clip the bitmap to the range of events, with some padding (1000 ms)

        long startTime = records[0].Timestamp;
        long duration = records[^1].Timestamp - startTime;
        long blankPeriodBefore = CropBlankPeriods ? Math.Max(0, eventsRange.Start - startTime - 1000) : 0;
        long blankPeriodAfter = CropBlankPeriods ? Math.Max(0, records[^1].Timestamp - eventsRange.End - 1000) : 0;

        var host = new VisualHost([tracks, timeline])
        {
            RenderTransform = new TranslateTransform(-(blankPeriodBefore + blankPeriodAfter) * MsToPixel, 0),
            Width = (duration - blankPeriodBefore - blankPeriodAfter) * MsToPixel + Margin,
            Height = trackCount * (TrackHeight + TrackSpacing) + Margin + TrackHeight
        };

        var timeMarker = new System.Windows.Shapes.Line
        {
            X1 = Margin,
            Y1 = 0,
            X2 = Margin,
            Y2 = host.Height,
            Stroke = Brushes.Red,
            StrokeThickness = 1
        };

        var canvas = new Canvas
        {
            Width = host.Width,
            Height = host.Height,
            Children = { host, timeMarker }
        };

        secondsOffset = (double)(eventsRange.Start - startTime - 1000) / 1000;  // -1000 due to padding
        return canvas;
    }

    // Internal

    record class Range(long Start, long End);

    const int TrackHeight = 20;     // pixels
    const int TrackSpacing = 10;    // pixels
    const int DotSize = 15;         // pixels
    const int Margin = 5;           // pixels
    const int TimeInterval = 1000;  // ms

    const double MsToPixel = 0.1;
    const double TreeIdFontSize = 9; // avoid sizes 10-11, as these are not printed at distances x >= 11000 (.NET bug since 2012)
    const double TimelineFontSize = 9.5; // avoid sizes 10-11, as these are not printed at distances x >= 11000 (.NET bug since 2012)

    readonly Typeface FontFamily = new("Segoe UI");
    readonly Brush FontBrush = Brushes.Black;
    readonly Brush BackgroundBrush = Brushes.WhiteSmoke;
    readonly Brush TrackBackgroundBrush = new SolidColorBrush(Color.FromRgb(220, 220, 220));
    readonly Brush TrackDrivingBackwardBrush = Brushes.Red;
    readonly Brush[] TrackBrushes =
    [
        Brushes.Green,
        Brushes.DarkRed,
        Brushes.Blue,
        Brushes.Orange,
        Brushes.Purple,
        Brushes.Olive,
        Brushes.Turquoise,
    ];

    DrawingVisual DrawTracks(TimelineRecord[] records, out Range timeRange)
    {
        double dpi = VisualTreeHelper.GetDpi(Application.Current.MainWindow).PixelsPerDip;
        int minTreeId = records.Min(r => r.GazeTargetTreeId == 0 ? int.MaxValue : r.GazeTargetTreeId);

        long startTime = records[0].Timestamp;
        long endTime = records[^1].Timestamp;

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
            dc.DrawRectangle(BackgroundBrush, null, new Rect(0, 0,
                width + Margin, Margin + trackCount * (TrackHeight + TrackSpacing) + TrackHeight));

            for (int i = 0; i < trackCount; i++)
            {
                int y = Margin + i * (TrackHeight + TrackSpacing);
                dc.DrawRectangle(TrackBackgroundBrush, null, new Rect(0, y, width + Margin, TrackHeight));
            }

            for (int i = 0; i < records.Length; i++)
            {
                var r = records[i];

                int x = (int)((r.Timestamp - startTime) * MsToPixel);

                int prevGazeTargetTreeId = trackStatus[6];

                trackStatus[0] = r.DrivingStart != 0 ? r.DrivingStart : (r.DrivingEnd != 0 ? 0 : trackStatus[0]);
                trackStatus[1] = r.GazeLeftWindow ? 1 : 0;
                trackStatus[2] = r.GazeFrontWindow ? 1 : 0;
                trackStatus[3] = r.GazeRightWindow ? 1 : 0;
                trackStatus[4] = r.GazeTDAScreen ? 1 : 0;
                trackStatus[5] = r.GazeHarvesterHead ? 1 : 0;
                trackStatus[6] = r.GazeTargetTreeId > 0 ? r.GazeTargetTreeId : 0;

                bool wasDrivingBackward = r.DrivingEnd > 0; // weird decision to use 1 for backward and -1 for forward

                latestEventTime = trackStatus.Any(s => s != 0) ? r.Timestamp : latestEventTime;
                if (earliestEventTime < 0)
                {
                    earliestEventTime = trackStatus.Any(s => s != 0) ? r.Timestamp : earliestEventTime;
                }

                for (int j = 0; j < trackStarts.Length; j++)
                {
                    if (trackStatus[j] != 0 && trackStarts[j] == 0)
                    {
                        trackStarts[j] = x;
                    }
                    else 
                    {
                        bool finalizeTrack = trackStatus[j] == 0 && trackStarts[j] > 0;

                        int y = Margin + j * (TrackHeight + TrackSpacing);

                        if (finalizeTrack)
                        {
                            var brush = TrackBrushes[j];
                            if (j == 0 && wasDrivingBackward)   // change the brush when driving backward
                            {
                                brush = TrackDrivingBackwardBrush;
                            }

                            dc.DrawRectangle(brush, null, new Rect(trackStarts[j], y, x - trackStarts[j], TrackHeight));
                        }

                        // For gaze target tree ID track, also print the tree ID above the track when it changes
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

    DrawingVisual DrawTimeline(TimelineRecord[] records, int y)
    {
        double dpi = VisualTreeHelper.GetDpi(Application.Current.MainWindow).PixelsPerDip;
        
        var dv = new DrawingVisual();
        using (var dc = dv.RenderOpen())
        {
            long startTime = records[0].Timestamp;
            long endTime = records[^1].Timestamp;

            long time = startTime;

            while (time < endTime)
            {
                var t = time - startTime;
                time += TimeInterval;

                int x = (int)(t * MsToPixel);

                var timespan = TimeSpan.FromMilliseconds(t);

                dc.DrawText(
                    new FormattedText(
                        $"{timespan.Minutes:D2}:{timespan.Seconds:D2}",
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

    private static BitmapSource CreateBitmapSource(RenderTargetBitmap bitmap, long startTime, Range timeRange)
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

    private static BitmapSource CreateBitmapSource(RenderTargetBitmap bitmap, int offsetX)
    {
        var clippedWidth = bitmap.PixelWidth - offsetX;
        var stride = clippedWidth * 4;
        var pixels = new byte[stride * bitmap.PixelHeight];

        bitmap.CopyPixels(new Int32Rect(offsetX, 0, clippedWidth, bitmap.PixelHeight),
            pixels, stride, 0);

        return BitmapSource.Create(clippedWidth, bitmap.PixelHeight,
            96, 96,
            PixelFormats.Pbgra32,
            null, pixels, stride);
    }
}
