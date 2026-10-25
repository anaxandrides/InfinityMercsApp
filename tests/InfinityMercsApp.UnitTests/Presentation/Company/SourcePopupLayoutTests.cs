using InfinityMercsApp.Presentation.Company;
using InfinityMercsApp.Presentation.Layouts;

namespace InfinityMercsApp.UnitTests.Presentation.Company;

public sealed class SourcePopupLayoutTests
{
    [Fact]
    public void For_Compact_StacksSmallerCardsAcrossTheScreen()
    {
        var layout = SourcePopupLayout.For(AdaptiveLayoutMode.Compact);

        Assert.Equal(Alignment.Fill, layout.ModalHorizontalOptions);
        Assert.Equal("*", layout.CardsColumns);
        Assert.Equal("Auto,Auto", layout.CardsRows);
        Assert.Equal(new GridCell(1, 0), layout.SecondCardCell);
        Assert.Equal(96, layout.IconHeight);
        Assert.Equal(150, layout.CardMinimumHeight);
    }

    [Theory]
    [InlineData(AdaptiveLayoutMode.Medium, 700)]
    [InlineData(AdaptiveLayoutMode.Expanded, 820)]
    [InlineData(AdaptiveLayoutMode.Wide, 820)]
    public void For_WiderModes_CentresACappedCardWithChoicesSideBySide(AdaptiveLayoutMode mode, double maximumWidth)
    {
        var layout = SourcePopupLayout.For(mode);

        Assert.Equal(Alignment.Center, layout.ModalHorizontalOptions);
        Assert.Equal(maximumWidth, layout.ModalMaximumWidth);
        Assert.Equal("*,*", layout.CardsColumns);
        Assert.Equal(new GridCell(0, 1), layout.SecondCardCell);
        Assert.Equal(140, layout.IconHeight);
        Assert.Equal(240, layout.CardMinimumHeight);
    }
}
