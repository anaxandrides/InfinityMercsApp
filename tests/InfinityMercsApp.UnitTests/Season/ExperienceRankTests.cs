using InfinityMercsApp.Domain.Models.Perks;

namespace InfinityMercsApp.UnitTests.Season;

public sealed class ExperienceRankTests
{
    [Theory]
    [InlineData(-5, "Unranked", 0)]
    [InlineData(4, "Unranked", 0)]
    [InlineData(5, "Newbie", 1)]
    [InlineData(14, "Newbie", 1)]
    [InlineData(15, "Hired Gun", 2)]
    [InlineData(29, "Hired Gun", 2)]
    [InlineData(140, "Legend", 7)]
    [InlineData(999, "Legend", 7)]
    public void GetRankNameAndLevel_Experience_FollowTheThresholds(int xp, string name, int level)
    {
        Assert.Equal(name, CompanyUnitExperienceRanks.GetRankName(xp));
        Assert.Equal(level, CompanyUnitExperienceRanks.GetRankLevel(xp));
    }

    [Fact]
    public void FreeSkillsFor_RanksTwoThreeAndFive_GrantFreeSkills()
    {
        Assert.Empty(CompanyUnitExperienceRanks.FreeSkillsFor(14));
        Assert.Equal(["Courage", "Fireteam: Mercs"], CompanyUnitExperienceRanks.FreeSkillsFor(30));
        Assert.Equal(["Courage", "Fireteam: Mercs", "Counts as Merc"], CompanyUnitExperienceRanks.FreeSkillsFor(75));
    }
}
