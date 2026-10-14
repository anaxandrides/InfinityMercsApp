using System.Text.Json.Serialization;

namespace InfinityMercsApp.Domain.Models.Perks;

public sealed class CompanyTrooperPerk
{
    [JsonPropertyName("id")]
    public string Id { get; init; } = string.Empty;

    [JsonPropertyName("name")]
    public string Name { get; init; } = string.Empty;

    [JsonPropertyName("description")]
    public string Description { get; init; } = string.Empty;

    [JsonPropertyName("maxRank")]
    public int MaxRank { get; init; } = 1;
}
