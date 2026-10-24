using InfinityMercsApp.Presentation.Layouts;

namespace InfinityMercsApp.UnitTests.Presentation.Layouts;

public sealed class AdaptiveLayoutModesTests
{
    [Theory]
    [InlineData(400, AdaptiveLayoutMode.Compact)]
    [InlineData(600, AdaptiveLayoutMode.Medium)]
    [InlineData(899, AdaptiveLayoutMode.Medium)]
    [InlineData(900, AdaptiveLayoutMode.Expanded)]
    [InlineData(1200, AdaptiveLayoutMode.Wide)]
    public void For_Width_IsItsMode(double width, AdaptiveLayoutMode expected) => Assert.Equal(expected, AdaptiveLayoutModes.For(width));
}
