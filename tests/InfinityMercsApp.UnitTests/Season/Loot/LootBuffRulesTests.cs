using InfinityMercsApp.Domain.Season.Loot;

namespace InfinityMercsApp.UnitTests.Season.Loot;

public sealed class LootBuffRulesTests
{
    private static readonly LootWeaponRow Rifle = LootWeaponChart.For(7);
    private static readonly LootWeaponRow Spotlight = LootWeaponChart.For(16);

    private static LootBuff Buff(int roll) => LootBuffChart.For(roll);

    [Fact]
    public void IsLegal_Duplicate_IsNot() => Assert.False(LootBuffRules.IsLegal(Buff(1), [Buff(1)], Rifle));

    [Fact]
    public void IsLegal_RepeatingAgain_Stacks() =>
        Assert.True(LootBuffRules.IsLegal(Buff(LootBuffChart.RepeatingRoll), [Buff(LootBuffChart.RepeatingRoll)], Rifle));

    [Fact]
    public void IsLegal_ContinuousOnANonLethalUpgrade_IsNot() =>
        Assert.False(LootBuffRules.IsLegal(Buff(LootBuffChart.ContinuousRoll), [], Spotlight));

    [Fact]
    public void IsLegal_ContinuousAfterHealing_IsNot() =>
        Assert.False(LootBuffRules.IsLegal(Buff(LootBuffChart.ContinuousRoll), [Buff(4)], Rifle));

    [Fact]
    public void IsLegal_NonLethalAfterContinuous_IsNot() =>
        Assert.False(LootBuffRules.IsLegal(Buff(17), [Buff(LootBuffChart.ContinuousRoll)], Rifle));

    [Fact]
    public void IsLegal_HealingWithRepairing_Is() => Assert.True(LootBuffRules.IsLegal(Buff(5), [Buff(4)], Rifle));

    [Fact]
    public void IsLegal_ExperimentalAfterPlasma_IsNot() =>
        Assert.False(LootBuffRules.IsLegal(Buff(LootBuffChart.ExperimentalRoll), [Buff(LootBuffChart.PlasmaRoll)], Rifle));

    [Fact]
    public void IsLegal_ExtraRoll_AlwaysIs() =>
        Assert.True(LootBuffRules.IsLegal(Buff(LootBuffChart.ExtraRoll), [Buff(LootBuffChart.ExtraRoll)], Rifle));

    [Fact]
    public void HasLegal_EveryBuffOfTheBandHeld_IsFalse()
    {
        List<LootBuff> held = [.. Enumerable.Range(1, 6).Select(Buff)];

        Assert.False(LootBuffRules.HasLegal(1, held, Rifle));
        Assert.True(LootBuffRules.HasLegal(2, held, Rifle));
    }

    // With 4 dice only the 24 would be left, which never gives a buff.
    [Fact]
    public void HasLegal_OnlyTheExtraRollLeft_IsFalse()
    {
        List<LootBuff> held = [.. Enumerable.Range(19, 5).Select(Buff)];

        Assert.False(LootBuffRules.HasLegal(4, held, Rifle));
    }
}
