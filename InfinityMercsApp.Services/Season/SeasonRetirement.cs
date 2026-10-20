using InfinityMercsApp.Domain.Models.Season;
using InfinityMercsApp.Domain.Season.Inventory;
using InfinityMercsApp.Domain.Season.Retirement;
using InfinityMercsApp.Domain.Models.Company;

namespace InfinityMercsApp.Services.Season;

/// <summary>Which of a saved company's troopers can retire, and what each would leave behind.</summary>
public static class SeasonRetirement
{
    /// <summary>
    /// Whether the trooper can retire: anyone still active but the captain. A Servant peripheral leaves with its
    /// trooper rather than on its own; any other peripheral is a trooper of its own. A Lone Wolf's Aspect is the
    /// Lone Wolf, so never leaves on its own.
    /// </summary>
    public static bool CanRetire(SavedCompanyEntry entry, SeasonFile? season, IReadOnlySet<int> servants) =>
        !entry.IsLieutenant && entry.AspectOf is null && !servants.Contains(entry.EntryIndex) && season?.IsRetired(entry.EntryIndex) != true;

    /// <summary>
    /// The trooper retiring, with the renown it keeps (its cost and XP; its gear goes back to the inventory) and
    /// the Servant peripherals that leave with it; null when it can't retire. Other peripherals stay, as troopers of
    /// their own; one from the Peripheral (Controller) augment is left uncontrolled, as when the augment comes off.
    /// </summary>
    public static RetiringTrooper? For(
        SavedCompanyFile company,
        int entryIndex,
        SeasonFile season,
        InventoryCatalog catalog,
        IReadOnlySet<int> servants)
    {
        if (company.Entries.FirstOrDefault(entry => entry.EntryIndex == entryIndex) is not { } entry || !CanRetire(entry, season, servants))
        {
            return null;
        }

        var peripherals = company.Entries
            .Where(candidate => candidate.IsPeripheralUnit && candidate.ParentEntryIndex == entryIndex && servants.Contains(candidate.EntryIndex))
            .Select(peripheral => new RetiringPeripheral(peripheral.EntryIndex, SeasonTroopers.NameOf(peripheral)))
            .ToList();
        return new RetiringTrooper(
            entryIndex,
            SeasonTroopers.NameOf(entry),
            SeasonRenown.Of(entry, company, season, catalog, servants).WithoutGear,
            peripherals);
    }
}
