using InfinityMercsApp.Domain.Models.Army.Stats;
using InfinityMercsApp.Domain.Season.Game;
using InfinityMercsApp.ViewModels.Season.Game.Items;
using InfinityMercsApp.ViewModels.Season.Items;

namespace InfinityMercsApp.UnitTests.ViewModels.Season.Game.Items;

public sealed class GameUnitItemTests
{
    private static readonly TransmutedForm BattleRavaged = new(
        "Battle-Ravaged",
        Threshold: 1,
        [new(Stat.Arm, "2"), new(Stat.Vita, "2")],
        ["Courage"],
        []);

    private static GameUnitItem Digger() => new(
        new DeploymentUnitItem { Name = "Digger", StartingVitality = 2, UnitArm = "3", Skills = "Transmutation (1)", TransmutesTo = BattleRavaged },
        hasTacticalToken: false,
        []);

    [Fact]
    public void TakeWoundCommand_ReachesTheThreshold_ShowsTheSecondProfile()
    {
        var digger = Digger();

        digger.TakeWoundCommand.Execute(null);

        Assert.True(digger.IsTransmuted);
        Assert.Equal("BATTLE-RAVAGED", digger.FormName);
        Assert.Equal(BattleRavaged.Statline, digger.Statline);
        Assert.Equal(["Courage"], digger.SkillItems);
    }

    [Fact]
    public void HealWoundCommand_BelowTheThreshold_ShowsTheFirstProfileAgain()
    {
        var digger = Digger();
        digger.TakeWoundCommand.Execute(null);

        digger.HealWoundCommand.Execute(null);

        Assert.False(digger.IsTransmuted);
        Assert.Contains(new StatValue(Stat.Arm, "3"), digger.Statline);
    }

    [Theory]
    [InlineData(true, true, true)]
    [InlineData(true, false, false)]
    [InlineData(false, true, false)]
    public void ShowsCoreFireteamTacticalNote_TokenAndPlace_ShowsWhenItHasBoth(bool hasToken, bool inCoreFireteam, bool expected)
    {
        var row = new GameUnitItem(new DeploymentUnitItem { Name = "Kamau", IsInCoreFireteam = inCoreFireteam }, hasToken, []);

        Assert.Equal(expected, row.ShowsCoreFireteamTacticalNote);
    }
}
