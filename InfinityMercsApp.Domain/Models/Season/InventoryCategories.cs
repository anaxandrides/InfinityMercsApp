namespace InfinityMercsApp.Domain.Models.Season;

/// <summary>Inventory categories besides the gear slots.</summary>
public static class InventoryCategories
{
    /// <summary>Items exchanged for SWC rather than equipped.</summary>
    public const string Exchange = "Exchange";

    /// <summary>Items with no category.</summary>
    public const string General = "General";

    public static string Normalize(string? category) => string.IsNullOrWhiteSpace(category) ? General : category.Trim();

    /// <summary>Where a category is listed: the gear slots in slot order, then Exchange, then others, General last.</summary>
    public static int Order(string category) => category switch
    {
        _ when GearSlots.TryParse(category, out var slot) && string.Equals(slot.Key(), category, StringComparison.Ordinal) => (int)slot,
        Exchange => 7,
        General => 99,
        _ => 50
    };
}
