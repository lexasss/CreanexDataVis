using CreanexDataVis.Models;
using System.Windows;
using System.Windows.Media;

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
        PathPen.Freeze();
    }

    public VisualHost? GetVisualHost(VarjoRecord[] records)
    {
        if (records.Length == 0)
            return null;

        var path = DrawPath(records, out Range boundingBox);

        /*
        var rtb = new RenderTargetBitmap(
            width,
            height,
            96, 96,
            PixelFormats.Pbgra32);

        rtb.Render(dv);
        rtb.Freeze();

         */
        var host = new VisualHost([path])
        {
            RenderTransform = new TranslateTransform(Margin, Margin),
            Width = 2 * Margin + 2 * MsToPixel,
            Height = 2 * Margin + 2 * MsToPixel
        };

        host.VerticalAlignment = VerticalAlignment.Top;
        host.HorizontalAlignment = HorizontalAlignment.Left;

        return host;
    }

    // Internal
    record class Range(double Left, double Right, double Top, double Bottom);

    const int Margin = 5;           // pixels
    const double MsToPixel = 1500;   // scale

    readonly Pen PathPen = new(Brushes.Red, 1);

    private DrawingVisual DrawPath(VarjoRecord[] records, out Range boundingBox)
    {
        double minX = double.MaxValue, maxX = double.MinValue, minY = double.MaxValue, maxY = double.MinValue;
        double prevX = 0, prevY = 0;
        /*
        var points = records.Select(r => {
                if (r.GazeStatus != GazeStatus.Valid)
                    return new Point();

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
                    return new Point(x, y);
                }
                return new Point();
            })
            .Where(p => p.X != 0 || p.Y != 0)
            .ToArray();

        var dv = new DrawingVisual();
        using (var dc = dv.RenderOpen())
        {
            var geometry = new StreamGeometry();

            using (var ctx = geometry.Open())
            {
                ctx.BeginFigure(points[0], false, false);
                ctx.PolyLineTo(points, true, false);
            }

            geometry.Freeze();

            dc.DrawGeometry(null, PathPen, geometry);
        }*/

        var dv = new DrawingVisual();
        using (var dc = dv.RenderOpen())
        {
            List<Point> points = [];

            var geometry = new StreamGeometry();

            using (var ctx = geometry.Open())
            {
                for (int i = 0; i < records.Length; i++)
                {
                    var r = records[i];

                    if (r.GazeStatus != GazeStatus.Valid)
                    {
                        if (points.Count > 1)
                        {
                            ctx.BeginFigure(points[0], false, false);
                            ctx.PolyLineTo(points, true, false);
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
            }

            geometry.Freeze();

            dc.DrawGeometry(null, PathPen, geometry);
        }

        boundingBox = new Range(minX, maxX, minY, maxY);
        return dv;
    }
}
