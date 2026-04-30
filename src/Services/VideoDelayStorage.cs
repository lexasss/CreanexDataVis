using System.Text.Json;

namespace CreanexDataVis.Services;

internal static class VideoDelayStorage
{
    public static bool TryGetDelay(string filename, out double delay) => 
        _items.TryGetValue(System.IO.Path.GetFileNameWithoutExtension(filename), out delay);

    public static void SetDelay(string filename, double delay) => 
        _items[System.IO.Path.GetFileNameWithoutExtension(filename)] = delay;

    // Internal

    static Dictionary<string, double> _items = [];

    static VideoDelayStorage()
    {
        App.Current.Exit += App_Exit;

        try
        {
            _items = JsonSerializer.Deserialize<Dictionary<string, double>>(Properties.Settings.Default.VideoDelays) ?? _items;
        }
        catch { }
    }

    private static void App_Exit(object sender, System.Windows.ExitEventArgs e)
    {
        Properties.Settings.Default.VideoDelays = JsonSerializer.Serialize(_items);
        Properties.Settings.Default.Save();
    }
}
