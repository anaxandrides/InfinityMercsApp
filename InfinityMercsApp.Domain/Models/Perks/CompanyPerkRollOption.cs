namespace InfinityMercsApp.Domain.Models.Perks;

public sealed class CompanyPerkRollOption
{
    public string ListId { get; init; } = string.Empty;
    public string ListName { get; init; } = string.Empty;
    public int TrackNumber { get; init; }
    public int Tier { get; init; }
    public string PerkText { get; init; } = string.Empty;
    public int? RequiredTier { get; init; }
}
