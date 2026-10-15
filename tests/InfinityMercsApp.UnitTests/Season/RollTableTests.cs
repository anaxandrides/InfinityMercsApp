using InfinityMercsApp.Domain.Season.Rolls;

namespace InfinityMercsApp.UnitTests.Season;

public sealed class RollTableTests
{
    [Theory]
    [InlineData(1, "MULTI Pistol")]
    [InlineData(4, "MULTI Pistol")]
    [InlineData(5, "AP Heavy Pistol")]
    [InlineData(20, "Viral Pistol")]
    public void PistolsCoverEveryRoll(int roll, string pistol) => Assert.Equal(pistol, SeasonRollTables.Pistols.Lookup(roll));

    [Theory]
    [InlineData(2, "Number One")]
    [InlineData(18, "Exrah Surplus")]
    [InlineData(19, null)]
    [InlineData(20, null)]
    public void HighRollsFindNoTemporaryStore(int roll, string? store) => Assert.Equal(store, SeasonRollTables.WarMarkets.Lookup(roll));

    [Fact]
    public void TheTablesHaveNoGapsOrOverlaps()
    {
        foreach (var table in new List<RollTable> { SeasonRollTables.Pistols, SeasonRollTables.WarMarkets })
        {
            Assert.Equal(1, table.Ranges[0].Min);
            Assert.All(table.Ranges.Zip(table.Ranges.Skip(1)), pair => Assert.Equal(pair.First.Max + 1, pair.Second.Min));
        }
    }
}
