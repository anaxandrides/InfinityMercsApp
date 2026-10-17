using InfinityMercsApp.Domain.Season.Rolls;

namespace InfinityMercsApp.UnitTests.Season;

public sealed class RollRangeTests
{
    [Fact]
    public void ARangeOrASingleRollIsRead()
    {
        Assert.Equal(new RollRange(1, 2, "+1 ARM"), RollRange.Parse("1-2", " +1 ARM"));
        Assert.Equal(new RollRange(15, 15, "MOV 8-4"), RollRange.Parse("15", "MOV 8-4"));
    }

    [Fact]
    public void ARangeThatIsNotNumbersIsNotRead() => Assert.Null(RollRange.Parse("high", "HMG"));
}
