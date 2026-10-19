using System.Text.Json.Serialization;

namespace InfinityMercsApp.Domain.Season.Loot;

/// <summary>The six tiers of loot box, from Common to Mythic.</summary>
[JsonConverter(typeof(JsonStringEnumConverter<LootBoxTier>))]
public enum LootBoxTier
{
    ContractCrate,
    FieldPack,
    BlackMarketCache,
    SponsorDrop,
    LegendaryBounty,
    ArtifactVault
}
