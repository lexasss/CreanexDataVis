using Microsoft.Win32;
using System.Windows;
using System.Windows.Input;

namespace CreanexDataVis;

public partial class MainWindow : Window
{
    public MainWindow()
    {
        InitializeComponent();
    }

    private void Window_PreviewMouseDoubleClick(object sender, MouseButtonEventArgs e)
    {
        OpenFileDialog ofd = new OpenFileDialog()
        {
            Filter = "CSV Files (*.csv)|*.csv|All Files (*.*)|*.*"
        };

        if (ofd.ShowDialog() == true)
        {
            var records = Services.DataParser.Parse(ofd.FileName);
            if (records != null)
            {
                var renderer = new Services.TimelineRenderer();
                TimelineImage.Source = renderer.Render(records);

                lblPrompt.Visibility = Visibility.Collapsed;

                Title = $"Creanex Data Visualization - {System.IO.Path.GetFileName(ofd.FileName)}";
            }
        }
    }
}