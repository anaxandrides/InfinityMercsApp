using InfinityMercsApp.Domain.Models.Stores;

namespace InfinityMercsApp.Domain.Season.Inventory;

/// <summary>
/// Something a store sells, as the inventory knows it: its name, gear category, store, price, the SWC it
/// takes up while equipped (0 for most gear), and for armour, the armour table's row.
/// </summary>
public sealed record CatalogItem(string Name, string Category, string StoreName, int CostCr, decimal CostSwc = 0, StoreTroopType? Armor = null);
