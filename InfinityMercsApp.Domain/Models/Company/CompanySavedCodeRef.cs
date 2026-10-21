using System.Text.Json.Serialization;

namespace InfinityMercsApp.Domain.Models.Company;

public sealed class CompanySavedCodeRef
{
    [JsonPropertyName("id")]
    public int Id { get; init; }

    [JsonPropertyName("extra")]
    [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
    public List<int>? Extra { get; init; }
}
