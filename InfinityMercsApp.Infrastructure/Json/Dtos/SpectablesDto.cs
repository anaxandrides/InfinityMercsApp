using System.Text.Json.Serialization;
using InfinityMercsApp.Infrastructure.Json.Converters;

namespace InfinityMercsApp.Infrastructure.Json.Dtos;

/// <summary>
/// A Spec-Ops or Team-Ops unit's <c>spectables</c>: the upgrade <c>table</c> it picks from when built, and the
/// <c>specball</c> it can call down in play (which Mercs doesn't use).
/// </summary>
internal sealed class SpectablesDto
{
    [JsonPropertyName("table")]
    [JsonConverter(typeof(LenientObjectConverter<SpecTableDto>))]
    public SpecTableDto? Table { get; set; }
}
