using System.Text.Json.Serialization;
using InfinityMercsApp.Infrastructure.Json.Converters;

namespace InfinityMercsApp.Infrastructure.Json.Dtos;

/// <summary><c>fireteamChart.teams[].units[]</c></summary>
internal sealed class FireteamUnitDto
{
    [JsonPropertyName("name")]
    [JsonConverter(typeof(LenientStringConverter))]
    public string? Name { get; set; }

    [JsonPropertyName("slug")]
    [JsonConverter(typeof(LenientStringConverter))]
    public string? Slug { get; set; }

    [JsonPropertyName("comment")]
    [JsonConverter(typeof(LenientStringConverter))]
    public string? Comment { get; set; }

    [JsonPropertyName("min")]
    public JsonScalar Min { get; set; }

    [JsonPropertyName("max")]
    public JsonScalar Max { get; set; }

    [JsonPropertyName("required")]
    public JsonScalar Required { get; set; }
}
