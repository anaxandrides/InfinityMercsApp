// FROZEN LEGACY ORACLE — verbatim copy of production JSON rules from main@224e1e2.
// Do not edit or "fix" this file. Characterization tests run it against real Infinity Army
// payloads so replacement code can be proven to produce identical results.
// Source: Services/CompanySelection/Units/CompanyUnitDetailsFireteamCommon.cs
using System.Text.Json;

namespace InfinityMercsApp.UnitTests.Legacy;

internal sealed record LegacyFireteamChartEntry(
    string Name,
    int Duo,
    int Haris,
    int Core,
    IReadOnlyList<(string Name, int Min, int Max, string? Slug, bool MinAsterisk)> UnitLimits);

internal static class LegacyCompanyFireteamParser
{
    internal static IReadOnlyList<LegacyFireteamChartEntry> ParseEntries(string? fireteamChartJson, Action<string>? logError = null)
    {
        var entries = new List<LegacyFireteamChartEntry>();
        if (string.IsNullOrWhiteSpace(fireteamChartJson))
        {
            return entries;
        }

        try
        {
            using var doc = JsonDocument.Parse(fireteamChartJson);
            if (!doc.RootElement.TryGetProperty("teams", out var teamsElement) ||
                teamsElement.ValueKind != JsonValueKind.Array)
            {
                return entries;
            }

            foreach (var teamElement in teamsElement.EnumerateArray())
            {
                var name = LegacySharedUtilities.ReadString(teamElement, "name", string.Empty).Trim();
                if (string.IsNullOrWhiteSpace(name))
                {
                    continue;
                }

                var (duo, haris, core) = ReadTeamTypeCounts(teamElement);
                var unitLimits = ReadTeamUnitLimits(teamElement);
                entries.Add(new LegacyFireteamChartEntry(name, duo, haris, core, unitLimits));
            }
        }
        catch (Exception ex)
        {
            logError?.Invoke($"CompanySelectionPage MergeFireteamEntries failed: {ex.Message}");
        }

        return entries;
    }

    private static (int Duo, int Haris, int Core) ReadTeamTypeCounts(JsonElement teamElement)
    {
        var duo = 0;
        var haris = 0;
        var core = 0;

        if (!teamElement.TryGetProperty("type", out var typeElement) || typeElement.ValueKind != JsonValueKind.Array)
        {
            return (duo, haris, core);
        }

        foreach (var type in typeElement.EnumerateArray())
        {
            if (type.ValueKind != JsonValueKind.String)
            {
                continue;
            }

            var value = type.GetString();
            if (string.IsNullOrWhiteSpace(value))
            {
                continue;
            }

            if (value.Equals("DUO", StringComparison.OrdinalIgnoreCase))
            {
                duo++;
            }
            else if (value.Equals("HARIS", StringComparison.OrdinalIgnoreCase))
            {
                haris++;
            }
            else if (value.Equals("CORE", StringComparison.OrdinalIgnoreCase))
            {
                core++;
            }
        }

        return (duo, haris, core);
    }

    private static List<(string Name, int Min, int Max, string? Slug, bool MinAsterisk)> ReadTeamUnitLimits(JsonElement teamElement)
    {
        var results = new List<(string Name, int Min, int Max, string? Slug, bool MinAsterisk)>();
        if (!teamElement.TryGetProperty("units", out var unitsElement) || unitsElement.ValueKind != JsonValueKind.Array)
        {
            return results;
        }

        foreach (var unitElement in unitsElement.EnumerateArray())
        {
            var name = LegacySharedUtilities.ReadString(unitElement, "name", string.Empty).Trim();
            if (string.IsNullOrWhiteSpace(name))
            {
                name = LegacySharedUtilities.ReadString(unitElement, "slug", "Unknown");
            }

            var comment = LegacySharedUtilities.ReadString(unitElement, "comment", string.Empty).Trim();
            var slug = LegacySharedUtilities.ReadString(unitElement, "slug", string.Empty).Trim();
            var displayName = string.IsNullOrWhiteSpace(comment)
                ? name
                : $"{name} {comment}".Trim();

            var min = LegacySharedUtilities.ReadInt(unitElement, "min", 0);
            var max = LegacySharedUtilities.ReadInt(unitElement, "max", 0);
            var minAsterisk = LegacySharedUtilities.HasAsteriskMin(unitElement) ||
                              LegacySharedUtilities.ReadBool(unitElement, "required", false);
            results.Add((displayName, min, max, string.IsNullOrWhiteSpace(slug) ? null : slug, minAsterisk));
        }

        return results;
    }
}


