using System.Globalization;
using System.Security.Cryptography;
using System.Text;
using System.Text.Json;

namespace InfinityMercsApp.Api.ArmyData;

/// <summary>The little the mirror reads from a payload it otherwise keeps exactly as served.</summary>
public static class ArmyPayloads
{
    /// <summary>The ids of the factions the metadata lists.</summary>
    /// <exception cref="JsonException">The payload isn't a JSON object.</exception>
    public static IReadOnlyList<int> FactionIds(string metadataJson)
    {
        using var document = JsonDocument.Parse(metadataJson);
        if (document.RootElement.ValueKind != JsonValueKind.Object ||
            !document.RootElement.TryGetProperty("factions", out var factions) ||
            factions.ValueKind != JsonValueKind.Array)
        {
            throw new JsonException("The metadata has no factions.");
        }

        return factions.EnumerateArray()
            .Select(IdOf)
            .OfType<int>()
            .Distinct()
            .Order()
            .ToList();
    }

    /// <summary>The army's version, or null when the payload has none.</summary>
    /// <exception cref="JsonException">The payload isn't JSON.</exception>
    public static string? Version(string armyJson)
    {
        using var document = JsonDocument.Parse(armyJson);
        return document.RootElement.ValueKind == JsonValueKind.Object &&
               document.RootElement.TryGetProperty("version", out var version) &&
               version.ValueKind == JsonValueKind.String &&
               version.GetString() is { Length: > 0 } text
            ? text
            : null;
    }

    /// <summary>The payload's SHA-256 in lower-case hex.</summary>
    public static string Hash(string json) => Convert.ToHexStringLower(SHA256.HashData(Encoding.UTF8.GetBytes(json)));

    private static int? IdOf(JsonElement faction)
    {
        if (faction.ValueKind != JsonValueKind.Object || !faction.TryGetProperty("id", out var id))
        {
            return null;
        }

        return id.ValueKind switch
        {
            JsonValueKind.Number when id.TryGetInt32(out var number) => number,
            JsonValueKind.String when int.TryParse(id.GetString(), NumberStyles.Integer, CultureInfo.InvariantCulture, out var number) => number,
            _ => null
        };
    }
}
