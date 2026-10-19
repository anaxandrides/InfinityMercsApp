using InfinityMercsApp.Domain.Models.Season;
using InfinityMercsApp.Domain.Season.Loot;
using InfinityMercsApp.Domain.Season.Rolls;
using InfinityMercsApp.Services.Season.Loot;
using NSubstitute;

namespace InfinityMercsApp.UnitTests.Services.Season.Loot;

public sealed class LootBoxOpenerTests
{
    private readonly IDice _dice = Substitute.For<IDice>();

    private LootBoxOpener Opener(int first, params IReadOnlyList<int> rest)
    {
        _dice.RollD6().Returns(first, [.. rest]);
        return new LootBoxOpener(_dice);
    }

    private static SeasonLootBox Box(int? weapon = null, params IReadOnlyList<int> buffs) => new()
    {
        Tier = LootBoxTier.BlackMarketCache,
        DiceLeft = 6,
        Weapon = weapon is { } roll ? new SeasonLootRoll { Dice = 1, Result = roll } : null,
        Buffs = [.. buffs.Select(buff => new SeasonLootRoll { Dice = (buff + 5) / 6, Result = buff })]
    };

    private static List<int> Buffs(SeasonLootBox box) => box.Buffs.Select(buff => buff.Result).ToList();

    [Fact]
    public void New_Tier_HoldsItsDice() => Assert.Equal(12, LootBoxOpener.New(LootBoxTier.ArtifactVault, 3).DiceLeft);

    // Three dice: two count as 6s and the last rolls a 3.
    [Fact]
    public void Roll_WeaponWithThreeDice_SpendsThemOnRoll15()
    {
        var box = Box();

        Opener(3).Roll(box, 3);

        Assert.Equal((15, 3), (box.Weapon?.Result, box.DiceLeft));
        Assert.False(LootBoxOpener.RollsWeapon(box));
    }

    [Fact]
    public void Roll_Weapon24_OwesAFreeRollThatSpendsNoDice()
    {
        var box = Box();
        var opener = Opener(6, 3);

        opener.Roll(box, 4);
        Assert.Equal((null, 1, 2), (box.Weapon?.Result, box.FreeRolls, box.DiceLeft));

        opener.Roll(box, 4);
        Assert.Equal((21, 0, 2), (box.Weapon?.Result, box.FreeRolls, box.DiceLeft));
    }

    // 4d6 = 4 is Mines, which has no range bands, so the turret rolls again: 21, a Spitfire.
    [Fact]
    public void Roll_ArmedTurret_RollsItsWeaponUntilOneHasRangeBands()
    {
        var box = Box();

        Opener(6, 1, 1, 1, 1, 6, 5, 5, 5).Roll(box, 3);

        Assert.Equal((LootWeaponChart.ArmedTurretRoll, 21), (box.Weapon?.Result, box.TurretWeapon));
    }

    [Fact]
    public void Roll_DuplicateBuff_IsRerolledWithTheSameDice()
    {
        var box = Box(7, 1);

        Opener(1, 6).Roll(box, 1);

        Assert.Equal([1, 6], Buffs(box));
        Assert.Equal(5, box.DiceLeft);
    }

    // Continuous (18) can't go on Upgrade Spotlight, which is non-lethal; Targeting (13) comes instead.
    [Fact]
    public void Roll_ContinuousOnANonLethalUpgrade_IsRerolled()
    {
        var box = Box(16);

        Opener(6, 1).Roll(box, 3);

        Assert.Equal([13], Buffs(box));
    }

    // Experimental is rerolled with its 2 dice: Experimental again is illegal beside Plasma, then Armor Piercing.
    [Fact]
    public void Roll_PlasmaAfterExperimental_KeepsPlasmaAndRerollsExperimental()
    {
        var box = Box(7, LootBuffChart.ExperimentalRoll);

        Opener(3, 1, 2).Roll(box, 4);

        Assert.Equal([LootBuffChart.PlasmaRoll, 8], Buffs(box));
        Assert.Equal(2, box.Buffs[1].Dice);
    }

    [Fact]
    public void Roll_NoBuffLeftForTheDice_IsRefused()
    {
        var box = Box(7, 1, 2, 3, 4, 5, 6);

        Assert.False(LootBoxOpener.CanRoll(box, 1));
        Assert.Equal(string.Empty, Opener(1).Roll(box, 1));
        Assert.Equal(6, box.DiceLeft);
    }

    [Fact]
    public void Roll_MoreDiceThanLeft_IsRefused()
    {
        var box = Box(7);
        box.DiceLeft = 2;

        Assert.False(LootBoxOpener.CanRoll(box, 3));
        Assert.True(LootBoxOpener.CanRoll(box, 2));
    }

    [Fact]
    public void RerollLastBuff_IllegalBuff_ComesBackAsAnotherForFree()
    {
        var box = Box(7, 1);

        Opener(1, 2).RerollLastBuff(box);

        Assert.Equal([2], Buffs(box));
        Assert.Equal(6, box.DiceLeft);
    }

    [Fact]
    public void Take_WeaponRolled_OpensTheBox()
    {
        var box = Box(7);

        LootBoxOpener.Take(box);

        Assert.True(box.IsOpened);
        Assert.Equal(0, LootBoxOpener.MaxDice(box));
    }

    [Fact]
    public void Take_NoWeaponYet_LeavesTheBoxShut()
    {
        var box = Box();

        LootBoxOpener.Take(box);

        Assert.False(box.IsOpened);
    }
}
