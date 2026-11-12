namespace InfinityMercsApp.Presentation.Leagues;

/// <summary>A league season whose standings can be shown.</summary>
public sealed record LeagueSeasonChoice(int Number, string Text)
{
    /// <summary>Every season so far, the current one first.</summary>
    public static IReadOnlyList<LeagueSeasonChoice> UpTo(int currentSeason) =>
        Enumerable.Range(1, Math.Max(1, currentSeason))
            .Reverse()
            .Select(number => new LeagueSeasonChoice(number, number == currentSeason ? $"Season {number} (now)" : $"Season {number}"))
            .ToList();

    public override string ToString() => Text;
}
