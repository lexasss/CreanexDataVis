using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using HelixToolkit.Geometry;
using HelixToolkit.SharpDX;
using HelixToolkit.Wpf.SharpDX;
using Microsoft.Extensions.DependencyInjection;
using System.Numerics;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Media;
using System.Windows.Media.Media3D;

namespace CreanexDataVis.ViewModels;

internal partial class MainViewModel : ObservableObject
{
    public partial class ExpandableColumnProps : ObservableObject
    {
        [ObservableProperty]
        public partial GridLength Width { get; set; } = new(1, GridUnitType.Star);
        [ObservableProperty]
        public partial bool IsExpanded { get; set; } = false;
    }

    [ObservableProperty]
    [NotifyPropertyChangedFor(nameof(IsTimelineReady))]
    public partial FrameworkElement? Timeline { get; set; }

    [ObservableProperty]
    public partial FrameworkElement? GazePlot { get; set; }

    [ObservableProperty]
    public partial double VideoDelay { get; set; } = 0;     // seconds

    [ObservableProperty]
    public partial double PlaybackTime { get; set; } = 0;   // seconds

    [ObservableProperty]
    public partial Transform GazePointPosition { get; set; } = Services.GazePointTranslationService.DefaultGazePointTransform;

    public bool IsTimelineReady => Timeline != null;

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
    public partial LineGeometry3D? GazePlot3D { get; private set; }

    [ObservableProperty]
    public partial EffectsManager GazePlotEffectsManager { get; set; } = new DefaultEffectsManager();

    [ObservableProperty]
    public partial HelixToolkit.SharpDX.MeshGeometry3D GazePlot3DHead { get; set; }

    [ObservableProperty]
    public partial Transform3D GazePlot3DHeadTransform { get; set; } = Services.GazePointTranslationService.DefaultGazePoint3DTransform;

    [ObservableProperty]
    public partial PhongMaterial GazePlot3DHeadMaterial { get; set; } = PhongMaterials.Red;

    [ObservableProperty]
    public partial int StudyDataIndex { get; set; } = -1;

    [ObservableProperty]
    [NotifyPropertyChangedFor(nameof(HasMultipleLogFiles))]
    public partial string[] StudyLogFiles { get; set; } = [];

    public bool HasMultipleLogFiles => StudyLogFiles.Length > 1;

    // Visualizations
    [ObservableProperty]
    public partial ExpandableColumnProps VisColumn1 { get; set; } = new();

    [ObservableProperty]
    public partial ExpandableColumnProps VisColumn2 { get; set; } = new();

    [ObservableProperty]
    public partial ExpandableColumnProps VisColumn3 { get; set; } = new();


    public MainViewModel()
    {
        _gazePlot3DRenderer = App.ServiceProvider.GetService<Services.IGazePlot3DRenderer>()!;

        _statisticsService = App.ServiceProvider.GetService<Services.IStatistics>()!;


        _logFileService = App.ServiceProvider.GetService<Services.ILogFileService>()!;
        _logFileService.CreanexLogFileSelected += LogFileService_CreanexLogFileSelected;
        _logFileService.VarjoLogFileSelected += LogFileService_VarjoLogFileSelected;
        _logFileService.VideoFileSelected += LogFileService_VideoFileSelected;

        _mediaPlayerService = App.ServiceProvider.GetService<Services.IMediaPlayerService>()!;
        _mediaPlayerService.ProgressChanged += MediaPlayerService_ProgressChanged;
        _mediaPlayerService.Stopped += MediaPlayerService_Stopped;

        var b1 = new MeshBuilder();
        b1.AddSphere(new Vector3(0, 0, 0), 0.02f);
        GazePlot3DHead = b1.ToMeshGeometry3D();

        _visColumnProps = [VisColumn1, VisColumn2, VisColumn3];
    }

    // Internal

    const int ExpandedColumnStarWidth = 3;
    const double TimelineMarkMaxRight = 0.8;
    const double TimelineMarkMinLeft = 0.05;

    readonly static string VideoCommandPlayLabel = "▶";
    readonly static string VideoCommandPauseLabel = "⏸";

    readonly Services.IStatistics _statisticsService;
    readonly Services.IMediaPlayerService _mediaPlayerService;
    readonly Services.IGazePlot3DRenderer _gazePlot3DRenderer;
    readonly Services.ILogFileService _logFileService;

    readonly ExpandableColumnProps[] _visColumnProps;

    IO.TimelineDataParser? _timelineParser;
    IO.VarjoDataParser? _varjoParser;
    Services.GazePointTranslationService? _gazePointTranslationService;

    double _timelineOffset;
    Point _gazePlotOffset;

    #region OnChange handlers for observable properties

