using InfinityMercsApp.Domain.Season.Inventory;

namespace InfinityMercsApp.ViewModels.Season.Overview;

/// <summary>A row of the Inventory tab: an owned item, where and when it came from, and what it sells for.</summary>
public sealed record InventoryItem(InventoryEntry Entry)
{
    public string Name => Entry.Name;

    public string CountText => $"x{Entry.Count}";

    public string SourcesText => string.Join(", ", Entry.Sources);

    public bool HasSources => Entry.Sources.Count > 0;

    /// <summary>"Bought: Round 2", or "Bought: Rounds 0, 2" for several (round 0 is the company's starting purchases).</summary>
    public string RoundsText => Entry.Rounds.Count switch
    {
        0 => string.Empty,
        1 => $"Bought: Round {Entry.Rounds[0]}",
        _ => $"Bought: Rounds {string.Join(", ", Entry.Rounds)}"
    };

    public bool HasRounds => Entry.Rounds.Count > 0;

    public bool CanSell => Entry.SellPrice > 0;

    public string SellText => $"SELL ({Entry.SellPrice}cr)";
}
