namespace InfinityMercsApp.Domain.Season.Game;

/// <summary>
/// The orders a company gets for a round: a lieutenant order per lieutenant, an impetuous order per impetuous
/// trooper, an irregular or regular order per trooper, and one extra regular order every Mercs company gets.
/// Only troopers still conscious count, and a hired troop over the six-trooper limit gives only its impetuous order.
/// </summary>
public sealed record OrderCounts(int Lieutenant, int Impetuous, int Irregular, int Regular)
{
    /// <summary>The bonus regular order every Mercs company gets.</summary>
    public const int MercsBonusRegular = 1;

    public static OrderCounts For(IEnumerable<OrderSource> troopers)
    {
        var conscious = troopers.Where(trooper => trooper.IsConscious).ToList();
        var ordered = conscious.Where(trooper => !trooper.IsOverTrooperLimit).ToList();
        return new OrderCounts(
            ordered.Count(trooper => trooper.IsLieutenant),
            conscious.Count(trooper => trooper.IsImpetuous),
            ordered.Count(trooper => trooper.IsIrregular),
            ordered.Count(trooper => !trooper.IsIrregular) + MercsBonusRegular);
    }

    public int Of(OrderKind kind) => kind switch
    {
        OrderKind.Lieutenant => Lieutenant,
        OrderKind.Impetuous => Impetuous,
        OrderKind.Irregular => Irregular,
        _ => Regular
    };
}
