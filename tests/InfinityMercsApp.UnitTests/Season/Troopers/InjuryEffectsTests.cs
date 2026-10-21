using InfinityMercsApp.Domain.Season.Injuries;
using InfinityMercsApp.Domain.Season.Rolls;
using InfinityMercsApp.Domain.Season.Troopers;
using NSubstitute;

namespace InfinityMercsApp.UnitTests.Season.Troopers;

public sealed class InjuryEffectsTests
{
    [Fact]
    public void Change_EachInjury_ChangesWhatTheTableSays()
    {
        Assert.Equal(["Frenzy"], Injury.BattleFury.Change().CharacteristicsGained);
        Assert.Equal((-1, -1), (Injury.PuncturedLung.Change().Ph, Injury.PuncturedLung.Change().SecondMoveInches));
        Assert.Equal(-1, Injury.Arms.Change().Bs);
        Assert.Equal((-1, -3), (Injury.BrainInjury.Change().Wip, Injury.BrainInjury.Change().Cc));
        Assert.Equal(-2, Injury.LegsInjury.Change().FirstMoveInches);
        Assert.Equal(-2, Injury.BodyInjury.Change().Ph);
        Assert.Equal(-1, Injury.EyesInjury.Change().Bs);
        Assert.Single(Injury.EyesInjury.Change().Notes);
        Assert.Equal(-2, Injury.ShellShocked.Change().Wip);
    }

    [Fact]
    public void TryParse_LabelOrName_ReadsTheInjury()
    {
        Assert.True(InjuryEffects.TryParse("Shell Shocked", out var injury));
        Assert.Equal(Injury.ShellShocked, injury);
        Assert.True(InjuryEffects.TryParse("brainInjury", out _));
        Assert.False(InjuryEffects.TryParse("Stubbed Toe", out _));
        Assert.Equal("-2 WIP", Injury.ShellShocked.EffectText());
    }

    [Fact]
    public void RollNew_InjuriesHeld_GivesOneNotHeld()
    {
        var dice = Substitute.For<IDice>();
        dice.RollD20().Returns(3, 5, 9);

        Assert.Equal(Injury.Arms, InjuryRules.RollNew(dice, [Injury.BattleFury]));
    }

    [Fact]
    public void RollNew_EveryInjuryHeld_ReturnsNull()
    {
        Assert.Null(InjuryRules.RollNew(Substitute.For<IDice>(), Enum.GetValues<Injury>()));
    }

    [Theory]
    [InlineData(false, false, true, true)]
    [InlineData(true, false, true, false)]
    [InlineData(true, true, true, true)]
    [InlineData(false, false, false, false)]
    public void Recovers_RestingOrCaptain_Recover(bool deployed, bool captain, bool injuredBefore, bool recovers)
    {
        Assert.Equal(recovers, InjuryRules.Recovers(deployed, captain, injuredBefore));
    }

    [Fact]
    public void EntryFor_Injury_FindsItsRow()
    {
        Assert.Equal("13-14 | Legs Injury", InjuryRules.EntryFor(Injury.LegsInjury).ToString());
    }
}
