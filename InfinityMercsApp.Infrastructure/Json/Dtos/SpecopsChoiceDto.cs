using System.Text.Json.Serialization;
using InfinityMercsApp.Infrastructure.Json.Converters;

namespace InfinityMercsApp.Infrastructure.Json.Dtos;

/// <summary>A spec-ops choice: <c>{"exp":8,"id":35,"extras":[6],"skills":[{"id":74,"extras":[304]}]}</c>.</summary>
internal sealed class SpecopsChoiceDto
{
    [JsonPropertyName("id")]
    [JsonConverter(typeof(FlexibleIdConverter))]
    public int? Id { get; set; }

    [JsonPropertyName("exp")]
    public JsonScalar Exp { get; set; }

    [JsonPropertyName("extras")]
    [JsonConverter(typeof(IdListConverter))]
    public List<int>? Extras { get; set; }

    [JsonPropertyName("skills")]
    [JsonConverter(typeof(SpecopsLinkListConverter))]
    public List<SpecopsLinkDto>? Skills { get; set; }

    [JsonPropertyName("equip")]
    [JsonConverter(typeof(SpecopsLinkListConverter))]
    public List<SpecopsLinkDto>? Equip { get; set; }

    [JsonPropertyName("weapons")]
    [JsonConverter(typeof(SpecopsLinkListConverter))]
    public List<SpecopsLinkDto>? Weapons { get; set; }
}
