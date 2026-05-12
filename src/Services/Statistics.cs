namespace CreanexDataVis.Services;

internal interface IStatistics
{
    void SetData(Models.TimelineRecord[] records);
    KeyValuePair<string, double>[] GetAttentionShares();
    KeyValuePair<string, double>[] GetOperations();
}

internal class Statistics : IStatistics
{
    public void SetData(Models.TimelineRecord[] records)
    {
        _records = records;
    }

    public KeyValuePair<string, double>[] GetAttentionShares()
    {
        if (_records == null)
            return [];

        double[] results = [0, 0, 0, 0, 0, 0];

        foreach (var record in _records)
        {
            results[0] += record.GazeLeftWindow ? 1 : 0;
            results[1] += record.GazeFrontWindow ? 1 : 0;
            results[2] += record.GazeRightWindow ? 1 : 0;
            results[3] += record.GazeTDAScreen ? 1 : 0;
            results[4] += record.GazeHarvesterHead ? 1 : 0;
            results[5] += record.GazeTargetTreeId > 0 ? 1 : 0;
        }

        return results
            .Select((r, i) => new KeyValuePair<string, double>(GazeAreas[i], r / _records.Length))
            .ToArray();
    }

    public KeyValuePair<string, double>[] GetOperations()
    {
        if (_records == null)
            return [];

        long[] results = [0, 0, 0];

        long drivingStartTimestamp = 0;
        foreach (var record in _records)
        {
            results[0] += record.GrabTargetTreeId > 0 ? 1 : 0;
            results[1] += record.GrabNonTargetTreeId > 0 ? 1 : 0;

            if (record.DrivingStart != 0)
                drivingStartTimestamp = record.Timestamp;
            else if (record.DrivingEnd != 0)
                results[2] += record.Timestamp - drivingStartTimestamp;
        }

        return results
            .Select((r, i) => i == 2
                ? new KeyValuePair<string, double>(Operations[i], r / 1000)
                : new KeyValuePair<string, double>(Operations[i], r))
            .ToArray();
    }
    #region Internal

    readonly static string[] GazeAreas = [
        "Left Window",
        "Front Window",
        "Right Window",
        "TDA Screen",
        "Harvester Head",
        "Trees"
    ];

    readonly static string[] Operations = [
        "Correct trees grabbed",
        "Incorrect trees grabbed",
        "Driving duration, sec",
    ];

    Models.TimelineRecord[]? _records;

    #endregion
}
