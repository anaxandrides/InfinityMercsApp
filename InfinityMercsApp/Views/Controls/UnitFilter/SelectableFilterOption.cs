using System.ComponentModel;

namespace InfinityMercsApp.Views.Controls.UnitFilter;

public sealed class SelectableFilterOption : INotifyPropertyChanged
{
    private bool _isSelected;

    public SelectableFilterOption(string value)
    {
        Value = value;
    }

    public string Value { get; }

    public bool IsSelected
    {
        get => _isSelected;
        set
        {
            if (_isSelected == value)
            {
                return;
            }

            _isSelected = value;
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(nameof(IsSelected)));
        }
    }

    public event PropertyChangedEventHandler? PropertyChanged;
}
