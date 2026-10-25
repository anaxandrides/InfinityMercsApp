using InfinityMercsApp.Domain.Season.Loot;

namespace InfinityMercsApp.ViewModels.Season.Loot.Items;

/// <summary>Loot an opened box gave, as the loot box page lists it.</summary>
public sealed record LootRewardItem(string Name, string Details)
{
    /// <summary>"Black Market Cache · Primary · 1.5 SWC · 15 renown".</summary>
    public static LootRewardItem From(LootItem item) =>
        new(item.Name, $"{item.Tier.Name} · {item.Slot} · {(item.Swc > 0 ? $"{item.Swc:0.#} SWC · " : string.Empty)}{item.Tier.Renown} renown");
}
