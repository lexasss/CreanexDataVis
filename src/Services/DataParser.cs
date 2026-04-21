using CreanexDataVis.Models;
using System.Globalization;
using System.IO;

namespace CreanexDataVis.Services;

internal static class DataParser
{
    public static MappingRecord[]? Parse(string filePath)
    {
        var result = new List<MappingRecord>();

        try
        {
            using var file = new StreamReader(filePath);
            file.ReadLine(); // skip header

            while (!file.EndOfStream)
            {
                var line = file.ReadLine();
                var parts = line?.Split(',');

                if (parts == null || parts.Length < 11)
                    continue; // skip malformed lines

                var record = new MappingRecord
                {
                    TimeStamp = long.Parse(parts[0], CultureInfo.InvariantCulture),
                    GazeLeftWindow = parts[1] == "1",
                    GazeFrontWindow = parts[2] == "1",
                    GazeRightWindow = parts[3] == "1",
                    GazeTDAScreen = parts[4] == "1",
                    GazeHarvesterHead = parts[5] == "1",
                    GazeTargetTreeId = int.Parse(parts[6]),
                    GrabTargetTreeId = int.Parse(parts[7]),
                    GrabNonTargetTreeId = int.Parse(parts[8]),
                    DrivingStart = int.Parse(parts[9]),
                    DrivingEnd = int.Parse(parts[10])
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