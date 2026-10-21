using System.Globalization;
using System.Text.RegularExpressions;
using InfinityMercsApp.Domain.Models.Army.Stats;

namespace InfinityMercsApp.Domain.Captain;

/// <summary>Reads a display statline such as <c>"MOV 10-5 | CC 14 | BS 12 | PH 10 | WIP 13 | ARM 1 | BTS 3 | VITA 1"</c>.</summary>
public static partial class Statline
{
    /// <summary>The numeric combat stats (CC, BS, PH, WIP, ARM, BTS, VITA, STR, W); later values win.</summary>
    /// <summary>The statline with its MOV replaced by <paramref name="move"/>; just the MOV when there is no statline.</summary>
    public static string WithMove(string? statline, string move) =>
        string.IsNullOrWhiteSpace(statline)
            ? $"MOV {move}"
            : MovePart().Replace(statline, move);

    public static IReadOnlyDictionary<Stat, int> NumericStats(string? statline)
    {
        var values = new Dictionary<Stat, int>();
        if (string.IsNullOrWhiteSpace(statline))
        {
            return values;
        }

        foreach (Match match in NumericStat().Matches(statline))
        {
            if (StatKeys.TryParse(match.Groups[1].Value, out var stat) &&
                int.TryParse(match.Groups[2].Value, NumberStyles.Integer, CultureInfo.InvariantCulture, out var parsed))
            {
                values[stat] = parsed;
            }
        }

        return values;
    }

    /// <summary>Every "KEY value" segment whose key is a stat, in order; other segments are skipped.</summary>
    public static List<StatValue> Entries(string? statline)
    {
        var entries = new List<StatValue>();
        if (string.IsNullOrWhiteSpace(statline))
        {
            return entries;
        }

        foreach (var segment in statline.Split('|', StringSplitOptions.TrimEntries))
        {
            var match = KeyValueSegment().Match(segment);
            if (match.Success && StatKeys.TryParse(match.Groups[1].Value, out var stat))
            {
                entries.Add(new StatValue(stat, match.Groups[2].Value.Trim()));
            }
        }

        return entries;
    }

    [GeneratedRegex(@"(?<=\bMOV\s)[^|]*[^|\s]", RegexOptions.IgnoreCase | RegexOptions.CultureInvariant)]
    private static partial Regex MovePart();

    [GeneratedRegex(@"\b(CC|BS|PH|WIP|ARM|BTS|VITA|STR|W)\s+(\d+)\b", RegexOptions.IgnoreCase)]
    private static partial Regex NumericStat();

    [GeneratedRegex(@"^\s*([A-Za-z]+)\s+(.+)\s*$", RegexOptions.IgnoreCase)]
    private static partial Regex KeyValueSegment();
}
