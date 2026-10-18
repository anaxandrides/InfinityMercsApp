using InfinityMercsApp.Domain.Captain;
using InfinityMercsApp.Domain.Company;
using InfinityMercsApp.Domain.Models.Army.Filters;
using InfinityMercsApp.Domain.Models.Army.Stats;
using InfinityMercsApp.Domain.Models.Season;
using InfinityMercsApp.Domain.Models.Season.Gear;
using InfinityMercsApp.Domain.Models.Stores;
using InfinityMercsApp.Domain.Season.Armor;
using InfinityMercsApp.Domain.Season.Inventory;
using InfinityMercsApp.Domain.Season.RecruitRolls;
using InfinityMercsApp.Domain.Season.Troopers;
using InfinityMercsApp.Services.CompanySelection.Models.Saved;
using TagGen = InfinityMercsApp.Infrastructure.Providers.CompanyFactions.TagCompanyFactionGenerator;

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
            entry.AspectOf ?? entry.EntryIndex,
            NameOf(entry),
            entry.ExperiencePoints,
            entry.Perks.Select(perk => perk.Id).ToList(),
            UnitTypeCodes.IsTag(entry.UnitTypeCode),
            entry.IsLoneWolf,
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

    /// <summary>
    /// The recruit table results on the trooper: its own Booty or Metachemistry rolls, then the result of the rolled
    /// armour it wears this season.
    /// </summary>
    public static IReadOnlyList<string> RollResultsOf(SavedCompanyEntry entry, SeasonFile? season)
    {
        var results = entry.RecruitRolls.Select(roll => roll.Result).ToList();
        if (season?.UnitGear.GetValueOrDefault(entry.EntryIndex) is { } gear &&
            UnitLoadout.From(gear)[GearSlot.Armor] is { } armorName &&
            RolledArmor.Parse(armorName) is { } rolled)
        {
            results.Add(rolled.Result);
        }

        return results;
    }

    /// <summary>
    /// The trooper controlling a peripheral from the Peripheral (Controller) augment; null when none does, or the
    /// one that did has retired.
    /// </summary>
    public static int? ControllerOf(SavedCompanyEntry peripheral, SeasonFile? season) =>
        peripheral.ParentEntryIndex is { } controller && season?.IsRetired(controller) != true ? controller : null;

    /// <summary>
    /// The faction a trooper's profiles come from: its source faction, except that TAG Company units (its own
    /// TAGs and the repurposed mining TAGs) keep the TAG Company, even when saved under another faction.
    /// </summary>
    public static int SourceFactionId(SavedCompanyEntry entry)
    {
        if (entry.SourceFactionId == TagGen.TagCompanyFactionId || entry.LogoSourceFactionId == TagGen.TagCompanyFactionId)
        {
            return TagGen.TagCompanyFactionId;
        }

        var baseName = entry.BaseUnitName ?? string.Empty;
        return baseName.Contains("Repurposed Mining Equipment", StringComparison.OrdinalIgnoreCase) ||
               baseName.Contains("Turtlemek", StringComparison.OrdinalIgnoreCase)
            ? TagGen.TagCompanyFactionId
            : entry.SourceFactionId;
    }

    /// <summary>
    /// Whether the entry is a Servant or Ancillary peripheral, which stays equipment of its trooper; any other
    /// peripheral is a trooper of its own.
    /// </summary>
    public static bool IsServantPeripheral(SavedCompanyEntry entry, ArmyFilterCatalog? filters) =>
        entry.IsPeripheralUnit &&
        PeripheralKinds.IsServant([
            .. SavedCodeNames.ForEntry(filters, entry, ProfileEntryKind.Skills),
            .. SavedCodeNames.ForEntry(filters, entry, ProfileEntryKind.Chars)
        ]);

    /// <summary>
    /// Whether the entry is a trooper that can deploy: not a Servant peripheral, and for a peripheral from the
    /// Peripheral (Controller) augment, one a trooper controls.
    /// </summary>
    public static bool IsDeployable(SavedCompanyEntry entry, SeasonFile? season, IReadOnlySet<int> servants) =>
        season?.IsRetired(entry.EntryIndex) != true &&
        (!entry.IsPeripheralUnit || (!servants.Contains(entry.EntryIndex) && (!entry.FromControllerAugment || ControllerOf(entry, season) is not null)));

    /// <summary>The troopers controlling a peripheral that is a trooper of its own, who gain FT Master (own peripheral).</summary>
    public static IReadOnlySet<int> ControllersOf(SavedCompanyFile company, SeasonFile? season, IReadOnlySet<int> servants) =>
        company.Entries
            .Where(entry => entry.IsPeripheralUnit && IsDeployable(entry, season, servants))
            .Select(entry => entry.FromControllerAugment ? ControllerOf(entry, season) : entry.ParentEntryIndex)
            .OfType<int>()
            .ToHashSet();

    /// <summary>The stat bonuses bought for the captain, which only the founding Captain's saved line holds.</summary>
    public static CaptainStatBonuses CaptainBonusesOf(SavedCompanyEntry entry, SavedImprovedCaptainStats? captain) =>
        entry.CarriesCaptainUpgrades && captain is { IsEnabled: true } ? captain.Bonuses : CaptainStatBonuses.None;

    /// <summary>The name the season screens show: the custom name, else the unit's.</summary>
    public static string NameOf(SavedCompanyEntry entry)
    {
        var baseName = string.IsNullOrWhiteSpace(entry.BaseUnitName) ? entry.Name : entry.BaseUnitName;
        return string.IsNullOrWhiteSpace(entry.CustomName) ? baseName : entry.CustomName.Trim();
    }
}
