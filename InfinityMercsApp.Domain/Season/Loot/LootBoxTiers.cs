namespace InfinityMercsApp.Domain.Season.Loot;

/// <summary>The loot box rarity table.</summary>
public static class LootBoxTiers
{
    /// <summary>Every tier, Common first.</summary>
    public static IReadOnlyList<LootBoxTierInfo> All { get; } =
    [
        new(LootBoxTier.ContractCrate, "Contract Crate", "Common", 2, 5),
        new(LootBoxTier.FieldPack, "Field Pack", "Uncommon", 4, 10),
        new(LootBoxTier.BlackMarketCache, "Black Market Cache", "Rare", 6, 15),
        new(LootBoxTier.SponsorDrop, "Sponsor Drop", "Epic", 8, 20),
        new(LootBoxTier.LegendaryBounty, "Legendary Bounty", "Legendary", 10, 25),
        new(LootBoxTier.ArtifactVault, "Artifact Vault", "Mythic", 12, 30)
    ];

    public static LootBoxTierInfo Of(LootBoxTier tier) => All.First(info => info.Tier == tier);
}
