using System.Text.Json.Serialization;

namespace InfinityMercsApp.Views.Common;

public sealed class CompanySavedCodeRef
{
    [JsonPropertyName("id")]
    public int Id { get; init; }

    [JsonPropertyName("extra")]
    [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
    public List<int>? Extra { get; init; }
}
