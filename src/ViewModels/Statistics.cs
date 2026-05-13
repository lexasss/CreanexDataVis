using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using Microsoft.Extensions.DependencyInjection;
using System.Collections.ObjectModel;
using System.Windows;

namespace CreanexDataVis.ViewModels;

internal partial class Statistics : ObservableObject
{
    [ObservableProperty]
    public partial ObservableCollection<Models.NamedValue<string>> GeneralItems { get; set; } = [];

    [ObservableProperty]
    public partial ObservableCollection<Models.NamedValue<double>> AttentionItems { get; set; } = [];

    [ObservableProperty]
    public partial ObservableCollection<Models.NamedValue<double>> OperationItems { get; set; } = [];

    [ObservableProperty]
    public partial Visibility CopyToClipboardConfirmationVisibility { get; set; } = Visibility.Hidden;

    public event EventHandler? HideCopyToClipboardConfirmation;

    public Statistics()
    {
        GeneralItems.Add(new Models.NamedValue<string>("Participant", _logFileService.Participant ?? "-"));
        GeneralItems.Add(new Models.NamedValue<string>("File", _logFileService.Filename ?? "-"));
        GeneralItems.Add(new Models.NamedValue<string>("Condition", _logFileService.Condition ?? "-"));

        var attentionShares = _statisticsService.GetAttentionShares();
        foreach (var item in attentionShares)
        {
            AttentionItems.Add(item with { Value = item.Value * 100 });
        }

        var operations = _statisticsService.GetOperations();
        foreach (var item in operations)
        {
            OperationItems.Add(item);
        }
    }

    #region Internal

    readonly Services.IStatistics _statisticsService = App.ServiceProvider.GetService<Services.IStatistics>()!;
    readonly Services.ILogFileService _logFileService = App.ServiceProvider.GetService<Services.ILogFileService>()!;

    #endregion

    #region Commands

    [RelayCommand]
    private void CopyToClipboard()
    {
        List<string> lines = [];

        foreach (var item in GeneralItems)
            lines.Add($"{item.Name}\t{item.Value:F2}");
        foreach (var item in AttentionItems)
            lines.Add($"{item.Name}\t{item.Value:F2}");
        foreach (var item in OperationItems)
            lines.Add($"{item.Name}\t{item.Value:F1}");

        Clipboard.SetText(string.Join('\n', lines));

        CopyToClipboardConfirmationVisibility = Visibility.Visible;
        Task.Run(async () =>
        {
            await Task.Delay(2000);
            HideCopyToClipboardConfirmation?.Invoke(this, EventArgs.Empty);
        });
    }

    #endregion
}
