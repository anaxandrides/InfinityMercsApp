namespace InfinityMercsApp.Api.Leagues.Changes;

/// <summary>What a change to the league document alone made of it.</summary>
/// <param name="League">The league after the change; the league unchanged when the change made no difference.</param>
public sealed record LeagueChangeResult(LeagueChangeStatus Status, LeagueDocument? League, IReadOnlyList<string> Problems)
{
    public static LeagueChangeResult Done(LeagueDocument league) => new(LeagueChangeStatus.Done, league, []);

    public static LeagueChangeResult NotFound { get; } = new(LeagueChangeStatus.NotFound, null, []);

    public static LeagueChangeResult Forbidden { get; } = new(LeagueChangeStatus.Forbidden, null, []);

    public static LeagueChangeResult Rejected(string problem) => new(LeagueChangeStatus.Rejected, null, [problem]);
}
