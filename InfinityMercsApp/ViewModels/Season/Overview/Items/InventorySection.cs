namespace InfinityMercsApp.ViewModels.Season.Overview.Items;

/// <summary>The owned items in one inventory category, headed by the category in capitals.</summary>
public sealed record InventorySection(string Title, IReadOnlyList<InventoryItem> Items);
