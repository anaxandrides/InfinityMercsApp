using InfinityMercsApp.Domain.Season.RecruitRolls;
using InfinityMercsApp.Domain.Season.Troopers;

namespace InfinityMercsApp.UnitTests.Season.RecruitRolls;

public sealed class RollEffectsTests
{
    [Fact]
    public void StatBonusesAndNamesSplitOnPlus()
    {
        var effects = RollEffects.Of("+1 ARM + Immunity (ARM)", "MI");

        Assert.Equal(1, effects.Change.Arm);
        Assert.Equal(["Immunity (ARM)"], effects.Named);
    }

    [Fact]
    public void TheOtherSideIsForAnyTroopTypeButTag()
    {
        Assert.Equal(2, RollEffects.Of("TAG: Immunity (AP) / Other Troop Types: + 2 ARM", "REM").Change.Arm);
        Assert.Equal(["Immunity (AP)"], RollEffects.Of("TAG: Immunity (AP) / Other Troop Types: + 2 ARM", "TAG").Named);
    }

    [Theory]
    [InlineData("MOV: 8-4")]
    [InlineData("MOV 8-4")]
    public void AMoveReplacesTheTroopers(string result) => Assert.Equal(new MoveInches(8, 4), RollEffects.Of(result, "LI").Change.Move);

    [Fact]
    public void AMoveAndASkillTogether()
    {
        var effects = RollEffects.Of("MOV: 6-4 + Super-Jump", "LI");

        Assert.Equal(new MoveInches(6, 4), effects.Change.Move);
        Assert.Equal(["Super-Jump"], effects.Named);
    }

    [Theory]
    [InlineData("+3 PH", 3, 0)]
    [InlineData("+6 BTS", 0, 6)]
    public void PhAndBtsBonuses(string result, int ph, int bts)
    {
        var change = RollEffects.Of(result, "LI").Change;

        Assert.Equal((ph, bts), (change.Ph, change.Bts));
    }
}
