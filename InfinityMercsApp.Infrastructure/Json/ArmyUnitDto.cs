using System.Text.Json.Serialization;

namespace InfinityMercsApp.Infrastructure.Json;

/// <summary>A unit of an army payload (<c>units[]</c> or <c>specops.units[]</c>).</summary>
internal sealed class ArmyUnitDto
{
    [JsonPropertyName("id")]
    [JsonConverter(typeof(FlexibleIdConverter))]
    public int? Id { get; set; }

    [JsonPropertyName("idArmy")]
    public JsonScalar IdArmy { get; set; }

    [JsonPropertyName("canonical")]
    public JsonScalar Canonical { get; set; }

    [JsonPropertyName("isc")]
    [JsonConverter(typeof(LenientStringConverter))]
    public string? Isc { get; set; }

    [JsonPropertyName("iscAbbr")]
    [JsonConverter(typeof(LenientStringConverter))]
    public string? IscAbbr { get; set; }

    [JsonPropertyName("name")]
    [JsonConverter(typeof(LenientStringConverter))]
    public string? Name { get; set; }

    [JsonPropertyName("slug")]
    [JsonConverter(typeof(LenientStringConverter))]
    public string? Slug { get; set; }

    [JsonPropertyName("profileGroups")]
    [JsonConverter(typeof(LenientObjectListConverter<ProfileGroupDto>))]
    public List<ProfileGroupDto>? ProfileGroups { get; set; }

    [JsonPropertyName("options")]
    [JsonConverter(typeof(LenientObjectListConverter<ProfileOptionDto>))]
    public List<ProfileOptionDto>? Options { get; set; }

    [JsonPropertyName("filters")]
    [JsonConverter(typeof(LenientObjectConverter<UnitFiltersDto>))]
    public UnitFiltersDto? Filters { get; set; }

    [JsonPropertyName("factions")]
    [JsonConverter(typeof(JsonScalarListConverter))]
    public List<JsonScalar>? Factions { get; set; }
}
