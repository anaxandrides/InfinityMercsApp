using InfinityMercsApp.ViewModels;
using InfinityMercsApp.ViewModels.Base;

namespace InfinityMercsApp.UnitTests.Layouts;

public sealed class RecordListLayoutTests
{
    [Fact]
    public void CompactFillsTheScreenInOneColumn() =>
        Assert.Equal(new RecordListLayout(double.PositiveInfinity, LayoutOptions.Fill, 1), RecordListLayout.For(AdaptiveLayoutMode.Compact));

    [Theory]
    [InlineData(AdaptiveLayoutMode.Medium)]
    [InlineData(AdaptiveLayoutMode.Expanded)]
    public void MediumAndExpandedCentreAReadableColumn(AdaptiveLayoutMode mode) =>
        Assert.Equal(new RecordListLayout(640, LayoutOptions.Center, 1), RecordListLayout.For(mode));

    [Fact]
    public void WideUsesTwoColumns() =>
        Assert.Equal(new RecordListLayout(1000, LayoutOptions.Center, 2), RecordListLayout.For(AdaptiveLayoutMode.Wide));
}
