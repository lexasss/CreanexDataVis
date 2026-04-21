using Microsoft.Win32;
using System.Windows;

namespace CreanexDataVis;

public partial class MainWindow : Window
{
    public MainWindow()
    {
        InitializeComponent();
    }

    private void LoadGazingTimeline_Click(object sender, RoutedEventArgs e)
    {
        OpenFileDialog ofd = new OpenFileDialog()
        {
            Filter = "CSV files (*.csv)|*.csv|All files (*.*)|*.*"
        };

        if (ofd.ShowDialog() == true)
        {
            var records = Services.TimelineDataParser.Parse(ofd.FileName);
            if (records != null)
            {
                var renderer = new Services.TimelineRenderer();
                //TimelineImage.Source = renderer.Render(records);
                Services.TimelineRenderer.VisualHost? host = renderer.GetVisualHost(records);
                scvTimeline.Content = host;

                Title = $"Creanex Data Visualization - {System.IO.Path.GetFileName(ofd.FileName)}";
            }
        }
    }

    private void LoadGazePoint_Click(object sender, RoutedEventArgs e)
    {
        OpenFileDialog ofd = new OpenFileDialog()
        {
            Filter = "CSV files (*.csv)|*.csv|All files (*.*)|*.*"
        };

        if (ofd.ShowDialog() == true)
        {
            var records = Services.VarjoDataParser.Parse(ofd.FileName);
            if (records != null)
            {
                var renderer = new Services.GazePlotRenderer();
                Services.GazePlotRenderer.VisualHost? host = renderer.GetVisualHost(records);
                scvGazePoints.Content = host;

                Title = $"Creanex Data Visualization - {System.IO.Path.GetFileName(ofd.FileName)}";
            }
        }
    }
}