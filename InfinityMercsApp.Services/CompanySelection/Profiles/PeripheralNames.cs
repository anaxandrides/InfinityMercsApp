using System.Text.RegularExpressions;

namespace InfinityMercsApp.Services.CompanySelection.Profiles;

/// <summary>Reading peripheral names as profiles list them ("Peripheral: Traktor Mul (2)", "Bit, Q-Drone").</summary>
public static partial class PeripheralNames
{
    /// <summary>The first peripheral of a comma-separated list, without "Peripheral:" or any bracketed count; empty when none.</summary>
    public static string FirstOf(string? peripheralsText) =>
        string.IsNullOrWhiteSpace(peripheralsText) || peripheralsText == "-"
            ? string.Empty
            : peripheralsText.Split(',', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries).FirstOrDefault() is { } first
                ? BaseName(first)
                : string.Empty;

    /// <summary>The peripheral's name for comparing listed configurations: without "Peripheral:" or any bracketed part.</summary>
    public static string DedupeKey(string? value) =>
        string.IsNullOrWhiteSpace(value) || value == "-" ? string.Empty : BaseName(value);

    /// <summary>How many peripherals the names bring: each one's bracketed count, or one.</summary>
    public static int TotalCount(IEnumerable<string> peripheralNames) =>
        peripheralNames
            .Where(name => !string.IsNullOrWhiteSpace(name) && name != "-")
            .Sum(name => TrailingCount().Match(name) is { Success: true } match && int.TryParse(match.Groups[1].Value, out var count)
                ? Math.Max(0, count)
                : 1);

    /// <summary>The one peripheral listed with a count ("Traktor Mul (2)"); null for any other list.</summary>
    public static CountedPeripheral? SingleCounted(IReadOnlyList<string> peripheralNames)
    {
        if (peripheralNames.Count != 1 || string.IsNullOrWhiteSpace(peripheralNames[0]) || peripheralNames[0] == "-")
        {
            return null;
        }

        var match = NameWithCount().Match(peripheralNames[0]);
        if (!match.Success || !int.TryParse(match.Groups[2].Value, out var count) || count <= 0)
        {
            return null;
        }

        var name = FirstOf(match.Groups[1].Value);
        return string.IsNullOrWhiteSpace(name) ? null : new CountedPeripheral(name, count);
    }

    // The name without a leading "Peripheral:" and without any trailing bracketed parts.
    private static string BaseName(string value)
    {
        var result = value.Trim();
        const string prefix = "Peripheral:";
        if (result.StartsWith(prefix, StringComparison.OrdinalIgnoreCase))
        {
            result = result[prefix.Length..].Trim();
        }

        while (TrailingBracket().Replace(result, string.Empty).Trim() is var updated && updated != result)
        {
            result = updated;
        }

        return result;
    }

    [GeneratedRegex(@"\((\d+)\)\s*$")]
    private static partial Regex TrailingCount();

    [GeneratedRegex(@"^(.*)\((\d+)\)\s*$")]
    private static partial Regex NameWithCount();

    [GeneratedRegex(@"\s*\([^)]*\)\s*$")]
    private static partial Regex TrailingBracket();
}
