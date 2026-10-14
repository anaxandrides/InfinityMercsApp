using System.Text.Json.Serialization;
using InfinityMercsApp.Infrastructure.Json.Converters;

namespace InfinityMercsApp.Infrastructure.Json.Dtos;

/// <summary><c>units[].filters</c> — the ids of everything a unit can field.</summary>
internal sealed class UnitFiltersDto
{
    [JsonPropertyName("categories")]
    [JsonConverter(typeof(JsonScalarListConverter))]
    public List<JsonScalar>? Categories { get; set; }

    [JsonPropertyName("types")]
    [JsonConverter(typeof(JsonScalarListConverter))]
    public List<JsonScalar>? Types { get; set; }

    [JsonPropertyName("chars")]
    [JsonConverter(typeof(JsonScalarListConverter))]
    public List<JsonScalar>? Chars { get; set; }

    [JsonPropertyName("skills")]
    [JsonConverter(typeof(JsonScalarListConverter))]
    public List<JsonScalar>? Skills { get; set; }

    [JsonPropertyName("equip")]
    [JsonConverter(typeof(JsonScalarListConverter))]
    public List<JsonScalar>? Equip { get; set; }

    [JsonPropertyName("weapons")]
    [JsonConverter(typeof(JsonScalarListConverter))]
    public List<JsonScalar>? Weapons { get; set; }

    [JsonPropertyName("ammunition")]
    [JsonConverter(typeof(JsonScalarListConverter))]
    public List<JsonScalar>? Ammunition { get; set; }
}
