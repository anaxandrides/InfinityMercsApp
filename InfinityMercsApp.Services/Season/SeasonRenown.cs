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
    /// The company's renown. Peripherals are left out: their cost and gear count toward the trooper they belong to.
    /// </summary>
    public static int OfCompany(SavedCompanyFile company, SeasonFile? season, InventoryCatalog catalog) =>
        CompanyRenown.Of(
            company.Entries.Where(entry => !entry.IsPeripheralUnit).Select(entry => Of(entry, season, catalog)),
            season);
}
