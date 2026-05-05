using CreanexDataVis.ViewModels;
using Microsoft.Extensions.DependencyInjection;
using System.Windows;
using System.Windows.Input;

namespace CreanexDataVis;

public partial class MainWindow : Window
{
    public MainWindow()
    {
        InitializeComponent();

        ServiceCollection services = new();
        services.AddSingleton<Services.IGazePlot3DRenderer, Services.GazePlot3DRenderer>();
        services.AddSingleton<Services.IMediaPlayerService, Services.MediaPlayerService>(sp => new Services.MediaPlayerService(VideoPlayer));

        App.ServiceProvider = services.BuildServiceProvider();

        (DataContext as MainViewModel)!.InjectServices();
    }

    private void Window_PreviewKeyDown(object sender, KeyEventArgs e)
    {
        if (e.Key == Key.Space)
        {
            if (DataContext is MainViewModel vm &&
                vm.TogglePlayVideoCommand.CanExecute(null))
            {
                vm.TogglePlayVideoCommand.Execute(null);
                e.Handled = true; // prevent TextBox from inserting space
            }
        }
    }
}