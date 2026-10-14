using System.Text.Json.Serialization;

namespace InfinityMercsApp.Infrastructure.Json;

// Typed shape of the Infinity Army API JSON (https://api.corvusbelli.com/army/units/en/{factionId}) for
// the parts the app reads. Property names match the payload exactly (case-sensitive), and every loosely
// typed value goes through a lenient converter so one odd value never discards a whole unit.

/// <summary><c>units[].profileGroups[]</c></summary>
internal sealed class ProfileGroupDto
{
    [JsonPropertyName("id")]
    [JsonConverter(typeof(FlexibleIdConverter))]
    public int? Id { get; set; }

    [JsonPropertyName("isc")]
    [JsonConverter(typeof(LenientStringConverter))]
    public string? Isc { get; set; }

    [JsonPropertyName("category")]
    public JsonScalar Category { get; set; }

    [JsonPropertyName("profiles")]
    [JsonConverter(typeof(LenientObjectListConverter<ProfileDto>))]
    public List<ProfileDto>? Profiles { get; set; }

    [JsonPropertyName("options")]
    [JsonConverter(typeof(LenientObjectListConverter<ProfileOptionDto>))]
    public List<ProfileOptionDto>? Options { get; set; }
}
