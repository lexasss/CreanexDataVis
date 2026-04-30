using CreanexDataVis.Helpers;
using CreanexDataVis.Models;
using System.Numerics;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Shapes;

namespace CreanexDataVis.Services;

internal class GazePlotRenderer
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

    public static Point GetGazeMarkLocation(VarjoRecord r)
    {
        var pt = GazeToPixels(r);
        return new(pt.X - GazeMarkSize / 2, pt.Y - GazeMarkSize / 2);
    }

    public GazePlotRenderer()
    {
        CoordGridPen.Freeze();
    }

    public Canvas? Create(VarjoRecord[] records, out Point offset)
    {
        if (records.Length == 0)
            return null;

        var path = DrawPath(records, out Range<int> boundingBox);

        var host = new VisualHost([path])
        {
            RenderTransform = new TranslateTransform(-boundingBox.Left, -boundingBox.Top),
            Width = boundingBox.Width,
            Height = boundingBox.Height
        };

        // Creating bitmap source
        var bitmap = new RenderTargetBitmap(
            boundingBox.Right,
            boundingBox.Bottom,
            96, 96,
            PixelFormats.Pbgra32);
        bitmap.Render(host);

        var stride = boundingBox.Width * 4;
        var pixels = new byte[stride * boundingBox.Height];

        ImageSource source;

        try
        {
            // Clipping
            bitmap.CopyPixels(new Int32Rect(
                    boundingBox.Left,
                    boundingBox.Top,
                    boundingBox.Width,
                    boundingBox.Height),
                pixels, stride, 0);

            source = BitmapSource.Create(boundingBox.Width, boundingBox.Height,
                96, 96,
                PixelFormats.Pbgra32,
                null, pixels, stride);
        }
        catch
        {
            source = CreateWarning();
            boundingBox = new Range<int>(0, WarningWidth, 0, WarningHeight);
        }

        var canvas = new Canvas
        {
            Width = boundingBox.Width,
            Height = boundingBox.Height,
            Margin = new Thickness(Margin),
            ClipToBounds = true,
            Children = {
                new Image() { Source = source },
                new Ellipse() { Width = GazeMarkSize, Height = GazeMarkSize, Fill = GazeMarkBrush }
            }
        };

        offset = new Point(boundingBox.Left, boundingBox.Top);
        return canvas;
    }

    // Internal
    record class Range<T>(T Left, T Right, T Top, T Bottom)
        where T : INumber<T>
    {
        public T Width => Right - Left;
        public T Height => Bottom - Top;
    }

    const int Margin = 8;           // pixels
    const int GazeMarkSize = 10;    // pixels

    const double VectorToPixel = 400;   // scale
    const double WarningFontSize = 12;
    const int WarningWidth = 250;
    const int WarningHeight = 50;

    readonly Pen CoordGridPen = new(Brushes.DarkGray, 2);
    readonly Brush GazeMarkBrush = Brushes.Black;
    readonly Typeface WarningFontFamily = new("Segoe UI");
    readonly Brush WarningFontBrush = Brushes.Black;

    private DrawingVisual DrawPath(VarjoRecord[] records, out Range<int> boundingBox)
    {
        void DrawPoints(DrawingContext dc, IList<Point> points, double hue)
        {
            if (points.Count > 1)
            {
                var geometry = new StreamGeometry();
                using (var ctx = geometry.Open())
                {
                    ctx.BeginFigure(points[0], false, false);
                    ctx.PolyLineTo(points, true, false);
                }

                geometry.Freeze();

                var pen = new Pen(new SolidColorBrush(ColorHelper.FromHsl(hue, 1, 0.4)), 1);
                dc.DrawGeometry(null, pen, geometry);
            }

            points.Clear();
        }

        double minX = double.MaxValue,
            maxX = double.MinValue,
            minY = double.MaxValue,
            maxY = double.MinValue;
        Point prev = new();

        var dv = new DrawingVisual();
        using (var dc = dv.RenderOpen())
        {
            List<Point> points = [];

            for (int i = 0; i < records.Length; i++)
            {
                var r = records[i];

                if (r.GazeStatus != GazeStatus.Valid)
                {
                    DrawPoints(dc, points, 360.0 * (i - points.Count) / records.Length);
                    continue;
                }

                var pt = GazeToPixels(r);
                if (pt.X != prev.X || pt.Y != prev.Y)
                {
                    if (pt.X < minX) minX = pt.X;
                    if (pt.X > maxX) maxX = pt.X;
                    if (pt.Y < minY) minY = pt.Y;
                    if (pt.Y > maxY) maxY = pt.Y;

                    prev = pt;
                    points.Add(pt);
                }
            }

            DrawPoints(dc, points, 360);

            dc.DrawLine(CoordGridPen,
                new Point(0, VectorToPixel),
                new Point(2 * VectorToPixel, VectorToPixel));
            dc.DrawLine(CoordGridPen,
                new Point(VectorToPixel, 0),
                new Point(VectorToPixel, 2 * VectorToPixel));
        }

        boundingBox = new Range<int>((int)minX, (int)maxX, (int)minY, (int)maxY);
        return dv;
    }

    private static Point GazeToPixels(VarjoRecord r) => new(
        (1.0 + r.GazeForwardZWorld) * VectorToPixel,
        (1.0 - r.GazeForwardYWorld) * VectorToPixel);

    private BitmapSource CreateWarning()
    {
        var dpi = VisualTreeHelper.GetDpi(Application.Current.MainWindow);

        var dv = new DrawingVisual();
        using (var dc = dv.RenderOpen())
        {
            dc.DrawText(
                new FormattedText(
                    $"Invalid data",
                    System.Globalization.CultureInfo.InvariantCulture,
                    FlowDirection.LeftToRight,
                    WarningFontFamily,
                    WarningFontSize,
                    WarningFontBrush,
                    dpi.PixelsPerDip),
                new Point(20, 20));
        }

        var bitmap = new RenderTargetBitmap(
            WarningWidth,
            WarningHeight,
            dpi.PixelsPerInchX,
            dpi.PixelsPerInchY,
            PixelFormats.Pbgra32);

        bitmap.Render(dv);

        return bitmap;
    }
}
