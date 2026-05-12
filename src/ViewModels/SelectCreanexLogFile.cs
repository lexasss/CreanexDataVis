using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using Microsoft.Extensions.DependencyInjection;
using System.Collections.ObjectModel;
using System.IO;

namespace CreanexDataVis.ViewModels;

internal partial class SelectCreanexLogFile : ObservableObject
{
    public event EventHandler<bool>? CloseRequest;

    [ObservableProperty]
    public partial ObservableCollection<KeyValuePair<string, string>> Items { get; set; } = [];

    [ObservableProperty]
    [NotifyPropertyChangedFor(nameof(HasItemSelected))]
    public partial int SelectedItemIndex { get; set; } = -1;

    public bool HasItemSelected => SelectedItemIndex > -1;

    public SelectCreanexLogFile()
    {
        _logFileService = App.ServiceProvider.GetService<Services.ILogFileService>()!;
        var filenames = _logFileService.GetCreanexFiles();

        foreach (var kv in filenames)
        {
            Items.Add(kv);
        }
    }

    #region Internal

    readonly Services.ILogFileService _logFileService;

    partial void OnSelectedItemIndexChanged(int value)
    {
        _logFileService.SelectedCreanexFile = value >= 0
            ? Path.Combine(_logFileService.Folder, Items[value].Value)
            : null;
    }

    [RelayCommand]
    private void Accept()
    {
        CloseRequest?.Invoke(this, true);
    }


    [RelayCommand]
    private void Cancel()
    {
        CloseRequest?.Invoke(this, false);
    }

    #endregion
}
