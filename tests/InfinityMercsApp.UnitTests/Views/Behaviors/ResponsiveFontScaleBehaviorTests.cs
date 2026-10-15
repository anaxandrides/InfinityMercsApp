using InfinityMercsApp.Views.Behaviors;

namespace InfinityMercsApp.UnitTests.Views.Behaviors;

public sealed class ResponsiveFontScaleBehaviorTests
{
    [Theory]
    [InlineData(400, 1.0)]
    [InlineData(500, 1.1)]
    [InlineData(699, 1.1)]
    [InlineData(700, 1.2)]
    [InlineData(1000, 1.35)]
    [InlineData(1600, 1.35)]
    public void FontsGrowWithTheWidth(double width, double scale)
    {
        Assert.Equal(scale, ResponsiveFontScaleBehavior.ScaleFor(width));
    }
}
