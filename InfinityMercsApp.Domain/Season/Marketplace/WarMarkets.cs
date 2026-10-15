using InfinityMercsApp.Domain.Models.Season;
using InfinityMercsApp.Domain.Models.Stores;

namespace InfinityMercsApp.Domain.Season.Marketplace;

/// <summary>
/// Which markets a company can shop at. Before the first contract and in each downtime it has the Base Market
/// and one War Market: one of its factions' markets, or, once a contract has been played, a rolled one.
/// </summary>
public static class WarMarkets
{
    /// <summary>The War Market chosen for the current shopping round, or null when none is chosen yet.</summary>
    public static string? Current(SeasonFile? seasonFile)
    {
        if (seasonFile is null)
        {
            return null;
        }

        var chosen = SeasonRounds.CurrentMarketplace(seasonFile).WarMarket;
        if (!string.IsNullOrWhiteSpace(chosen))
        {
            return chosen;
        }

        // Seasons from before War Markets kept a rolled temporary store on the round.
        var legacy = SeasonRounds.Latest(seasonFile)?.TemporaryStore;
        return string.IsNullOrWhiteSpace(legacy) ? null : legacy;
    }

    /// <summary>A War Market can only be rolled for after the first contract.</summary>
    public static bool CanRoll(SeasonFile? seasonFile) => SeasonRounds.Latest(seasonFile) is not null;

    /// <summary>The markets of the company's factions, which it may choose without rolling.</summary>
    public static IReadOnlyList<Store> FactionMarkets(IEnumerable<Store> stores, IReadOnlySet<string> factionNames) =>
        stores.Where(store => store.IsWarMarket && store.AssociatedFactions.Any(factionNames.Contains)).ToList();
}
