using InfinityMercsApp.Presentation.Layouts;

namespace InfinityMercsApp.UnitTests.Presentation.Layouts;

public sealed class TextFitTests
{
    [Fact]
    public void SizeFor_RoomyRow_KeepsTheUsualSize() =>
        Assert.Equal(20, TextFit.SizeFor(20, 20, [new CellText(60, 30), new CellText(60, 40)], 7));

    // "MOV" needs 50 at 24 but its cell is 30 wide: every cell takes the size MOV fits at.
    [Fact]
    public void SizeFor_SeveralCells_TightestCellSetsTheSize() =>
        Assert.Equal(14, TextFit.SizeFor(24, 24, [new CellText(30, 50), new CellText(30, 20)], 7));

    [Fact]
    public void SizeFor_RoomGiven_GrowsBack() =>
        Assert.Equal(24, TextFit.SizeFor(24, 12, [new CellText(100, 25)], 7));

    [Fact]
    public void SizeFor_VeryTightCell_NeverGoesBelowTheSmallestSize() =>
        Assert.Equal(7, TextFit.SizeFor(24, 24, [new CellText(5, 50)], 7));

    // Before layout, labels have no width yet.
    [Fact]
    public void SizeFor_UnmeasuredCells_KeepsTheUsualSize() =>
        Assert.Equal(18, TextFit.SizeFor(18, 18, [new CellText(-1, 30), new CellText(0, 0)], 7));
}
