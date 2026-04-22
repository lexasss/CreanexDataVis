using System.Windows;
using System.Windows.Controls;
using System.Windows.Threading;

namespace CreanexDataVis.Services;

public interface IMediaPlayerService
{
    event EventHandler<double>? OnProgressChanged;
    event EventHandler? OnStopped;
    bool IsPlaying { get; }
    void Load(Uri source);
    void Play(double fromTime); // seconds
    void Pause();
    void Stop();
}

public class MediaPlayerService : IMediaPlayerService
{
    public event EventHandler<double>? OnProgressChanged;
    public event EventHandler? OnStopped;

    public bool IsPlaying { get; private set; } = false;

    public MediaPlayerService(MediaElement element)
    {
        _mediaElement = element;
        _mediaElement.MediaEnded += OnMediaEnded;

        _timer = new DispatcherTimer
        {
            Interval = TimeSpan.FromSeconds(TimeStep)
        };

        _timer.Tick += (_, _) =>
        {
            var pos = _mediaElement.Position;
            OnProgressChanged?.Invoke(this, pos.TotalSeconds);
        };
    }

    public void Load(Uri source)
    {
        if (_mediaElement != null)
            _mediaElement.Source = source;
    }

    public void Play(double fromTime)
    {
        if (_mediaElement != null)
        {
            _mediaElement.Position = TimeSpan.FromSeconds(fromTime);
            _mediaElement.Play();
            _timer.Start();
            IsPlaying = true;
        }
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

    readonly MediaElement? _mediaElement;
    readonly DispatcherTimer _timer;

    private void OnMediaEnded(object sender, RoutedEventArgs e)
    {
        _timer.Stop();
        IsPlaying = false;

        OnStopped?.Invoke(this, EventArgs.Empty);
    }
}