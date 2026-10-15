using InfinityMercsApp.Domain.Models.Season.Gear;

namespace InfinityMercsApp.Domain.Season.Inventory;

/// <summary>
/// The SWC of the gear a company's troopers have equipped, which may never exceed the company's SWC. Gear
/// the catalog does not know takes up none.
/// </summary>
public static class EquippedSwc
{
    /// <summary>The SWC an item takes up while equipped.</summary>
    public static double Of(string itemName, InventoryCatalog catalog) => (double)(catalog.Find(itemName)?.CostSwc ?? 0);

    /// <summary>The SWC of everything the company's troopers have equipped.</summary>
    public static double Of(CompanyLoadouts loadouts, InventoryCatalog catalog) =>
        loadouts.AllItems().Sum(item => Of(item, catalog));

    /// <summary>
    /// The company's equipped SWC once <paramref name="itemName"/> is in the trooper's slot. An item taken from
    /// another trooper's slot only moves, so its SWC is already counted.
    /// </summary>
    public static double After(CompanyLoadouts loadouts, int entryIndex, GearSlot slot, string? itemName, InventoryCatalog catalog)
    {
        var total = Of(loadouts, catalog);
        if (loadouts.For(entryIndex)[slot] is { } current)
        {
            total -= Of(current, catalog);
        }

        if (string.IsNullOrWhiteSpace(itemName) || loadouts.HolderOf(itemName, slot, entryIndex) is not null)
        {
            return total;
        }

        return total + Of(itemName, catalog);
    }
}
