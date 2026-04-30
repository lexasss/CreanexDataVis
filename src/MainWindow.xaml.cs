using CreanexDataVis.ViewModels;
using System.Windows;
using System.Windows.Input;

namespace CreanexDataVis;

public partial class MainWindow : Window
{
    public MainWindow()
    {
        InitializeComponent();

        var mediaService = new Services.MediaPlayerService(VideoPlayer);
        var gazePlot3dRenderer = new Services.GazePlot3DRenderer();

        DataContext = new MainViewModel(mediaService, gazePlot3dRenderer);
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