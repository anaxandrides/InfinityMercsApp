using InfinityMercsApp.Presentation.Layouts;

namespace InfinityMercsApp.UnitTests.Presentation.Layouts;

public sealed class RecordListLayoutTests
{
    [Fact]
    public void For_Compact_FillsTheScreenInOneColumn() =>
        Assert.Equal(new RecordListLayout(double.PositiveInfinity, Alignment.Fill, 1), RecordListLayout.For(AdaptiveLayoutMode.Compact));

    [Theory]
    [InlineData(AdaptiveLayoutMode.Medium)]
    [InlineData(AdaptiveLayoutMode.Expanded)]
    public void For_MediumOrExpanded_CentresAReadableColumn(AdaptiveLayoutMode mode) =>
        Assert.Equal(new RecordListLayout(640, Alignment.Center, 1), RecordListLayout.For(mode));

    [Fact]
    public void For_Wide_UsesTwoColumns() =>
        Assert.Equal(new RecordListLayout(1000, Alignment.Center, 2), RecordListLayout.For(AdaptiveLayoutMode.Wide));
}
