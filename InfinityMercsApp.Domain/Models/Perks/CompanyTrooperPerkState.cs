using System.Text.Json.Serialization;

namespace InfinityMercsApp.Domain.Models.Perks;

public sealed class CompanyTrooperPerkState
{
    [JsonPropertyName("id")]
    public string Id { get; set; } = string.Empty;

    [JsonPropertyName("rank")]
    public int Rank { get; set; } = 1;
}