    partial void OnVideoDelayChanged(double value)
    {
        if (_mediaPlayerService.Filename != null)
        {
            IO.VideoDelayStorage.SetDelay(_mediaPlayerService.Filename, VideoDelay);
        }
    }

    partial void OnStudyDataIndexChanged(int value)
    {
        _logFileService.SetCreanexLogFile(value < 0 || value >= StudyLogFiles.Length ? null : StudyLogFiles[value]);
    }

    #endregion

    #region Commands

    [RelayCommand]
    private void LoadData()
    {
        if (_logFileService.ChooseParticipantFolder())
        {
            StudyLogFiles = _logFileService.FolderCreanexFiles;
            if (StudyLogFiles.Length > 0)
            {
                StudyDataIndex = 0;
            }
        }
    }

    [RelayCommand]
    private void TogglePlayVideo()
    {
        if (_mediaPlayerService.IsPlaying == true)
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


    [RelayCommand]
    private void ChangeVisColumnWidth(string column)
    {
        int index = int.Parse(column);

        bool isVisColumnSelectedAlready = !_visColumnProps[index].IsExpanded; // reversed, as the flag is toggled already

        int i = 0;
        foreach (var colProps in _visColumnProps)
        {
            if (i != index)
            {
                colProps.Width = new GridLength(1, GridUnitType.Star);
                colProps.IsExpanded = false;
            }
            else
            {
                colProps.Width = new GridLength(isVisColumnSelectedAlready ? 1 : ExpandedColumnStarWidth, GridUnitType.Star);
                colProps.IsExpanded = !isVisColumnSelectedAlready;
            }
            ++i;
        }
    }

    [RelayCommand]
    private static void ShowStatistics()
    {
        var window = new Views.Statistics();
        window.ShowDialog();
    }

    #endregion

    #region Event handlers for services

    public void LogFileService_CreanexLogFileSelected(object? sender, Models.LogFileProps? logFileProps)
    {
        if (logFileProps == null)
        {
            Timeline = null;
            return;
        }

        _timelineParser = new IO.TimelineDataParser(logFileProps.Filename);
        if (_timelineParser.Records != null)
        {
            _statisticsService.SetData(_timelineParser.Records);

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

    private void LogFileService_VarjoLogFileSelected(object? sender, Models.LogFileProps? logFileProps)
    {
        if (logFileProps == null)
        {
            GazePlot = null;
            GazePlot3D = null;
            return;
        }

        _varjoParser = new IO.VarjoDataParser(logFileProps.Filename);
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

    private void LogFileService_VideoFileSelected(object? sender, string? filename)
    {
        if (filename == null)
        {
            _mediaPlayerService.Stop();
            _mediaPlayerService.Load(null);

            IsPlaybackEnabled = false;
            VideoDelay = 0;
            TimelineScrollX = 0;

            return;
        }

        _mediaPlayerService.Load(new Uri(filename));
        IsPlaybackEnabled = true;

        if (IO.VideoDelayStorage.TryGetDelay(filename, out double videoDelay))
        {
            VideoDelay = videoDelay;
        }
    }

    private void MediaPlayerService_ProgressChanged(object? sender, double e)
    {
        PlaybackTime = e + VideoDelay;

        var x = Services.TimelineRenderer.SecondsToPixels(PlaybackTime);
        if (TimelineScrollX < x - TimelineMarkMaxRight * TimelineWidth)
        {
            TimelineScrollX = x - TimelineMarkMaxRight * TimelineWidth;
        }
        else if (TimelineScrollX > x - TimelineMarkMinLeft * TimelineWidth)
        {
            TimelineScrollX = x - TimelineMarkMinLeft * TimelineWidth;
        }

        if (_gazePointTranslationService != null)
        {
            var currentGazeData = _gazePointTranslationService.GetGazeDataAt(PlaybackTime + _timelineOffset);
            GazePointPosition = _gazePointTranslationService.GetPosition2D(currentGazeData);
            GazePlot3DHeadTransform = Services.GazePointTranslationService.GetPosition3D(currentGazeData);
        }
    }

    private void MediaPlayerService_Stopped(object? sender, EventArgs e)
    {
        PlaybackTime = 0;
        IsPlaying = false;
        TogglePlayVideoCommandLabel = VideoCommandPlayLabel;
    }

    #endregion

    private void TimelineCanvas_MouseLeftButtonDown(object sender, System.Windows.Input.MouseButtonEventArgs e)
    {
        var canvas = sender as Canvas;
        e.Handled = true;
        var pos = e.GetPosition(canvas);
        PlaybackTime = Services.TimelineRenderer.PixelsToSeconds(pos.X);
    }
}
