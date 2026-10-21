using System.Text.Json.Serialization;
using InfinityMercsApp.Infrastructure.Json.Converters;

namespace InfinityMercsApp.Infrastructure.Json.Dtos;

/// <summary><c>options[].orders[]</c></summary>
internal sealed class OrderDto
{
    [JsonPropertyName("type")]
    [JsonConverter(typeof(LenientStringConverter))]
    public string? Type { get; set; }

    [JsonPropertyName("list")]
    public JsonScalar List { get; set; }

    [JsonPropertyName("total")]
    public JsonScalar Total { get; set; }
}
