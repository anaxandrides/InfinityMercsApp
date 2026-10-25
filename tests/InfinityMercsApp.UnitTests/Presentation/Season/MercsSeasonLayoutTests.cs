using InfinityMercsApp.Presentation.Layouts;
using InfinityMercsApp.Presentation.Season;

namespace InfinityMercsApp.UnitTests.Presentation.Season;

public sealed class MercsSeasonLayoutTests
{
    [Fact]
    public void For_Compact_AnchorsFullWidthActionsBelowTheBrand()
    {
        var layout = MercsSeasonLayout.For(AdaptiveLayoutMode.Compact);

        Assert.Equal("*", layout.Columns);
        Assert.Equal("*,Auto", layout.Rows);
        Assert.Equal(new GridCell(1, 0), layout.ActionCell);
        Assert.Equal(new Inset(24, 0, 24, 40), layout.ActionPadding);
        Assert.Equal(Alignment.End, layout.ActionVerticalOptions);
        Assert.Equal(Alignment.Fill, layout.ActionHorizontalOptions);
        Assert.Equal(double.PositiveInfinity, layout.ActionMaximumWidth);
    }

    [Fact]
    public void For_Medium_CentresActionsAtAReadableWidth()
    {
        var layout = MercsSeasonLayout.For(AdaptiveLayoutMode.Medium);

        Assert.Equal(new GridCell(1, 0), layout.ActionCell);
        Assert.Equal(Alignment.Center, layout.ActionHorizontalOptions);
        Assert.Equal(520, layout.ActionMaximumWidth);
    }

    [Theory]
    [InlineData(AdaptiveLayoutMode.Expanded)]
    [InlineData(AdaptiveLayoutMode.Wide)]
    public void For_WiderModes_PutsActionsBesideTheBrand(AdaptiveLayoutMode mode)
    {
        var layout = MercsSeasonLayout.For(mode);

        Assert.Equal("*,*", layout.Columns);
        Assert.Equal("*", layout.Rows);
        Assert.Equal(new GridCell(0, 1), layout.ActionCell);
        Assert.Equal(Inset.Of(24, 0), layout.ActionPadding);
        Assert.Equal(Alignment.Center, layout.ActionVerticalOptions);
        Assert.Equal(420, layout.ActionMaximumWidth);
    }
}
