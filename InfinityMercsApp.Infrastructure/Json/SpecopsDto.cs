using System.Text.Json.Serialization;

namespace InfinityMercsApp.Infrastructure.Json;

/// <summary>An army's <c>specops</c>: the choices a spec-ops trooper can buy, and the spec-ops units.</summary>
internal sealed class SpecopsDto
{
    [JsonPropertyName("skills")]
    [JsonConverter(typeof(LenientObjectListConverter<SpecopsChoiceDto>))]
    public List<SpecopsChoiceDto>? Skills { get; set; }

    [JsonPropertyName("equip")]
    [JsonConverter(typeof(LenientObjectListConverter<SpecopsChoiceDto>))]
    public List<SpecopsChoiceDto>? Equip { get; set; }

    [JsonPropertyName("weapons")]
    [JsonConverter(typeof(LenientObjectListConverter<SpecopsChoiceDto>))]
    public List<SpecopsChoiceDto>? Weapons { get; set; }

    [JsonPropertyName("units")]
    [JsonConverter(typeof(LenientObjectListConverter<ArmyUnitDto>))]
    public List<ArmyUnitDto>? Units { get; set; }
}
