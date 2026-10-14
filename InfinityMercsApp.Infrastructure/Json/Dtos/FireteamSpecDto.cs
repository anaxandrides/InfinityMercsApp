using System.Text.Json.Serialization;
using InfinityMercsApp.Infrastructure.Json.Converters;

namespace InfinityMercsApp.Infrastructure.Json.Dtos;

/// <summary><c>fireteamChart.spec</c>: fireteam kind → allowed count, in payload order. A non-object reads as empty.</summary>
[JsonConverter(typeof(FireteamSpecConverter))]
internal sealed class FireteamSpecDto
{
    public List<KeyValuePair<string, JsonScalar>> Entries { get; init; } = [];
}
