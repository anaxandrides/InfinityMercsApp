using System.Text.Json.Serialization;

namespace InfinityMercsApp.Infrastructure.Json;

/// <summary><c>fireteamChart.teams[]</c></summary>
internal sealed class FireteamTeamDto
{
    [JsonPropertyName("name")]
    [JsonConverter(typeof(LenientStringConverter))]
    public string? Name { get; set; }

    [JsonPropertyName("type")]
    public ScalarOrArrayDto Type { get; set; }

    [JsonPropertyName("units")]
    [JsonConverter(typeof(LenientObjectListConverter<FireteamUnitDto>))]
    public List<FireteamUnitDto>? Units { get; set; }
}
