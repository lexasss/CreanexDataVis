using CreanexDataVis.ViewModels;
using Microsoft.Extensions.DependencyInjection;
using System.Windows;
using System.Windows.Input;

namespace CreanexDataVis.Views;

public partial class MainWindow : Window
{
    public MainWindow()
    {
        InitializeComponent();

        App.ServiceProvider
            .GetService<Services.IMediaPlayerService>()!
            .SetMediaElement(VideoPlayer);
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