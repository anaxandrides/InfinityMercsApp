using System.Collections.ObjectModel;
using System.Collections.Specialized;
using System.ComponentModel;

namespace InfinityMercsApp.ViewModels;

/// <summary>
/// An <see cref="ObservableCollection{T}"/> that can replace its entire contents with a single
/// <see cref="NotifyCollectionChangedAction.Reset"/> notification. Populating a bound
/// <c>CollectionView</c> one <c>Add</c> at a time triggers a measure/layout pass per item; a bulk
/// replace lets it re-lay out once, which matters when a tap swaps in dozens of rows.
/// </summary>
public sealed class RangeObservableCollection<T> : ObservableCollection<T>
{
    public RangeObservableCollection()
    {
    }

    public RangeObservableCollection(IEnumerable<T> items)
        : base(items)
    {
    }

    /// <summary>Clears the collection and adds <paramref name="items"/>, raising one Reset.</summary>
    public void ReplaceAll(IEnumerable<T> items)
    {
        Items.Clear();
        foreach (var item in items)
        {
            Items.Add(item);
        }

        OnPropertyChanged(new PropertyChangedEventArgs(nameof(Count)));
        OnPropertyChanged(new PropertyChangedEventArgs("Item[]"));
        OnCollectionChanged(new NotifyCollectionChangedEventArgs(NotifyCollectionChangedAction.Reset));
    }
}
