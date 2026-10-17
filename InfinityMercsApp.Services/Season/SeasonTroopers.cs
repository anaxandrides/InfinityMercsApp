using InfinityMercsApp.Domain.Captain;
using InfinityMercsApp.Domain.Models.Army.Stats;
using InfinityMercsApp.Domain.Models.Season;
using InfinityMercsApp.Domain.Models.Season.Gear;
using InfinityMercsApp.Domain.Models.Stores;
using InfinityMercsApp.Domain.Season.Armor;
using InfinityMercsApp.Domain.Season.Inventory;
using InfinityMercsApp.Domain.Season.Troopers;
using InfinityMercsApp.Services.CompanySelection.Models.Saved;

namespace InfinityMercsApp.Services.Season;

/// <summary>Saved company troopers seen through the season so far.</summary>
public static class SeasonTroopers
{
    /// <summary>
    /// The trooper's season progress. Results saved before entries were recorded match the name the deployment
    /// screens used: the custom name, else the unit's.
    /// </summary>
    public static TrooperProgress ProgressOf(SavedCompanyEntry entry, SeasonFile? season) =>
        TrooperProgress.For(
            entry.EntryIndex,
            NameOf(entry),
            entry.ExperiencePoints,
            entry.Perks.Select(perk => perk.Id).ToList(),
            UnitTypeCodes.IsTag(entry.UnitTypeCode),
            season);

    /// <summary>The armour the trooper wears this season, as its market's armour table lists it.</summary>
    public static StoreTroopType? ArmorOf(SavedCompanyEntry entry, SeasonFile? season, InventoryCatalog catalog) =>
        season?.UnitGear.GetValueOrDefault(entry.EntryIndex) is { } gear &&
        UnitLoadout.From(gear)[GearSlot.Armor] is { } armorName
            ? catalog.Find(armorName)?.Armor
            : null;

    /// <summary>
    /// The trooper's saved line in the armour it wears this season. A lieutenant's line holds the captain's bonuses,
    /// which stay on top of any value the armour replaces.
    /// </summary>
    public static WornArmor InArmor(
        SavedCompanyEntry entry,
        TrooperProfile profile,
        SeasonFile? season,
        InventoryCatalog catalog,
        SavedImprovedCaptainStats? captain) =>
        ArmorFitting.Wear(
            profile,
            entry.UnitTypeCode,
            ArmorOf(entry, season, catalog),
            CaptainBonusesOf(entry, captain));

    /// <summary>The stat bonuses bought for the captain, which only the lieutenant's saved line holds.</summary>
    public static CaptainStatBonuses CaptainBonusesOf(SavedCompanyEntry entry, SavedImprovedCaptainStats? captain) =>
        entry.IsLieutenant && captain is { IsEnabled: true } ? captain.Bonuses : CaptainStatBonuses.None;

    /// <summary>The name the season screens show: the custom name, else the unit's.</summary>
    public static string NameOf(SavedCompanyEntry entry)
    {
        var baseName = string.IsNullOrWhiteSpace(entry.BaseUnitName) ? entry.Name : entry.BaseUnitName;
        return string.IsNullOrWhiteSpace(entry.CustomName) ? baseName : entry.CustomName.Trim();
    }
}
