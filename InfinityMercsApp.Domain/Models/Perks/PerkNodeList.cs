namespace InfinityMercsApp.Domain.Models.Perks;

public sealed class PerkNodeList
{
    public string ListId { get; init; } = string.Empty;
    public string ListName { get; init; } = string.Empty;
    public bool IsRandomlyGenerated { get; init; }
    public IReadOnlyList<CompanyPerkRollRange> ListRollRanges { get; init; } = [];
    public IReadOnlyList<PerkNode> Roots { get; init; } = [];
}
