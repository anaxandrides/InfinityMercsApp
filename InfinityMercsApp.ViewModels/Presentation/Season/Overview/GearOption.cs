namespace InfinityMercsApp.Presentation.Season.Overview;

/// <summary>
/// A choice in a gear slot's picker: an owned item, or nothing. An item no spare copy of is left is offered as the
/// copy a trooper carries (<see cref="TakenFrom"/>), noting who. An augment the trooper doesn't meet the
/// requirement of notes it.
/// </summary>
public sealed record GearOption(string? ItemName, string Display, int? TakenFrom = null)
{
    public static GearOption None { get; } = new(null, "(none)");

    /// <summary>
    /// An owned item, e.g. "Spitfire (1.5 SWC)  [Attached: Ana]" when it takes SWC and is Ana's copy, or
    /// "Armed Turret (MMR)  [Needs Engineer]" for an augment the trooper can't take.
    /// </summary>
    public static GearOption For(string itemName, double swc, string? carriedBy, int? takenFrom = null, string? unmet = null)
    {
        var display = swc > 0 ? $"{itemName} ({SeasonEconomy.SwcText((decimal)swc)} SWC)" : itemName;
        if (!string.IsNullOrWhiteSpace(carriedBy))
        {
            display += $"  [Attached: {carriedBy}]";
        }

        return new(itemName, string.IsNullOrWhiteSpace(unmet) ? display : $"{display}  [{unmet}]", takenFrom);
    }
}
