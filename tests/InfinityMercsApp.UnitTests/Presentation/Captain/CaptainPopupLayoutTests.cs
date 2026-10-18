using InfinityMercsApp.Presentation.Captain;
using InfinityMercsApp.Presentation.Layouts;

namespace InfinityMercsApp.UnitTests.Presentation.Captain;

public sealed class CaptainPopupLayoutTests
{
    [Fact]
    public void For_Compact_StacksTheUpgradesUnderTheCaptain()
    {
        var layout = CaptainPopupLayout.For(AdaptiveLayoutMode.Compact);

        Assert.Equal(("*", "*,*"), (layout.Columns, layout.Rows));
        Assert.Equal(new GridCell(1, 0), layout.UpgradesCell);
        Assert.Equal("*", layout.ActionColumns);
    }

    [Theory]
    [InlineData(AdaptiveLayoutMode.Medium)]
    [InlineData(AdaptiveLayoutMode.Expanded)]
    [InlineData(AdaptiveLayoutMode.Wide)]
    public void For_MediumOrWider_PutsThePanesSideBySide(AdaptiveLayoutMode mode)
    {
        var layout = CaptainPopupLayout.For(mode);

        Assert.Equal(("*,*", "*"), (layout.Columns, layout.Rows));
        Assert.Equal(new GridCell(0, 1), layout.UpgradesCell);
        Assert.Equal("*,*", layout.ActionColumns);
    }
}
