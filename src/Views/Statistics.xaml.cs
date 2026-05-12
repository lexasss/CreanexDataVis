using System.Windows;

namespace CreanexDataVis.Views;

public partial class Statistics : Window
{
    public Statistics()
    {
        Owner = Application.Current.MainWindow;

        InitializeComponent();

        if (DataContext is ViewModels.Statistics vm)
            vm.HideCopyToClipboardConfirmation += (s, e) =>
                Dispatcher.Invoke(() => vm.CopyToClipboardConfirmationVisibility = Visibility.Hidden);
    }
}
