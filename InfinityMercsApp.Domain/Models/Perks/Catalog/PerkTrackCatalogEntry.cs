namespace InfinityMercsApp.Domain.Models.Perks.Catalog;

public sealed class PerkTrackCatalogEntry
{
    public int TrackNumber { get; init; }
    public List<CompanyPerkRollRange> RollRanges { get; init; } = [];
}
