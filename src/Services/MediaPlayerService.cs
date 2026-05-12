using System.Windows;
using System.Windows.Controls;
using System.Windows.Threading;

namespace CreanexDataVis.Services;

public interface IMediaPlayerService
{
    event EventHandler<double>? ProgressChanged;
    event EventHandler? Stopped;
    bool IsPlaying { get; }
    bool IsLoaded { get; }
    string? Filename { get; }
    void SetMediaElement(MediaElement element);
    void Load(Uri source);
    void Play(double fromTime); // seconds
    void Pause();
    void Stop();
}

public class MediaPlayerService : IMediaPlayerService
{
    public event EventHandler<double>? ProgressChanged;
    public event EventHandler? Stopped;

    public bool IsPlaying { get; private set; } = false;
    public bool IsLoaded { get; private set; } = false;
    public string? Filename { get; private set; } = null;

    public MediaPlayerService()
    {
        _timer = new DispatcherTimer
        {
            Interval = TimeSpan.FromSeconds(TimeStep)
        };

        _timer.Tick += (_, _) =>
        {
            if (_mediaElement != null)
            {
                var pos = _mediaElement.Position;
                ProgressChanged?.Invoke(this, pos.TotalSeconds);
            }
        };
    }

    public void SetMediaElement(MediaElement element)
    {
        _mediaElement = element;
        _mediaElement.MediaEnded += OnMediaEnded;
    }

    public void Load(Uri source)
    {
        Filename = source.LocalPath;

        if (_mediaElement != null)
        {
            _mediaElement.Source = source;
            _mediaElement.MediaOpened += OnMediaOpened;
            _mediaElement.Play();
        }
    }

    public void Play(double fromTime)
    {
        _mediaElement?.Position = TimeSpan.FromSeconds(fromTime);
        _mediaElement?.Play();
        _timer.Start();
        IsPlaying = true;
    }

    public void Pause()
    {
        _mediaElement?.Pause();
        _timer.Stop();
        IsPlaying = false;
    }

    public void Stop()
    {
        _mediaElement?.Stop();
        _timer.Stop();
        IsPlaying = false;
    }

    // Internal

    const double TimeStep = 0.05; // seconds

    readonly DispatcherTimer _timer;

    MediaElement? _mediaElement;

    private void OnMediaEnded(object sender, RoutedEventArgs e)
    {
        _timer.Stop();
        IsPlaying = false;

        Stopped?.Invoke(this, EventArgs.Empty);
    }

    private void OnMediaOpened(object sender, RoutedEventArgs e)
    {
        _mediaElement?.Pause();
        _mediaElement?.MediaOpened -= OnMediaOpened;

        IsLoaded = true;
    }
}