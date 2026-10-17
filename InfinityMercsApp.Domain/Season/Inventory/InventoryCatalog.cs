using InfinityMercsApp.Domain.Models.Season.Gear;
using InfinityMercsApp.Domain.Models.Stores;
using InfinityMercsApp.Domain.Season.Marketplace;
using InfinityMercsApp.Domain.Season.RecruitRolls;

namespace InfinityMercsApp.Domain.Season.Inventory;

/// <summary>
/// Everything the markets sell, so a purchase recorded by store and name can be given its category and price.
/// Troop types are listed as armour under "{ArmorName} ({Type})"; augments under the Augments category.
/// </summary>
public sealed class InventoryCatalog
{
    private readonly Dictionary<string, CatalogItem> _byStoreAndName;
    private readonly Dictionary<string, CatalogItem> _byName;

    private InventoryCatalog(Dictionary<string, CatalogItem> byStoreAndName)
    {
        _byStoreAndName = byStoreAndName;
        _byName = byStoreAndName.Values
            .GroupBy(item => item.Name, StringComparer.OrdinalIgnoreCase)
            .ToDictionary(group => group.Key, group => group.First(), StringComparer.OrdinalIgnoreCase);
    }

    public static InventoryCatalog Empty { get; } = From([]);

    /// <summary>The catalog of the given markets; services sell nothing that is kept.</summary>
    public static InventoryCatalog From(IEnumerable<Store> stores)
    {
        var byStoreAndName = new Dictionary<string, CatalogItem>(StringComparer.OrdinalIgnoreCase);
        void Add(Store store, string name, string category, int costCr, decimal costSwc = 0, StoreTroopType? armor = null, string? requirement = null)
        {
            if (!string.IsNullOrWhiteSpace(name))
            {
                byStoreAndName[Key(store.Name, name)] =
                    new CatalogItem(name.Trim(), category, store.Name, costCr, costSwc, armor, AugmentRequirement.From(requirement));
            }
        }

        foreach (var store in stores.Where(store => !store.IsServiceStore))
        {
            foreach (var item in store.Items)
            {
                Add(store, item.Name, InventoryCategories.Normalize(item.Category), item.CostCr, item.CostSwc ?? 0);
            }

            foreach (var troop in store.TroopTypes.Where(troop => !string.IsNullOrWhiteSpace(troop.ArmorName)))
            {
                Add(store, ArmorName(troop), GearSlot.Armor.Key(), troop.CostCr, armor: troop);
            }

            foreach (var augment in store.Augments)
            {
                Add(store, augment.Name, GearSlot.Augments.Key(), augment.CostCr, requirement: augment.Requirement);
            }
        }

        return new InventoryCatalog(byStoreAndName);
    }

    /// <summary>How a troop type's armour is named when bought: "Light Armour (LI)", or the armour alone.</summary>
    public static string ArmorName(StoreTroopType troop) =>
        string.IsNullOrWhiteSpace(troop.Type) ? troop.ArmorName.Trim() : $"{troop.ArmorName.Trim()} ({troop.Type})";

    /// <summary>The item sold under this name by this store, or by any store when this one does not list it.</summary>
    public CatalogItem? Find(string storeName, string itemName) =>
        _byStoreAndName.GetValueOrDefault(Key(storeName, itemName)) ?? Find(itemName);

    /// <summary>The item sold under this name by any store; a rolled copy of armour is found as its armour.</summary>
    public CatalogItem? Find(string itemName) =>
        _byName.GetValueOrDefault(itemName.Trim()) ??
        (RolledArmor.Parse(itemName) is { } rolled ? _byName.GetValueOrDefault(rolled.ArmorName) : null);

    private static string Key(string storeName, string itemName) => $"{StoreLabels.StoreName(storeName)}|{itemName.Trim()}";
}
