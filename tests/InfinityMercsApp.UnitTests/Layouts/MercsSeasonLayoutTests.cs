using InfinityMercsApp.ViewModels;
using InfinityMercsApp.ViewModels.Base;

namespace InfinityMercsApp.UnitTests.Layouts;

public sealed class MercsSeasonLayoutTests
{
    [Fact]
    public void CompactAnchorsFullWidthActionsBelowTheBrand()
    {
        var layout = MercsSeasonLayout.For(AdaptiveLayoutMode.Compact);

        Assert.Equal("*", layout.Columns);
        Assert.Equal("*,Auto", layout.Rows);
        Assert.Equal(new GridCell(1, 0), layout.ActionCell);
        Assert.Equal(new Thickness(24, 0, 24, 40), layout.ActionPadding);
        Assert.Equal(LayoutOptions.End, layout.ActionVerticalOptions);
        Assert.Equal(LayoutOptions.Fill, layout.ActionHorizontalOptions);
        Assert.Equal(double.PositiveInfinity, layout.ActionMaximumWidth);
    }

    [Fact]
    public void MediumCentresTheActionsAtAReadableWidth()
    {
        var layout = MercsSeasonLayout.For(AdaptiveLayoutMode.Medium);

        Assert.Equal(new GridCell(1, 0), layout.ActionCell);
        Assert.Equal(LayoutOptions.Center, layout.ActionHorizontalOptions);
        Assert.Equal(520, layout.ActionMaximumWidth);
    }

    [Theory]
    [InlineData(AdaptiveLayoutMode.Expanded)]
    [InlineData(AdaptiveLayoutMode.Wide)]
    public void WiderScreensPutTheActionsBesideTheBrand(AdaptiveLayoutMode mode)
    {
        var layout = MercsSeasonLayout.For(mode);

        Assert.Equal("*,*", layout.Columns);
        Assert.Equal("*", layout.Rows);
        Assert.Equal(new GridCell(0, 1), layout.ActionCell);
        Assert.Equal(new Thickness(24, 0), layout.ActionPadding);
        Assert.Equal(LayoutOptions.Center, layout.ActionVerticalOptions);
        Assert.Equal(420, layout.ActionMaximumWidth);
    }
}
