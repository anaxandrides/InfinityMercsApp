using System.Collections.Specialized;
using System.ComponentModel;
using InfinityMercsApp.Web.Components.Base;
using Microsoft.AspNetCore.Components;
using Microsoft.AspNetCore.Components.Rendering;

namespace InfinityMercsApp.Web.UnitTests.Components.Base;

/// <summary>Watches what it is given and counts how often it draws.</summary>
public sealed class WatchingComponent : ObservingComponent
{
    [Parameter] public INotifyPropertyChanged? Source { get; set; }

    [Parameter] public INotifyCollectionChanged? Items { get; set; }

    public int Draws { get; private set; }

    public void LetGo() => StopWatching();

    protected override void OnParametersSet()
    {
        Watch(Source);
        Watch(Source);
        WatchItems(Items);
    }

    protected override void BuildRenderTree(RenderTreeBuilder builder)
    {
        Draws++;
        builder.AddContent(0, Draws);
    }
}
