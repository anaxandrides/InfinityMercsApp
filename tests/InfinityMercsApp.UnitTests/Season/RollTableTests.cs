using InfinityMercsApp.Domain.Models.Perks;
using InfinityMercsApp.Domain.Season;

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
    public void HighRollsFindNoTemporaryStore(int roll, string? store) => Assert.Equal(store, SeasonRollTables.TemporaryStores.Lookup(roll));

    [Fact]
    public void TheTablesHaveNoGapsOrOverlaps()
    {
        foreach (var table in new List<RollTable> { SeasonRollTables.Pistols, SeasonRollTables.TemporaryStores })
        {
            Assert.Equal(1, table.Ranges[0].Min);
            Assert.All(table.Ranges.Zip(table.Ranges.Skip(1)), pair => Assert.Equal(pair.First.Max + 1, pair.Second.Min));
        }
    }
}

public sealed class ExperienceRankTests
{
    [Theory]
    [InlineData(-5, "Unranked", 0)]
    [InlineData(4, "Unranked", 0)]
    [InlineData(5, "Newbie", 1)]
    [InlineData(29, "Hired Gun", 2)]
    [InlineData(140, "Legend", 7)]
    [InlineData(999, "Legend", 7)]
    public void RanksFollowTheExperienceThresholds(int xp, string name, int level)
    {
        Assert.Equal(name, CompanyUnitExperienceRanks.GetRankName(xp));
        Assert.Equal(level, CompanyUnitExperienceRanks.GetRankLevel(xp));
    }
}
