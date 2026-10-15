using InfinityMercsApp.Domain.Season.Rolls;

namespace InfinityMercsApp.Domain.Season.Marketplace;

/// <summary>
/// The roll for a round's War Market: a d20 on the War Market table, where 19-20 lets the company choose any
/// market, and one of its own faction markets (which it could simply have chosen) means rolling again.
/// </summary>
public sealed record WarMarketRoll(int Roll, WarMarketRollKind Kind, string? Store)
{
    /// <summary>The highest roll that names a market; anything above lets the company choose.</summary>
    public const int HighestStoreRoll = 18;

    /// <summary>Rolls tried before giving up on finding a market that is not the company's own.</summary>
    public const int MaxAttempts = 50;

    /// <summary>What <paramref name="roll"/> gives a company whose faction markets are <paramref name="factionMarkets"/>.</summary>
    public static WarMarketRoll For(int roll, IReadOnlySet<string> factionMarkets)
    {
        if (roll > HighestStoreRoll)
        {
            return new(roll, WarMarketRollKind.ChooseAny, null);
        }

        var store = SeasonRollTables.WarMarkets.Lookup(roll);
        if (string.IsNullOrWhiteSpace(store))
        {
            return new(roll, WarMarketRollKind.Invalid, null);
        }

        return factionMarkets.Contains(store)
            ? new(roll, WarMarketRollKind.FactionMarket, store)
            : new(roll, WarMarketRollKind.Found, store);
    }

    /// <summary>Rolls until another faction's market or a free choice comes up, giving up after <see cref="MaxAttempts"/> rolls.</summary>
    public static WarMarketRoll Search(Func<int> rollD20, IReadOnlySet<string> factionMarkets)
    {
        var last = 0;
        for (var attempt = 0; attempt < MaxAttempts; attempt++)
        {
            var result = For(rollD20(), factionMarkets);
            if (result.Kind is WarMarketRollKind.Found or WarMarketRollKind.ChooseAny)
            {
                return result;
            }

            last = result.Roll;
        }

        return new(last, WarMarketRollKind.Exhausted, null);
    }
}
