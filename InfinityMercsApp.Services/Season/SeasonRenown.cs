using InfinityMercsApp.Domain.Models.Season;
using InfinityMercsApp.Domain.Season.Inventory;
using InfinityMercsApp.Domain.Season.Renown;
using InfinityMercsApp.Services.CompanySelection.Models.Saved;

namespace InfinityMercsApp.Services.Season;

/// <summary>The renown of a saved company's troopers, and of the company, so far this season.</summary>
public static class SeasonRenown
{
    /// <summary>The trooper's renown: its saved cost, the base cost of its season gear, and its XP.</summary>
    public static TrooperRenown Of(SavedCompanyEntry entry, SeasonFile? season, InventoryCatalog catalog) =>
        new(
            entry.Cost,
            CompanyRenown.GearCost(season?.UnitGear.GetValueOrDefault(entry.EntryIndex) ?? [], catalog),
            SeasonTroopers.ProgressOf(entry, season).TotalXp);

    /// <summary>
    /// The company's renown. A Servant peripheral is left out, its cost and gear counting toward the trooper it
    /// belongs to; any other peripheral is a trooper of its own. Retired troopers count with the renown they kept
    /// when they left.
    /// </summary>
    public static int OfCompany(SavedCompanyFile company, SeasonFile? season, InventoryCatalog catalog, Func<SavedCompanyEntry, bool> isServant) =>
        CompanyRenown.Of(
            company.Entries
                .Where(entry => !(entry.IsPeripheralUnit && isServant(entry)) && season?.IsRetired(entry.EntryIndex) != true)
                .Select(entry => Of(entry, season, catalog)),
            season);
}
