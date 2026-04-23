using CreanexDataVis.Helpers;
using CreanexDataVis.Models;
using System.Numerics;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;
using System.Windows.Media.Imaging;

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

    public GazePlotRenderer()
    {
        CoordGridPen.Freeze();
    }

    public Canvas? Create(VarjoRecord[] records)
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

        bitmap.CopyPixels(new Int32Rect(
                boundingBox.Left,
                boundingBox.Top,
                boundingBox.Width,
                boundingBox.Height),
            pixels, stride, 0);

        var source = BitmapSource.Create(boundingBox.Width, boundingBox.Height,
            96, 96,
            PixelFormats.Pbgra32,
            null, pixels, stride);

        var canvas = new Canvas
        {
            Width = boundingBox.Width,
            Height = boundingBox.Height,
            Margin = new Thickness(Margin),
            Children = { new Image() { Source = source } }
        };

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
    const double MsToPixel = 800;   // scale

    readonly Pen CoordGridPen = new(Brushes.Black, 2);

    private DrawingVisual DrawPath(VarjoRecord[] records, out Range<int> boundingBox)
    {
        double minX = double.MaxValue, 
            maxX = double.MinValue, 
            minY = double.MaxValue, 
            maxY = double.MinValue;
        double prevX = 0, prevY = 0;

        var dv = new DrawingVisual();
        using (var dc = dv.RenderOpen())
        {
            List<Point> points = [];

            for (int i = 0; i < records.Length; i++)
            {
                var r = records[i];

                if (r.GazeStatus != GazeStatus.Valid)
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

                        double h = 360.0 * (i - points.Count) / records.Length;
                        var pen = new Pen(new SolidColorBrush(ColorHelper.FromHsl(h, 1, 0.4)), 1);
                        dc.DrawGeometry(null, pen, geometry);
                    }

                    points.Clear();
                    continue;
                }

                var x = (1.0 + r.GazeForwardX) * MsToPixel;
                var y = (1.0 + r.GazeForwardY) * MsToPixel;
                if (x != prevX || y != prevY)
                {
                    if (x < minX) minX = x;
                    if (x > maxX) maxX = x;
                    if (y < minY) minY = y;
                    if (y > maxY) maxY = y;

                    prevX = x;
                    prevY = y;

                    points.Add(new Point(x, y));
                }
            }

            dc.DrawLine(CoordGridPen, new Point(0, MsToPixel), new Point(2 * MsToPixel, MsToPixel));
            dc.DrawLine(CoordGridPen, new Point(MsToPixel, 0), new Point(MsToPixel, 2 * MsToPixel));
        }

        boundingBox = new Range<int>((int)minX, (int)maxX, (int)minY, (int)maxY);
        return dv;
    }
}
