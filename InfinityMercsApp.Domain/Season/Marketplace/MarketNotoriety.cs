using InfinityMercsApp.Domain.Models.Stores;

namespace InfinityMercsApp.Domain.Season.Marketplace;

/// <summary>
/// How a company's notoriety moves market prices: lawful markets charge a credit more per point, chaotic
/// ones a credit less, and nothing ever drops below half its price.
/// </summary>
public readonly record struct MarketNotoriety(int Adjustment, bool Applies)
{
    /// <summary>No adjustment, as at a neutral market.</summary>
    public static MarketNotoriety None { get; } = new(0, false);

    /// <summary>The adjustment <paramref name="store"/> makes for a company with <paramref name="companyNotoriety"/>.</summary>
    public static MarketNotoriety For(Store store, int companyNotoriety) =>
        store.IsLawful ? new(companyNotoriety, true)
        : store.IsChaotic ? new(-companyNotoriety, true)
        : None;

    /// <summary>The price after the adjustment, never below half the base price (rounded up).</summary>
    public int Price(int baseCost)
    {
        if (Adjustment == 0)
        {
            return baseCost;
        }

        var minimum = (int)Math.Ceiling(baseCost / 2.0);
        return Math.Max(baseCost + Adjustment, minimum);
    }

    /// <summary>The store's alignment, followed by the adjustment when there is one: "Lawful  (Notoriety: +2 CR)".</summary>
    public string AlignmentText(string alignment) =>
        Applies && Adjustment != 0
            ? $"{alignment}  (Notoriety: {(Adjustment >= 0 ? "+" : string.Empty)}{Adjustment} CR)"
            : alignment;
}
