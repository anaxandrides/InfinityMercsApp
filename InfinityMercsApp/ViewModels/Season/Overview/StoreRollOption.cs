using InfinityMercsApp.Domain.Season.Marketplace;
using InfinityMercsApp.Domain.Season.Rolls;

namespace InfinityMercsApp.ViewModels.Season.Overview;

/// <summary>A number to pick instead of rolling: "3 — Jade Temu", or "19-20 — Choose any".</summary>
public sealed record StoreRollOption(int Roll, string Label)
{
    /// <summary>One option per market on the War Market table, then the free choice.</summary>
    public static IReadOnlyList<StoreRollOption> All { get; } =
    [
        .. Enumerable.Range(1, WarMarketRoll.HighestStoreRoll)
            .Select(roll => new StoreRollOption(roll, $"{roll} — {SeasonRollTables.WarMarkets.Lookup(roll)}")),
        new StoreRollOption(WarMarketRoll.HighestStoreRoll + 1, "19-20 — Choose any")
    ];
}
