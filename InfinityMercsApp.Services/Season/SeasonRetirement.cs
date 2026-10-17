using InfinityMercsApp.Domain.Models.Season;
using InfinityMercsApp.Domain.Season.Inventory;
using InfinityMercsApp.Domain.Season.Retirement;
using InfinityMercsApp.Services.CompanySelection.Models.Saved;

namespace InfinityMercsApp.Services.Season;

/// <summary>Which of a saved company's troopers can retire, and what each would leave behind.</summary>
public static class SeasonRetirement
{
    /// <summary>
    /// Whether the trooper can retire: anyone still active but the captain. A Servant peripheral leaves with its
    /// trooper rather than on its own; any other peripheral is a trooper of its own.
    /// </summary>
    public static bool CanRetire(SavedCompanyEntry entry, SeasonFile? season, Func<SavedCompanyEntry, bool> isServant) =>
        !entry.IsLieutenant && !(entry.IsPeripheralUnit && isServant(entry)) && season?.IsRetired(entry.EntryIndex) != true;

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
        Func<SavedCompanyEntry, bool> isServant)
    {
        if (company.Entries.FirstOrDefault(entry => entry.EntryIndex == entryIndex) is not { } entry || !CanRetire(entry, season, isServant))
        {
            return null;
        }

        var peripherals = company.Entries
            .Where(candidate => candidate.IsPeripheralUnit && candidate.ParentEntryIndex == entryIndex && isServant(candidate))
            .Select(peripheral => new RetiringPeripheral(peripheral.EntryIndex, SeasonTroopers.NameOf(peripheral)))
            .ToList();
        return new RetiringTrooper(
            entryIndex,
            SeasonTroopers.NameOf(entry),
            SeasonRenown.Of(entry, season, catalog).WithoutGear,
            peripherals);
    }
}
