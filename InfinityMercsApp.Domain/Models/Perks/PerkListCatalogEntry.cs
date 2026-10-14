namespace InfinityMercsApp.Domain.Models.Perks;

public sealed class PerkListCatalogEntry
{
    public string Id { get; init; } = string.Empty;
    public string Name { get; init; } = string.Empty;
    public bool IsRandomlyGenerated { get; init; }
    public List<CompanyPerkRollRange> ListRollRanges { get; init; } = [];
    public List<PerkTrackCatalogEntry> Tracks { get; init; } = [];
}
