using System.Collections.Specialized;
using System.ComponentModel;
using Microsoft.AspNetCore.Components;

namespace InfinityMercsApp.Web.Components.Base;

/// <summary>
/// A component over shared view models: it draws again when one it watches changes a property or a collection. Blazor
/// has no bindings, so this is how a view model's changes reach the page; what it watches is let go when it is disposed.
/// </summary>
public abstract class ObservingComponent : ComponentBase, IDisposable
{
    private readonly HashSet<INotifyPropertyChanged> _sources = [];
    private readonly HashSet<INotifyCollectionChanged> _collections = [];

    /// <summary>Draws again whenever <paramref name="source"/> changes a property.</summary>
    protected void Watch(INotifyPropertyChanged? source)
    {
        if (source is not null && _sources.Add(source))
        {
            source.PropertyChanged += SourceChanged;
        }
    }

    /// <summary>Draws again whenever items are added to, removed from or moved in <paramref name="collection"/>.</summary>
    protected void WatchItems(INotifyCollectionChanged? collection)
    {
        if (collection is not null && _collections.Add(collection))
        {
            collection.CollectionChanged += CollectionChanged;
        }
    }

    /// <summary>Stops watching everything, as when a component's parameters change to another view model.</summary>
    protected void StopWatching()
    {
        foreach (var source in _sources)
        {
            source.PropertyChanged -= SourceChanged;
        }

        foreach (var collection in _collections)
        {
            collection.CollectionChanged -= CollectionChanged;
        }

        _sources.Clear();
        _collections.Clear();
    }

    public virtual void Dispose()
    {
        StopWatching();
        GC.SuppressFinalize(this);
    }

    private void SourceChanged(object? sender, PropertyChangedEventArgs e) => _ = InvokeAsync(StateHasChanged);

    private void CollectionChanged(object? sender, NotifyCollectionChangedEventArgs e) => _ = InvokeAsync(StateHasChanged);
}
