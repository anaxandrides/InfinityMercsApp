using System.Text.Json.Serialization;

namespace InfinityMercsApp.Domain.Models.Perks;

public sealed class CompanyTrooperPerkState
{
    [JsonPropertyName("id")]
    public string Id { get; init; } = string.Empty;

    [JsonPropertyName("rank")]
    public int Rank { get; init; } = 1;
}
