using InfinityMercsApp.Domain.Models.Season;
using InfinityMercsApp.Domain.Models.Season.Gear;
using InfinityMercsApp.Domain.Season.Loot;

namespace InfinityMercsApp.UnitTests.Season.Loot;

public sealed class LootItemTests
{
    private static LootItem Item(int weapon, params IReadOnlyList<int> buffs) =>
        new(LootBoxTiers.Of(LootBoxTier.BlackMarketCache), LootWeaponChart.For(weapon), null, buffs.Select(LootBuffChart.For).ToList(), 0);

    private static LootWeaponMode Mode(LootItem item) => Assert.Single(item.Modes());

    [Fact]
    public void Name_NoBuffs_IsMarkedAsLoot() => Assert.Equal("Rifle [Loot]", Item(7).Name);

    [Fact]
    public void Name_RepeatedBuff_IsCounted() => Assert.Equal("Rifle [Repeating ×2, Shock]", Item(7, 11, 1, 11).Name);

    [Fact]
    public void From_ArmedTurret_TakesItsWeaponsSwcAndTheAugmentSlot()
    {
        var box = new SeasonLootBox
        {
            Tier = LootBoxTier.FieldPack,
            IsOpened = true,
            Weapon = new SeasonLootRoll { Dice = 3, Result = LootWeaponChart.ArmedTurretRoll },
            TurretWeapon = 21
        };

        var item = LootItem.From(box);

        Assert.NotNull(item);
        Assert.Equal(("Armed Turret (Spitfire) [Loot]", GearSlot.Augments, 1.5m, 10), (item.Name, item.Slot, item.Swc, item.Tier.Renown));
        Assert.Equal("4", Mode(item).Weapon.Burst);
    }

    [Fact]
    public void From_BoxStillBeingOpened_IsNull() =>
        Assert.Null(LootItem.From(new SeasonLootBox { Weapon = new SeasonLootRoll { Dice = 1, Result = 2 } }));

    [Fact]
    public void Modes_RepeatingShockAndTeseum_AddBurstAndJoinAmmo()
    {
        var mode = Mode(Item(21, 11, 1, 19));

        Assert.Equal(("5", "6", "Shock+T2"), (mode.Weapon.Burst, mode.Weapon.Damage, mode.Ammo));
        Assert.Contains(LootBuffChart.AntiMaterial, mode.Weapon.Properties);
        Assert.NotNull(mode.Weapon.Distance);
    }

    // High Caliber (3) lowers PS by 1; K1 (14) sets it to 7.
    public static TheoryData<List<int>, string> PsBuffs => new()
    {
        { [3, 14], "7" },
        { [14, 3], "6" }
    };

    // Experimental (7) then Armor Piercing (8), Plasma (21), Paralyzing (16) with Electromagnetic (17), Explosive (15).
    public static TheoryData<List<int>, string, string> SavingBuffs => new()
    {
        { [7, 8], "BTS/2", "1" },
        { [21], "ARM+BTS", "1" },
        { [16, 17], "PH-6 + BTS/2", "3" },
        { [15], "ARM", "3" }
    };

    [Theory]
    [MemberData(nameof(PsBuffs))]
    public void Modes_PsBuffs_ApplyInTheOrderRolled(List<int> buffs, string damage) =>
        Assert.Equal(damage, Mode(Item(7, buffs)).Weapon.Damage);

    [Theory]
    [MemberData(nameof(SavingBuffs))]
    public void Modes_SavingBuffs_ChangeTheSave(List<int> buffs, string saving, string savingNum)
    {
        var weapon = Mode(Item(7, buffs)).Weapon;

        Assert.Equal((saving, savingNum), (weapon.Saving, weapon.SavingNum));
    }

    [Fact]
    public void Modes_Multi_GivesApShockAndAntiMaterialModes()
    {
        var modes = Item(7, LootBuffChart.MultiRoll).Modes();

        Assert.Equal(["AP Mode", "Shock Mode", "Anti-Material Mode"], modes.Select(mode => mode.Weapon.Mode));
        Assert.Equal(["AP", "Shock", "DA"], modes.Select(mode => mode.Ammo));
        Assert.Equal(["3", "3", "1"], modes.Select(mode => mode.Weapon.Burst));
    }

    [Fact]
    public void Modes_Upgrade_FiresItsOwnAmmoWithoutBurst()
    {
        var mode = Mode(Item(12));

        Assert.Equal(("DA", "-"), (mode.Ammo, mode.Weapon.Burst));
    }
}
