namespace InfinityMercsApp.UnitTests.Season;

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
