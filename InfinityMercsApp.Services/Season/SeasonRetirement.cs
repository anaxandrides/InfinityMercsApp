using InfinityMercsApp.Domain.Models.Season;
using InfinityMercsApp.Domain.Season.Inventory;
using InfinityMercsApp.Domain.Season.Retirement;
using InfinityMercsApp.Services.CompanySelection.Models.Saved;

namespace InfinityMercsApp.Services.Season;

/// <summary>Which of a saved company's troopers can retire, and what each would leave behind.</summary>
public static class SeasonRetirement
{
    /// <summary>
    /// Whether the trooper can retire: anyone still active but the captain. A peripheral leaves with its trooper
    /// rather than on its own.
    /// </summary>
    public static bool CanRetire(SavedCompanyEntry entry, SeasonFile? season) =>
        !entry.IsLieutenant && !entry.IsPeripheralUnit && season?.IsRetired(entry.EntryIndex) != true;

    /// <summary>
    /// The trooper retiring, with the renown it keeps (its cost and XP; its gear goes back to the inventory) and
    /// the peripherals that leave with it; null when it can't retire.
    /// </summary>
    public static RetiringTrooper? For(SavedCompanyFile company, int entryIndex, SeasonFile season, InventoryCatalog catalog)
    {
        if (company.Entries.FirstOrDefault(entry => entry.EntryIndex == entryIndex) is not { } entry || !CanRetire(entry, season))
        {
            return null;
        }

        var peripherals = company.Entries
            .Where(candidate => candidate.IsPeripheralUnit && candidate.ParentEntryIndex == entryIndex)
            .Select(peripheral => new RetiringPeripheral(peripheral.EntryIndex, SeasonTroopers.NameOf(peripheral)))
            .ToList();
        return new RetiringTrooper(
            entryIndex,
            SeasonTroopers.NameOf(entry),
            SeasonRenown.Of(entry, season, catalog).WithoutGear,
            peripherals);
    }
}
