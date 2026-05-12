using System.Windows;

namespace CreanexDataVis.Views;

public partial class SelectCreanexLogFile : Window
{
    public SelectCreanexLogFile()
    {
        InitializeComponent();

        if (DataContext is ViewModels.SelectCreanexLogFile sclf)
        {
            sclf.CloseRequest += (s, e) =>
            {
                DialogResult = e;
            };
        }
    }
}
