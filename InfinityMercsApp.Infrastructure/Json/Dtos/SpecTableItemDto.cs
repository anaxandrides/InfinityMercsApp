using System.Text.Json.Serialization;
using InfinityMercsApp.Infrastructure.Json.Converters;

namespace InfinityMercsApp.Infrastructure.Json.Dtos;

/// <summary>One upgrade of a <c>spectables</c> table: everything it grants, e.g. +1 ARM together with Dogged.</summary>
internal sealed class SpecTableItemDto
{
    [JsonPropertyName("attrs")]
    [JsonConverter(typeof(LenientObjectListConverter<SpecTableAttrDto>))]
    public List<SpecTableAttrDto>? Attrs { get; set; }
}
