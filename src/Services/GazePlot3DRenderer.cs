using CreanexDataVis.Models;
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
            if (record.GazeForwardXWorld != 0 || record.GazeForwardYWorld != 0 || record.GazeForwardZWorld != 0)
                positions.Add(GetPoint(record));
        }

        var builder = new LineBuilder();
        builder.Add(false, positions.ToArray());

        return builder.ToLineGeometry3D(true);
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
