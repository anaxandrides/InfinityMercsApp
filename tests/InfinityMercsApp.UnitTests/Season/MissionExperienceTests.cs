using InfinityMercsApp.Domain.Season.Experience;

namespace InfinityMercsApp.UnitTests.Season;

public sealed class MissionExperienceTests
{
    [Fact]
    public void Lines_DeployedTrooper_EarnsTheMissionNumber()
    {
        var unit = new ExperienceUnitResult { DeploymentXp = 3 };

        Assert.Equal([new XpLine("Deployed (Mission 3)", 3)], MissionExperience.Lines(unit));
        Assert.Equal(3, unit.TotalXp);
    }

    [Fact]
    public void Lines_EverySource_ListedInOrderAndAddedToTotal()
    {
        var unit = new ExperienceUnitResult
        {
            DeploymentXp = 2,
            IsEliteDeployment = true,
            IsConsciousAtEnd = true,
            GainedInjury = true,
            IsMvp = true
        };
        unit.XpData.Assist[0] = unit.XpData.Assist[1] = true;
        unit.XpData.InflictState[2] = true;
        unit.XpData.AttemptButton = unit.XpData.SucceedButton = true;
        unit.XpData.ScanEnemy = unit.XpData.ScanEnemyFo = unit.XpData.TagAndBag = true;
        unit.XpData.DowntimeBonusXp = 1;

        Assert.Equal(
            [
                new XpLine("Deployed (Mission 2)", 2),
                new XpLine("Elite Deployment", 3),
                new XpLine("Survived", 2),
                new XpLine("Injury Sustained", 2),
                new XpLine("Assisted ×2", 2),
                new XpLine("Inflicted State", 1),
                new XpLine("Attempted Objective", 1),
                new XpLine("Completed Objective", 1),
                new XpLine("Scan Enemy", 1),
                new XpLine("Scan Enemy (FO)", 1),
                new XpLine("Tag and Bag", 2),
                new XpLine("Downtime Bonus", 1)
            ],
            MissionExperience.Lines(unit));
        Assert.Equal(19 + MissionExperience.MvpXp, unit.TotalXp);
        Assert.Equal(10, unit.XpData.TotalXp);
    }
}
