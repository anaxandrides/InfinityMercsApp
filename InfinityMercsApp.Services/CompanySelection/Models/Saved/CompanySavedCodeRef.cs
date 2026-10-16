using System.Text.Json.Serialization;

namespace InfinityMercsApp.Services.CompanySelection.Models.Saved;

public sealed class CompanySavedCodeRef
{
    [JsonPropertyName("id")]
    public int Id { get; init; }

    [JsonPropertyName("extra")]
    [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
    public List<int>? Extra { get; init; }
}
