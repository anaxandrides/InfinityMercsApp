using System.Text.Json.Serialization;
using InfinityMercsApp.Infrastructure.Json.Converters;

namespace InfinityMercsApp.Infrastructure.Json.Dtos;

/// <summary><c>options[].includes[]</c> — a reference to another option whose entries are also granted.</summary>
internal sealed class IncludeDto
{
    [JsonPropertyName("group")]
    [JsonConverter(typeof(FlexibleIdConverter))]
    public int? Group { get; set; }

    [JsonPropertyName("option")]
    [JsonConverter(typeof(FlexibleIdConverter))]
    public int? Option { get; set; }

    [JsonPropertyName("q")]
    public JsonScalar Q { get; set; }
}
