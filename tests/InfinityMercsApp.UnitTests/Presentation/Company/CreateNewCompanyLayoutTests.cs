using InfinityMercsApp.Presentation.Company;
using InfinityMercsApp.Presentation.Layouts;

namespace InfinityMercsApp.UnitTests.Presentation.Company;

public sealed class CreateNewCompanyLayoutTests
{
    [Theory]
    [InlineData(AdaptiveLayoutMode.Compact, 1, "*", "*,*,*,*,*")]
    [InlineData(AdaptiveLayoutMode.Medium, 2, "*,*", "*,*,*")]
    [InlineData(AdaptiveLayoutMode.Expanded, 3, "*,*,*", "*,*")]
    [InlineData(AdaptiveLayoutMode.Wide, 3, "*,*,*", "*,*")]
    public void For_EachMode_FitsFiveCardsIntoColumns(AdaptiveLayoutMode mode, int columns, string gridColumns, string gridRows)
    {
        var layout = CreateNewCompanyLayout.For(mode);

        Assert.Equal(columns, layout.Columns);
        Assert.Equal(gridColumns, layout.GridColumns);
        Assert.Equal(gridRows, layout.GridRowsFor(5));
    }

    [Fact]
    public void CellFor_FiveCards_FillsRowsLeftToRight()
    {
        var layout = CreateNewCompanyLayout.For(AdaptiveLayoutMode.Medium);

        Assert.Equal<GridCell>([new(0, 0), new(0, 1), new(1, 0), new(1, 1), new(2, 0)], Enumerable.Range(0, 5).Select(layout.CellFor));
    }

    [Fact]
    public void CompanyCardStyleFor_Compact_PutsTitleBesideTheIcon()
    {
        var card = CompanyCardStyle.For(AdaptiveLayoutMode.Compact);

        Assert.Equal("Auto,*", card.Columns);
        Assert.Equal(new GridCell(0, 1), card.TitleCell);
        Assert.Equal(56, card.IconSize);
        Assert.Equal(TextAlignment.Start, card.TitleTextAlignment);
        Assert.True(card.UsesCompactTitle);
        Assert.Equal(72, card.MinimumHeight);
    }

    [Fact]
    public void For_Expanded_StacksALargeIconAboveTheTitle()
    {
        var layout = CreateNewCompanyLayout.For(AdaptiveLayoutMode.Expanded);

        Assert.Equal(1100, layout.MaximumWidth);
        Assert.Equal(new GridCell(1, 0), layout.Card.TitleCell);
        Assert.Equal(120, layout.Card.IconSize);
        Assert.False(layout.Card.UsesCompactTitle);
        Assert.Equal(160, layout.Card.MinimumHeight);
    }
}
