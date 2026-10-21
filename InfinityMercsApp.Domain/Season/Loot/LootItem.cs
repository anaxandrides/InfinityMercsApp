using InfinityMercsApp.Domain.Models.Season;
using InfinityMercsApp.Domain.Models.Season.Gear;
using InfinityMercsApp.Domain.Season.Inventory;

namespace InfinityMercsApp.Domain.Season.Loot;

/// <summary>
/// What an opened loot box gave: a weapon (or an Upgrade, or an Armed Turret and its weapon) and its buffs. It goes
/// in the slot the sheet gives it, takes up the SWC of its weapon, and counts its tier's renown while equipped.
/// </summary>
public sealed record LootItem(
    LootBoxTierInfo Tier,
    LootWeaponRow Weapon,
    LootWeaponRow? TurretWeapon,
    IReadOnlyList<LootBuff> Buffs,
    int GainedAfterRound)
{
    /// <summary>Where loot comes from, as the inventory lists it.</summary>
    public const string StoreName = "Loot Box";

    /// <summary>"Armed Turret (Spitfire)" for a turret, otherwise the weapon.</summary>
    public string BaseName => TurretWeapon is { } turret ? $"{Weapon.Name} ({turret.Name})" : Weapon.Name;

    /// <summary>
    /// "Spitfire [Shock, Repeating ×2]"; without buffs "Spitfire [Loot]", so it never takes a market item's name.
    /// </summary>
    public string Name => $"{BaseName} [{(Buffs.Count == 0 ? "Loot" : string.Join(", ", BuffNames()))}]";

    public GearSlot Slot => Weapon.Slot;

    public decimal Swc => TurretWeapon?.Swc ?? Weapon.Swc;

    /// <summary>The row whose profile the item fires with: the turret's weapon, or the item itself.</summary>
    public LootWeaponRow Profile => TurretWeapon ?? Weapon;

    /// <summary>The item of a fully opened box; null for one still being opened.</summary>
    public static LootItem? From(SeasonLootBox box) => box.IsOpened ? SoFar(box) : null;

    /// <summary>The item a box has given so far, opened or not; null until its weapon is rolled.</summary>
    public static LootItem? SoFar(SeasonLootBox box)
    {
        if (box.Weapon is not { } weapon)
        {
            return null;
        }

        var turret = box.TurretWeapon is { } turretRoll ? LootWeaponChart.For(turretRoll) : null;
        var buffs = box.Buffs.Select(buff => LootBuffChart.For(buff.Result)).Where(buff => !buff.IsExtra).ToList();
        return new LootItem(LootBoxTiers.Of(box.Tier), LootWeaponChart.For(weapon.Result), turret, buffs, box.GainedAfterRound);
    }

    /// <summary>The item as the inventory catalog knows it: its renown stands in for a price, which renown counts.</summary>
    public CatalogItem ToCatalogItem() => new(Name, Slot.Key(), StoreName, Tier.Renown, Swc);

    /// <summary>The weapon's modes with its buffs applied.</summary>
    public IReadOnlyList<LootWeaponMode> Modes() => LootProfile.Build(this);

    private IEnumerable<string> BuffNames() =>
        Buffs.GroupBy(buff => buff.Roll)
            .Select(group => group.Count() > 1 ? $"{group.First().Name} ×{group.Count()}" : group.First().Name);
}
