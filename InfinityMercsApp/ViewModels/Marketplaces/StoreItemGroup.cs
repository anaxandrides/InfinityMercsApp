using InfinityMercsApp.Domain.Models.Stores;

namespace InfinityMercsApp.ViewModels.Marketplaces;

/// <summary>A store's items in one category, in the order the store lists them.</summary>
public sealed record StoreItemGroup(string Category, IReadOnlyList<StoreItemRow> Items)
{
    /// <summary>The items grouped by category, keeping the store's order of first appearance.</summary>
    public static IReadOnlyList<StoreItemGroup> From(IEnumerable<StoreItem> items) =>
        items.GroupBy(item => item.Category)
            .Select(group => new StoreItemGroup(group.Key, group.Select(item => new StoreItemRow(item)).ToList()))
            .ToList();
}
