namespace CreanexDataVis.Services;

internal interface IStatistics
{
    void SetData(Models.TimelineRecord[] records);
    Models.NamedValue<double>[] GetAttentionShares();
    Models.NamedValue<int>[] GetAttentionCounts();
    Models.NamedValue<double>[] GetOperations();
}

internal class Statistics : IStatistics
{
    public void SetData(Models.TimelineRecord[] records)
    {
        _records = records;
    }

    public Models.NamedValue<double>[] GetAttentionShares()
    {
        if (_records == null)
            return [];

        int startIndex = 0;
        int stopIndex = _records.Length - 1;

        // Skip records from the beginning of the timeline until the first record with gaze on any AOI.
        while (startIndex < stopIndex)
        {
            var record  = _records[startIndex];
            if (record.GazeLeftWindow ||
                record.GazeFrontWindow ||
                record.GazeRightWindow ||
                record.GazeTDAScreen ||
                record.GazeHarvesterHead ||
                record.GazeTargetTreeId > 0)
            {
                break;
            }
            startIndex++;
        }

        // Skip records from the end of the timeline until the first record with gaze on any AOI.
        while (stopIndex > startIndex)
        {
            var record  = _records[stopIndex];
            if (record.GazeLeftWindow ||
                record.GazeFrontWindow ||
                record.GazeRightWindow ||
                record.GazeTDAScreen ||
                record.GazeHarvesterHead ||
                record.GazeTargetTreeId > 0)
            {
                break;
            }
            stopIndex--;
        }

        // Compute shares of attention for each AOI as a ratio of number of records with gaze on the AOI to the total number of records in the timeline (after trimming).

        double[] results = [0, 0, 0, 0, 0, 0];

        for (int i = startIndex; i <= stopIndex; i++)
        {
            var record = _records[i];
            results[0] += record.GazeLeftWindow ? 1 : 0;
            results[1] += record.GazeFrontWindow ? 1 : 0;
            results[2] += record.GazeRightWindow ? 1 : 0;
            results[3] += record.GazeTDAScreen ? 1 : 0;
            results[4] += record.GazeHarvesterHead ? 1 : 0;
            results[5] += record.GazeTargetTreeId > 0 ? 1 : 0;
        }

        return results
            .Select((r, i) => new Models.NamedValue<double>(GazeAreas[i], r / (stopIndex - startIndex + 1)))
            .ToArray();
    }

    public Models.NamedValue<int>[] GetAttentionCounts()
    {
        if (_records == null)
            return [];

        AttentionData[] attentionData = Enumerable.Range(0, GazeAreas.Length)
            .Select(_ => new AttentionData())
            .ToArray();
        IEnumerable<bool[]> gazeData = _records.Select(record => new bool[] {
            record.GazeLeftWindow,
            record.GazeFrontWindow,
            record.GazeRightWindow,
            record.GazeTDAScreen,
            record.GazeHarvesterHead,
            record.GazeTargetTreeId > 0
        });

        // This algorithm computed number of entrancies into each AOI. 
        // However, an entrance is not counted if a fixation is too short.
        // Also, too short break between 2 subsequent fixations is not counted
        // as a break and therefore the algorithm counts only one fixation in this case.

        int i = 0;
        foreach (var gd in gazeData)
        {
            var timestamp = _records[i].Timestamp;

            int j = 0;
            foreach (bool isAoiFixated in gd)
            {
                AttentionData ad = attentionData[j];
                if (!isAoiFixated)
                {
                    if (ad.IsStateOn)
                    {
                        if (!ad.HasStateChanged)
                        {
                            ad.HasStateChanged = true;
                            ad.StateOffTimestamp = timestamp;
                        }
                        else
                        {
                            ad.HasStateChanged = false;
                        }
                        ad.IsStateOn = false;
                    }

                    if (ad.HasStateChanged && timestamp - ad.StateOffTimestamp > ContinuityThreshold)
                    {
                        ad.HasStateChanged = false;
                    }
                }
                else
                {
                    if (!ad.IsStateOn)
                    {
                        if (!ad.HasStateChanged)
                        {
                            ad.HasStateChanged = true;
                            ad.StateOnTimestamp = timestamp;
                        }
                        else
                        {
                            ad.HasStateChanged = false;
                        }
                        ad.IsStateOn = true;
                    }

                    if (ad.HasStateChanged && timestamp - ad.StateOnTimestamp > FixationThreshold)
                    {
                        ad.Count++;
                        ad.HasStateChanged = false;
                    }
                }
                j++;
            }

            i++;
        }

        return attentionData
            .Select((r, i) => new Models.NamedValue<int>(GazeAreas[i], r.Count))
            .ToArray();
    }

    public Models.NamedValue<double>[] GetOperations()
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
                ? new Models.NamedValue<double>(Operations[i], (double)(r / 100) / 10)    // driving duration
                : new Models.NamedValue<double>(Operations[i], r))
            .ToArray();
    }

    #region Internal

    class AttentionData
    {
        public int Count = 0;
        public long StateOnTimestamp = 0;
        public long StateOffTimestamp = 0;
        public bool IsStateOn = false;
        public bool HasStateChanged = false;
    }

    const long ContinuityThreshold = 300;
    const long FixationThreshold = 300;

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
