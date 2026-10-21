using InfinityMercsApp.Domain.Season.Rolls;

namespace InfinityMercsApp.UnitTests.Season;

public sealed class RollRangeTests
{
    [Fact]
    public void Parse_RangeOrSingleRoll_ReadsIt()
    {
        Assert.Equal(new RollRange(1, 2, "+1 ARM"), RollRange.Parse("1-2", " +1 ARM"));
        Assert.Equal(new RollRange(15, 15, "MOV 8-4"), RollRange.Parse("15", "MOV 8-4"));
    }

    [Fact]
    public void Parse_NotNumbers_ReturnsNull() => Assert.Null(RollRange.Parse("high", "HMG"));
}
