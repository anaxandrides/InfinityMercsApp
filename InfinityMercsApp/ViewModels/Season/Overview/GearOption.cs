namespace InfinityMercsApp.ViewModels.Season.Overview;

/// <summary>A choice in a gear slot's picker: an owned item (noting who else carries it), or nothing.</summary>
public sealed record GearOption(string? ItemName, string Display)
{
    public static GearOption None { get; } = new(null, "(none)");

    /// <summary>An owned item; when another trooper carries it, the picker says so.</summary>
    public static GearOption For(string itemName, string? carriedBy) =>
        new(itemName, string.IsNullOrWhiteSpace(carriedBy) ? itemName : $"{itemName}  [Attached: {carriedBy}]");
}
