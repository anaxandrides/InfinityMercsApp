namespace InfinityMercsApp.Domain.Season;

/// <summary>A result of a d20 table and the rolls (Min to Max, inclusive) that give it.</summary>
public sealed record RollRange(int Min, int Max, string Result)
{
    public bool Contains(int roll) => roll >= Min && roll <= Max;
}

/// <summary>A d20 table: each result covers a range of rolls.</summary>
public sealed class RollTable(IReadOnlyList<RollRange> ranges)
{
    public IReadOnlyList<RollRange> Ranges { get; } = ranges;

    /// <summary>The result for a roll; null when no range covers it.</summary>
    public string? Lookup(int roll) => Ranges.FirstOrDefault(range => range.Contains(roll))?.Result;
}

/// <summary>The season's d20 tables.</summary>
public static class SeasonRollTables
{
    /// <summary>The pistol a downtime "Weapon" result awards.</summary>
    public static RollTable Pistols { get; } = new(
    [
        new(1, 4, "MULTI Pistol"),
        new(5, 8, "AP Heavy Pistol"),
        new(9, 12, "Breaker Pistol"),
        new(13, 16, "Silenced Pistol"),
        new(17, 20, "Viral Pistol")
    ]);

    /// <summary>The market found when searching for a temporary store; 19-20 finds none.</summary>
    public static RollTable TemporaryStores { get; } = new(
    [
        new(1, 2, "Number One"),
        new(3, 4, "Jade Temu"),
        new(5, 6, "Arachne Req"),
        new(7, 8, "Salaam Suuk"),
        new(9, 10, "Frontier General"),
        new(11, 12, "Alpha Sec"),
        new(13, 14, "Greengrocer"),
        new(15, 16, "Bantai Yamaco"),
        new(17, 18, "Exrah Surplus")
    ]);
}
