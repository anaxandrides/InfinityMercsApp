namespace InfinityMercsApp.ViewModels.Season.Overview.Items;

/// <summary>A choice in a gear slot's picker: an owned item (noting its SWC and who else carries it), or nothing.</summary>
public sealed record GearOption(string? ItemName, string Display)
{
    public static GearOption None { get; } = new(null, "(none)");

    /// <summary>An owned item, e.g. "Spitfire (1.5 SWC)  [Attached: Ana]" when it takes SWC and another trooper carries it.</summary>
    public static GearOption For(string itemName, double swc, string? carriedBy)
    {
        var display = swc > 0 ? $"{itemName} ({SeasonEconomy.SwcText((decimal)swc)} SWC)" : itemName;
        return new(itemName, string.IsNullOrWhiteSpace(carriedBy) ? display : $"{display}  [Attached: {carriedBy}]");
    }
}
