using System.Windows;

namespace CreanexDataVis.Views;

public partial class SelectCreanexLogFile : Window
{
    public Models.LogFileProps? SelectedLogFileProps => (DataContext as ViewModels.SelectCreanexLogFile)!.SelectedLogFile;

    public SelectCreanexLogFile(Models.LogFileProps[] items)
    {
        Owner = Application.Current.MainWindow;

        InitializeComponent();
        
        if (DataContext is ViewModels.SelectCreanexLogFile vm)
        {
            vm.SetItems(items);
            vm.CloseRequest += (s, e) =>
            {
                DialogResult = e;
            };
        }
    }
}
