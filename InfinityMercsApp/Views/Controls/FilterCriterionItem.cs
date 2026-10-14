using System.ComponentModel;

namespace InfinityMercsApp.Views.Controls;

public sealed class FilterCriterionItem : INotifyPropertyChanged
{
    private string _summary = "Any";
    private bool _isCurrent;
    private bool _hasSelections;

    public FilterCriterionItem(UnitFilterField field, string title)
    {
        Field = field;
        Title = title;
    }

    public UnitFilterField Field { get; }

    public string Title { get; }

    public string Summary
    {
        get => _summary;
        set
        {
            if (string.Equals(_summary, value, StringComparison.Ordinal))
            {
                return;
            }

            _summary = value;
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(nameof(Summary)));
        }
    }

    /// <summary>The criterion whose value chips are currently shown in the editor.</summary>
    public bool IsCurrent
    {
        get => _isCurrent;
        set
        {
            if (_isCurrent == value)
            {
                return;
            }

            _isCurrent = value;
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(nameof(IsCurrent)));
        }
    }

    /// <summary>True when this criterion has committed (active) values; drives the chip's active marker.</summary>
    public bool HasSelections
    {
        get => _hasSelections;
        set
        {
            if (_hasSelections == value)
            {
                return;
            }

            _hasSelections = value;
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(nameof(HasSelections)));
        }
    }

    public event PropertyChangedEventHandler? PropertyChanged;
}
