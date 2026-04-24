using CreanexDataVis.Models;
using System.Windows;
using System.Windows.Media;

namespace CreanexDataVis.Services;

internal class GazePointLocationProvider
{
    public readonly static Transform DefaultGazePointTransform = new TranslateTransform(-100, 0);

    public GazePointLocationProvider(TimelineRecord[] timelineRecords, VarjoRecord[] varjoRecords, Point gazePlotOffset)
    {
        _timelineRecords = timelineRecords;
        _varjoRecords = varjoRecords;
        _gazePlotOffset = gazePlotOffset;

        _startTime = _timelineRecords[0].Timestamp;
    }

    public Transform Get(double timelineSeconds)
    {
        if (timelineSeconds < _lastSearchTimeline)
            Reset();

        _lastSearchTimeline = timelineSeconds;

        long ms = _startTime + (long)(1000 * timelineSeconds);

        // Find index of the timeline record that matches the given timestamp
        TimelineRecord? timelineRecord = null;
        int i = _timelineRecordIndex;
        while (i < _timelineRecords.Length)
        {
            var r = _timelineRecords[i];
            if (r.Timestamp >= ms)
            {
                _timelineRecordIndex = i;
                timelineRecord = r;
                break;
            }
            i++;
        }

        if (timelineRecord == null)
            return DefaultGazePointTransform;

        var timestamp = timelineRecord.Timestamp;

        // Find index of the varjo record that matches the timeline record
        VarjoRecord? varjoRecord = null;
        i = _varjoRecordIndex;
        while (i < _varjoRecords.Length)
        {
            var r = _varjoRecords[i];
            if (r.Timestamp >= timestamp)
            {
                _varjoRecordIndex = i;
                varjoRecord = r;
                break;
            }
            i++;
        }

        if (varjoRecord == null)
            return DefaultGazePointTransform;

        // Return the translation object for the gaze point
        var pt = GazePlotRenderer.GazeToPixels(varjoRecord);
        return new TranslateTransform(pt.X - _gazePlotOffset.X, pt.Y - _gazePlotOffset.Y);
    }

    public void Reset()
    {
        _timelineRecordIndex = 0;
        _varjoRecordIndex = 0;
        _lastSearchTimeline = 0;
    }

    // Internal

    readonly TimelineRecord[] _timelineRecords;
    readonly VarjoRecord[] _varjoRecords;
    readonly Point _gazePlotOffset;
    readonly long _startTime;

    int _timelineRecordIndex = 0;
    int _varjoRecordIndex = 0;
    double _lastSearchTimeline = 0;
}
