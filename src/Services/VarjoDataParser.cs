using CreanexDataVis.Models;
using System.Globalization;
using System.IO;

namespace CreanexDataVis.Services;

internal static class VarjoDataParser
{
    public static VarjoRecord[]? Parse(string filePath)
    {
        var result = new List<VarjoRecord>();

        try
        {
            using var file = new StreamReader(filePath);
            file.ReadLine(); // skip header, or just the first sample

            while (!file.EndOfStream)
            {
                var line = file.ReadLine();
                var parts = line?.Split(',');

                if (parts == null || parts.Length < 50)
                    continue; // skip malformed lines

                var record = new VarjoRecord
                {
                    Timestamp = long.Parse(parts[0]),
                    FrameNumber = long.Parse(parts[1]),
                    CaptureTime = long.Parse(parts[2]),
                    HMDPositionX = double.Parse(parts[3], CultureInfo.InvariantCulture),
                    HMDPositionY = double.Parse(parts[4], CultureInfo.InvariantCulture),
                    HMDPositionZ = double.Parse(parts[5], CultureInfo.InvariantCulture),
                    HMDEulerX = double.Parse(parts[6], CultureInfo.InvariantCulture),
                    HMDEulerY = double.Parse(parts[7], CultureInfo.InvariantCulture),
                    HMDEulerZ = double.Parse(parts[8], CultureInfo.InvariantCulture),
                    HMDForwardX = double.Parse(parts[9], CultureInfo.InvariantCulture),
                    HMDForwardY = double.Parse(parts[10], CultureInfo.InvariantCulture),
                    HMDForwardZ = double.Parse(parts[11], CultureInfo.InvariantCulture),
                    GazeStatus = (GazeStatus)int.Parse(parts[12]),
                    GazeOriginX = double.Parse(parts[13], CultureInfo.InvariantCulture),
                    GazeOriginY = double.Parse(parts[14], CultureInfo.InvariantCulture),
                    GazeOriginZ = double.Parse(parts[15], CultureInfo.InvariantCulture),
                    GazeForwardX = double.Parse(parts[16], CultureInfo.InvariantCulture),
                    GazeForwardY = double.Parse(parts[17], CultureInfo.InvariantCulture),
                    GazeForwardZ = double.Parse(parts[18], CultureInfo.InvariantCulture),
                    GazeOriginXWorld = double.Parse(parts[19], CultureInfo.InvariantCulture),
                    GazeOriginYWorld = double.Parse(parts[20], CultureInfo.InvariantCulture),
                    GazeOriginZWorld = double.Parse(parts[21], CultureInfo.InvariantCulture),
                    GazeForwardXWorld = double.Parse(parts[22], CultureInfo.InvariantCulture),
                    GazeForwardYWorld = double.Parse(parts[23], CultureInfo.InvariantCulture),
                    GazeForwardZWorld = double.Parse(parts[24], CultureInfo.InvariantCulture),
                    GazeFocusDistance = double.Parse(parts[25], CultureInfo.InvariantCulture),
                    GazeFocusStability = double.Parse(parts[26], CultureInfo.InvariantCulture),
                    GazeLeftStatus = (GazeStatus)int.Parse(parts[27]),
                    GazeLeftOriginX = double.Parse(parts[28], CultureInfo.InvariantCulture),
                    GazeLeftOriginY = double.Parse(parts[29], CultureInfo.InvariantCulture),
                    GazeLeftOriginZ = double.Parse(parts[30], CultureInfo.InvariantCulture),
                    GazeLeftForwardX = double.Parse(parts[31], CultureInfo.InvariantCulture),
                    GazeLeftForwardY = double.Parse(parts[32], CultureInfo.InvariantCulture),
                    GazeLeftForwardZ = double.Parse(parts[33], CultureInfo.InvariantCulture),
                    GazeRightStatus = (GazeStatus)int.Parse(parts[34]),
                    GazeRightOriginX = double.Parse(parts[35], CultureInfo.InvariantCulture),
                    GazeRightOriginY = double.Parse(parts[36], CultureInfo.InvariantCulture),
                    GazeRightOriginZ = double.Parse(parts[37], CultureInfo.InvariantCulture),
                    GazeRightForwardX = double.Parse(parts[38], CultureInfo.InvariantCulture),
                    GazeRightForwardY = double.Parse(parts[39], CultureInfo.InvariantCulture),
                    GazeRightForwardZ = double.Parse(parts[40], CultureInfo.InvariantCulture),
                    InterpupillaryDistance = double.Parse(parts[41], CultureInfo.InvariantCulture),
                    LeftIrisDiameterRatio = double.Parse(parts[42], CultureInfo.InvariantCulture),
                    LeftPupilDiameter = double.Parse(parts[43], CultureInfo.InvariantCulture),
                    LeftIrisDiameter = double.Parse(parts[44], CultureInfo.InvariantCulture),
                    LeftEyeOpenness = double.Parse(parts[45], CultureInfo.InvariantCulture),
                    RightIrisDiameterRatio = double.Parse(parts[46], CultureInfo.InvariantCulture),
                    RightPupilDiameter = double.Parse(parts[47], CultureInfo.InvariantCulture),
                    RightIrisDiameter = double.Parse(parts[48], CultureInfo.InvariantCulture),
                    RightEyeOpenness = double.Parse(parts[49], CultureInfo.InvariantCulture)
                };

                result.Add(record);
            }
        }
        catch (Exception)
        {
            return null;
        }

        return result.ToArray();
    }
}