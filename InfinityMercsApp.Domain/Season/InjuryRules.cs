namespace InfinityMercsApp.Domain.Season;

/// <summary>A row of the injury table: a d20 range and the injury it gives.</summary>
public sealed record InjuryTableEntry(int Min, int Max, string Label)
{
    public override string ToString() => $"{Min}-{Max} | {Label}";
}

/// <summary>Rules for a trooper taken out during a game: avoiding injury and which injury it takes.</summary>
public static class InjuryRules
{
    public static IReadOnlyList<InjuryTableEntry> Table { get; } =
    [
        new(1, 6, "Battle Fury"),
        new(7, 8, "Punctured Lung"),
        new(9, 10, "Arms"),
        new(11, 12, "Brain Injury"),
        new(13, 14, "Legs Injury"),
        new(15, 16, "Body Injury"),
        new(17, 18, "Eyes Injury"),
        new(19, 20, "Shell Shocked"),
    ];

    /// <summary>The d20 roll needed to avoid injury (PH - 3), or null when PH is not a number.</summary>
    public static int? AvoidTarget(string? ph) => int.TryParse(ph, out var value) ? value - 3 : null;

    /// <summary>The table row for a d20 roll, or -1 when the roll is off the table.</summary>
    public static int TableIndexForRoll(int roll) => Table.ToList().FindIndex(e => roll >= e.Min && roll <= e.Max);
}
