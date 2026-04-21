namespace CreanexDataVis.Models;

internal class TimelineRecord
{
    public long TimeStamp;
    public bool GazeLeftWindow;
    public bool GazeFrontWindow;
    public bool GazeRightWindow;
    public bool GazeTDAScreen;
    public bool GazeHarvesterHead;
    public int GazeTargetTreeId;
    public int GrabTargetTreeId;
    public int GrabNonTargetTreeId;
    public int DrivingStart;
    public int DrivingEnd;
}
