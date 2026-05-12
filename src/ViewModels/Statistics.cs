using CommunityToolkit.Mvvm.ComponentModel;
using Microsoft.Extensions.DependencyInjection;
using System.Collections.ObjectModel;

namespace CreanexDataVis.ViewModels;

internal partial class Statistics : ObservableObject
{
    [ObservableProperty]
    public partial ObservableCollection<KeyValuePair<string, double>> Items { get; set; } = [];

    public Statistics()
    {
        var statisticsService = App.ServiceProvider.GetService<Services.IStatistics>()!;
        var statistics = statisticsService.GetAttentionShare();

        Items.Clear();
        foreach (var kv in statistics)
        {
            Items.Add(new KeyValuePair<string, double>(kv.Key, kv.Value * 100));
        }
    }
}
