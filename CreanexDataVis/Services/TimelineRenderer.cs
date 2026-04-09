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

        int trackCount = 1 + TrackBrushes.Length; // +1 for grab events

        int width = (int)((endTime - startTime) * MsToPixel) + 20;
        int height = trackCount * (TrackHeight + TrackSpacing) + Margin + TrackHeight; // +extra track for timeline labels

        int stride = width * 4;
        byte[] pixels = new byte[height * stride];

        //var context = new Context(pixels, width, height, stride, trackCount);

        var bitmap = new RenderTargetBitmap(
            width,
            height,
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
    private const double StripWidth = 60 * MsToPixel; // pixels
    private const int TimeInterval = 1000;            // ms

    private readonly double TimelineFontSize = 9.5; // avoid sizes 10-11, as these are not printed at distances x >= 11000 (.NET bug reported at 2012 already!)
    private readonly Typeface TimelineFontFamily = new Typeface("Segoe UI");
    private readonly Brush TimelineBrush = Brushes.Black;
    private readonly Brush TrackBackgroundBrush = new SolidColorBrush(Color.FromRgb(220, 220, 220));
    private readonly Brush[] TrackBrushes =
    [
        new SolidColorBrush(Colors.DarkRed),
        new SolidColorBrush(Colors.Blue),
        new SolidColorBrush(Colors.Orange),
        new SolidColorBrush(Colors.Purple),
        new SolidColorBrush(Colors.Olive),
        new SolidColorBrush(Colors.Green),
    ];
    /*
    private readonly string[] TrackNames =
    [
        "Grab",
        "Left Window",
        "Front Window",
        "Right Window",
        "TDA Screen",
        "Harvester Head",
        "Tree"
    ];

    private record class Context(byte[] Pixels, int Width, int Height, int Stride, int TrackCount);

    DrawingVisual DrawTracks(Context context, MappingRecord[] records)
    {
        long startTime = records[0].TimeStamp;

        for (int i = 0; i < context.TrackCount; i++)
        {
            int y = Margin + i * (TrackHeight + TrackSpacing);
            DrawBackgroundStrip(context, y);
        }

        for (int i = 0; i < records.Length; i++)
        {
            var r = records[i];

            int x = (int)((r.TimeStamp - startTime) * MsToPixel);

            DrawTrack(context, r.GazeLeftWindow, 0, x);
            DrawTrack(context, r.GazeFrontWindow, 1, x);
            DrawTrack(context, r.GazeRightWindow, 2, x);
            DrawTrack(context, r.GazeTDAScreen, 3, x);
            DrawTrack(context, r.GazeHarvesterHead, 4, x);
            DrawTrack(context, r.GazeTargetTreeId > 0, 5, x);

            // Grab event (above tracks)
            int dotY = Margin + TrackHeight / 2;

            if (r.GrabTargetTreeId > 0)
                DrawCircle(context, x, dotY, DotSize, Colors.Green);

            if (r.GrabNonTargetTreeId > 0)
                DrawCircle(context, x, dotY, DotSize, Colors.Red);
        }

        var wb = new WriteableBitmap(
            context.Width,
            context.Height,
            96, 96,
            PixelFormats.Bgra32,
            null);
        wb.WritePixels(
            new Int32Rect(0, 0, context.Width, context.Height),
            context.Pixels,
            context.Stride,
            0);

        var dv = new DrawingVisual();
        using (var dc = dv.RenderOpen())
        {
            dc.DrawImage(wb, new Rect(0, 0, context.Width, context.Height));
        }

        return dv;
    }*/

    DrawingVisual DrawTracks(MappingRecord[] records)
    {
        long startTime = records[0].TimeStamp;
        long endTime = records[^1].TimeStamp;

        int trackCount = 1 + TrackBrushes.Length; // +1 for grab events

        int width = (int)((endTime - startTime) * MsToPixel) + 20;

        var dv = new DrawingVisual();
        using (var dc = dv.RenderOpen())
        {

            for (int i = 0; i < trackCount; i++)
            {
                int y = Margin + i * (TrackHeight + TrackSpacing);
                dc.DrawRectangle(TrackBackgroundBrush, null, new Rect(0, y, width, TrackHeight));
            }

            for (int i = 0; i < records.Length; i++)
            {
                var r = records[i];

                int x = (int)((r.TimeStamp - startTime) * MsToPixel);

                DrawTrack(dc, r.GazeLeftWindow, TrackBrushes, 0, x);
                DrawTrack(dc, r.GazeFrontWindow, TrackBrushes, 1, x);
                DrawTrack(dc, r.GazeRightWindow, TrackBrushes, 2, x);
                DrawTrack(dc, r.GazeTDAScreen, TrackBrushes, 3, x);
                DrawTrack(dc, r.GazeHarvesterHead, TrackBrushes, 4, x);
                DrawTrack(dc, r.GazeTargetTreeId > 0, TrackBrushes, 5, x);

                // Grab event (above tracks)
                int dotY = Margin + TrackHeight / 2;

                if (r.GrabTargetTreeId > 0)
                    dc.DrawEllipse(new SolidColorBrush(Colors.Green), null, new Point(x, dotY), DotSize/2, DotSize/2);

                if (r.GrabNonTargetTreeId > 0)
                    dc.DrawEllipse(new SolidColorBrush(Colors.Red), null, new Point(x, dotY), DotSize / 2, DotSize / 2);
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

    void DrawTrack(DrawingContext dc, bool condition, Brush[] brushes, int trackIndex, int x)
    {
        if (!condition) return;

        int y = Margin + (1 + trackIndex) * (TrackHeight + TrackSpacing);

        dc.DrawRectangle(brushes[trackIndex], null, new Rect(x, y, StripWidth, TrackHeight));
    }

    /*
    void DrawBackgroundStrip(Context context, int yStart)
    {
        for (int y = 0; y < TrackHeight; y++)
        {
            int py = yStart + y;
            if (py < 0 || py >= context.Height) continue;

            for (int px = 0; px < context.Width; px++)
            {
                int index = py * context.Stride + px * 4;

                context.Pixels[index + 0] = TrackBackgroundColor.B;
                context.Pixels[index + 1] = TrackBackgroundColor.G;
                context.Pixels[index + 2] = TrackBackgroundColor.R;
                context.Pixels[index + 3] = 255;
            }
        }
    }

    void DrawTrack(Context context, bool condition, int trackIndex, int x)
    {
        if (!condition) return;

        int y = Margin + (1 + trackIndex) * (TrackHeight + TrackSpacing);

        for (int dx = 0; dx < StripWidth; dx++)
        {
            int px = x + dx;
            if (px < 0 || px >= context.Width) continue;

            for (int dy = 0; dy < TrackHeight; dy++)
            {
                int py = y + dy;
                SetPixel(context, px, py, TrackColors[trackIndex]);
            }
        }
    }

    static void DrawCircle(Context context, int cx, int cy, int diameter, Color color)
    {
        int radius = diameter / 2;

        for (int y0 = -radius; y0 <= radius; y0++)
        {
            for (int x0 = -radius; x0 <= radius; x0++)
            {
                if (x0 * x0 + y0 * y0 <= radius * radius)
                {
                    int px = cx + x0;
                    int py = cy + y0;

                    if (px >= 0 && px < context.Width && py >= 0 && py < context.Height)
                        SetPixel(context, px, py, color);
                }
            }
        }
    }

    static void SetPixel(Context context, int px, int py, Color color)
    {
        int index = py * context.Stride + px * 4;

        context.Pixels[index + 0] = color.B;
        context.Pixels[index + 1] = color.G;
        context.Pixels[index + 2] = color.R;
        context.Pixels[index + 3] = 255;
    }

    static void DrawText(DrawingContext dc, string text, int x, int y, double size = 14, Brush? brush = null, Typeface? font = null)
    {
        double dpi = VisualTreeHelper.GetDpi(Application.Current.MainWindow).PixelsPerDip;

        dc.DrawText(
            new FormattedText(
                text,
                System.Globalization.CultureInfo.InvariantCulture,
                FlowDirection.LeftToRight,
                font ?? new Typeface("Segoe UI"),
                size,
                brush ?? Brushes.Black,
                dpi),
            new Point(x, y));
    }*/
}
