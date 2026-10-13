using System.Globalization;
using System.Text.RegularExpressions;

namespace InfinityMercsApp.Domain.Captain;

/// <summary>Reads a display statline such as <c>"MOV 10-5 | CC 14 | BS 12 | PH 10 | WIP 13 | ARM 1 | BTS 3 | VITA 1"</c>.</summary>
public static partial class Statline
{
    /// <summary>The numeric combat stats (CC, BS, PH, WIP, ARM, BTS, VITA, STR, W) by upper-case key; later values win.</summary>
    public static IReadOnlyDictionary<string, int> NumericStats(string? statline)
    {
        var values = new Dictionary<string, int>(StringComparer.OrdinalIgnoreCase);
        if (string.IsNullOrWhiteSpace(statline))
        {
            return values;
        }

        foreach (Match match in NumericStat().Matches(statline))
        {
            if (int.TryParse(match.Groups[2].Value, NumberStyles.Integer, CultureInfo.InvariantCulture, out var parsed))
            {
                values[match.Groups[1].Value.ToUpperInvariant()] = parsed;
            }
        }

        return values;
    }

    /// <summary>Every "KEY value" segment in order, keys upper-cased; segments of another shape are skipped.</summary>
    public static List<(string Key, string Value)> Entries(string? statline)
    {
        var entries = new List<(string Key, string Value)>();
        if (string.IsNullOrWhiteSpace(statline))
        {
            return entries;
        }

        foreach (var segment in statline.Split('|', StringSplitOptions.TrimEntries))
        {
            var match = KeyValueSegment().Match(segment);
            if (match.Success)
            {
                entries.Add((match.Groups[1].Value.ToUpperInvariant(), match.Groups[2].Value.Trim()));
            }
        }

        return entries;
    }

    [GeneratedRegex(@"\b(CC|BS|PH|WIP|ARM|BTS|VITA|STR|W)\s+(\d+)\b", RegexOptions.IgnoreCase)]
    private static partial Regex NumericStat();

    [GeneratedRegex(@"^\s*([A-Za-z]+)\s+(.+)\s*$", RegexOptions.IgnoreCase)]
    private static partial Regex KeyValueSegment();
}
