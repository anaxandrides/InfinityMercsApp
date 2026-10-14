namespace InfinityMercsApp.Domain.Season.Rolls;

/// <summary>A d20 table: each result covers a range of rolls.</summary>
public sealed class RollTable(IReadOnlyList<RollRange> ranges)
{
    public IReadOnlyList<RollRange> Ranges { get; } = ranges;

    /// <summary>The result for a roll; null when no range covers it.</summary>
    public string? Lookup(int roll) => Ranges.FirstOrDefault(range => range.Contains(roll))?.Result;
}
