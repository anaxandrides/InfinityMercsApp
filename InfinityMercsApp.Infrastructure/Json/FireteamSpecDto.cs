using System.Text.Json.Serialization;

namespace InfinityMercsApp.Infrastructure.Json;

/// <summary><c>fireteamChart.spec</c>: fireteam kind → allowed count, in payload order. A non-object reads as empty.</summary>
[JsonConverter(typeof(FireteamSpecConverter))]
internal sealed class FireteamSpecDto
{
    public List<KeyValuePair<string, JsonScalar>> Entries { get; init; } = [];
}
