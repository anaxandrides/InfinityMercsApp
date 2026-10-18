using InfinityMercsApp.Presentation.Layouts;

namespace InfinityMercsApp.UnitTests.Presentation.Layouts;

public sealed class SplitPaneLayoutTests
{
    private static readonly SplitPaneSizes Sizes = new(300, 320, 340, 860);

    [Fact]
    public void For_Compact_StacksThePanesInOneColumn()
    {
        var layout = SplitPaneLayout.For(AdaptiveLayoutMode.Compact, Sizes);

        Assert.Equal("*", layout.Columns);
        Assert.Equal(new GridCell(0, 0), layout.DetailCell);
        Assert.True(layout.StacksPanes);
        Assert.True(layout.ShowsList(isShowingDetail: false));
        Assert.False(layout.ShowsDetail(isShowingDetail: false));
        Assert.False(layout.ShowsList(isShowingDetail: true));
        Assert.True(layout.ShowsDetail(isShowingDetail: true));
    }

    [Theory]
    [InlineData(AdaptiveLayoutMode.Medium, "300,*", double.PositiveInfinity)]
    [InlineData(AdaptiveLayoutMode.Expanded, "320,*", double.PositiveInfinity)]
    [InlineData(AdaptiveLayoutMode.Wide, "340,*", 860)]
    public void For_WiderModes_ShowsBothPanes(AdaptiveLayoutMode mode, string columns, double readingWidth)
    {
        var layout = SplitPaneLayout.For(mode, Sizes);

        Assert.Equal(columns, layout.Columns);
        Assert.Equal(16, layout.ColumnSpacing);
        Assert.Equal(new GridCell(0, 1), layout.DetailCell);
        Assert.Equal(readingWidth, layout.ReadingMaxWidth);
        Assert.True(layout.ShowsList(isShowingDetail: true) && layout.ShowsDetail(isShowingDetail: false));
    }
}
