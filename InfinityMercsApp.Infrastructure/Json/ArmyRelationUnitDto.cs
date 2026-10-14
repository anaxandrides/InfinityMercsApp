using System.Text.Json.Serialization;

namespace InfinityMercsApp.Infrastructure.Json;

internal sealed class ArmyRelationUnitDto
{
    [JsonPropertyName("unit")]
    [JsonConverter(typeof(FlexibleIdConverter))]
    public int? Unit { get; set; }
}
