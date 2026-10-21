using InfinityMercsApp.Domain.Models.Season;
using InfinityMercsApp.Domain.Season.Inventory;
using InfinityMercsApp.Domain.Season.Renown;
using InfinityMercsApp.Domain.Models.Company;

namespace InfinityMercsApp.Services.Season;

/// <summary>The renown of a saved company's troopers, and of the company, so far this season.</summary>
public static class SeasonRenown
{
    /// <summary>
    /// The trooper's renown: its saved cost with that of its Servant peripherals, the base cost of its season gear,
    /// its XP, and a Lone Wolf's 10 a rank. A Servant that came with the trooper costs nothing of its own; one recruited on its own adds its cost.
    /// </summary>
    public static TrooperRenown Of(
        SavedCompanyEntry entry,
        SavedCompanyFile company,
        SeasonFile? season,
        InventoryCatalog catalog,
        IReadOnlySet<int> servants)
    {
        var gear = CompanyRenown.GearCost(season?.UnitGear.GetValueOrDefault(entry.EntryIndex) ?? [], catalog);
        if (entry.AspectOf is not null)
        {
            // An Aspect adds its loadout; its cost, XP and ranks are the Lone Wolf's, counted once.
            return new(0, gear, 0);
        }

        var progress = SeasonTroopers.ProgressOf(entry, season);
        return new(
            entry.Cost + company.Entries
                .Where(servant => servant.ParentEntryIndex == entry.EntryIndex && servants.Contains(servant.EntryIndex))
                .Sum(servant => servant.Cost),
            gear,
            progress.TotalXp,
            progress.RankRenown);
    }

    /// <summary>
    /// The company's renown. A Servant peripheral is left out, its cost counting toward the trooper it belongs to; any other peripheral is a trooper of its own. Retired troopers count with the renown they kept
    /// when they left.
    /// </summary>
    public static int OfCompany(SavedCompanyFile company, SeasonFile? season, InventoryCatalog catalog, IReadOnlySet<int> servants) =>
        CompanyRenown.Of(
            company.Entries
                .Where(entry => !servants.Contains(entry.EntryIndex) && season?.IsRetired(entry.EntryIndex) != true)
                .Select(entry => Of(entry, company, season, catalog, servants)),
            season);
}
