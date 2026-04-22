using CreanexDataVis.Services;
using CreanexDataVis.ViewModels;
using System.Windows;

namespace CreanexDataVis;

public partial class MainWindow : Window
{
    public MainWindow()
    {
        InitializeComponent();

        var mediaService = new MediaPlayerService(VideoPlayer);

        DataContext = new MainViewVM(mediaService);
    }
}