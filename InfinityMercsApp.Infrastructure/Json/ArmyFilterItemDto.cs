using System.Text.Json.Serialization;

namespace InfinityMercsApp.Infrastructure.Json;

/// <summary>An item of an army <c>filters</c> section (<c>skills[]</c>, <c>weapons[]</c>, <c>extras[]</c>...).</summary>
internal sealed class ArmyFilterItemDto
{
    [JsonPropertyName("id")]
    public JsonScalar Id { get; set; }

    [JsonPropertyName("name")]
    [JsonConverter(typeof(LenientStringConverter))]
    public string? Name { get; set; }

    [JsonPropertyName("type")]
    [JsonConverter(typeof(LenientStringConverter))]
    public string? Type { get; set; }

    [JsonPropertyName("url")]
    [JsonConverter(typeof(LenientStringConverter))]
    public string? Url { get; set; }

    [JsonPropertyName("href")]
    [JsonConverter(typeof(LenientStringConverter))]
    public string? Href { get; set; }

    [JsonPropertyName("link")]
    [JsonConverter(typeof(LenientStringConverter))]
    public string? Link { get; set; }

    [JsonPropertyName("wiki")]
    [JsonConverter(typeof(LenientStringConverter))]
    public string? Wiki { get; set; }

    [JsonPropertyName("web")]
    [JsonConverter(typeof(LenientStringConverter))]
    public string? Web { get; set; }

    [JsonPropertyName("abbr")]
    [JsonConverter(typeof(LenientStringConverter))]
    public string? Abbr { get; set; }

    [JsonPropertyName("min")]
    public JsonScalar Min { get; set; }

    [JsonPropertyName("max")]
    public JsonScalar Max { get; set; }

    [JsonPropertyName("mercs")]
    public JsonScalar Mercs { get; set; }

    [JsonPropertyName("specops")]
    public JsonScalar SpecOps { get; set; }
}
