using System.Text.Json.Serialization;
using InfinityMercsApp.Infrastructure.Json.Converters;

namespace InfinityMercsApp.Infrastructure.Json.Dtos;

/// <summary>A whole army payload (<c>/army/units/{id}</c>): its units, unit index, filters and charts.</summary>
internal sealed class ArmyPayloadDto
{
    [JsonPropertyName("version")]
    public JsonScalar Version { get; set; }

    [JsonPropertyName("units")]
    [JsonConverter(typeof(LenientObjectListConverter<ArmyUnitDto>))]
    public List<ArmyUnitDto>? Units { get; set; }

    [JsonPropertyName("resume")]
    [JsonConverter(typeof(LenientObjectListConverter<ArmyResumeDto>))]
    public List<ArmyResumeDto>? Resume { get; set; }

    [JsonPropertyName("reinforcements")]
    public JsonScalar Reinforcements { get; set; }

    [JsonPropertyName("filters")]
    [JsonConverter(typeof(LenientObjectConverter<Dictionary<string, ArmyFilterSectionDto>>))]
    public Dictionary<string, ArmyFilterSectionDto>? Filters { get; set; }

    [JsonPropertyName("relations")]
    [JsonConverter(typeof(LenientObjectListConverter<ArmyRelationDto>))]
    public List<ArmyRelationDto>? Relations { get; set; }

    [JsonPropertyName("specops")]
    [JsonConverter(typeof(LenientObjectConverter<SpecopsDto>))]
    public SpecopsDto? Specops { get; set; }

    [JsonPropertyName("fireteamChart")]
    [JsonConverter(typeof(LenientObjectConverter<FireteamChartDto>))]
    public FireteamChartDto? FireteamChart { get; set; }
}
