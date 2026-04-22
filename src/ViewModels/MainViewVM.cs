using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using Microsoft.Win32;
using System.Windows;
using System.Windows.Data;
using System.Windows.Threading;

namespace CreanexDataVis.ViewModels;

internal partial class MainViewVM : ObservableObject
{
    [ObservableProperty]
    public partial string Title { get; set; } = MainTitle;

    [ObservableProperty]
    public partial FrameworkElement? Timeline { get; set; }

    [ObservableProperty]
    public partial FrameworkElement? GazePlot { get; set; }

    [ObservableProperty]
    public partial Uri? VideoSource { get; set; } = null;

    [ObservableProperty]
    public partial double VideoDelay { get; set; } = 0;     // seconds

    [ObservableProperty]
    public partial double PlaybackTime { get; set; } = 0;   // seconds

    [ObservableProperty]
    public partial bool CanPlay { get; set; } = false;
    [ObservableProperty]
    public partial bool CanStop { get; set; } = false;

    [ObservableProperty]
    public partial double TimelineScrollX { get; set; }
    [ObservableProperty]
    public partial double TimelineWidth { get; set; }

    public MainViewVM(Services.IMediaPlayerService mediaPlayerService)
    {
        _mediaPlayerService = mediaPlayerService;
        _timer = new DispatcherTimer
        {
            Interval = TimeSpan.FromSeconds(TimeStep)
        };

        _timer.Tick += (_, _) =>
        {
            PlaybackTime += TimeStep;
            if (PlaybackTime >= _duration)
            {
                _timer.Stop();
                PlaybackTime = 0;
            }
            else
            {
                var x = PlaybackTime * 1000 * Services.TimelineRenderer.MsToPixel;
                if (TimelineScrollX < x - 0.9 * TimelineWidth)
                {
                    TimelineScrollX = x - 0.9 * TimelineWidth;
                }
            }
        };
    }

    // Internal

    const double TimeStep = 0.05; // seconds

    readonly static string MainTitle = "Creanex Data Visualization";

    readonly Services.IMediaPlayerService _mediaPlayerService;
    readonly DispatcherTimer _timer;

    double _duration = 0; // seconds
    double _timelineStartTime = 0; // seconds

    [RelayCommand]
    private void LoadCreanexData()
    {
        var ofd = new OpenFileDialog()
        {
            Filter = "CSV files (*.csv)|*.csv|All files (*.*)|*.*"
        };

        if (ofd.ShowDialog() == true)
        {
            var records = Services.TimelineDataParser.Parse(ofd.FileName);
            if (records != null)
            {
                var renderer = new Services.TimelineRenderer();
                var canvas = renderer.Create(records);
                if (canvas != null)
                {
                    _timelineStartTime = renderer.StartTime / 1000.0; // convert to seconds
                    _duration = canvas.Width / Services.TimelineRenderer.MsToPixel / 1000.0;
                }

                Timeline = canvas;
                CanPlay = true;
                Title = $"{MainTitle} - {System.IO.Path.GetFileName(ofd.FileName)}";

                if (canvas?.Children.Count > 1 && canvas.Children[1] is System.Windows.Shapes.Line timeMark)
                {
                    var xBinding = new Binding("PlaybackTime")
                    {
                        Source = this,
                        Mode = BindingMode.OneWay,
                        Converter = new Converters.TimeToPixelConverter(),
                        UpdateSourceTrigger = UpdateSourceTrigger.PropertyChanged,
                    };

                    timeMark.SetBinding(System.Windows.Shapes.Line.X1Property, xBinding);
                    timeMark.SetBinding(System.Windows.Shapes.Line.X2Property, xBinding);
                }

                if (canvas?.Children.Count > 2)
                {
                    canvas?.Children[2].MouseLeftButtonDown += (s, e) =>
                    {
                        var pos = e.GetPosition(canvas);
                        PlaybackTime = TimeSpan.FromMilliseconds(pos.X / Services.TimelineRenderer.MsToPixel).TotalSeconds;
                    };
                }
            }
        }
    }

    [RelayCommand]
    private void LoadVarjoData()
    {
        OpenFileDialog ofd = new OpenFileDialog()
        {
            Filter = "CSV files (*.csv)|*.csv|All files (*.*)|*.*"
        };

        if (ofd.ShowDialog() == true)
        {
            var records = Services.VarjoDataParser.Parse(ofd.FileName);
            if (records != null)
            {
                var renderer = new Services.GazePlotRenderer();
                GazePlot = renderer.Create(records);
                Title = $"{MainTitle} - {System.IO.Path.GetFileName(ofd.FileName)}";
            }
        }
    }

    [RelayCommand]
    private void LoadVideo()
    {
        OpenFileDialog ofd = new OpenFileDialog()
        {
            Filter = "Video files (*.mp4;*.avi)|*.mp4;*.avi|All files (*.*)|*.*"
        };

        if (ofd.ShowDialog() == true)
        {
            VideoSource = new Uri(ofd.FileName);
        }
    }

    [RelayCommand]
    private void PlayTimeline()
    {
        _timer.Start();
        if (VideoSource != null)
        {
            var videoStartTime = TimeSpan.FromSeconds(_timelineStartTime + VideoDelay);
            if (PlaybackTime >= videoStartTime.TotalSeconds)
            {
                _mediaPlayerService.Play(PlaybackTime - videoStartTime.TotalSeconds);
            }
        }
        CanPlay = false;
        CanStop = true;
    }

    [RelayCommand]
    private void StopTimelinePlayback()
    {
        _timer.Stop();
        _mediaPlayerService.Stop();
        CanPlay = true;
        CanStop = false;
    }
}
