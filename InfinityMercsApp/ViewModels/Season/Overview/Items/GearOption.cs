namespace InfinityMercsApp.ViewModels.Season.Overview.Items;

/// <summary>
/// A choice in a gear slot's picker: an owned item, or nothing. An item no spare copy of is left is offered as the
/// copy a trooper carries (<see cref="TakenFrom"/>), noting who.
/// </summary>
public sealed record GearOption(string? ItemName, string Display, int? TakenFrom = null)
{
    public static GearOption None { get; } = new(null, "(none)");

    /// <summary>An owned item, e.g. "Spitfire (1.5 SWC)  [Attached: Ana]" when it takes SWC and is Ana's copy.</summary>
    public static GearOption For(string itemName, double swc, string? carriedBy, int? takenFrom = null)
    {
        var display = swc > 0 ? $"{itemName} ({SeasonEconomy.SwcText((decimal)swc)} SWC)" : itemName;
        return new(itemName, string.IsNullOrWhiteSpace(carriedBy) ? display : $"{display}  [Attached: {carriedBy}]", takenFrom);
    }
}
