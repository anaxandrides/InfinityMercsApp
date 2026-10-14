using System.Text.Json.Serialization;
using InfinityMercsApp.Infrastructure.Json.Converters;

namespace InfinityMercsApp.Infrastructure.Json.Dtos;

internal sealed class ArmyRelationUnitDto
{
    [JsonPropertyName("unit")]
    [JsonConverter(typeof(FlexibleIdConverter))]
    public int? Unit { get; set; }
}
