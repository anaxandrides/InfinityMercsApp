using InfinityMercsApp.ViewModels.Base;
using InfinityMercsApp.ViewModels.UnitBrowser;

namespace InfinityMercsApp.UnitTests.ViewModels.UnitBrowser;

public sealed class UnitEncyclopediaLayoutTests
{
    [Fact]
    public void CompactShowsOnlyTheCurrentStep()
    {
        var layout = UnitEncyclopediaLayout.For(AdaptiveLayoutMode.Compact);

        Assert.True(layout.ShowsOneStep);
        Assert.Equal(new GridCell(0, 0), layout.DetailCell);
        Assert.True(layout.Shows(BrowseStep.Units, BrowseStep.Units));
        Assert.False(layout.Shows(BrowseStep.Factions, BrowseStep.Units));
    }

    [Fact]
    public void MediumStacksFactionsAboveUnitsBesideTheDetail()
    {
        var layout = UnitEncyclopediaLayout.For(AdaptiveLayoutMode.Medium);

        Assert.Equal("300,*", layout.Columns);
        Assert.Equal("*,*", layout.Rows);
        Assert.Equal(new GridCell(1, 0), layout.BrowseCell);
        Assert.Equal(new GridCell(0, 1, RowSpan: 2), layout.DetailCell);
        Assert.Equal(new GridCell(0, 0, RowSpan: 2, ColumnSpan: 2), layout.OverlayCell);
        Assert.True(layout.Shows(BrowseStep.Factions, BrowseStep.Detail));
    }

    [Theory]
    [InlineData(AdaptiveLayoutMode.Expanded, "260,0.7*,1.3*")]
    [InlineData(AdaptiveLayoutMode.Wide, "280,340,*")]
    public void WiderScreensShowThreeColumns(AdaptiveLayoutMode mode, string columns)
    {
        var layout = UnitEncyclopediaLayout.For(mode);

        Assert.Equal(columns, layout.Columns);
        Assert.Equal(new GridCell(0, 1), layout.BrowseCell);
        Assert.Equal(new GridCell(0, 2), layout.DetailCell);
        Assert.Equal(new GridCell(0, 0, ColumnSpan: 3), layout.OverlayCell);
        Assert.False(layout.ShowsOneStep);
    }
}
