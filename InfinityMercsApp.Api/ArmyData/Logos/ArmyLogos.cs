using System.Globalization;
using System.Security.Cryptography;
using System.Text;
using System.Text.Json;

namespace InfinityMercsApp.Api.ArmyData.Logos;

/// <summary>The logos the army data names: each faction's in the metadata, and each unit's in an army's resume.</summary>
public static class ArmyLogos
{
    /// <summary>Each faction's logo. A faction without an http(s) logo is left out.</summary>
    public static IReadOnlyList<LogoFile> OfMetadata(string metadataJson) =>
        Read(metadataJson, "factions").Select(logo => new LogoFile($"{logo.Id}.svg", logo.Url)).ToList();

    /// <summary>Each unit's logo in an army. A unit without an http(s) logo is left out.</summary>
    public static IReadOnlyList<LogoFile> OfArmy(int factionId, string armyJson) =>
        Read(armyJson, "resume").Select(logo => new LogoFile($"units/{factionId}-{logo.Id}.svg", logo.Url)).ToList();

    /// <summary>
    /// The hash of a set of logos: their paths and addresses, in order. The same logos always have the same hash, so
    /// the bundle is only built again when one changes.
    /// </summary>
    public static string Hash(IEnumerable<LogoFile> logos) =>
        Convert.ToHexStringLower(SHA256.HashData(Encoding.UTF8.GetBytes(string.Join('\n',
            logos.OrderBy(logo => logo.Path, StringComparer.Ordinal).Select(logo => $"{logo.Path}={logo.Url}")))));

    // Each item in the named list with an id and an http(s) logo, once per id.
    private static List<IdentifiedLogo> Read(string json, string listName)
    {
        using var document = JsonDocument.Parse(json);
        if (document.RootElement.ValueKind != JsonValueKind.Object ||
            !document.RootElement.TryGetProperty(listName, out var list) ||
            list.ValueKind != JsonValueKind.Array)
        {
            return [];
        }

        var logos = new List<IdentifiedLogo>();
        foreach (var item in list.EnumerateArray())
        {
            if (IdOf(item) is { } id && id > 0 && LogoUrlOf(item) is { } url)
            {
                logos.Add(new IdentifiedLogo(id, url));
            }
        }

        return logos.DistinctBy(logo => logo.Id).ToList();
    }

    private static int? IdOf(JsonElement item) =>
        item.ValueKind == JsonValueKind.Object && item.TryGetProperty("id", out var id)
            ? id.ValueKind switch
            {
                JsonValueKind.Number when id.TryGetInt32(out var number) => number,
                JsonValueKind.String when int.TryParse(id.GetString(), NumberStyles.Integer, CultureInfo.InvariantCulture, out var number) => number,
                _ => null
            }
            : null;

    private static string? LogoUrlOf(JsonElement item) =>
        item.TryGetProperty("logo", out var logo) && logo.ValueKind == JsonValueKind.String &&
        Uri.TryCreate(logo.GetString(), UriKind.Absolute, out var uri) && (uri.Scheme == Uri.UriSchemeHttps || uri.Scheme == Uri.UriSchemeHttp)
            ? uri.ToString()
            : null;
}
