using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using Microsoft.Win32;
using System.Windows;
using System.Windows.Data;

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
    public partial double VideoDelay { get; set; } = 0;     // seconds

    [ObservableProperty]
    public partial double PlaybackTime { get; set; } = 0;   // seconds

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

    public MainViewVM(Services.IMediaPlayerService mediaPlayerService)
    {
        _mediaPlayerService = mediaPlayerService;
        _mediaPlayerService.OnProgressChanged += (s, e) =>
        {
            PlaybackTime = e + VideoDelay;

            var x = PlaybackTime * 1000 * Services.TimelineRenderer.MsToPixel;
            if (TimelineScrollX < x - 0.8 * TimelineWidth)
            {
                TimelineScrollX = x - 0.8 * TimelineWidth;
            }
            else if (TimelineScrollX > x - 0.05 * TimelineWidth)
            {
                TimelineScrollX = x - 0.05 * TimelineWidth;
            }
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

                Timeline = canvas;
                Title = $"{MainTitle} - {System.IO.Path.GetFileName(ofd.FileName)}";

                if (canvas?.Children.Count > 1 && canvas.Children[1] is System.Windows.Shapes.Line timeMark)
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

                canvas?.MouseLeftButtonDown += (s, e) =>
                {
                    e.Handled = true;
                    var pos = e.GetPosition(canvas);
                    PlaybackTime = TimeSpan.FromMilliseconds(pos.X / Services.TimelineRenderer.MsToPixel).TotalSeconds;
                };
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
