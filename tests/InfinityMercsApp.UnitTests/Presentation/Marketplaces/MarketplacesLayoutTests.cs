using InfinityMercsApp.Presentation.Layouts;
using InfinityMercsApp.Presentation.Marketplaces;
namespace InfinityMercsApp.UnitTests.Presentation.Marketplaces;

public sealed class MarketplacesLayoutTests
{
    [Fact]
    public void StacksForAPhone()
    {
        var layout = MarketplacesLayout.For(AdaptiveLayoutMode.Compact);

        Assert.Equal("*", layout.ContentColumns);
        Assert.Equal("Auto,*", layout.ContentRows);
        Assert.Equal(new GridCell(1, 0), layout.SectionsCell);
        Assert.Equal(new GridCell(0, 0, RowSpan: 2), layout.PlaceholderCell);
        Assert.Equal("Auto,Auto,Auto", layout.SectionsRows);
        Assert.Equal(new GridCell(2, 0), layout.AugmentsCell);
        Assert.False(layout.OffersBuying);
    }

    [Fact]
    public void MediumStacksTheStoreInfoButSplitsTheSections()
    {
        var layout = MarketplacesLayout.For(AdaptiveLayoutMode.Medium);

        Assert.Equal("Auto,*", layout.ContentRows);
        Assert.Equal("*,*", layout.SectionsColumns);
        Assert.Equal(new GridCell(0, 0, RowSpan: 2), layout.ItemsCell);
        Assert.Equal(new GridCell(0, 1), layout.TroopTypesCell);
        Assert.Equal(new GridCell(1, 1), layout.AugmentsCell);
    }

    [Fact]
    public void ExpandedPutsTheStoreInfoInARail()
    {
        var layout = MarketplacesLayout.For(AdaptiveLayoutMode.Expanded);

        Assert.Equal("280,*", layout.ContentColumns);
        Assert.Equal("*", layout.ContentRows);
        Assert.Equal(12, layout.ContentColumnSpacing);
        Assert.Equal(0, layout.ContentRowSpacing);
        Assert.Equal(new GridCell(0, 1), layout.SectionsCell);
        Assert.Equal(new GridCell(0, 0, ColumnSpan: 2), layout.PlaceholderCell);
        Assert.False(layout.OffersBuying);
    }

    [Fact]
    public void WideAddsTheCartRailAndBuying()
    {
        var layout = MarketplacesLayout.For(AdaptiveLayoutMode.Wide);

        Assert.Equal("280,*,320", layout.ContentColumns);
        Assert.Equal(new GridCell(0, 2), layout.CartCell);
        Assert.Equal(new GridCell(0, 0, ColumnSpan: 3), layout.PlaceholderCell);
        Assert.True(layout.OffersBuying);
    }
}
