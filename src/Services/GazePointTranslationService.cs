using CreanexDataVis.Models;
using System.Windows;
using System.Windows.Media;
using System.Windows.Media.Media3D;

namespace CreanexDataVis.Services;

internal class GazePointTranslationService
{
    public readonly static Transform DefaultGazePointTransform = new TranslateTransform(-100, 0);
    public readonly static TranslateTransform3D DefaultGazePoint3DTransform = new TranslateTransform3D(-100, 0, 0);

    public GazePointTranslationService(TimelineRecord[] timelineRecords, VarjoRecord[] varjoRecords, Point gazePlotOffset)
    {
        _timelineRecords = timelineRecords;
        _varjoRecords = varjoRecords;
        _gazePlotOffset = gazePlotOffset;

        _startTime = _timelineRecords[0].Timestamp;
    }

    public VarjoRecord? GetGazeDataAt(double timelineSeconds)
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
            return null;

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

        return varjoRecord;
    }

    public Transform GetPosition2D(VarjoRecord? varjoRecord)
    {
        if (varjoRecord == null)
            return DefaultGazePointTransform;

        // Return the translation object for the gaze point
        var pt = GazePlotRenderer.GetGazeMarkLocation(varjoRecord);
        return new TranslateTransform(pt.X - _gazePlotOffset.X, pt.Y - _gazePlotOffset.Y);
    }

    public static TranslateTransform3D GetPosition3D(VarjoRecord? varjoRecord)
    {
        if (varjoRecord == null)
            return DefaultGazePoint3DTransform;

        var pt = GazePlotRenderer.GetGazeMarkLocation(varjoRecord);
        return new TranslateTransform3D(GazePlot3DRenderer.GetPoint3D(varjoRecord));
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
