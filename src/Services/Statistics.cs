namespace CreanexDataVis.Services;

internal interface IStatistics
{
    void SetData(Models.TimelineRecord[] records);
    KeyValuePair<string, double>[] GetAttentionShare();
}

internal class Statistics : IStatistics
{
    public void SetData(Models.TimelineRecord[] records)
    {
        _records = records;
    }

    public KeyValuePair<string, double>[] GetAttentionShare()
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

    #region Internal

    readonly static string[] GazeAreas = [
        "Left Window",
        "Front Window",
        "Right Window",
        "TDA Screen",
        "Harvester Head",
        "Trees"
    ];

    Models.TimelineRecord[]? _records;

    #endregion
}
