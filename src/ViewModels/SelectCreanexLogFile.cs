using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using System.Collections.ObjectModel;

namespace CreanexDataVis.ViewModels;

internal partial class SelectCreanexLogFile : ObservableObject
{
    public event EventHandler<bool>? CloseRequest;

    [ObservableProperty]
    public partial ObservableCollection<Models.LogFileProps> Items { get; set; } = [];

    [ObservableProperty]
    [NotifyPropertyChangedFor(nameof(HasItemSelected))]
    public partial int SelectedItemIndex { get; set; } = -1;

    public Models.LogFileProps? SelectedLogFile { get; set; }

    public bool HasItemSelected => SelectedItemIndex > -1;

    public void SetItems(Models.LogFileProps[] items)
    {
        foreach (var item in items)
        {
            Items.Add(item);
        }
    }

    #region Internal

    partial void OnSelectedItemIndexChanged(int value)
    {
        SelectedLogFile = value >= 0
            ? Items[value]
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
