using System.Text.RegularExpressions;

namespace InfinityMercsApp.Services.CompanySelection.Common;

internal static partial class CompanySelectionSharedUtilities
{
    internal static string ConvertDistanceText(string distanceText, bool showUnitsInInches) =>
        InfinityMercsApp.Domain.Models.Army.Profiles.DistanceText.Convert(distanceText, showUnitsInInches);

    internal static string ReplaceSubtitleMoveDisplay(string? subtitle, string moveDisplay)
    {
        if (string.IsNullOrWhiteSpace(subtitle))
        {
            return $"MOV {moveDisplay}";
        }

        return Regex.Replace(
            subtitle,
            @"(?<=\bMOV\s)[^|]+",
            moveDisplay,
            RegexOptions.IgnoreCase | RegexOptions.CultureInvariant);
    }

    internal static string ExtractFirstPeripheralName(string? peripheralsText)
    {
        if (string.IsNullOrWhiteSpace(peripheralsText) || peripheralsText == "-")
        {
            return string.Empty;
        }

        var firstEntry = peripheralsText
            .Split(',', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries)
            .FirstOrDefault();
        if (string.IsNullOrWhiteSpace(firstEntry))
        {
            return string.Empty;
        }

        return NormalizePeripheralBaseName(firstEntry);
    }

    internal static string NormalizePeripheralNameForDedupe(string? value)
    {
        if (string.IsNullOrWhiteSpace(value) || value == "-")
        {
            return string.Empty;
        }

        return NormalizePeripheralBaseName(value);
    }

    private static string NormalizePeripheralBaseName(string value)
    {
        var result = value.Trim();
        const string prefix = "Peripheral:";
        if (result.StartsWith(prefix, StringComparison.OrdinalIgnoreCase))
        {
            result = result[prefix.Length..].Trim();
        }
        while (true)
        {
            var updated = Regex.Replace(result, @"\s*\([^)]*\)\s*$", string.Empty).Trim();
            if (updated == result)
            {
                break;
            }

            result = updated;
        }

        return result;
    }

    internal static int GetPeripheralTotalCount(IEnumerable<string> peripheralNames)
    {
        var total = 0;
        foreach (var name in peripheralNames)
        {
            if (string.IsNullOrWhiteSpace(name) || name == "-")
            {
                continue;
            }

            var match = Regex.Match(name, @"\((\d+)\)\s*$");
            if (match.Success && int.TryParse(match.Groups[1].Value, out var parsedCount))
            {
                total += Math.Max(0, parsedCount);
            }
            else
            {
                total += 1;
            }
        }

        return total;
    }
}
