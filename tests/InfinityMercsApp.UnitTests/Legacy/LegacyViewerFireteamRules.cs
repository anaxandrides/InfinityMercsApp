// FROZEN LEGACY ORACLE — verbatim copy of production JSON rules from main@224e1e2.
// Do not edit or "fix" this file. Characterization tests run it against real Infinity Army
// payloads so replacement code can be proven to produce identical results.
// Source: pure helpers of ViewModels/ViewerViewModel.Fireteams.cs (FireteamUnitLimitItem replaced by an identical record).
using System.Globalization;
using System.Text.Json;

namespace InfinityMercsApp.UnitTests.Legacy;

internal static class LegacyViewerFireteamRules
{
    internal sealed class FireteamUnitLimitItem
    {
        public string Name { get; init; } = string.Empty;
        public string Slug { get; init; } = string.Empty;
        public string Min { get; init; } = "0";
        public string Max { get; init; } = "0";
    }

    internal static List<FireteamUnitLimitItem> ReadUnitLimits(JsonElement teamElement)
    {
        var results = new List<FireteamUnitLimitItem>();
        if (!teamElement.TryGetProperty("units", out var unitsElement) || unitsElement.ValueKind != JsonValueKind.Array)
        {
            return results;
        }

        foreach (var unitElement in unitsElement.EnumerateArray())
        {
            var unitName = ReadString(unitElement, "name", string.Empty);
            if (string.IsNullOrWhiteSpace(unitName))
            {
                unitName = ReadString(unitElement, "slug", "Unknown");
            }
            var unitSlug = ReadString(unitElement, "slug", string.Empty);

            var min = TryReadIntProperty(unitElement, "min", out var minValue) ? minValue : 0;
            var max = TryReadIntProperty(unitElement, "max", out var maxValue) ? maxValue : 0;

            results.Add(new FireteamUnitLimitItem
            {
                Name = unitName,
                Slug = unitSlug,
                Min = min.ToString(CultureInfo.InvariantCulture),
                Max = max.ToString(CultureInfo.InvariantCulture)
            });
        }

        return results;
    }

    internal static string ReadTeamTypes(JsonElement teamElement)
    {
        if (!teamElement.TryGetProperty("type", out var typeElement))
        {
            return string.Empty;
        }

        if (typeElement.ValueKind == JsonValueKind.Array)
        {
            var types = typeElement.EnumerateArray()
                .Where(x => x.ValueKind == JsonValueKind.String)
                .Select(x => x.GetString())
                .OfType<string>()
                .Where(x => !string.IsNullOrWhiteSpace(x))
                .Select(x => x.Trim());

            return string.Join(", ", types);
        }

        if (typeElement.ValueKind == JsonValueKind.String)
        {
            return typeElement.GetString() ?? string.Empty;
        }

        return string.Empty;
    }

    internal static bool TryReadIntProperty(JsonElement element, string propertyName, out int value)
    {
        if (!element.TryGetProperty(propertyName, out var propertyValue))
        {
            value = 0;
            return false;
        }

        return TryReadInt(propertyValue, out value);
    }

    internal static string ReadString(JsonElement element, string propertyName, string fallback)
    {
        if (!element.TryGetProperty(propertyName, out var property) || property.ValueKind != JsonValueKind.String)
        {
            return fallback;
        }

        return property.GetString() ?? fallback;
    }

    internal static string ReadFireteamCount(JsonElement specElement, string key)
    {
        foreach (var property in specElement.EnumerateObject())
        {
            if (!string.Equals(property.Name, key, StringComparison.OrdinalIgnoreCase))
            {
                continue;
            }

            if (TryReadInt(property.Value, out var rawValue))
            {
                return rawValue > 5 ? "T" : rawValue.ToString(CultureInfo.InvariantCulture);
            }

            return "-";
        }

        return "-";
    }

    internal static bool TryReadInt(JsonElement element, out int value)
    {
        if (element.ValueKind == JsonValueKind.Number && element.TryGetInt32(out value))
        {
            return true;
        }

        if (element.ValueKind == JsonValueKind.String &&
            int.TryParse(element.GetString(), NumberStyles.Integer, CultureInfo.InvariantCulture, out value))
        {
            return true;
        }

        value = 0;
        return false;
    }
}
