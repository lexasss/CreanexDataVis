using System.Windows;

namespace CreanexDataVis.Views;

public partial class SelectCreanexLogFile : Window
{
    public string SelectedFilename => (DataContext as ViewModels.SelectCreanexLogFile)?.SelectedFilename ?? string.Empty;

    public SelectCreanexLogFile(KeyValuePair<string, string>[] items)
    {
        Owner = Application.Current.MainWindow;

        InitializeComponent();
        
        if (DataContext is ViewModels.SelectCreanexLogFile sclf)
        {
            sclf.SetItems(items);
            sclf.CloseRequest += (s, e) =>
            {
                DialogResult = e;
            };
        }
    }
}
