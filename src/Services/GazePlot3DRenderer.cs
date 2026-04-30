using CreanexDataVis.Helpers;
using CreanexDataVis.Models;
using HelixToolkit;
using HelixToolkit.SharpDX;
using System.Numerics;
using System.Windows.Media.Media3D;

namespace CreanexDataVis.Services;

internal class GazePlot3DRenderer
{
    public LineGeometry3D Create(VarjoRecord[] records)
    {
        var positions = new List<Vector3>();

        foreach (var record in records)
        {
            if ((record.GazeForwardXWorld != 0 || record.GazeForwardYWorld != 0 || record.GazeForwardZWorld != 0)
                && record.GazeStatus == GazeStatus.Valid)
                positions.Add(GetPoint(record));
        }

        var builder = new LineBuilder();
        builder.Add(false, positions.ToArray());

        var result = builder.ToLineGeometry3D(true);
        result.Colors = [];

        var count = result.Indices?.Count() ?? 0;
        float c = 0.00392157f;  // 1/255
        for (int i = 0; i < count; i++)
        {
            var hue = 360.0 * i / count;
            var color = ColorHelper.FromHsl(hue, 1, 0.4);
            result.Colors.Add(new HelixToolkit.Maths.Color4(
                c * color.R,
                c * color.G,
                c * color.B,
                1f));
        }

        return result;
    }

    public static Vector3 GetPoint(VarjoRecord record) => new(
                    (float)record.GazeForwardXWorld,
                    (float)record.GazeForwardYWorld,
                    (float)record.GazeForwardZWorld);

    public static Vector3D GetPoint3D(VarjoRecord record) => new(
                    (float)record.GazeForwardXWorld,
                    (float)record.GazeForwardYWorld,
                    (float)record.GazeForwardZWorld);
}
