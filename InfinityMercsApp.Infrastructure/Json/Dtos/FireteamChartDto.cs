using System.Text.Json.Serialization;
using InfinityMercsApp.Infrastructure.Json.Converters;

namespace InfinityMercsApp.Infrastructure.Json.Dtos;

/// <summary>An army's <c>fireteamChart</c>.</summary>
internal sealed class FireteamChartDto
{
    [JsonPropertyName("spec")]
    public FireteamSpecDto? Spec { get; set; }

    [JsonPropertyName("teams")]
    [JsonConverter(typeof(LenientObjectListConverter<FireteamTeamDto>))]
    public List<FireteamTeamDto>? Teams { get; set; }
}
