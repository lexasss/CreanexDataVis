using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using Microsoft.Win32;

namespace CreanexDataVis.ViewModels;

internal partial class MainViewVM : ObservableObject
{
    [ObservableProperty]
    public partial string Title { get; set; } = MainTitle;

    [ObservableProperty]
    public partial Services.TimelineRenderer.VisualHost? Timeline { get; set; }

    [ObservableProperty]
    public partial Services.GazePlotRenderer.VisualHost? GazePlot { get; set; }

    // Internal

    readonly static string MainTitle = "Creanex Data Visualization";

    [RelayCommand]
    private void LoadCreanexData()
    {
        var ofd = new OpenFileDialog()
        {
            Filter = "CSV files (*.csv)|*.csv|All files (*.*)|*.*"
        };

        if (ofd.ShowDialog() == true)
        {
            var records = Services.TimelineDataParser.Parse(ofd.FileName);
            if (records != null)
            {
                var renderer = new Services.TimelineRenderer();
                Timeline = renderer.GetVisualHost(records);
                Title = $"{MainTitle} - {System.IO.Path.GetFileName(ofd.FileName)}";
            }
        }
    }

    [RelayCommand]
    private void LoadVarjoData()
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
                GazePlot = renderer.GetVisualHost(records);
                Title = $"{MainTitle} - {System.IO.Path.GetFileName(ofd.FileName)}";
            }
        }
    }
}
