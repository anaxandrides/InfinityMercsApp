using System.Text.Json.Serialization;
using InfinityMercsApp.Infrastructure.Json.Converters;

namespace InfinityMercsApp.Infrastructure.Json.Dtos;

/// <summary>A <c>spectables</c> table: <c>{"items":[{"attrs":[...]}]}</c>.</summary>
internal sealed class SpecTableDto
{
    [JsonPropertyName("items")]
    [JsonConverter(typeof(LenientObjectListConverter<SpecTableItemDto>))]
    public List<SpecTableItemDto>? Items { get; set; }
}
