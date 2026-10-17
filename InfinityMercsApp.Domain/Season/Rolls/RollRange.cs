using System.Globalization;

namespace InfinityMercsApp.Domain.Season.Rolls;

/// <summary>A result of a d20 table and the rolls (Min to Max, inclusive) that give it.</summary>
public sealed record RollRange(int Min, int Max, string Result)
{
    public bool Contains(int roll) => roll >= Min && roll <= Max;

    /// <summary>A row written as a table writes it, "1-2" or "15"; null when the range isn't numbers.</summary>
    public static RollRange? Parse(string range, string result)
    {
        var parts = range.Split('-', StringSplitOptions.TrimEntries);
        return parts.Length is 1 or 2 &&
               int.TryParse(parts[0], NumberStyles.Integer, CultureInfo.InvariantCulture, out var min) &&
               int.TryParse(parts[^1], NumberStyles.Integer, CultureInfo.InvariantCulture, out var max)
            ? new RollRange(min, max, result.Trim())
            : null;
    }
}
