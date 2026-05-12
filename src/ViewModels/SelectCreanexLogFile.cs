using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using System.Collections.ObjectModel;

namespace CreanexDataVis.ViewModels;

internal partial class SelectCreanexLogFile : ObservableObject
{
    public event EventHandler<bool>? CloseRequest;

    [ObservableProperty]
    public partial ObservableCollection<KeyValuePair<string, string>> Items { get; set; } = [];

    [ObservableProperty]
    [NotifyPropertyChangedFor(nameof(HasItemSelected))]
    public partial int SelectedItemIndex { get; set; } = -1;

    public string? SelectedFilename { get; set; }

    public bool HasItemSelected => SelectedItemIndex > -1;

    public void SetItems(KeyValuePair<string, string>[] items)
    {
        foreach (var kv in items)
        {
            Items.Add(kv);
        }
    }

    #region Internal

    partial void OnSelectedItemIndexChanged(int value)
    {
        SelectedFilename = value >= 0
            ? Items[value].Value
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
