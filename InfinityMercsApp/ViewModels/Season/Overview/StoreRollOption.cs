using InfinityMercsApp.Domain.Season.Marketplace;
using InfinityMercsApp.Domain.Season.Rolls;

namespace InfinityMercsApp.ViewModels.Season.Overview;

/// <summary>A number to pick instead of rolling: "3 — Jade Temu", or "19-20 — Choose any".</summary>
public sealed record StoreRollOption(int Roll, string Label)
{
    /// <summary>One option per store on the temporary store table, then the free choice.</summary>
    public static IReadOnlyList<StoreRollOption> All { get; } =
    [
        .. Enumerable.Range(1, TemporaryStoreRoll.HighestStoreRoll)
            .Select(roll => new StoreRollOption(roll, $"{roll} — {SeasonRollTables.TemporaryStores.Lookup(roll)}")),
        new StoreRollOption(TemporaryStoreRoll.HighestStoreRoll + 1, "19-20 — Choose any")
    ];
}
