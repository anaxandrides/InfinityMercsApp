using System.Text.Json.Serialization;
using InfinityMercsApp.Infrastructure.Json.Converters;

namespace InfinityMercsApp.Infrastructure.Json.Dtos;

/// <summary>
/// One thing an upgrade grants: <c>{"type":"stat","stat":"bts","q":3}</c>, or a skill, piece of equipment or weapon
/// by id with its extras, <c>{"type":"equip","id":183,"extra":[6]}</c>.
/// </summary>
internal sealed class SpecTableAttrDto
{
    [JsonPropertyName("type")]
    [JsonConverter(typeof(LenientStringConverter))]
    public string? Type { get; set; }

    [JsonPropertyName("id")]
    [JsonConverter(typeof(FlexibleIdConverter))]
    public int? Id { get; set; }

    [JsonPropertyName("stat")]
    [JsonConverter(typeof(LenientStringConverter))]
    public string? Stat { get; set; }

    [JsonPropertyName("q")]
    public JsonScalar Q { get; set; }

    [JsonPropertyName("extra")]
    [JsonConverter(typeof(IdListConverter))]
    public List<int>? Extra { get; set; }
}
