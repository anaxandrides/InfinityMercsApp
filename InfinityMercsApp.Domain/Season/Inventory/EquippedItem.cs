using InfinityMercsApp.Domain.Models.Season.Gear;

namespace InfinityMercsApp.Domain.Season.Inventory;

/// <summary>An item a trooper carries, under the kind of slot it works as: an extra slot's item works as its own kind.</summary>
public sealed record EquippedItem(GearSlot Slot, string Name)
{
    /// <summary>
    /// The saved gear's items, each extra one under the kind the catalog gives it. Unknown slots, and extra items no
    /// market sells (which have no kind to work as), are left out.
    /// </summary>
    public static IReadOnlyList<EquippedItem> In(IEnumerable<SeasonUnitGear> gear, InventoryCatalog catalog) =>
        gear.Where(item => !string.IsNullOrWhiteSpace(item.ItemName))
            .Select(item => GearSlots.TryParse(item.Slot, out var slot) ? new EquippedItem(KindOf(slot, item.ItemName, catalog), item.ItemName) : null)
            .OfType<EquippedItem>()
            .Where(item => item.Slot != GearSlot.Extra)
            .ToList();

    private static GearSlot KindOf(GearSlot slot, string itemName, InventoryCatalog catalog) =>
        slot == GearSlot.Extra && GearSlots.TryParse(catalog.Find(itemName)?.Category, out var kind) && GearSlots.ExtraSlotTakes.Contains(kind)
            ? kind
            : slot;
}
