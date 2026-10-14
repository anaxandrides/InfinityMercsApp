namespace InfinityMercsApp.Domain.Models.Perks;

public sealed class PerkTrackCatalogEntry
{
    public int TrackNumber { get; init; }
    public List<CompanyPerkRollRange> RollRanges { get; init; } = [];
}
