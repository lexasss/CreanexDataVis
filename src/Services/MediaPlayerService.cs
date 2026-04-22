using System.Windows.Controls;

namespace CreanexDataVis.Services;

public interface IMediaPlayerService
{
    void Load(Uri source);
    void Play(double fromTime); // seconds
    void Pause();
    void Stop();
}

public class MediaPlayerService : IMediaPlayerService
{
    public void SetMediaElement(MediaElement element)
    {
        _mediaElement = element;
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
        }
    }

    public void Pause()
    {
        _mediaElement?.Pause();
    }

    public void Stop()
    {
        _mediaElement?.Stop();
    }

    // Internal

    private MediaElement? _mediaElement;
}