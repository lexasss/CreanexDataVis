using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using Microsoft.Win32;
using System.Windows;
using System.Windows.Data;
using System.Windows.Media;

namespace CreanexDataVis.ViewModels;

internal partial class MainViewModel : ObservableObject
{
    [ObservableProperty]
    public partial string Title { get; set; } = MainTitle;

    [ObservableProperty]
    public partial FrameworkElement? Timeline { get; set; }

    [ObservableProperty]
    public partial FrameworkElement? GazePlot { get; set; }

    [ObservableProperty]
    public partial double VideoDelay { get; set; } = 0;     // seconds

    [ObservableProperty]
    public partial double PlaybackTime { get; set; } = 0;   // seconds

    [ObservableProperty]
    public partial Transform GazePointPosition { get; set; } = Services.GazePointLocationProvider.DefaultGazePointTransform;

    [ObservableProperty]
    public partial bool IsPlaybackEnabled { get; set; } = false;

    [ObservableProperty]
    public partial bool IsPlaying { get; set; } = false;

    [ObservableProperty]
    public partial double TimelineScrollX { get; set; }

    [ObservableProperty]
    public partial double TimelineWidth { get; set; }

    [ObservableProperty]
    public partial string TogglePlayVideoCommandLabel { get; set; } = VideoCommandPlayLabel;

    public MainViewModel(Services.IMediaPlayerService mediaPlayerService)
    {
        _mediaPlayerService = mediaPlayerService;
        _mediaPlayerService.OnProgressChanged += (s, e) =>
        {
            PlaybackTime = e + VideoDelay;

            var x = Services.TimelineRenderer.SecondsToPixels(PlaybackTime);
            if (TimelineScrollX < x - 0.8 * TimelineWidth)
            {
                TimelineScrollX = x - 0.8 * TimelineWidth;
            }
            else if (TimelineScrollX > x - 0.05 * TimelineWidth)
            {
                TimelineScrollX = x - 0.05 * TimelineWidth;
            }

            if (_gazePointLocationProvider != null)
                GazePointPosition = _gazePointLocationProvider.Get(PlaybackTime + _timelineOffset);
        };
        _mediaPlayerService.OnStopped += (s, e) =>
        {
            PlaybackTime = 0;
            IsPlaying = false;
            TogglePlayVideoCommandLabel = VideoCommandPlayLabel;
        };
    }

    // Internal

    readonly static string MainTitle = "Creanex Data Visualization";
    readonly static string VideoCommandPlayLabel = "▶";
    readonly static string VideoCommandPauseLabel = "⏸";

    readonly Services.IMediaPlayerService _mediaPlayerService;

    Services.TimelineDataParser? _timelineParser;
    Services.VarjoDataParser? _varjoParser;
    Services.GazePointLocationProvider? _gazePointLocationProvider;

    double _timelineOffset;
    Point _gazePlotOffset;

    [RelayCommand]
    private void LoadCreanexData()
    {
        var ofd = new OpenFileDialog()
        {
            Filter = "CSV files (*.csv)|*.csv|All files (*.*)|*.*"
        };

        if (ofd.ShowDialog() == true)
        {
            _timelineParser = new Services.TimelineDataParser(ofd.FileName);
            if (_timelineParser.Records != null)
            {
                var renderer = new Services.TimelineRenderer();
                var canvas = renderer.Create(_timelineParser.Records, out _timelineOffset);

                if (canvas == null)
                {
                    Timeline = null;
                    Title = MainTitle;
                    return;
                }

                Timeline = canvas;
                Title = $"{MainTitle} - {System.IO.Path.GetFileName(ofd.FileName)}";

                if (canvas.Children.Count > 1 && canvas.Children[1] is System.Windows.Shapes.Line timeMark)
                {
                    var xBinding = new Binding(nameof(PlaybackTime))
                    {
                        Source = this,
                        Mode = BindingMode.OneWay,
                        Converter = new Converters.TimeToPixelConverter(),
                        UpdateSourceTrigger = UpdateSourceTrigger.PropertyChanged,
                    };

                    timeMark.SetBinding(System.Windows.Shapes.Line.X1Property, xBinding);
                    timeMark.SetBinding(System.Windows.Shapes.Line.X2Property, xBinding);
                }

                canvas.MouseLeftButtonDown += (s, e) =>
                {
                    e.Handled = true;
                    var pos = e.GetPosition(canvas);
                    PlaybackTime = Services.TimelineRenderer.PixelsToSeconds(pos.X);
                };

                if (_varjoParser?.Records != null)
                    _gazePointLocationProvider = new Services.GazePointLocationProvider(
                        _timelineParser.Records,
                        _varjoParser.Records,
                        _gazePlotOffset);
            }
        }
    }

    [RelayCommand]
    private void LoadVarjoData()
    {
        var ofd = new OpenFileDialog()
        {
            Filter = "CSV files (*.csv)|*.csv|All files (*.*)|*.*"
        };

        if (ofd.ShowDialog() == true)
        {
            _varjoParser = new Services.VarjoDataParser(ofd.FileName);
            if (_varjoParser.Records != null)
            {
                var renderer = new Services.GazePlotRenderer();
                var canvas = renderer.Create(_varjoParser.Records, out _gazePlotOffset);

                GazePlot = canvas;
                Title = $"{MainTitle} - {System.IO.Path.GetFileName(ofd.FileName)}";

                if (canvas?.Children.Count > 1 && canvas.Children[1] is System.Windows.Shapes.Ellipse gazeMark)
                {
                    var positionBinding = new Binding(nameof(GazePointPosition))
                    {
                        Source = this,
                        Mode = BindingMode.OneWay,
                        UpdateSourceTrigger = UpdateSourceTrigger.PropertyChanged,
                    };

                    gazeMark.SetBinding(UIElement.RenderTransformProperty, positionBinding);
                }

                if (_timelineParser?.Records != null)
                    _gazePointLocationProvider = new Services.GazePointLocationProvider(_timelineParser.Records, _varjoParser.Records, _gazePlotOffset);
            }
        }
    }

    [RelayCommand]
    private void LoadVideo()
    {
        var ofd = new OpenFileDialog()
        {
            Filter = "Video files (*.mp4;*.avi)|*.mp4;*.avi|All files (*.*)|*.*"
        };

        if (ofd.ShowDialog() == true)
        {
            _mediaPlayerService.Load(new Uri(ofd.FileName));
            IsPlaybackEnabled = true;
        }
    }

    [RelayCommand]
    private void TogglePlayVideo()
    {
        if (_mediaPlayerService.IsPlaying)
        {
            _mediaPlayerService.Pause();
            IsPlaying = false;
            TogglePlayVideoCommandLabel = VideoCommandPlayLabel;
        }
        else if (IsPlaybackEnabled)
        {
            _mediaPlayerService.Play(PlaybackTime >= VideoDelay ? PlaybackTime - VideoDelay : 0);
            IsPlaying = true;
            TogglePlayVideoCommandLabel = VideoCommandPauseLabel;
        }
    }
}
