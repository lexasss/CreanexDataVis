using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using Microsoft.Extensions.DependencyInjection;
using System.Collections.ObjectModel;
using System.Windows;

namespace CreanexDataVis.ViewModels;

internal partial class Statistics : ObservableObject
{
    [ObservableProperty]
    public partial ObservableCollection<KeyValuePair<string, double>> AttentionItems { get; set; } = [];

    [ObservableProperty]
    public partial ObservableCollection<KeyValuePair<string, double>> OperationItems { get; set; } = [];

    [ObservableProperty]
    public partial Visibility CopyToClipboardConfirmationVisibility { get; set; } = Visibility.Hidden;

    public Statistics()
    {
        var statisticsService = App.ServiceProvider.GetService<Services.IStatistics>()!;
        
        var attentionShares = statisticsService.GetAttentionShares();
        foreach (var kv in attentionShares)
        {
            AttentionItems.Add(new KeyValuePair<string, double>(kv.Key, kv.Value * 100));
        }

        var operations = statisticsService.GetOperations();
        foreach (var kv in operations)
        {
            OperationItems.Add(kv);
        }
    }

    #region Commands

    [RelayCommand]
    private void CopyToClipboard()
    {
        List<string> lines = [];
        foreach (var kv in AttentionItems)
            lines.Add($"{kv.Key}\t{kv.Value:F2}");
        foreach (var kv in OperationItems)
            lines.Add($"{kv.Key}\t{kv.Value:F1}");

        Clipboard.SetText(string.Join('\n', lines));

        CopyToClipboardConfirmationVisibility = Visibility.Visible;
        Task.Run(async () =>
        {
            await Task.Delay(2000);
            Application.Current.MainWindow.Dispatcher.Invoke(() => CopyToClipboardConfirmationVisibility = Visibility.Hidden);
        });
    }

    #endregion
}
