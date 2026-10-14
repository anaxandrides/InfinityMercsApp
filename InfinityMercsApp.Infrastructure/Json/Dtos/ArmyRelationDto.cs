using System.Text.Json.Serialization;
using InfinityMercsApp.Infrastructure.Json.Converters;

namespace InfinityMercsApp.Infrastructure.Json.Dtos;

/// <summary>An army <c>relations[]</c> entry: <c>{"units":[{"unit":42}],"min":1,"max":1,"group":false}</c>.</summary>
internal sealed class ArmyRelationDto
{
    [JsonPropertyName("units")]
    [JsonConverter(typeof(LenientObjectListConverter<ArmyRelationUnitDto>))]
    public List<ArmyRelationUnitDto>? Units { get; set; }

    [JsonPropertyName("min")]
    public JsonScalar Min { get; set; }

    [JsonPropertyName("max")]
    public JsonScalar Max { get; set; }

    [JsonPropertyName("group")]
    public JsonScalar Group { get; set; }
}
