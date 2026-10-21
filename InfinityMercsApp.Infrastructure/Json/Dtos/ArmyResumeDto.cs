using System.Text.Json.Serialization;
using InfinityMercsApp.Infrastructure.Json.Converters;

namespace InfinityMercsApp.Infrastructure.Json.Dtos;

/// <summary>An entry of an army payload's <c>resume</c> unit index.</summary>
internal sealed class ArmyResumeDto
{
    [JsonPropertyName("id")]
    [JsonConverter(typeof(FlexibleIdConverter))]
    public int? Id { get; set; }

    [JsonPropertyName("isc")]
    [JsonConverter(typeof(LenientStringConverter))]
    public string? Isc { get; set; }

    [JsonPropertyName("idArmy")]
    public JsonScalar IdArmy { get; set; }

    [JsonPropertyName("name")]
    [JsonConverter(typeof(LenientStringConverter))]
    public string? Name { get; set; }

    [JsonPropertyName("slug")]
    [JsonConverter(typeof(LenientStringConverter))]
    public string? Slug { get; set; }

    [JsonPropertyName("logo")]
    [JsonConverter(typeof(LenientStringConverter))]
    public string? Logo { get; set; }

    [JsonPropertyName("type")]
    public JsonScalar Type { get; set; }

    [JsonPropertyName("category")]
    public JsonScalar Category { get; set; }
}
