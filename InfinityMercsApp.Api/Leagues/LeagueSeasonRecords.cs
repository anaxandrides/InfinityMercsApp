namespace InfinityMercsApp.Api.Leagues;

/// <summary>What a member sees of one league season: its entries and games, and every season they have entered.</summary>
/// <param name="EnteredSeasonIds">The member's company seasons entered in any league season.</param>
public sealed record LeagueSeasonRecords(int Season, IReadOnlyList<LeagueEntry> Entries, IReadOnlyList<LeagueGame> Games, IReadOnlyList<string> EnteredSeasonIds)
{
    /// <summary>A league season nothing has been entered in.</summary>
    public static LeagueSeasonRecords Empty(int season) => new(season, [], [], []);
}
