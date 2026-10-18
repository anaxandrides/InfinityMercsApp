using InfinityMercsApp.Domain.Season.Game;

namespace InfinityMercsApp.UnitTests.Season;

public sealed class EliteDeploymentTests
{
    [Theory]
    [InlineData(false, false, false, false)]
    [InlineData(true, false, false, true)]
    [InlineData(false, false, true, true)]
    [InlineData(false, true, true, false)]
    [InlineData(true, true, false, true)]
    public void HasTacticalAwareness_SkillCaptainAndDeployment_OwnSkillOrEliteNonCaptain(
        bool hasTheSkill, bool isCaptain, bool isElite, bool expected) =>
        Assert.Equal(expected, EliteDeployment.HasTacticalAwareness(hasTheSkill, isCaptain, isElite));
}
