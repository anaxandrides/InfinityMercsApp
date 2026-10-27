using System.Collections.ObjectModel;
using Bunit;

namespace InfinityMercsApp.Web.UnitTests.Components.Base;

/// <summary>A component over view models draws again when they change, until it lets them go.</summary>
public sealed class ObservingComponentTests : BunitContext
{
    private readonly ObservableObjectProbe _source = new();
    private readonly ObservableCollection<string> _items = [];

    private IRenderedComponent<WatchingComponent> Watching() =>
        Render<WatchingComponent>(parameters => parameters.Add(p => p.Source, _source).Add(p => p.Items, _items));

    [Fact]
    public void PropertyChanged_Watched_DrawsAgainOnce()
    {
        var component = Watching();

        _source.Name = "changed";

        component.WaitForAssertion(() => Assert.Equal(2, component.Instance.Draws));
    }

    [Fact]
    public void CollectionChanged_Watched_DrawsAgain()
    {
        var component = Watching();

        _items.Add("one");

        component.WaitForAssertion(() => Assert.Equal(2, component.Instance.Draws));
    }

    [Fact]
    public void Changes_AfterLettingGo_DrawNothing()
    {
        var component = Watching();

        component.Instance.LetGo();
        _source.Name = "changed";
        _items.Add("one");

        Assert.Equal(1, component.Instance.Draws);
    }

    [Fact]
    public void Changes_AfterDisposing_DrawNothing()
    {
        var component = Watching();

        component.Instance.Dispose();
        _source.Name = "changed";

        Assert.Equal(1, component.Instance.Draws);
    }
}
