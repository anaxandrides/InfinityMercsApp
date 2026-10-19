using InfinityMercsApp.Domain.Models.Season;
using InfinityMercsApp.Domain.Season.Inventory;

namespace InfinityMercsApp.Domain.Season.Loot;

/// <summary>The loot a season's opened boxes gave.</summary>
public static class SeasonLoot
{
    /// <summary>Every opened box's item, in the order the boxes came.</summary>
    public static IReadOnlyList<LootItem> Items(SeasonFile? seasonFile) =>
        seasonFile?.LootBoxes.Select(LootItem.From).OfType<LootItem>().ToList() ?? [];

    /// <summary>The store catalog with the season's loot added, so its slot, SWC and renown are known by name.</summary>
    public static InventoryCatalog Catalog(InventoryCatalog catalog, SeasonFile? seasonFile) =>
        catalog.With(Items(seasonFile).Select(item => item.ToCatalogItem()));
}
