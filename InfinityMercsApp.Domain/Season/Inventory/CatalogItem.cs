namespace InfinityMercsApp.Domain.Season.Inventory;

/// <summary>Something a store sells, as the inventory knows it: its name, gear category, store and price.</summary>
public sealed record CatalogItem(string Name, string Category, string StoreName, int CostCr);
