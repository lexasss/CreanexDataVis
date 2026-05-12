using Microsoft.Extensions.DependencyInjection;
using System.Windows;

namespace CreanexDataVis;

public partial class App : Application
{
    public static IServiceProvider ServiceProvider { get; private set; }

    static App()
    {
        ServiceCollection services = new();
        services.AddSingleton<Services.IGazePlot3DRenderer, Services.GazePlot3DRenderer>();
        services.AddSingleton<Services.IMediaPlayerService, Services.MediaPlayerService>();
        services.AddSingleton<Services.IStatistics, Services.Statistics>();

        ServiceProvider = services.BuildServiceProvider();
    }
}
