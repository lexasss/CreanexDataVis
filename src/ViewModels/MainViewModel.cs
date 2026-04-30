using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using HelixToolkit.Geometry;
using HelixToolkit.SharpDX;
using HelixToolkit.Wpf.SharpDX;
using Microsoft.Win32;
using System.Numerics;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Media;
using System.Windows.Media.Media3D;

namespace CreanexDataVis.ViewModels;

internal partial class MainViewModel : ObservableObject
{
    [ObservableProperty]
    public partial FrameworkElement? Timeline { get; set; }

    [ObservableProperty]
    public partial FrameworkElement? GazePlot { get; set; }

    [ObservableProperty]
    public partial double VideoDelay { get; set; } = 0;     // seconds

    [ObservableProperty]
    public partial double PlaybackTime { get; set; } = 0;   // seconds

    [ObservableProperty]
    public partial Transform GazePointPosition { get; set; } = Services.GazePointTranslationService.DefaultGazePointTransform;

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

    [ObservableProperty]
    //[NotifyPropertyChangedFor(nameof(LineThicknessMaximum))]
    public partial LineGeometry3D? GazePlot3D { get; private set; }

    public MainViewModel(
        Services.IMediaPlayerService mediaPlayerService,
        Services.GazePlot3DRenderer gazePlot3DRenderer)
    {
        _gazePlot3DRenderer = gazePlot3DRenderer;
        _mediaPlayerService = mediaPlayerService;
        _mediaPlayerService.OnProgressChanged += MediaPlayerService_OnProgressChanged;
        _mediaPlayerService.OnStopped += MediaPlayerService_OnStopped;

        var b1 = new MeshBuilder();
        b1.AddSphere(new Vector3(0, 0, 0), 0.02f);
        _gazePlot3DHead = b1.ToMeshGeometry3D();
    }

    // Observables

    [ObservableProperty]
    EffectsManager _gazePlotEffectsManager = new DefaultEffectsManager();

    [ObservableProperty]
    HelixToolkit.SharpDX.MeshGeometry3D _gazePlot3DHead;

    [ObservableProperty]
    Transform3D _gazePlot3DHeadTransform = Services.GazePointTranslationService.DefaultGazePoint3DTransform;

    [ObservableProperty]
    PhongMaterial _gazePlot3DHeadMaterial = PhongMaterials.Red;

    // Internal

    readonly static string VideoCommandPlayLabel = "▶";
    readonly static string VideoCommandPauseLabel = "⏸";

    readonly Services.IMediaPlayerService _mediaPlayerService;
    readonly Services.GazePlot3DRenderer _gazePlot3DRenderer;

    Services.TimelineDataParser? _timelineParser;
    Services.VarjoDataParser? _varjoParser;
    Services.GazePointTranslationService? _gazePointTranslationService;

    double _timelineOffset;
    Point _gazePlotOffset;

    #region OnChange handlers for observable properties

    partial void OnVideoDelayChanged(double value)
    {
        if (_mediaPlayerService.Filename != null)
        {
            Services.VideoDelayStorage.SetDelay(_mediaPlayerService.Filename, VideoDelay);
        }
    }

    #endregion

    #region Commands

    [RelayCommand]
    private void LoadCreanexData()
    {
        var ofd = new OpenFileDialog()
        {
            Filter = "Creanex-Mixer files (*.csv)|MixerEventLog*.csv|All files (*.*)|*.*"
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
                    return;
                }

                Timeline = canvas;

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

                canvas.MouseLeftButtonDown += TimelineCanvas_MouseLeftButtonDown;

                if (_varjoParser?.Records != null)
                    _gazePointTranslationService = new Services.GazePointTranslationService(
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
            Filter = "Creanex-Varjo files (*.csv)|VarjoEyeTracking*.csv|All files (*.*)|*.*"
        };

        if (ofd.ShowDialog() == true)
        {
            _varjoParser = new Services.VarjoDataParser(ofd.FileName);
            if (_varjoParser.Records != null)
            {
                var renderer = new Services.GazePlotRenderer();
                var canvas = renderer.Create(_varjoParser.Records, out _gazePlotOffset);

                GazePlot = canvas;

                GazePlot3D = _gazePlot3DRenderer.Create(_varjoParser.Records);

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
                    _gazePointTranslationService = new Services.GazePointTranslationService(
                        _timelineParser.Records,
                        _varjoParser.Records,
                        _gazePlotOffset);
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

            if (Services.VideoDelayStorage.TryGetDelay(ofd.FileName, out double videoDelay))
            {
                VideoDelay = videoDelay;
            }
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

    #endregion

    private void TimelineCanvas_MouseLeftButtonDown(object sender, System.Windows.Input.MouseButtonEventArgs e)
    {
        var canvas = sender as Canvas;
        e.Handled = true;
        var pos = e.GetPosition(canvas);
        PlaybackTime = Services.TimelineRenderer.PixelsToSeconds(pos.X);
    }

    private void MediaPlayerService_OnProgressChanged(object? sender, double e)
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

        if (_gazePointTranslationService != null)
        {
            var currentGazeData = _gazePointTranslationService.GetGazeDataAt(PlaybackTime + _timelineOffset);
            GazePointPosition = _gazePointTranslationService.GetPosition2D(currentGazeData);
            GazePlot3DHeadTransform = Services.GazePointTranslationService.GetPosition3D(currentGazeData);
        }
    }

    private void MediaPlayerService_OnStopped(object? sender, EventArgs e)
    {
        PlaybackTime = 0;
        IsPlaying = false;
        TogglePlayVideoCommandLabel = VideoCommandPlayLabel;
    }
}
